using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Session Activity Host")]
    public sealed class SessionActivityHost : MonoBehaviour, ISessionActivityRouteExitTeardownBoundary, ISessionActivityVisualReadinessBoundary
    {
        [Header("Config")]
        // Campo de tooling/QA. Nao e owner de lifecycle e nao pode iniciar Activity automaticamente.
        [SerializeField] private bool autoStart;
        [SerializeField] private string sessionStateId = "SessionActivitySandboxSession";
        [SerializeField] private ActivityCatalogAsset activityCatalog;
        [Header("QA Tooling")]
        [SerializeField] private bool qaObserveAsyncStateChanges = true;
        [SerializeField] private float qaObserveIntervalSeconds = 0.1f;

        private SessionActivityCatalog _catalog;
        private SessionActivityPipeline _pipeline;
        private string _lastObservedStateToken = string.Empty;
        private float _nextObserveAt;

        public event Action StateObservedChanged;

        public SessionActivityRuntimeState State => _pipeline?.State;
        public SessionActivityCatalog Catalog => _catalog;
        public SessionActivityPipeline Pipeline => _pipeline;
        public ActivityExecutionBlockingState GateState => _pipeline?.GateState;
        public SessionActivityRailKind CurrentRailKind => _pipeline?.ActiveRailKind ?? SessionActivityRailKind.None;
        public SessionActivityStage CurrentStage => _pipeline != null ? _pipeline.State.CurrentStage : SessionActivityStage.Unknown;
        public bool HasPendingOperation => _pipeline != null && _pipeline.State.CurrentPendingOperation.IsValid;

        private void Awake()
        {
            if (activityCatalog == null)
            {
                throw new InvalidOperationException("SessionActivityHost requires activityCatalog.");
            }

            var installer = GetOrCreateCompositionInstaller();
            installer.Compose(this, activityCatalog.BuildRuntimeCatalog(), sessionStateId);
        }

        private void Start()
        {
            if (autoStart)
            {
                if (IsQaDebugAllowed())
                {
                    DebugUtility.LogVerbose(typeof(SessionActivityHost), $"SessionActivityHostAutoStartIgnored sessionStateId='{sessionStateId}' autoStart='true' reason='auto_start_is_not_canonical_in_base11' source='SessionActivityHost/Start'.");
                    return;
                }

                throw new InvalidOperationException($"[FATAL][Config][SessionActivityPipeline][Host] SessionActivityHostAutoStartBlocked sessionStateId='{sessionStateId}' autoStart='true' reason='auto_start_is_not_allowed_in_runtime_normal'.");
            }
        }

        private void OnDisable()
        {
            if (_pipeline == null || !_pipeline.State.HasStarted || _pipeline.State.HasCompleted)
            {
                return;
            }

            var stage = _pipeline.State.CurrentStage;
            if (stage == SessionActivityStage.Deactivation ||
                stage == SessionActivityStage.Completed ||
                stage == SessionActivityStage.ClosedForRouteExit ||
                _pipeline.ActiveRailKind == SessionActivityRailKind.ActivityRouteExitRail)
            {
                return;
            }

            throw new InvalidOperationException(
                $"[FATAL][Lifecycle][SessionActivityPipeline][Host] SessionActivityRouteExitWithoutCanonicalDeactivation sessionStateId='{sessionStateId}' stage='{stage}' activityId='{_pipeline.State.CurrentDefinition.ActivityId}' reason='route_exit_or_scene_unload_requires_explicit_activity_closure_before_unload'.");
        }

        private void Update()
        {
            if (!qaObserveAsyncStateChanges || _pipeline == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextObserveAt)
            {
                return;
            }

            float interval = qaObserveIntervalSeconds <= 0f ? 0.1f : qaObserveIntervalSeconds;
            _nextObserveAt = now + interval;

            string currentToken = BuildStateObservationToken();
            if (string.Equals(currentToken, _lastObservedStateToken, StringComparison.Ordinal))
            {
                return;
            }

            _lastObservedStateToken = currentToken;
            StateObservedChanged?.Invoke();
            DebugUtility.LogVerbose(typeof(SessionActivityHost), $"{currentToken}");
        }

        public void DebugStartActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostDebugStartRemoved sessionStateId='{sessionStateId}' reason='debug_start_is_not_allowed_in_canonical_qa_lifecycle'.");
        }

        public void CompleteCurrentActivity()
        {
            EnsurePipeline();
            var result = _pipeline.CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult("CompleteCurrentActivity", result);
        }

        public void CompleteActivationWindow()
        {
            EnsurePipeline();
            var result = _pipeline.CompleteActivationWindow(QaSource("CompleteActivationWindow"), QaReason("CompleteActivationWindow"));
            LogResult("CompleteActivationWindow", result);
        }

        public void CompleteDeactivationWindow()
        {
            EnsurePipeline();
            var result = _pipeline.CompleteDeactivationWindow(QaSource("CompleteDeactivationWindow"), QaReason("CompleteDeactivationWindow"));
            LogResult("CompleteDeactivationWindow", result);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            var result = _pipeline.ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
            LogResult("ContinueToNextActivity", result);
        }

        public void GoToNextActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToNextActivity' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void GoToPreviousActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToPreviousActivity' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void RestartCurrentActivity()
        {
            EnsurePipeline();
            var result = _pipeline.RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult("RestartCurrentActivity", result);
        }

        public void ResetSession()
        {
            EnsurePipeline();
            var result = _pipeline.ResetSession(QaSource("ResetSession"), QaReason("ResetSession"));
            LogResult("ResetSession", result);
        }

        public SessionActivitySessionResetResult ResetSessionAfterRouteExit(
            string sessionStateId,
            string source,
            string reason)
        {
            EnsurePipeline();
            return _pipeline.ResetSessionAfterRouteExit(sessionStateId, source, reason);
        }

        public void GoToActivity(string activityId)
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToActivity' targetActivityId='{activityId}' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void RequestPause()
        {
            EnsurePipeline();
            var result = _pipeline.PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult("RequestPause", result);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            var result = _pipeline.ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult("RequestResume", result);
        }

        internal bool QaSubtractActorAttribute(string actorId, string attributeId, float amount = 10f)
        {
            return QaApplyActorAttributeCommand("QaSubtractActorAttribute", actorId, attributeId, ActorAttributeOperation.Subtract, amount, 0f);
        }

        internal bool QaAddActorAttribute(string actorId, string attributeId, float amount = 5f)
        {
            return QaApplyActorAttributeCommand("QaAddActorAttribute", actorId, attributeId, ActorAttributeOperation.Add, amount, 0f);
        }

        internal bool QaSetActorAttribute(string actorId, string attributeId, float value)
        {
            return QaApplyActorAttributeCommand("QaSetActorAttribute", actorId, attributeId, ActorAttributeOperation.Set, 0f, value);
        }

        internal bool QaResetActorAttributeToInitial(string actorId, string attributeId)
        {
            return QaApplyActorAttributeCommand("QaResetActorAttributeToInitial", actorId, attributeId, ActorAttributeOperation.ResetToInitial, 0f, 0f);
        }

        internal bool QaRestoreActorAttributeToMax(string actorId, string attributeId)
        {
            return QaApplyActorAttributeCommand("QaRestoreActorAttributeToMax", actorId, attributeId, ActorAttributeOperation.RestoreToMax, 0f, 0f);
        }

        internal bool QaResetCurrentPlayerActor()
        {
            EnsurePipeline();
            bool applied;
            string outcomeReason;
            try
            {
                applied = _pipeline.TryQaResetCurrentPlayerActor(
                    State.CurrentIdentity,
                    QaSource("QaResetCurrentPlayerActor"),
                    QaReason("QaResetCurrentPlayerActor"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                applied = false;
                outcomeReason = $"actor_reset_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHost), 
                    $"event='ActorResetQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            }

            DebugUtility.Log(typeof(SessionActivityHost), 
                $"action='QaResetCurrentPlayerActor' outcomeKind='{(applied ? "Applied" : "Rejected")}' reason='{outcomeReason}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            return applied;
        }

        internal bool QaResetCurrentActivityObjects()
        {
            EnsurePipeline();
            bool applied;
            string outcomeReason;
            try
            {
                applied = _pipeline.TryQaResetCurrentActivityObjects(
                    State.CurrentIdentity,
                    QaSource("QaResetCurrentActivityObjects"),
                    QaReason("QaResetCurrentActivityObjects"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                applied = false;
                outcomeReason = $"activity_object_reset_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHost), 
                    $"event='ActivityObjectResetQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            }

            DebugUtility.Log(typeof(SessionActivityHost), 
                $"action='QaResetCurrentActivityObjects' outcomeKind='{(applied ? "Applied" : "SkippedOrRejected")}' reason='{outcomeReason}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            return applied;
        }

        internal bool QaCaptureCurrentActivitySnapshotPayload()
        {
            EnsurePipeline();
            bool captured;
            string outcomeReason;
            try
            {
                captured = _pipeline.TryQaCaptureCurrentActivitySnapshotPayload(
                    State.CurrentIdentity,
                    QaSource("QaCaptureCurrentActivitySnapshotPayload"),
                    QaReason("QaCaptureCurrentActivitySnapshotPayload"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                captured = false;
                outcomeReason = $"activity_snapshot_capture_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHost), 
                    $"event='ActivitySnapshotCaptureQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            }

            DebugUtility.LogVerbose(typeof(SessionActivityHost), 
                $"action='QaCaptureCurrentActivitySnapshotPayload' outcomeKind='{(captured ? "Captured" : "SkippedOrRejected")}' reason='{outcomeReason}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}' stage='{State.CurrentStage}'.");
            return captured;
        }

        public SessionActivityRouteExitTeardownResult RequestRouteExitTeardown(string requestedSessionStateId, string source, string reason)
        {
            EnsurePipeline();
            return _pipeline.RequestRouteExitTeardown(requestedSessionStateId, source, reason);
        }

        public Task<SessionActivityRouteExitTeardownResult> AwaitRouteExitTeardownAsync(
            string requestedSessionStateId,
            string source,
            string reason,
            CancellationToken cancellationToken)
        {
            EnsurePipeline();
            return _pipeline.AwaitRouteExitTeardownAsync(requestedSessionStateId, source, reason, cancellationToken);
        }

        public Task<SessionActivityVisualReadinessResult> AwaitVisualReadinessAsync(
            SessionActivityVisualReadinessRequest request,
            CancellationToken cancellationToken)
        {
            EnsurePipeline();
            return _pipeline.AwaitVisualReadinessAsync(request, cancellationToken);
        }

        public SessionActivityCommandResult ExecuteCommand(SessionActivityCommand command, string actionLabel)
        {
            EnsurePipeline();
            var result = _pipeline.Execute(command);
            LogResult(actionLabel, result);
            return result;
        }

        private void EnsurePipeline()
        {
            if (_pipeline != null)
            {
                return;
            }

            throw new InvalidOperationException("SessionActivityHost pipeline is not initialized.");
        }

        private void LogResult(string action, SessionActivityCommandResult result)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Invalid result returned by action '{action}'.");
            }

            if (!result.IsStarted)
            {
                string outcome = result.Kind.ToString();
                DebugUtility.LogVerbose(typeof(SessionActivityHost), $"action='{action}' outcomeKind='{outcome}' reason='{result.Reason}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}'");
            }

            for (int index = 0; index < result.Facts.Count; index++)
            {
                DebugUtility.LogVerbose(typeof(SessionActivityHost), $"{result.Facts[index]}");
            }
        }

        private string GetPendingHandoffTarget()
        {
            return State.CurrentHandoff.IsValid
                ? State.CurrentHandoff.NextActivityId
                : "<none>";
        }

        private string GetNextExpectedQaAction()
        {
            var stage = State.CurrentStage;
            bool hasPendingHandoff = State.CurrentHandoff.IsValid;

            if (stage == SessionActivityStage.ActivityContentProfileResolved ||
                stage == SessionActivityStage.ActivityContentLoadStarted ||
                stage == SessionActivityStage.ActivityContentSceneLoading ||
                stage == SessionActivityStage.ActivityContentSceneLoaded ||
                stage == SessionActivityStage.ActivityContentLoadedSetReady ||
                stage == SessionActivityStage.ActivityContentLoadSkippedNoContent ||
                stage == SessionActivityStage.ActivityContentLoadFailed ||
                stage == SessionActivityStage.ActivitySetupInventoryBuildStarted ||
                stage == SessionActivityStage.ActivitySetupInventoryBuilt ||
                stage == SessionActivityStage.ActivitySetupInventorySkippedNoRequirements ||
                stage == SessionActivityStage.ActivitySetupInventoryBuildFailed ||
                stage == SessionActivityStage.ActivityParticipantBindingStarted ||
                stage == SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements ||
                stage == SessionActivityStage.ActivityParticipantBindingCompleted ||
                stage == SessionActivityStage.ActivityParticipantBindingFailed)
            {
                return "No local QA action";
            }

            if (stage == SessionActivityStage.ActivationWindowReady)
            {
                return "CompleteActivationWindow";
            }

            if (stage == SessionActivityStage.ActivityRunning)
            {
                return "CompleteCurrentActivity";
            }

            if (stage == SessionActivityStage.DeactivationWindowReady)
            {
                return "CompleteDeactivationWindow";
            }

            if (stage == SessionActivityStage.NextActivitySetupCompleted &&
                hasPendingHandoff &&
                ResolveCurrentContinuePolicy() == ActivityTransitionContinuePolicy.ManualContinue)
            {
                return "ContinueToNextActivity";
            }

            return "No local QA action";
        }

        private string BuildHostBanner()
        {
            return $"initialized sessionStateId='{sessionStateId}' autoStart='{autoStart}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}' catalog='{_catalog.Summary}'";
        }

        private string BuildStateObservationToken()
        {
            var pending = State.CurrentPendingOperation;
            string pendingToken = pending.IsValid
                ? $"{pending.OperationKind}/{pending.OperationId}/{pending.WindowKind}"
                : "<none>";
            string handoffToken = State.CurrentHandoff.IsValid
                ? $"{State.CurrentHandoff.NextActivityId}/{State.CurrentHandoff.ToIdentity.EntrySequence}"
                : "<none>";
            return $"stage='{State.CurrentStage}' entrySequence='{State.CurrentEntrySequence}' activity='{State.CurrentDefinition.ActivityId}' pendingOperation='{pendingToken}' handoff='{handoffToken}'";
        }

        private static string QaSource(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }

        private static string QaReason(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }

        private static bool IsQaDebugAllowed()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private SessionActivityCompositionInstaller GetOrCreateCompositionInstaller()
        {
            var installer = GetComponent<SessionActivityCompositionInstaller>();
            if (installer != null)
            {
                return installer;
            }

            return gameObject.AddComponent<SessionActivityCompositionInstaller>();
        }

        internal void BindComposition(SessionActivityCatalog catalog, SessionActivityPipeline pipeline)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            DebugUtility.LogVerbose(typeof(SessionActivityHost), BuildHostBanner());
        }


        private bool QaApplyActorAttributeCommand(
            string action,
            string actorId,
            string attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue)
        {
            EnsurePipeline();

            bool applied = _pipeline.TryApplyActorAttributeCommand(
                State.CurrentIdentity,
                actorId,
                operation,
                attributeId,
                amount,
                setValue,
                QaSource(action),
                QaReason(action),
                out var result);

            string outcome = applied ? "Applied" : (result.Rejected ? "Rejected" : "Failed");
            DebugUtility.LogVerbose(typeof(SessionActivityHost), 
                $"action='{action}' outcomeKind='{outcome}' operation='{operation}' actorId='{Normalize(actorId)}' attributeId='{Normalize(attributeId)}' amount='{amount:0.###}' setValue='{setValue:0.###}' reason='{result.Reason}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}'");

            if (applied && result.HasFact)
            {
                var fact = result.Fact;
                DebugUtility.Log(typeof(SessionActivityHost), 
                    $"operation='{fact.Operation}' actorId='{Normalize(actorId)}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
            }

            return applied;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private ActivityTransitionContinuePolicy ResolveCurrentContinuePolicy()
        {
            var current = State.CurrentDefinition;
            return current.IsValid
                ? current.NextActivityTransitionContinuePolicy
                : ActivityTransitionContinuePolicy.Unknown;
        }

        private string FormatActivitySetupInventory()
        {
            if (_pipeline == null)
            {
                return "<none>";
            }

            var inventory = _pipeline.EntryPipeline.GetCurrentActivitySetupInventory();
            if (!inventory.IsValid || string.IsNullOrWhiteSpace(inventory.InventoryId))
            {
                return "<none>";
            }

            return $"inventoryId='{inventory.InventoryId}', totalRequirements='{inventory.TotalRequirementCount}'";
        }
    }
}
