using System;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionActivity.Adapters;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionActivity/Session Activity Host")]
    public sealed class SessionActivityHost : MonoBehaviour, ISessionActivityRouteExitTeardownBoundary
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

        private void Awake()
        {
            if (activityCatalog == null)
            {
                throw new InvalidOperationException("SessionActivityHost requires activityCatalog.");
            }

            _catalog = activityCatalog.BuildRuntimeCatalog();
            UnitySessionActivityWindowSceneAdapter windowSceneAdapter = new();
            _pipeline = new SessionActivityPipeline(
                _catalog,
                sessionStateId,
                new PauseOverlayAdapter(),
                new InputModeAdapter(),
                new SessionActivityTransitionAdapter(),
                new SessionActivityTransitionLoadingAdapter(),
                windowSceneAdapter,
                new UnitySessionActivityPendingOperationRunner(windowSceneAdapter));
            RegisterGlobal(_catalog);
            RegisterGlobal(_pipeline);
            RegisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
            RegisterGlobal<ISessionActivityRouteExitTeardownBoundary>(this);
            Debug.Log(BuildHostBanner());
        }

        private void Start()
        {
            if (autoStart)
            {
                if (IsQaDebugAllowed())
                {
                    Debug.Log($"[OBS][SessionActivityPipeline][Host] SessionActivityHostAutoStartIgnored sessionStateId='{sessionStateId}' autoStart='true' reason='auto_start_is_not_canonical_in_base11' source='SessionActivityHost/Start'.");
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

            SessionActivityStage stage = _pipeline.State.CurrentStage;
            if (stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
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
            Debug.Log($"[OBS][SessionActivityPipeline][Host][StateObservedChanged] {currentToken}");
        }

        public void DebugStartActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostDebugStartRemoved sessionStateId='{sessionStateId}' reason='debug_start_is_not_allowed_in_canonical_qa_lifecycle'.");
        }

        public void CompleteCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult("CompleteCurrentActivity", result);
        }

        public void CompleteActivationWindow()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteActivationWindow(QaSource("CompleteActivationWindow"), QaReason("CompleteActivationWindow"));
            LogResult("CompleteActivationWindow", result);
        }

        public void CompleteDeactivationWindow()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteDeactivationWindow(QaSource("CompleteDeactivationWindow"), QaReason("CompleteDeactivationWindow"));
            LogResult("CompleteDeactivationWindow", result);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
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
            SessionActivityCommandResult result = _pipeline.RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult("RestartCurrentActivity", result);
        }

        public void GoToActivity(string activityId)
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToActivity' targetActivityId='{activityId}' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void RequestPause()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult("RequestPause", result);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult("RequestResume", result);
        }

        public SessionActivityRouteExitTeardownResult RequestRouteExitTeardown(string requestedSessionStateId, string source, string reason)
        {
            EnsurePipeline();
            string normalizedSessionStateId = Normalize(requestedSessionStateId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (string.IsNullOrWhiteSpace(normalizedSessionStateId))
            {
                throw new InvalidOperationException("RequestRouteExitTeardown requires sessionStateId.");
            }

            if (!string.Equals(normalizedSessionStateId, sessionStateId, StringComparison.Ordinal))
            {
                return BuildRouteExitTeardownBlocked(
                    "stale_or_foreign_session_activity_teardown_request",
                    $"teardown request sessionStateId='{normalizedSessionStateId}' does not match host sessionStateId='{sessionStateId}'.");
            }

            if (!_pipeline.State.HasStarted || _pipeline.State.HasCompleted)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.NoActiveSessionActivity,
                    sessionStateId,
                    _pipeline.State.CurrentStage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    _pipeline.State.CurrentHandoff.IsValid,
                    "no_active_session_activity",
                    "Pipeline is not started or already completed.");
            }

            SessionActivityStage stage = _pipeline.State.CurrentStage;
            if (_pipeline.State.CurrentPendingOperation.IsValid)
            {
                return BuildRouteExitTeardownBlocked(
                    "pending_operation_active",
                    $"SessionActivity route-exit teardown blocked while pending operation is active. pendingOperation='{_pipeline.State.CurrentPendingOperation}'.");
            }

            if (stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.TeardownCompleted,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    _pipeline.State.CurrentHandoff.IsValid,
                    "already_deactivated",
                    "Activity is already deactivated.");
            }

            SessionActivityCommandResult closeForRouteExitResult = _pipeline.CloseForRouteExit(
                normalizedSource,
                $"{normalizedReason}/route_exit_teardown_close_for_route_exit");
            if (!closeForRouteExitResult.IsValid || closeForRouteExitResult.IsRejected)
            {
                return BuildRouteExitTeardownBlocked(
                    "close_for_route_exit_rejected",
                    $"CloseForRouteExit rejected during route exit teardown. resultKind='{closeForRouteExitResult.Kind}' reason='{closeForRouteExitResult.Reason}'.");
            }

            stage = _pipeline.State.CurrentStage;
            bool hasPendingHandoff = _pipeline.State.CurrentHandoff.IsValid;
            if ((stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit) && !hasPendingHandoff)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.TeardownCompleted,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    false,
                    "teardown_completed",
                    "SessionActivity deactivated before route unload.");
            }

            return BuildRouteExitTeardownBlocked(
                "teardown_not_completed_before_unload",
                $"SessionActivity stage '{stage}' cannot proceed to route unload without explicit canonical close.");
        }

        public SessionActivityCommandResult ExecuteCommand(SessionActivityCommand command, string actionLabel)
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.Execute(command);
            LogResult(actionLabel, result);
            return result;
        }

        public string DumpState()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpState");
            builder.AppendLine($"sessionStateId='{sessionStateId}' autoStart='{autoStart}'");
            builder.AppendLine($"pipelineId='{State.PipelineId}' sessionStateId='{State.SessionId}'");
            builder.AppendLine($"entrySequence='{State.CurrentEntrySequence}'");
            builder.AppendLine($"executionState='{State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{GateState}'");
            builder.AppendLine($"catalog='{_catalog.Summary}'");
            builder.AppendLine($"started='{State.HasStarted}' completed='{State.HasCompleted}' stage='{State.CurrentStage}'");
            builder.AppendLine($"currentActivity='{State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"definition='{State.CurrentDefinition}'");
            builder.AppendLine($"identity='{State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{State.CurrentPendingOperation}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine("qaLifecycleRail='ActivityRunning -> CompleteCurrentActivity/RestartCurrentActivity; CompleteActivationWindow/CompleteDeactivationWindow apenas quando window stage=Ready; ContinueToNextActivity apenas se policy=ManualContinue'");
            builder.AppendLine("facts:");
            for (int index = 0; index < State.Facts.Count; index++)
            {
                builder.AppendLine($"- {State.Facts[index]}");
            }

            builder.AppendLine("snapshots:");
            for (int index = 0; index < State.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {State.Snapshots[index]}");
            }

            builder.AppendLine("trace:");
            for (int index = 0; index < State.Trace.Count; index++)
            {
                builder.AppendLine($"- {State.Trace[index]}");
            }

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpTrace()
        {
            EnsurePipeline();
            string trace = BuildTraceDump();
            Debug.Log(trace);
            return trace;
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

            string outcome = result.Kind.ToString();
            Debug.Log($"[OBS][SessionActivityPipeline][Host] action='{action}' outcomeKind='{outcome}' reason='{result.Reason}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}'");

            for (int index = 0; index < result.Facts.Count; index++)
            {
                Debug.Log(result.Facts[index].ToString());
            }

            // Facts assíncronos podem ser emitidos após o retorno do command result.
            // Logamos o estado acumulado para garantir observabilidade canônica por kind.
            for (int index = 0; index < State.Facts.Count; index++)
            {
                Debug.Log($"[OBS][SessionActivityPipeline][Host][StateFact] {State.Facts[index]}");
            }

            for (int index = 0; index < State.Snapshots.Count; index++)
            {
                Debug.Log(State.Snapshots[index].ToString());
            }

            Debug.Log(BuildTraceDump());
        }

        private string BuildTraceDump()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] Trace");
            for (int index = 0; index < State.Trace.Count; index++)
            {
                builder.AppendLine(State.Trace[index]);
            }

            return builder.ToString().TrimEnd();
        }

        private string GetPendingHandoffTarget()
        {
            return State.CurrentHandoff.IsValid
                ? State.CurrentHandoff.NextActivityId
                : "<none>";
        }

        private string GetNextExpectedQaAction()
        {
            SessionActivityStage stage = State.CurrentStage;
            bool hasPendingHandoff = State.CurrentHandoff.IsValid;

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

            if (stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
            {
                return "No local QA action";
            }

            return "No local QA action";
        }

        private string BuildHostBanner()
        {
            return $"[OBS][SessionActivityPipeline][Host] initialized sessionStateId='{sessionStateId}' autoStart='{autoStart}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}' catalog='{_catalog.Summary}'";
        }

        private string BuildStateObservationToken()
        {
            SessionActivityPendingOperation pending = State.CurrentPendingOperation;
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

        private SessionActivityRouteExitTeardownResult BuildRouteExitTeardownBlocked(string reason, string detail)
        {
            return new SessionActivityRouteExitTeardownResult(
                SessionActivityRouteExitTeardownKind.Blocked,
                sessionStateId,
                _pipeline.State.CurrentStage,
                _pipeline.State.CurrentDefinition.ActivityId,
                _pipeline.State.CurrentHandoff.IsValid,
                reason,
                detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private ActivityTransitionContinuePolicy ResolveCurrentContinuePolicy()
        {
            SessionActivityDefinition current = State.CurrentDefinition;
            return current.IsValid
                ? current.NextActivityTransitionContinuePolicy
                : ActivityTransitionContinuePolicy.Unknown;
        }

        private static void RegisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    throw new InvalidOperationException($"Global dependency '{typeof(T).Name}' is already registered with a different instance.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal(instance);
        }
    }
}



