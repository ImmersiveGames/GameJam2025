using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Damage.Runtime;
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
        public ActivityExecutionBlockingState GateState => _pipeline?.GateState;
        internal bool HasPipeline => _pipeline != null;

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
            _pipeline?.ValidateHostDisableOrFail(
                sessionStateId,
                "SessionActivityHost/OnDisable",
                "host_disabled");
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
            SessionActivityHostQaCommandSurface.CompleteCurrentActivity(_pipeline);
        }

        public void CompleteActivationWindow()
        {
            EnsurePipeline();
            SessionActivityHostQaCommandSurface.CompleteActivationWindow(_pipeline);
        }

        public void CompleteDeactivationWindow()
        {
            EnsurePipeline();
            SessionActivityHostQaCommandSurface.CompleteDeactivationWindow(_pipeline);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            SessionActivityHostQaCommandSurface.ContinueToNextActivity(_pipeline);
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
            SessionActivityHostQaCommandSurface.RestartCurrentActivity(_pipeline);
        }

        public void ResetSession()
        {
            EnsurePipeline();
            SessionActivityHostQaCommandSurface.ResetSession(_pipeline);
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
            SessionActivityHostQaCommandSurface.RequestPause(_pipeline);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            SessionActivityHostQaCommandSurface.RequestResume(_pipeline);
        }

        internal bool QaSubtractActorAttribute(string actorId, string attributeId, float amount = 10f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeCommand(_pipeline, "QaSubtractActorAttribute", actorId, attributeId, ActorAttributeOperation.Subtract, amount, 0f);
        }

        internal bool QaAddActorAttribute(string actorId, string attributeId, float amount = 5f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeCommand(_pipeline, "QaAddActorAttribute", actorId, attributeId, ActorAttributeOperation.Add, amount, 0f);
        }

        internal bool QaSetActorAttribute(string actorId, string attributeId, float value)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeCommand(_pipeline, "QaSetActorAttribute", actorId, attributeId, ActorAttributeOperation.Set, 0f, value);
        }

        internal bool QaResetActorAttributeToInitial(string actorId, string attributeId)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeCommand(_pipeline, "QaResetActorAttributeToInitial", actorId, attributeId, ActorAttributeOperation.ResetToInitial, 0f, 0f);
        }

        internal bool QaRestoreActorAttributeToMax(string actorId, string attributeId)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeCommand(_pipeline, "QaRestoreActorAttributeToMax", actorId, attributeId, ActorAttributeOperation.RestoreToMax, 0f, 0f);
        }

        internal bool QaMutationSubtractActorAttribute(string actorId, string attributeId, float amount = 10f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeMutationIntent(_pipeline, "QaMutationSubtractActorAttribute", actorId, attributeId, ActorAttributeOperation.Subtract, amount, 0f);
        }

        internal bool QaMutationAddActorAttribute(string actorId, string attributeId, float amount = 5f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeMutationIntent(_pipeline, "QaMutationAddActorAttribute", actorId, attributeId, ActorAttributeOperation.Add, amount, 0f);
        }

        internal bool QaMutationSetActorAttribute(string actorId, string attributeId, float value)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeMutationIntent(_pipeline, "QaMutationSetActorAttribute", actorId, attributeId, ActorAttributeOperation.Set, 0f, value);
        }

        internal bool QaMutationResetActorAttributeToInitial(string actorId, string attributeId)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeMutationIntent(_pipeline, "QaMutationResetActorAttributeToInitial", actorId, attributeId, ActorAttributeOperation.ResetToInitial, 0f, 0f);
        }

        internal bool QaMutationRestoreActorAttributeToMax(string actorId, string attributeId)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorAttributeMutationIntent(_pipeline, "QaMutationRestoreActorAttributeToMax", actorId, attributeId, ActorAttributeOperation.RestoreToMax, 0f, 0f);
        }

        internal bool QaDamageActor(string actorId, float rawDamageAmount = 25f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorDamageIntent(_pipeline, "QaDamageActor", actorId, rawDamageAmount);
        }

        internal bool QaDamageSourceActor(string sourceActorId, string targetActorId, float rawDamageAmount = 25f)
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ApplyActorDamageSourceIntent(_pipeline, "QaDamageSourceActor", sourceActorId, targetActorId, rawDamageAmount);
        }

        internal bool QaResetCurrentPlayerActor()
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ResetCurrentPlayerActor(_pipeline);
        }

        internal bool QaResetCurrentActivityObjects()
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.ResetCurrentActivityObjects(_pipeline);
        }

        internal bool QaCaptureCurrentActivitySnapshotPayload()
        {
            EnsurePipeline();
            return SessionActivityHostQaCommandSurface.CaptureCurrentActivitySnapshotPayload(_pipeline);
        }


        public SessionActivityRouteExitTeardownPreflightResult EvaluateRouteExitTeardownPreflight(string requestedSessionStateId, string source, string reason)
        {
            EnsurePipeline();
            return _pipeline.EvaluateRouteExitTeardownPreflight(requestedSessionStateId, source, reason);
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
            return SessionActivityHostQaCommandSurface.ExecuteCommand(_pipeline, command, actionLabel);
        }

        private void EnsurePipeline()
        {
            if (_pipeline != null)
            {
                return;
            }

            throw new InvalidOperationException("SessionActivityHost pipeline is not initialized.");
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


        internal ActivityContentLoadedSet GetCurrentActivityContentLoadedSet()
        {
            EnsurePipeline();
            return _pipeline.GetCurrentActivityContentLoadedSet();
        }

        internal ActivitySetupInventory GetCurrentActivitySetupInventory()
        {
            EnsurePipeline();
            return _pipeline.GetCurrentActivitySetupInventory();
        }

        internal void BindComposition(SessionActivityCatalog catalog, SessionActivityPipeline pipeline)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            DebugUtility.LogVerbose(typeof(SessionActivityHost), BuildHostBanner());
        }


        private ActivityTransitionContinuePolicy ResolveCurrentContinuePolicy()
        {
            var current = State.CurrentDefinition;
            return current.IsValid
                ? current.NextActivityTransitionContinuePolicy
                : ActivityTransitionContinuePolicy.Unknown;
        }
    }
}
