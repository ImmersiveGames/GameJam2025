using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityPipeline : ISessionActivityEntryHandoffReceiver, ISessionActivityPendingOperationCallback
    {
        private const string PipelineId = "SessionActivityPipeline.Base11.Sandbox";
        private readonly SessionActivityCatalog _catalog;
        private readonly SessionActivityRuntimeState _state;
        private readonly SessionActivitySimulationGate _sessionActivitySimulationGate;
        private readonly ISessionActivityPauseOverlayAdapter _pauseOverlayAdapter;
        private readonly ISessionActivityInputModeAdapter _inputModeAdapter;
        private readonly ISessionActivityTransitionAdapter _transitionAdapter;
        private readonly ISessionActivityTransitionLoadingAdapter _transitionLoadingAdapter;
        private readonly ISessionActivityWindowSceneAdapter _windowSceneAdapter;
        private readonly ISessionActivityPendingOperationRunner _pendingOperationRunner;
        private readonly IPlayerActorMaterializationAdapter _playerActorMaterializationAdapter;
        private readonly IPlayerActorParticipationAdapter _playerActorParticipationAdapter;
        private readonly IPlayerActorResetAdapter _playerActorResetAdapter;
        private readonly ActivityPlayerActorRegistry _activityPlayerActorRegistry;
        private readonly string _sessionId;
        private PendingNavigationTransition _pendingNavigationTransition;
        private SessionActivityRouteTransitionContext _routeTransitionContext;
        private SessionActivityTransitionResolution _pendingTransitionResolution;
        private bool _pendingTransitionCurtainReveal;
        private bool _pendingTransitionCurtainClosed;
        private bool _pendingTransitionLoadingVisible;
        private PendingInternalActivityTransition _pendingInternalActivityTransition;
        private PendingRestartTransition _pendingRestartTransition;
        private string _pendingRestartCompletionActivityId;
        private int _pendingRestartCompletionEntrySequence;
        private PlayerSelectionSnapshot _lastPlayerSelectionSnapshot;

        private readonly struct PendingNavigationTransition
        {
            public PendingNavigationTransition(SessionActivityDefinition target, bool wrapped, int targetEntrySequence)
            {
                Target = target;
                Wrapped = wrapped;
                TargetEntrySequence = targetEntrySequence;
            }

            public SessionActivityDefinition Target { get; }
            public bool Wrapped { get; }
            public int TargetEntrySequence { get; }
            public bool IsValid => Target.IsValid && TargetEntrySequence > 0;
        }

        private readonly struct PendingInternalActivityTransition
        {
            public PendingInternalActivityTransition(
                string fromActivityId,
                int fromEntrySequence,
                string toActivityId,
                int toEntrySequence,
                bool handoffPrepared,
                bool continueAccepted)
            {
                FromActivityId = Normalize(fromActivityId);
                FromEntrySequence = fromEntrySequence;
                ToActivityId = Normalize(toActivityId);
                ToEntrySequence = toEntrySequence;
                HandoffPrepared = handoffPrepared;
                ContinueAccepted = continueAccepted;
            }

            public string FromActivityId { get; }
            public int FromEntrySequence { get; }
            public string ToActivityId { get; }
            public int ToEntrySequence { get; }
            public bool HandoffPrepared { get; }
            public bool ContinueAccepted { get; }
            public bool IsValid =>
                !string.IsNullOrWhiteSpace(FromActivityId) &&
                !string.IsNullOrWhiteSpace(ToActivityId) &&
                FromEntrySequence > 0 &&
                ToEntrySequence > 0 &&
                HandoffPrepared;
        }

        private readonly struct PendingRestartTransition
        {
            public PendingRestartTransition(
                SessionActivityDefinition activity,
                int fromEntrySequence,
                int nextEntrySequence,
                string source,
                string reason)
            {
                Activity = activity;
                FromEntrySequence = fromEntrySequence;
                NextEntrySequence = nextEntrySequence;
                Source = Normalize(source);
                Reason = Normalize(reason);
            }

            public SessionActivityDefinition Activity { get; }
            public int FromEntrySequence { get; }
            public int NextEntrySequence { get; }
            public string Source { get; }
            public string Reason { get; }
            public bool IsValid => Activity.IsValid && FromEntrySequence > 0 && NextEntrySequence > 0;
        }

        public SessionActivityPipeline(
            SessionActivityCatalog catalog,
            string sessionStateId,
            ISessionActivityPauseOverlayAdapter pauseOverlayAdapter,
            ISessionActivityInputModeAdapter inputModeAdapter,
            ISessionActivityTransitionAdapter transitionAdapter,
            ISessionActivityTransitionLoadingAdapter transitionLoadingAdapter,
            ISessionActivityWindowSceneAdapter windowSceneAdapter,
            ISessionActivityPendingOperationRunner pendingOperationRunner)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _state = new SessionActivityRuntimeState();
            _sessionActivitySimulationGate = new SessionActivitySimulationGate();
            _pauseOverlayAdapter = pauseOverlayAdapter ?? throw new ArgumentNullException(nameof(pauseOverlayAdapter));
            _inputModeAdapter = inputModeAdapter ?? throw new ArgumentNullException(nameof(inputModeAdapter));
            _transitionAdapter = transitionAdapter ?? throw new ArgumentNullException(nameof(transitionAdapter));
            _transitionLoadingAdapter = transitionLoadingAdapter ?? throw new ArgumentNullException(nameof(transitionLoadingAdapter));
            _windowSceneAdapter = windowSceneAdapter ?? throw new ArgumentNullException(nameof(windowSceneAdapter));
            _pendingOperationRunner = pendingOperationRunner ?? throw new ArgumentNullException(nameof(pendingOperationRunner));
            _playerActorMaterializationAdapter = new PlayerActorMaterializationAdapter();
            _playerActorParticipationAdapter = new PlayerActorParticipationAdapter();
            _playerActorResetAdapter = new PlayerActorResetAdapter();
            _activityPlayerActorRegistry = new ActivityPlayerActorRegistry();
            _sessionId = Normalize(sessionStateId);

            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                throw new ArgumentException("sessionStateId is required.", nameof(sessionStateId));
            }

            ResolveFirstActivityOrFail();
        }

        public SessionActivityRuntimeState State => _state;
        public SessionActivityCatalog Catalog => _catalog;
        public ActivityExecutionBlockingState GateState => _sessionActivitySimulationGate.State;
        public string SessionId => _sessionId;

        public SessionActivityCommand BuildStartCommand(string source, string reason)
        {
            SessionActivityDefinition firstDefinition = ResolveFirstActivityOrFail();

            return new SessionActivityCommand(
                SessionActivityCommandKind.StartActivity,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivityActivationStarted, 1),
                source,
                reason);
        }

        public SessionActivityCommand BuildCompleteCurrentActivityCommand(string source, string reason)
        {
            EnsureStartedOrFail("CompleteCurrentActivityCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommand BuildCompleteActivationWindowCommand(string source, string reason)
        {
            EnsureStartedOrFail("CompleteActivationWindowCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteActivationWindow,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommand BuildCompleteDeactivationWindowCommand(string source, string reason)
        {
            EnsureStartedOrFail("CompleteDeactivationWindowCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteDeactivationWindow,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommand BuildContinueToNextActivityCommand(string source, string reason)
        {
            EnsureStartedOrFail("ContinueToNextActivityCommand");
            if (!_state.CurrentHandoff.IsValid)
            {
                throw new InvalidOperationException("No handoff is available to continue.");
            }

            return new SessionActivityCommand(
                SessionActivityCommandKind.ContinueToNextActivity,
                _state.CurrentHandoff.ToIdentity,
                source,
                reason);
        }

        public SessionActivityCommand BuildCloseForRouteExitCommand(string source, string reason)
        {
            EnsureStartedOrFail("CloseForRouteExitCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.CloseForRouteExit,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommandResult Start(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.StartActivity, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            return Execute(BuildStartCommand(source, reason));
        }

        public SessionActivityCommandResult DebugStartActivity(string source, string reason)
        {
            return Start(source, reason);
        }

        public SessionActivityCommandResult StartFromPreparedHandoff(
            SessionActivityEntryHandoff handoff,
            string source,
            string reason)
        {
            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("SessionActivityEntryHandoff is invalid.");
            }

            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.StartActivity, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            if (!string.Equals(handoff.SessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return RejectPreparedHandoff(
                    handoff,
                    source,
                    reason,
                    "stale_or_foreign_handoff",
                    $"Prepared handoff session state '{handoff.SessionStateId}' does not match pipeline session '{_sessionId}'.");
            }

            SessionActivityDefinition initialDefinition = handoff.HasResolvedActivity
                ? ResolveActivityByIdOrFail(handoff.ActivityId)
                : ResolveFirstActivityOrFail();

            if (handoff.HasResolvedActivity && initialDefinition.ActivityOrdinal != handoff.ActivityOrdinal)
            {
                throw new InvalidOperationException($"Prepared handoff activity ordinal mismatch. expected='{initialDefinition.ActivityOrdinal}' got='{handoff.ActivityOrdinal}'.");
            }

            int entrySequence = ResolveNextEntrySequence();
            if (entrySequence <= 0)
            {
                throw new InvalidOperationException("Prepared handoff could not allocate the next entry sequence.");
            }

            SessionActivityIdentity activationIdentity = BuildIdentity(initialDefinition, SessionActivityStage.ActivityActivationStarted, entrySequence);
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartActivity,
                activationIdentity,
                source,
                reason);

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
            {
                return rejectedResult;
            }

            _state.Reset(PipelineId, _sessionId);
            _routeTransitionContext = default;
            _pendingTransitionResolution = default;
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingInternalActivityTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;
            _lastPlayerSelectionSnapshot = BuildMvpPlayerSelectionSnapshotFromPlayerPreparation(handoff, activationIdentity, source, reason);
            _activityPlayerActorRegistry.ClearAllRouteRetained();
            _state.SetCurrentDefinition(initialDefinition);
            _state.SetCurrentIdentity(activationIdentity, SessionActivityStage.ActivityActivationStarted);
            _state.MarkStarted();
            _routeTransitionContext = handoff.RouteTransitionContext;
            if (!handoff.HasResolvedActivity)
            {
                _state.AppendTrace($"[OBS][SessionActivityPipeline] FirstCatalogActivityResolved activityId='{initialDefinition.ActivityId}' activityOrdinal='{initialDefinition.ActivityOrdinal}' handoff='{handoff}' source='{source}' reason='{reason}'");
            }
            _state.AppendTrace($"[OBS][SessionActivityPipeline] start_from_prepared_handoff handoff='{handoff}' source='{source}' reason='{reason}'");
            _state.AppendTrace($"[OBS][SessionActivityPipeline] SessionActivityEntryHandoffAccepted handoff='{handoff}' source='{source}' reason='{reason}'");
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Handoff] SessionActivityEntryHandoffAccepted pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{initialDefinition.ActivityId}' activityOrdinal='{initialDefinition.ActivityOrdinal}' entrySequence='{entrySequence}' source='{source}' reason='{reason}' playerPreparationOutcome='{handoff.PlayerPreparation.Outcome}' plannedPlayers='{handoff.PlayerPreparation.PlannedPlayers}' materializedPlayers='{handoff.PlayerPreparation.MaterializedPlayers}' pendingRequiredPlayers='{handoff.PlayerPreparation.PendingRequiredPlayers}'.",
                DebugUtility.Colors.Success);

            EmitFact(emittedFacts, SessionActivityFactKind.PipelineStarted, activationIdentity, source, reason, "SessionActivityPipeline started from prepared handoff.");
            EmitSnapshot(emittedSnapshots, "pipeline_started_from_handoff", source, reason, "Pipeline started from prepared handoff.");

            EnterActivity(initialDefinition, command, emittedFacts, emittedSnapshots, entrySequence);

            SessionActivityCommandResult result = new(
                SessionActivityCommandResultKind.Accepted,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] start_from_prepared_handoff_completed handoff='{handoff}' result='{result.Kind}'");
            return result;
        }

        public SessionActivityCommandResult CompleteCurrentActivity(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.CompleteCurrentActivity, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.CompleteCurrentActivity,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            return Execute(BuildCompleteCurrentActivityCommand(source, reason));
        }

        public SessionActivityCommandResult CompleteActivationWindow(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.CompleteActivationWindow, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.CompleteActivationWindow,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            return Execute(BuildCompleteActivationWindowCommand(source, reason));
        }

        public SessionActivityCommandResult CompleteDeactivationWindow(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.CompleteDeactivationWindow, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.CompleteDeactivationWindow,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            return Execute(BuildCompleteDeactivationWindowCommand(source, reason));
        }

        public SessionActivityCommandResult ContinueToNextActivity(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.ContinueToNextActivity, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.ContinueToNextActivity,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            if (!_state.CurrentHandoff.IsValid)
            {
                if (_state.CurrentIdentity.IsValid)
                {
                    return RejectNoHandoffAvailable(source, reason);
                }

                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.ContinueToNextActivity,
                    source,
                    reason,
                    "no_handoff_available");
            }

            return Execute(BuildContinueToNextActivityCommand(source, reason));
        }

        public SessionActivityCommandResult CloseForRouteExit(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.CloseForRouteExit, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.CloseForRouteExit,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            if (_state.CurrentPendingOperation.IsValid)
            {
                SessionActivityCommand command = new(
                    SessionActivityCommandKind.CloseForRouteExit,
                    _state.CurrentIdentity,
                    source,
                    reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(
                    command,
                    rejectedFacts,
                    "pending_operation_active",
                    $"CloseForRouteExit blocked because pending operation is active. pendingOperation='{_state.CurrentPendingOperation}'.",
                    _state.CurrentIdentity,
                    true);
                return new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    command,
                    rejectedFacts,
                    "pending_operation_active");
            }

            return Execute(BuildCloseForRouteExitCommand(source, reason));
        }

        public SessionActivityCommandResult GoToNextActivity(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToNextActivity, source, reason);
        }

        public SessionActivityCommandResult GoToPreviousActivity(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToPreviousActivity, source, reason);
        }

        public SessionActivityCommandResult RestartCurrentActivity(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.RestartCurrentActivity, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.RestartCurrentActivity,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                SessionActivityCommand rejectedCommand = new(SessionActivityCommandKind.RestartCurrentActivity, _state.CurrentIdentity, source, reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(
                    rejectedCommand,
                    rejectedFacts,
                    "unexpected_stage",
                    $"RestartCurrentActivity requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.",
                    _state.CurrentIdentity,
                    true);
                EmitFact(
                    rejectedFacts,
                    SessionActivityFactKind.ActivityRestartRejected,
                    _state.CurrentIdentity,
                    source,
                    "unexpected_stage",
                    $"RestartCurrentActivity rejected because stage is '{_state.CurrentStage}'.");
                return new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    rejectedCommand,
                    rejectedFacts,
                    "unexpected_stage");
            }

            return Execute(new SessionActivityCommand(
                SessionActivityCommandKind.RestartCurrentActivity,
                _state.CurrentIdentity,
                source,
                reason));
        }

        public SessionActivityCommandResult GoToActivity(string activityId, string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToActivity, source, reason, activityId);
        }

        public SessionActivityCommandResult PauseRequested(string source, string reason)
        {
            return ExecutePauseRequested(source, reason);
        }

        public SessionActivityCommandResult ResumeRequested(string source, string reason)
        {
            return ExecuteResumeRequested(source, reason);
        }

        public SessionActivityCommandResult Execute(SessionActivityCommand command)
        {
            if (!command.IsValid)
            {
                if (command.Kind == SessionActivityCommandKind.GoToActivity && string.IsNullOrWhiteSpace(command.TargetActivityId))
                {
                    return RejectInvalidGoToActivityCommand(command);
                }

                throw new InvalidOperationException("SessionActivityCommand is invalid.");
            }

            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(command.Kind, command.Source, command.Reason);
            }

            if (command.Kind == SessionActivityCommandKind.PauseRequested ||
                command.Kind == SessionActivityCommandKind.PauseSimulation)
            {
                return ExecutePauseRequested(command.Source, command.Reason, command.Identity);
            }

            if (command.Kind == SessionActivityCommandKind.ResumeRequested ||
                command.Kind == SessionActivityCommandKind.ResumeSimulation)
            {
                return ExecuteResumeRequested(command.Source, command.Reason, command.Identity);
            }

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
            {
                return rejectedResult;
            }

            switch (command.Kind)
            {
                case SessionActivityCommandKind.StartActivity:
                    EmitStart(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.CompleteActivationWindow:
                    EnqueueLifecycleAsyncCommand(command);
                    break;
                case SessionActivityCommandKind.CompleteDeactivationWindow:
                    EnqueueLifecycleAsyncCommand(command);
                    break;
                case SessionActivityCommandKind.CompleteCurrentActivity:
                    EnqueueLifecycleAsyncCommand(command);
                    break;
                case SessionActivityCommandKind.RestartCurrentActivity:
                    EnqueueLifecycleAsyncCommand(command);
                    break;
                case SessionActivityCommandKind.ContinueToNextActivity:
                    EnqueueLifecycleAsyncCommand(command);
                    break;
                case SessionActivityCommandKind.CloseForRouteExit:
                    EmitCloseForRouteExit(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.GoToNextActivity:
                case SessionActivityCommandKind.GoToPreviousActivity:
                case SessionActivityCommandKind.GoToActivity:
                    EmitNavigation(command, emittedFacts, emittedSnapshots);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported command kind '{command.Kind}'.");
            }

            SessionActivityCommandResultKind resultKind = emittedFacts.Count == 0
                ? SessionActivityCommandResultKind.Accepted
                : emittedFacts[emittedFacts.Count - 1].Kind switch
                {
                    SessionActivityFactKind.CommandRejected => SessionActivityCommandResultKind.Rejected,
                    SessionActivityFactKind.PipelineCompleted => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.ActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.ActivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.GameplayContentSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.DeactivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.NextActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.SimulationPaused => SessionActivityCommandResultKind.Accepted,
                    SessionActivityFactKind.SimulationResumed => SessionActivityCommandResultKind.Accepted,
                    _ => SessionActivityCommandResultKind.Accepted,
                };

            return new SessionActivityCommandResult(resultKind, command, emittedFacts, emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        public IReadOnlyList<SessionActivityFact> Facts => _state.Facts;
        public IReadOnlyList<string> Trace => _state.Trace;

        private async Task ExecuteLifecycleAsync(SessionActivityCommand command)
        {
            try
            {
                if (!command.IsValid)
                {
                    throw new InvalidOperationException("SessionActivityCommand is invalid.");
                }

                if (IsTerminalCompleted())
                {
                    return;
                }

                if (command.Kind == SessionActivityCommandKind.CompleteCurrentActivity ||
                    command.Kind == SessionActivityCommandKind.RestartCurrentActivity ||
                    command.Kind == SessionActivityCommandKind.ContinueToNextActivity ||
                    command.Kind == SessionActivityCommandKind.CompleteDeactivationWindow ||
                    command.Kind == SessionActivityCommandKind.CompleteActivationWindow)
                {
                    List<SessionActivityFact> emittedFacts = new();
                    List<SessionActivitySnapshot> emittedSnapshots = new();

                    if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
                    {
                        return;
                    }

                    if (command.Kind == SessionActivityCommandKind.CompleteCurrentActivity)
                    {
                        await EmitCompleteAsync(command, emittedFacts, emittedSnapshots);
                    }
                    else if (command.Kind == SessionActivityCommandKind.RestartCurrentActivity)
                    {
                        await EmitRestartCurrentActivityAsync(command, emittedFacts, emittedSnapshots);
                    }
                    else if (command.Kind == SessionActivityCommandKind.CompleteActivationWindow)
                    {
                        await EmitCompleteActivationWindowAsync(command, emittedFacts, emittedSnapshots);
                    }
                    else if (command.Kind == SessionActivityCommandKind.CompleteDeactivationWindow)
                    {
                        await EmitCompleteDeactivationWindowAsync(command, emittedFacts, emittedSnapshots);
                    }
                    else
                    {
                        await EmitContinueAsync(command, emittedFacts, emittedSnapshots);
                    }

                    return;
                }
            }
            catch (Exception exception)
            {
                _pendingInternalActivityTransition = default;
                _pendingRestartCompletionActivityId = string.Empty;
                _pendingRestartCompletionEntrySequence = 0;
                _state.AppendTrace(
                    $"[FATAL][SessionActivityPipeline] lifecycle_async_failed command='{command.Kind}' commandIdentity='{command.Identity}' pipelineIdentity='{_state.CurrentIdentity}' pipelineId='{_state.PipelineId}' sessionStateId='{_state.SessionId}' stage='{_state.CurrentStage}' executionState='{_state.CurrentExecutionState}' pendingTransitionMode='{_pendingTransitionResolution.Mode}' pendingTransitionCurtainReveal='{_pendingTransitionCurtainReveal}' pendingTransitionCurtainClosed='{_pendingTransitionCurtainClosed}' pendingTransitionLoadingVisible='{_pendingTransitionLoadingVisible}' exceptionType='{exception.GetType().FullName}' exceptionMessage='{exception.Message}' source='{command.Source}' reason='{command.Reason}'.");
                throw;
            }
        }

        private void EnqueueLifecycleAsyncCommand(SessionActivityCommand command)
        {
            _ = ExecuteLifecycleAsync(command);
        }

        private SessionActivityPendingOperation BuildWindowPendingOperation(
            SessionActivityPendingOperationKind operationKind,
            SessionActivityPendingWindowKind windowKind,
            SessionActivityDefinition definition,
            int entrySequence,
            Foundation.Platform.SceneReferences.SceneKeyAsset sceneKey,
            string source,
            string reason)
        {
            string sceneKeyName = sceneKey != null ? sceneKey.name : string.Empty;
            string sceneName = sceneKey != null ? Normalize(sceneKey.SceneName) : string.Empty;
            return new SessionActivityPendingOperation(
                Guid.NewGuid().ToString("N"),
                _state.PipelineId,
                _state.SessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                windowKind,
                operationKind,
                sceneKeyName,
                sceneName,
                source,
                reason);
        }

        public void CompletePendingOperation(SessionActivityPendingOperation operation, string source, string reason)
        {
            if (!TryValidatePendingOperationCompletion(operation, source, reason))
            {
                return;
            }

            // Consome a pending operation validada antes de processar o callback.
            // Isso evita limpar indevidamente uma nova pending operation criada durante o mesmo callback (ex.: reentry de restart).
            _state.ClearPendingOperation();

            SessionActivityDefinition definition = _state.CurrentDefinition;
            int entrySequence = _state.CurrentEntrySequence;
            List<SessionActivityFact> facts = new();
            List<SessionActivitySnapshot> snapshots = new();
            SessionActivityCommand syntheticCommand = new(
                SessionActivityCommandKind.CompleteCurrentActivity,
                _state.CurrentIdentity,
                source,
                reason);

            switch (operation.OperationKind)
            {
                case SessionActivityPendingOperationKind.ActivationWindowSceneLoad:
                {
                    SessionActivityIdentity loadedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowAdditiveSceneLoaded, entrySequence);
                    _state.SetCurrentIdentity(loadedIdentity, SessionActivityStage.ActivationWindowAdditiveSceneLoaded);
                    EmitFact(facts, SessionActivityFactKind.ActivationWindowAdditiveSceneLoaded, loadedIdentity, source, reason, $"'{definition.ActivityId}' activation additive scene loaded. scene='{operation.SceneName}' completionReason='{reason}'.");
                    EmitSnapshot(snapshots, "activation_window_additive_scene_loaded", source, reason, $"'{definition.ActivityId}' activation additive scene loaded. scene='{operation.SceneName}' completionReason='{reason}'.");

                    SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowReady, entrySequence);
                    _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivationWindowReady);
                    EmitFact(facts, SessionActivityFactKind.ActivationWindowReady, readyIdentity, source, reason, $"'{definition.ActivityId}' activation window ready.");
                    EmitSnapshot(snapshots, "activation_window_ready", source, reason, $"'{definition.ActivityId}' activation window ready.");
                    break;
                }
                case SessionActivityPendingOperationKind.ActivationWindowSceneUnload:
                {
                    SessionActivityIdentity unloadedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowAdditiveSceneUnloaded, entrySequence);
                    _state.SetCurrentIdentity(unloadedIdentity, SessionActivityStage.ActivationWindowAdditiveSceneUnloaded);
                    EmitFact(facts, SessionActivityFactKind.ActivationWindowAdditiveSceneUnloaded, unloadedIdentity, source, reason, $"'{definition.ActivityId}' activation additive scene unloaded. scene='{operation.SceneName}'.");
                    EmitSnapshot(snapshots, "activation_window_additive_scene_unloaded", source, reason, $"'{definition.ActivityId}' activation additive scene unloaded. scene='{operation.SceneName}'.");
                    EnterRunning(definition, syntheticCommand, facts, snapshots, entrySequence);
                    break;
                }
                case SessionActivityPendingOperationKind.DeactivationWindowSceneLoad:
                {
                    SessionActivityIdentity loadedIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowAdditiveSceneLoaded, entrySequence);
                    _state.SetCurrentIdentity(loadedIdentity, SessionActivityStage.DeactivationWindowAdditiveSceneLoaded);
                    EmitFact(facts, SessionActivityFactKind.DeactivationWindowAdditiveSceneLoaded, loadedIdentity, source, reason, $"'{definition.ActivityId}' deactivation additive scene loaded. scene='{operation.SceneName}'.");
                    EmitSnapshot(snapshots, "deactivation_window_additive_scene_loaded", source, reason, $"'{definition.ActivityId}' deactivation additive scene loaded. scene='{operation.SceneName}'.");

                    SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowReady, entrySequence);
                    _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.DeactivationWindowReady);
                    EmitFact(facts, SessionActivityFactKind.DeactivationWindowReady, readyIdentity, source, reason, $"'{definition.ActivityId}' deactivation window ready.");
                    EmitSnapshot(snapshots, "deactivation_window_ready", source, reason, $"'{definition.ActivityId}' deactivation window ready.");
                    break;
                }
                case SessionActivityPendingOperationKind.DeactivationWindowSceneUnload:
                {
                    SessionActivityIdentity unloadedIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded, entrySequence);
                    _state.SetCurrentIdentity(unloadedIdentity, SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded);
                    EmitFact(facts, SessionActivityFactKind.DeactivationWindowAdditiveSceneUnloaded, unloadedIdentity, source, reason, $"'{definition.ActivityId}' deactivation additive scene unloaded. scene='{operation.SceneName}'.");
                    EmitSnapshot(snapshots, "deactivation_window_additive_scene_unloaded", source, reason, $"'{definition.ActivityId}' deactivation additive scene unloaded. scene='{operation.SceneName}'.");

                    if (_pendingRestartTransition.IsValid)
                    {
                        _ = FinalizePendingRestartTransition(
                            definition,
                            syntheticCommand,
                            facts,
                            snapshots,
                            entrySequence);
                    }
                    else if (operation.Reason.IndexOf("route_exit", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        FinalizeDeactivationForRouteExit(definition, syntheticCommand, facts, snapshots, entrySequence);
                    }
                    else
                    {
                        _ = FinalizeDeactivationAndContinuation(definition, syntheticCommand, facts, snapshots, entrySequence);
                    }

                    break;
                }
                default:
                    throw new InvalidOperationException($"Unsupported pending operation completion kind '{operation.OperationKind}'.");
            }
        }

        public void FailPendingOperation(SessionActivityPendingOperation operation, string source, string reason, string error)
        {
            if (!TryValidatePendingOperationCompletion(operation, source, reason))
            {
                return;
            }

            SessionActivityPendingOperation active = _state.CurrentPendingOperation;
            throw new InvalidOperationException(
                $"[FATAL][SessionActivityPipeline] Pending operation failed operationId='{active.OperationId}' operationKind='{active.OperationKind}' activityId='{active.ActivityId}' currentStage='{_state.CurrentStage}' sceneName='{active.SceneName}' reason='{error}'.");
        }

        private bool TryValidatePendingOperationCompletion(SessionActivityPendingOperation operation, string source, string reason)
        {
            SessionActivityPendingOperation active = _state.CurrentPendingOperation;
            if (!active.IsValid)
            {
                _state.AppendTrace($"[OBS][SessionActivityPipeline] pending_operation_completion_rejected reason='no_pending_operation' incoming='{operation}' source='{source}' reason='{reason}'.");
                return false;
            }

            bool matches =
                string.Equals(active.OperationId, operation.OperationId, StringComparison.Ordinal) &&
                string.Equals(active.PipelineId, operation.PipelineId, StringComparison.Ordinal) &&
                string.Equals(active.SessionStateId, operation.SessionStateId, StringComparison.Ordinal) &&
                string.Equals(active.ActivityId, operation.ActivityId, StringComparison.Ordinal) &&
                active.ActivityOrdinal == operation.ActivityOrdinal &&
                active.EntrySequence == operation.EntrySequence &&
                active.OperationKind == operation.OperationKind &&
                active.WindowKind == operation.WindowKind;

            if (matches)
            {
                return true;
            }

            List<SessionActivityFact> facts = new();
            EmitFact(
                facts,
                SessionActivityFactKind.CommandRejected,
                _state.CurrentIdentity,
                source,
                reason,
                $"Pending operation completion rejected as stale/foreign. active='{active}' incoming='{operation}'.");
            return false;
        }

        private static SessionActivityCommandResult BuildCommandResult(SessionActivityCommand command, List<SessionActivityFact> emittedFacts)
        {
            SessionActivityCommandResultKind resultKind = emittedFacts.Count == 0
                ? SessionActivityCommandResultKind.Accepted
                : emittedFacts[emittedFacts.Count - 1].Kind switch
                {
                    SessionActivityFactKind.CommandRejected => SessionActivityCommandResultKind.Rejected,
                    SessionActivityFactKind.PipelineCompleted => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.ActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.ActivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.GameplayContentSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.DeactivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.NextActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.SimulationPaused => SessionActivityCommandResultKind.Accepted,
                    SessionActivityFactKind.SimulationResumed => SessionActivityCommandResultKind.Accepted,
                    _ => SessionActivityCommandResultKind.Accepted,
                };

            return new SessionActivityCommandResult(
                resultKind,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        private void EmitStart(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (_state.HasStarted)
            {
                EmitRejected(command, facts, "pipeline_already_started", "SessionActivityPipeline already started.", _state.CurrentIdentity, true);
                return;
            }

            SessionActivityDefinition firstDefinition = ResolveFirstActivityOrFail();
            int entrySequence = 1;
            SessionActivityIdentity activationIdentity = BuildIdentity(firstDefinition, SessionActivityStage.ActivityActivationStarted, entrySequence);
            SessionActivitySnapshot pipelineStartedSnapshot = new(
                activationIdentity,
                firstDefinition,
                default,
                command.Source,
                command.Reason,
                "pipeline_started: Pipeline started.");

            if (!activationIdentity.IsValid)
            {
                throw new InvalidOperationException("Activation identity is invalid.");
            }

            if (!pipelineStartedSnapshot.IsValid)
            {
                throw new InvalidOperationException("PipelineStarted snapshot is invalid.");
            }

            _state.Reset(PipelineId, _sessionId);
            _routeTransitionContext = default;
            _pendingTransitionResolution = default;
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingInternalActivityTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;
            _lastPlayerSelectionSnapshot = new PlayerSelectionSnapshot(
                activationIdentity,
                Array.Empty<PlayerSelectionEntry>(),
                PlayerSelectionSnapshotSource.ExplicitPayload,
                0,
                command.Source,
                command.Reason);
            _activityPlayerActorRegistry.ClearAllRouteRetained();
            _state.SetCurrentDefinition(firstDefinition);
            _state.SetCurrentIdentity(activationIdentity, SessionActivityStage.ActivityActivationStarted);
            _state.MarkStarted();

            EmitFact(facts, SessionActivityFactKind.PipelineStarted, activationIdentity, command.Source, command.Reason, "SessionActivityPipeline started.");
            EmitSnapshot(snapshots, "pipeline_started", command.Source, command.Reason, "Pipeline started.");

            EnterActivity(firstDefinition, command, facts, snapshots, entrySequence);
        }

        private Task EmitCompleteActivationWindowAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivationWindowReady, "complete_activation_window"))
            {
                return Task.CompletedTask;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "complete_activation_window"))
            {
                return Task.CompletedTask;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;
            SessionActivityIdentity completedIdentity = BuildIdentity(current, SessionActivityStage.ActivationWindowCompleted, currentEntrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivationWindowCompleted);
            EmitFact(facts, SessionActivityFactKind.ActivationWindowCompleted, completedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' activation window completed.");
            EmitSnapshot(snapshots, "activation_window_completed", command.Source, command.Reason, $"'{current.ActivityId}' activation window completed.");

            if (current.ActivationWindowMode == ActivityWindowMode.AdditiveScene)
            {
                ExecuteActivationWindowAdditiveSceneUnload(current, command, facts, snapshots, currentEntrySequence);
                return Task.CompletedTask;
            }

            EnterRunning(current, command, facts, snapshots, currentEntrySequence);
            return Task.CompletedTask;
        }

        private async Task EmitCompleteAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            ClearPendingNavigationTransition();

            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityRunning, "complete_current_activity"))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "complete_current_activity"))
            {
                return;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;
            SessionActivityIdentity completionRequestedIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompletionRequested, currentEntrySequence);
            _state.SetCurrentIdentity(completionRequestedIdentity, SessionActivityStage.ActivityCompletionRequested);
            EmitFact(facts, SessionActivityFactKind.ActivityCompletionRequested, completionRequestedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completion requested.");
            EmitSnapshot(snapshots, "activity_completion_requested", command.Source, command.Reason, $"'{current.ActivityId}' completion requested.");

            SessionActivityIdentity completingIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompleting, currentEntrySequence);
            _state.SetCurrentIdentity(completingIdentity, SessionActivityStage.ActivityCompleting);
            EmitFact(facts, SessionActivityFactKind.ActivityCompleting, completingIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completing.");
            EmitSnapshot(snapshots, "activity_completing", command.Source, command.Reason, $"'{current.ActivityId}' completing.");
            EmitPlayerActorParticipationExitIfNeeded(current, command, facts, snapshots, currentEntrySequence);

            SessionActivityIdentity deactivationWindowIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowStarted, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationWindowIdentity, SessionActivityStage.DeactivationWindowStarted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowStarted, deactivationWindowIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");
            EmitSnapshot(snapshots, "deactivation_window_started", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");

            EnsureSupportedDeactivationWindowOrFail(current);

            if (current.DeactivationWindowMode == ActivityWindowMode.None)
            {
                SessionActivityIdentity deactivationWindowSkippedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(deactivationWindowSkippedIdentity, SessionActivityStage.DeactivationWindowSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.DeactivationWindowSkippedNoContent, deactivationWindowSkippedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                EmitSnapshot(snapshots, "deactivation_window_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                await FinalizeDeactivationAndContinuation(current, command, facts, snapshots, currentEntrySequence);
                return;
            }

            ExecuteDeactivationWindowAdditiveSceneLoad(current, command, facts, snapshots, currentEntrySequence);
        }

        private async Task EmitRestartCurrentActivityAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            ClearPendingNavigationTransition();

            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityRunning, "restart_current_activity"))
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityRestartRejected,
                    _state.CurrentIdentity,
                    command.Source,
                    "unexpected_stage",
                    $"Restart requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.");
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "restart_current_activity"))
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityRestartRejected,
                    _state.CurrentIdentity,
                    command.Source,
                    "identity_mismatch",
                    "Restart rejected because command identity does not match the active cycle.");
                return;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;
            int nextEntrySequence = ResolveNextEntrySequence();

            SessionActivityIdentity restartRequestedIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompletionRequested, currentEntrySequence);
            _state.SetCurrentIdentity(restartRequestedIdentity, SessionActivityStage.ActivityCompletionRequested);
            EmitFact(facts, SessionActivityFactKind.ActivityRestartRequested, restartRequestedIdentity, command.Source, command.Reason, $"Restart requested for '{current.ActivityId}'.");
            EmitSnapshot(snapshots, "activity_restart_requested", command.Source, command.Reason, $"Restart requested for '{current.ActivityId}'.");
            EmitFact(facts, SessionActivityFactKind.ActivityRestartAccepted, restartRequestedIdentity, command.Source, command.Reason, $"Restart accepted for '{current.ActivityId}' with nextEntrySequence='{nextEntrySequence}'.");
            EmitSnapshot(snapshots, "activity_restart_accepted", command.Source, command.Reason, $"Restart accepted for '{current.ActivityId}' with nextEntrySequence='{nextEntrySequence}'.");

            ClearStateForRestartTransition();
            _pendingRestartTransition = new PendingRestartTransition(current, currentEntrySequence, nextEntrySequence, command.Source, command.Reason);

            SessionActivityIdentity teardownIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompleting, currentEntrySequence);
            _state.SetCurrentIdentity(teardownIdentity, SessionActivityStage.ActivityCompleting);
            EmitFact(facts, SessionActivityFactKind.ActivityRestartTeardownStarted, teardownIdentity, command.Source, command.Reason, $"Restart teardown started for '{current.ActivityId}'.");
            EmitSnapshot(snapshots, "activity_restart_teardown_started", command.Source, command.Reason, $"Restart teardown started for '{current.ActivityId}'.");
            EmitPlayerActorParticipationExitIfNeeded(current, command, facts, snapshots, currentEntrySequence);

            SessionActivityIdentity deactivationWindowIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowStarted, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationWindowIdentity, SessionActivityStage.DeactivationWindowStarted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowStarted, deactivationWindowIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");
            EmitSnapshot(snapshots, "deactivation_window_started", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");

            EnsureSupportedDeactivationWindowOrFail(current);
            if (current.DeactivationWindowMode == ActivityWindowMode.None)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.DeactivationWindowSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.DeactivationWindowSkippedNoContent, skippedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                EmitSnapshot(snapshots, "deactivation_window_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                await FinalizePendingRestartTransition(current, command, facts, snapshots, currentEntrySequence);
                return;
            }

            ExecuteDeactivationWindowAdditiveSceneLoad(current, command, facts, snapshots, currentEntrySequence);
        }

        private void EmitCloseForRouteExit(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            ClearPendingNavigationTransition();
            _state.ClearHandoff();

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;

            if (_state.CurrentStage == SessionActivityStage.ActivityRunning)
            {
                SessionActivityIdentity routeExitRequestedIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompletionRequested, currentEntrySequence);
                _state.SetCurrentIdentity(routeExitRequestedIdentity, SessionActivityStage.ActivityCompletionRequested);
                EmitFact(facts, SessionActivityFactKind.ActivityRouteExitRequested, routeExitRequestedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' route-exit requested.");
                EmitSnapshot(snapshots, "activity_route_exit_requested", command.Source, command.Reason, $"'{current.ActivityId}' route-exit requested.");

                SessionActivityIdentity completingIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompleting, currentEntrySequence);
                _state.SetCurrentIdentity(completingIdentity, SessionActivityStage.ActivityCompleting);
                EmitFact(facts, SessionActivityFactKind.ActivityCompleting, completingIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completing for route-exit.");
                EmitSnapshot(snapshots, "activity_completing", command.Source, command.Reason, $"'{current.ActivityId}' completing for route-exit.");
                EmitPlayerActorParticipationExitIfNeeded(current, command, facts, snapshots, currentEntrySequence);

                SessionActivityIdentity deactivationWindowIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowStarted, currentEntrySequence);
                _state.SetCurrentIdentity(deactivationWindowIdentity, SessionActivityStage.DeactivationWindowStarted);
                EmitFact(facts, SessionActivityFactKind.DeactivationWindowStarted, deactivationWindowIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");
                EmitSnapshot(snapshots, "deactivation_window_started", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");

                EnsureSupportedDeactivationWindowOrFail(current);
                if (current.DeactivationWindowMode == ActivityWindowMode.None)
                {
                    SessionActivityIdentity deactivationWindowSkippedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowSkippedNoContent, currentEntrySequence);
                    _state.SetCurrentIdentity(deactivationWindowSkippedIdentity, SessionActivityStage.DeactivationWindowSkippedNoContent);
                    EmitFact(facts, SessionActivityFactKind.DeactivationWindowSkippedNoContent, deactivationWindowSkippedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                    EmitSnapshot(snapshots, "deactivation_window_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                    FinalizeDeactivationForRouteExit(current, command, facts, snapshots, currentEntrySequence);
                    return;
                }

                ExecuteDeactivationWindowAdditiveSceneLoad(current, command, facts, snapshots, currentEntrySequence);
                return;
            }

            if (_state.CurrentStage == SessionActivityStage.DeactivationWindowReady)
            {
                SessionActivityIdentity completedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowCompleted, currentEntrySequence);
                _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.DeactivationWindowCompleted);
                EmitFact(facts, SessionActivityFactKind.DeactivationWindowCompleted, completedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window completed.");
                EmitSnapshot(snapshots, "deactivation_window_completed", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window completed.");

                ExecuteDeactivationWindowAdditiveSceneUnload(current, command, facts, snapshots, currentEntrySequence);
                return;
            }

            EmitRejected(
                command,
                facts,
                "unexpected_stage",
                $"Route-exit close requires stage '{SessionActivityStage.ActivityRunning}' or '{SessionActivityStage.DeactivationWindowReady}', but current stage is '{_state.CurrentStage}'.",
                _state.CurrentIdentity,
                true);
        }

        private Task EmitCompleteDeactivationWindowAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.DeactivationWindowReady, "complete_deactivation_window"))
            {
                return Task.CompletedTask;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "complete_deactivation_window"))
            {
                return Task.CompletedTask;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;
            SessionActivityIdentity completedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowCompleted, currentEntrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.DeactivationWindowCompleted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowCompleted, completedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window completed.");
            EmitSnapshot(snapshots, "deactivation_window_completed", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window completed.");

            ExecuteDeactivationWindowAdditiveSceneUnload(current, command, facts, snapshots, currentEntrySequence);
            return Task.CompletedTask;
        }

        private async Task FinalizeDeactivationAndContinuation(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            ReleaseActivityGateIfBlocked(command);
            _state.SetExecutionState(ActivityExecutionState.Stopped);
            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            if (_pendingNavigationTransition.IsValid)
            {
                await FinalizePendingNavigationTransition(current, command, facts, snapshots, deactivationIdentity);
                return;
            }

            if (!TryResolveNextActivityForContinuation(current, out SessionActivityDefinition next, out bool wrapped))
            {
                _state.SetCurrentIdentity(BuildIdentity(current, SessionActivityStage.Completed, currentEntrySequence), SessionActivityStage.Completed);
                _state.MarkCompleted();
                _pendingInternalActivityTransition = default;
                EmitFact(facts, SessionActivityFactKind.PipelineCompleted, _state.CurrentIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                EmitSnapshot(snapshots, "pipeline_completed", command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                return;
            }

            SessionActivityTransitionResolution transitionResolution = ResolveNextActivityTransitionResolutionOrFail(current, next);
            int nextEntrySequence = ResolveNextEntrySequence();
            SessionActivityIdentity nextActivationIdentity = BuildIdentity(next, SessionActivityStage.ActivityActivationStarted, nextEntrySequence);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionProfileSelected,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Transition profile selected source='{current.NextActivityTransitionProfileSource}' mode='{transitionResolution.Mode}' from '{current.ActivityId}' to '{next.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_profile_selected",
                command.Source,
                command.Reason,
                $"Transition profile selected source='{current.NextActivityTransitionProfileSource}' mode='{transitionResolution.Mode}' from '{current.ActivityId}' to '{next.ActivityId}'.");
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionProfileResolved,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Transition profile resolved source='{current.NextActivityTransitionProfileSource}' resolvedMode='{transitionResolution.Mode}' resolvedFadeProfileSource='{transitionResolution.ResolvedFadeProfileSource}' resolvedLoadingProfileSource='{transitionResolution.ResolvedLoadingProfileSource}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_profile_resolved",
                command.Source,
                command.Reason,
                $"Transition profile resolved source='{current.NextActivityTransitionProfileSource}' resolvedMode='{transitionResolution.Mode}' resolvedFadeProfileSource='{transitionResolution.ResolvedFadeProfileSource}' resolvedLoadingProfileSource='{transitionResolution.ResolvedLoadingProfileSource}'.");
            _pendingTransitionResolution = transitionResolution;
            _pendingTransitionCurtainReveal = transitionResolution.Mode == ActivityTransitionMode.CutWithCurtain;
            _pendingTransitionCurtainClosed = false;
            await ApplyPendingTransitionFadeInBeforeNextSetupIfNeededAsync(current, command, facts, snapshots, currentEntrySequence);
            EmitNominalNextActivitySetup(current, next, command, facts, snapshots, currentEntrySequence);
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                BuildIdentity(current, SessionActivityStage.NextActivitySetupCompleted, currentEntrySequence),
                command,
                facts,
                snapshots,
                0.5f,
                "NextActivitySetupCompleted");
            SessionActivityHandoff handoff = new(
                BuildIdentity(current, _state.CurrentStage, currentEntrySequence),
                nextActivationIdentity,
                next.ActivityId,
                command.Source,
                command.Reason);

            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("Generated handoff is invalid.");
            }

            _state.SetHandoff(handoff);
            _pendingInternalActivityTransition = new PendingInternalActivityTransition(
                current.ActivityId,
                currentEntrySequence,
                next.ActivityId,
                nextEntrySequence,
                handoffPrepared: true,
                continueAccepted: false);
            EmitFact(facts, SessionActivityFactKind.ActivityHandoffPrepared, nextActivationIdentity, command.Source, command.Reason, $"Handoff prepared for '{next.ActivityId}'.", handoff);
            EmitSnapshot(snapshots, "handoff_created", command.Source, command.Reason, $"Handoff prepared for '{next.ActivityId}'.");
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                BuildIdentity(current, _state.CurrentStage, currentEntrySequence),
                command,
                facts,
                snapshots,
                0.6f,
                "ActivityHandoffPrepared");
            if (wrapped)
            {
                _state.IncrementCatalogLoopCount();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCatalogLooped,
                    nextActivationIdentity,
                    command.Source,
                    command.Reason,
                    $"Activity catalog looped from '{current.ActivityId}' to '{next.ActivityId}'. catalogLoopCount='{_state.CatalogLoopCount}'.");
                EmitSnapshot(
                    snapshots,
                    "catalog_looped",
                    command.Source,
                    command.Reason,
                    $"Activity catalog looped from '{current.ActivityId}' to '{next.ActivityId}'. catalogLoopCount='{_state.CatalogLoopCount}'.");
            }
            await ApplyContinuePolicyAfterHandoffPreparedAsync(current, command, facts, snapshots);
        }

        private void FinalizeDeactivationForRouteExit(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            ReleaseActivityGateIfBlocked(command);
            _state.SetExecutionState(ActivityExecutionState.Stopped);
            _state.ClearHandoff();
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingTransitionResolution = default;
            _pendingInternalActivityTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;

            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            SessionActivityIdentity routeExitClosedIdentity = BuildIdentity(current, SessionActivityStage.ClosedForRouteExit, currentEntrySequence);
            _state.SetCurrentIdentity(routeExitClosedIdentity, SessionActivityStage.ClosedForRouteExit);
            _state.MarkCompleted();
            EmitFact(facts, SessionActivityFactKind.ActivityRouteExitCompleted, routeExitClosedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' route-exit closed.");
            EmitSnapshot(snapshots, "activity_route_exit_completed", command.Source, command.Reason, $"'{current.ActivityId}' route-exit closed.");
        }

        private async Task FinalizePendingNavigationTransition(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity deactivationIdentity)
        {
            PendingNavigationTransition pending = _pendingNavigationTransition;
            ClearPendingNavigationTransition();

            SessionActivityDefinition target = pending.Target;
            SessionActivityTransitionResolution transitionResolution = ResolveNextActivityTransitionResolutionOrFail(current, target);
            SessionActivityIdentity targetActivationIdentity = BuildIdentity(target, SessionActivityStage.ActivityActivationStarted, pending.TargetEntrySequence);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionProfileSelected,
                deactivationIdentity,
                command.Source,
                command.Reason,
                $"Transition profile selected source='{current.NextActivityTransitionProfileSource}' mode='{transitionResolution.Mode}' from '{current.ActivityId}' to '{target.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_profile_selected",
                command.Source,
                command.Reason,
                $"Transition profile selected source='{current.NextActivityTransitionProfileSource}' mode='{transitionResolution.Mode}' from '{current.ActivityId}' to '{target.ActivityId}'.");
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionProfileResolved,
                deactivationIdentity,
                command.Source,
                command.Reason,
                $"Transition profile resolved source='{current.NextActivityTransitionProfileSource}' resolvedMode='{transitionResolution.Mode}' resolvedFadeProfileSource='{transitionResolution.ResolvedFadeProfileSource}' resolvedLoadingProfileSource='{transitionResolution.ResolvedLoadingProfileSource}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_profile_resolved",
                command.Source,
                command.Reason,
                $"Transition profile resolved source='{current.NextActivityTransitionProfileSource}' resolvedMode='{transitionResolution.Mode}' resolvedFadeProfileSource='{transitionResolution.ResolvedFadeProfileSource}' resolvedLoadingProfileSource='{transitionResolution.ResolvedLoadingProfileSource}'.");
            _pendingTransitionResolution = transitionResolution;
            _pendingTransitionCurtainReveal = transitionResolution.Mode == ActivityTransitionMode.CutWithCurtain;
            _pendingTransitionCurtainClosed = false;
            await ApplyPendingTransitionFadeInBeforeNextSetupIfNeededAsync(current, command, facts, snapshots, deactivationIdentity.EntrySequence);
            EmitNominalNextActivitySetup(current, target, command, facts, snapshots, deactivationIdentity.EntrySequence);
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                BuildIdentity(current, SessionActivityStage.NextActivitySetupCompleted, deactivationIdentity.EntrySequence),
                command,
                facts,
                snapshots,
                0.5f,
                "NextActivitySetupCompleted");

            SessionActivityHandoff handoff = new(
                deactivationIdentity,
                targetActivationIdentity,
                target.ActivityId,
                command.Source,
                command.Reason);

            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("Generated navigation handoff is invalid.");
            }

            _state.SetHandoff(handoff);
            _pendingInternalActivityTransition = new PendingInternalActivityTransition(
                current.ActivityId,
                deactivationIdentity.EntrySequence,
                target.ActivityId,
                pending.TargetEntrySequence,
                handoffPrepared: true,
                continueAccepted: false);
            EmitFact(facts, SessionActivityFactKind.ActivityHandoffPrepared, targetActivationIdentity, command.Source, command.Reason, $"Handoff prepared for '{target.ActivityId}'.", handoff);
            EmitSnapshot(snapshots, "handoff_created", command.Source, command.Reason, $"Handoff prepared for '{target.ActivityId}'.");
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                BuildIdentity(current, _state.CurrentStage, deactivationIdentity.EntrySequence),
                command,
                facts,
                snapshots,
                0.6f,
                "ActivityHandoffPrepared");

            if (pending.Wrapped)
            {
                _state.IncrementCatalogLoopCount();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCatalogLooped,
                    targetActivationIdentity,
                    command.Source,
                    command.Reason,
                    $"Activity catalog looped from '{current.ActivityId}' to '{target.ActivityId}'. catalogLoopCount='{_state.CatalogLoopCount}'.");
                EmitSnapshot(
                    snapshots,
                    "catalog_looped",
                    command.Source,
                    command.Reason,
                    $"Activity catalog looped from '{current.ActivityId}' to '{target.ActivityId}'. catalogLoopCount='{_state.CatalogLoopCount}'.");
            }

            await ApplyContinuePolicyAfterHandoffPreparedAsync(current, command, facts, snapshots);
        }

        private async Task FinalizePendingRestartTransition(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            PendingRestartTransition restart = _pendingRestartTransition;
            if (!restart.IsValid)
            {
                return;
            }

            if (!string.Equals(restart.Activity.ActivityId, current.ActivityId, StringComparison.Ordinal) ||
                restart.FromEntrySequence != currentEntrySequence)
            {
                return;
            }

            _pendingRestartTransition = default;
            ReleaseActivityGateIfBlocked(command);
            _state.SetExecutionState(ActivityExecutionState.Stopped);

            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            SessionActivityIdentity restartSetupIdentity = BuildIdentity(restart.Activity, SessionActivityStage.ActivitySetupStarted, restart.NextEntrySequence);
            EmitFact(facts, SessionActivityFactKind.ActivityRestartSetupStarted, restartSetupIdentity, command.Source, command.Reason, $"Restart setup started for '{restart.Activity.ActivityId}' at entrySequence='{restart.NextEntrySequence}'.");
            EmitSnapshot(snapshots, "activity_restart_setup_started", command.Source, command.Reason, $"Restart setup started for '{restart.Activity.ActivityId}' at entrySequence='{restart.NextEntrySequence}'.");

            _state.SetCurrentDefinition(restart.Activity);
            _pendingRestartCompletionActivityId = restart.Activity.ActivityId;
            _pendingRestartCompletionEntrySequence = restart.NextEntrySequence;
            EnterActivity(restart.Activity, command, facts, snapshots, restart.NextEntrySequence);

            await Task.CompletedTask;
        }

        private async Task EmitContinueAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStageForContinue(command, facts))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentHandoff.ToIdentity, "continue_to_next_activity"))
            {
                return;
            }

            SessionActivityHandoff handoff = _state.CurrentHandoff;
            EmitFact(facts, SessionActivityFactKind.ContinueAccepted, _state.CurrentIdentity, command.Source, command.Reason, $"Continue accepted to '{handoff.NextActivityId}'.", handoff);
            EmitSnapshot(snapshots, "continue_accepted", command.Source, command.Reason, $"Continue accepted to '{handoff.NextActivityId}'.");
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                _state.CurrentIdentity,
                command,
                facts,
                snapshots,
                0.7f,
                "ContinueAccepted");

            SessionActivityDefinition next = ResolveActivityByIdOrFail(handoff.NextActivityId);
            int nextEntrySequence = handoff.ToIdentity.EntrySequence;
            MarkPendingInternalTransitionContinueAccepted(handoff);
            _state.ClearHandoff();
            _state.SetCurrentDefinition(next);
            await ApplyPendingTransitionBeforeNextEntryIfNeededAsync(_state.CurrentIdentity, command.Source, command.Reason);
            EnterActivity(next, command, facts, snapshots, nextEntrySequence);
            await ApplyPendingTransitionRevealIfNeededAsync(next, command, facts, snapshots);
        }

        private async Task ApplyContinuePolicyAfterHandoffPreparedAsync(
            SessionActivityDefinition current,
            SessionActivityCommand triggerCommand,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!_state.CurrentHandoff.IsValid)
            {
                return;
            }

            ActivityTransitionContinuePolicy continuePolicy = current.NextActivityTransitionContinuePolicy;
            if (continuePolicy == ActivityTransitionContinuePolicy.Unknown)
            {
                throw new InvalidOperationException($"Activity '{current.ActivityId}' requires explicit nextActivityTransitionContinuePolicy when a next activity exists.");
            }

            if (continuePolicy == ActivityTransitionContinuePolicy.ManualContinue)
            {
                EmitSnapshot(
                    snapshots,
                    "activity_transition_manual_continue_pending",
                    triggerCommand.Source,
                    triggerCommand.Reason,
                    $"Manual continue is pending for '{_state.CurrentHandoff.NextActivityId}'.");
                return;
            }

            SessionActivityCommand autoContinueCommand = new(
                SessionActivityCommandKind.ContinueToNextActivity,
                _state.CurrentHandoff.ToIdentity,
                "SessionActivityPipeline/Policy/AutoContinue",
                "activity_transition_policy_auto_continue");
            await EmitContinueAsync(autoContinueCommand, facts, snapshots);
        }

        private void EmitNavigation(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityRunning, "navigation"))
            {
                return;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            if (!current.IsValid)
            {
                throw new InvalidOperationException("Current activity definition is invalid.");
            }

            int nextEntrySequence = ResolveNextEntrySequence();

            if (!TryResolveNavigationTarget(command, current, out SessionActivityDefinition target, out bool wrapped, out string rejectionReason))
            {
                EmitRejected(
                    command,
                    facts,
                    rejectionReason,
                    ResolveNavigationRejectionMessage(command.Kind, current, rejectionReason),
                    _state.CurrentIdentity,
                    true);
                return;
            }

            if (command.Kind != SessionActivityCommandKind.RestartCurrentActivity &&
                string.Equals(target.ActivityId, current.ActivityId, StringComparison.OrdinalIgnoreCase))
            {
                EmitRejected(
                    command,
                    facts,
                    "already_on_activity",
                    $"Navigation target '{target.ActivityId}' is already active.",
                    _state.CurrentIdentity,
                    true);
                return;
            }

            StartNavigationExitThroughDeactivation(command, current, target, wrapped, nextEntrySequence, facts, snapshots);
        }

        private void StartNavigationExitThroughDeactivation(
            SessionActivityCommand command,
            SessionActivityDefinition current,
            SessionActivityDefinition target,
            bool wrapped,
            int targetEntrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            int currentEntrySequence = _state.CurrentEntrySequence;
            _pendingNavigationTransition = new PendingNavigationTransition(target, wrapped, targetEntrySequence);

            SessionActivityIdentity navigationRequestedIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompletionRequested, currentEntrySequence);
            _state.SetCurrentIdentity(navigationRequestedIdentity, SessionActivityStage.ActivityCompletionRequested);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityNavigationExitRequested,
                navigationRequestedIdentity,
                command.Source,
                command.Reason,
                $"Navigation exit requested from '{current.ActivityId}' to '{target.ActivityId}'. commandKind='{command.Kind}'.");
            EmitSnapshot(
                snapshots,
                "activity_navigation_exit_requested",
                command.Source,
                command.Reason,
                $"Navigation exit requested from '{current.ActivityId}' to '{target.ActivityId}'. commandKind='{command.Kind}'.");

            SessionActivityIdentity completingIdentity = BuildIdentity(current, SessionActivityStage.ActivityCompleting, currentEntrySequence);
            _state.SetCurrentIdentity(completingIdentity, SessionActivityStage.ActivityCompleting);
            EmitFact(facts, SessionActivityFactKind.ActivityCompleting, completingIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completing.");
            EmitSnapshot(snapshots, "activity_completing", command.Source, command.Reason, $"'{current.ActivityId}' completing.");
            EmitPlayerActorParticipationExitIfNeeded(current, command, facts, snapshots, currentEntrySequence);

            SessionActivityIdentity deactivationWindowIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowStarted, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationWindowIdentity, SessionActivityStage.DeactivationWindowStarted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowStarted, deactivationWindowIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");
            EmitSnapshot(snapshots, "deactivation_window_started", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window started.");

            EnsureSupportedDeactivationWindowOrFail(current);

            if (current.DeactivationWindowMode == ActivityWindowMode.None)
            {
                SessionActivityIdentity deactivationWindowSkippedIdentity = BuildIdentity(current, SessionActivityStage.DeactivationWindowSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(deactivationWindowSkippedIdentity, SessionActivityStage.DeactivationWindowSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.DeactivationWindowSkippedNoContent, deactivationWindowSkippedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                EmitSnapshot(snapshots, "deactivation_window_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' deactivation window skipped as no-content.");
                _ = FinalizeDeactivationAndContinuation(current, command, facts, snapshots, currentEntrySequence);
                return;
            }

            ExecuteDeactivationWindowAdditiveSceneLoad(current, command, facts, snapshots, currentEntrySequence);
        }

        private void EmitPlayerActorParticipationExitIfNeeded(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            if (!current.RequiresPlayerActor)
            {
                return;
            }

            SessionActivityIdentity stageStartedIdentity = BuildIdentity(current, SessionActivityStage.PlayerActorParticipationExitStageStarted, currentEntrySequence);
            _state.SetCurrentIdentity(stageStartedIdentity, SessionActivityStage.PlayerActorParticipationExitStageStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageStarted,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' player actor participation exit stage started.");
            EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_started",
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' player actor participation exit stage started.");

            IReadOnlyList<PlayerActorIdentityRecord> actors = _activityPlayerActorRegistry.GetActiveActorIdentitiesOrFail(stageStartedIdentity);
            if (actors.Count > 0)
            {
                PlayerActorParticipationExitCommand exitCommand = new(stageStartedIdentity, actors, command.Source, command.Reason);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorParticipationExitCommandIssued,
                    stageStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actor participation exit command issued. actors='{actors.Count}'.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_participation_exit_command_issued",
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actor participation exit command issued. actors='{actors.Count}'.");

                IReadOnlyList<PlayerActorParticipationExitRecord> exitRecords = _playerActorParticipationAdapter.Execute(
                    exitCommand,
                    stageStartedIdentity,
                    _activityPlayerActorRegistry);

                for (int index = 0; index < exitRecords.Count; index++)
                {
                    PlayerActorParticipationExitRecord record = exitRecords[index];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"PlayerActorParticipationExitRecord at index '{index}' is invalid for activity '{current.ActivityId}'.");
                    }
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorParticipationExited,
                    stageStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actor participation exited. actors='{exitRecords.Count}'.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_participation_exited",
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actor participation exited. actors='{exitRecords.Count}'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorRetainedForRoute,
                    stageStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actors retained for route. actors='{exitRecords.Count}'.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_retained_for_route",
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' player actors retained for route. actors='{exitRecords.Count}'.");
            }

            SessionActivityIdentity stageCompletedIdentity = BuildIdentity(current, SessionActivityStage.PlayerActorParticipationExitStageCompleted, currentEntrySequence);
            _state.SetCurrentIdentity(stageCompletedIdentity, SessionActivityStage.PlayerActorParticipationExitStageCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageCompleted,
                stageCompletedIdentity,
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' player actor participation exit stage completed.");
            EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_completed",
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' player actor participation exit stage completed.");
        }

        private void EnterActivity(SessionActivityDefinition definition, SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots, int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            SessionActivityIdentity scopedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _lastPlayerSelectionSnapshot = RebindPlayerSelectionSnapshotIdentity(_lastPlayerSelectionSnapshot, scopedIdentity, command.Source, command.Reason);

            EmitNominalActivitySetup(definition, command, facts, snapshots, entrySequence);

            SessionActivityIdentity activationStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityActivationStarted, entrySequence);
            _state.SetCurrentIdentity(activationStartedIdentity, SessionActivityStage.ActivityActivationStarted);
            EmitFact(facts, SessionActivityFactKind.ActivityActivationStarted, activationStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation started.");
            EmitSnapshot(snapshots, "activity_activation_started", command.Source, command.Reason, $"'{definition.ActivityId}' activation started.");

            SessionActivityIdentity activationWindowStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowStarted, entrySequence);
            _state.SetCurrentIdentity(activationWindowStartedIdentity, SessionActivityStage.ActivationWindowStarted);
            EmitFact(facts, SessionActivityFactKind.ActivationWindowStarted, activationWindowStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation window started.");
            EmitSnapshot(snapshots, "activation_window_started", command.Source, command.Reason, $"'{definition.ActivityId}' activation window started.");

            EnsureSupportedActivationWindowOrFail(definition);

            if (definition.ActivationWindowMode == ActivityWindowMode.None)
            {
                SessionActivityIdentity activationWindowSkippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowSkippedNoContent, entrySequence);
                _state.SetCurrentIdentity(activationWindowSkippedIdentity, SessionActivityStage.ActivationWindowSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.ActivationWindowSkippedNoContent, activationWindowSkippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation window skipped as no-content.");
                EmitSnapshot(snapshots, "activation_window_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' activation window skipped as no-content.");
                EnterRunning(definition, command, facts, snapshots, entrySequence);
            }
            else
            {
                ExecuteActivationWindowAdditiveScene(definition, command, facts, snapshots, entrySequence);
            }
        }

        private void EmitNominalActivitySetup(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity setupStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(setupStartedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(facts, SessionActivityFactKind.ActivitySetupStarted, setupStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity setup started.");
            EmitSnapshot(snapshots, "activity_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' activity setup started.");
            ObserveActivitySceneContractOrSkip(definition, command, facts, snapshots, entrySequence);

            if (definition.RequiresPlayerActor)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorSetupStarted,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor setup started.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_setup_started",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor setup started.");

                if (!_lastPlayerSelectionSnapshot.IsValid || !_lastPlayerSelectionSnapshot.Identity.Equals(setupStartedIdentity))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionActivityPipeline][PlayerActorSetup] Missing valid PlayerSelectionSnapshot for activity setup activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                if (_lastPlayerSelectionSnapshot.SelectionSource == PlayerSelectionSnapshotSource.MvpDefaultFromPlayerPreparation &&
                    _lastPlayerSelectionSnapshot.Entries.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionActivityPipeline][PlayerActorSetup] MVP player selection bridge produced empty snapshot for player-required activity activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorSelectionSnapshotValidated,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player selection snapshot validated selectedPlayers='{_lastPlayerSelectionSnapshot.Entries.Count}' ignoredOptionalPlayers='{_lastPlayerSelectionSnapshot.IgnoredOptionalPlayersCount}' playerSelectionSource='{FormatPlayerSelectionSource(_lastPlayerSelectionSnapshot.SelectionSource)}'.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_selection_snapshot_validated",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player selection snapshot validated selectedPlayers='{_lastPlayerSelectionSnapshot.Entries.Count}' ignoredOptionalPlayers='{_lastPlayerSelectionSnapshot.IgnoredOptionalPlayersCount}' playerSelectionSource='{FormatPlayerSelectionSource(_lastPlayerSelectionSnapshot.SelectionSource)}'.");

                PlayerActorSetupResult setupResult = PlayerActorSetupStage.Execute(
                    setupStartedIdentity,
                    _lastPlayerSelectionSnapshot,
                    definition.PlayerSetDefinition,
                    _playerActorMaterializationAdapter,
                    _playerActorParticipationAdapter,
                    _playerActorResetAdapter,
                    _activityPlayerActorRegistry,
                    command.Source,
                    command.Reason);

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorEntryPlanResolved,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor entry plan resolved entries='{setupResult.EntryPlans.Count}'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorResetPlanResolved,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor reset plan resolved entries='{setupResult.ResetPlans.Count}' groups='Placement,ActivityParticipation,MovementTransient'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorActivityParticipationPlanResolved,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor activity participation plan resolved entries='{setupResult.ActivityParticipationPlans.Count}'.");
                if (setupResult.HasRetainedReentry)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorRetainedForRouteFound,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' retained player actors found entries='{setupResult.RetainedActors.Count}'.");
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorParticipationEnterCommandIssued,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actor participation enter command issued entries='{setupResult.RetainedActors.Count}'.");
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorParticipationEntered,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actor participation entered entries='{setupResult.RetainedActors.Count}'.");
                }

                if (setupResult.HasMaterialization)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorMaterializationCommandIssued,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actor materialization command issued entries='{setupResult.Records.Count}'.");
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorMaterialized,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actor materialization executed entries='{setupResult.Records.Count}'.");
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorResetCommandIssued,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor reset command issued entries='{setupResult.ResetPlans.Count}' groups='Placement,ActivityParticipation,MovementTransient'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorResetApplied,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor reset applied entries='{setupResult.ResetAppliedRecords.Count}' appliedGroups='{setupResult.TotalResetAppliedGroups}' appliedGroupNames='{setupResult.ResetAppliedGroupsToken}' skippedGroups='{setupResult.TotalResetSkippedGroups}' skippedGroupNames='{setupResult.ResetSkippedGroupsToken}' skippedGroupReasons='{setupResult.ResetSkippedGroupReasonsToken}'.");

                if (setupResult.IsRetainedForActivityReady)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorReadyRetainedForActivity,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actors ready stage='RetainedForActivity' entries='{setupResult.RetainedActors.Count}'.");
                }
                else if (setupResult.IsMaterializedOnlyReady)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerActorReadyMaterializedOnly,
                        setupStartedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actors ready stage='MaterializedOnly' entries='{setupResult.Records.Count}'.");
                }
                else
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionActivityPipeline][PlayerActorSetup] PlayerActorReadyFact missing valid readiness activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorSetupStageCompleted,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor setup stage completed readyStage='{setupResult.ReadyStage}'.");
                if (setupResult.IsRetainedForActivityReady)
                {
                    EmitSnapshot(
                        snapshots,
                        "player_actor_ready_retained_for_activity",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actors ready stage='RetainedForActivity' entries='{setupResult.RetainedActors.Count}'.");
                }
                else
                {
                    EmitSnapshot(
                        snapshots,
                        "player_actor_ready_materialized_only",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player actors ready stage='MaterializedOnly' entries='{setupResult.Records.Count}'.");
                }
            }
            else
            {
                SessionActivityIdentity setupSkippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupSkippedNoContent, entrySequence);
                _state.SetCurrentIdentity(setupSkippedIdentity, SessionActivityStage.ActivitySetupSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.ActivitySetupSkippedNoContent, setupSkippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity setup skipped as no-content.");
                EmitSnapshot(snapshots, "activity_setup_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' activity setup skipped as no-content.");
            }

            SessionActivityIdentity setupCompletedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupCompleted, entrySequence);
            _state.SetCurrentIdentity(setupCompletedIdentity, SessionActivityStage.ActivitySetupCompleted);
            EmitFact(facts, SessionActivityFactKind.ActivitySetupCompleted, setupCompletedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity setup completed.");
            EmitSnapshot(snapshots, "activity_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' activity setup completed.");
        }

        private void ObserveActivitySceneContractOrSkip(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity setup requires valid loaded active scene for ActivitySceneContract observation.");
            }

            List<ActivitySceneContractAuthoring> contracts = new();
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                contracts.AddRange(roots[index].GetComponentsInChildren<ActivitySceneContractAuthoring>(true));
            }

            if (contracts.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupStarted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySceneContractSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity scene contract skipped as no-content in activeScene='{activeScene.name}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_scene_contract_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity scene contract skipped as no-content in activeScene='{activeScene.name}'.");
                return;
            }

            if (contracts.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity scene contract must have exactly one instance per active scene. activeScene='{activeScene.name}' count='{contracts.Count}'.");
            }

            ActivitySceneContractAuthoring contract = contracts[0];
            if (contract == null)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity scene contract resolution returned null instance.");
            }

            ActivitySceneContractSnapshot contractSnapshot = contract.BuildSnapshotOrThrow();
            if (!contractSnapshot.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity scene contract snapshot is invalid. activeScene='{activeScene.name}' component='{contract.name}'.");
            }

            SessionActivityIdentity observedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(observedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySceneContractObserved,
                observedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract observed sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_observed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract observed sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}'.");

            SessionActivityIdentity validatedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySceneContractValidated,
                validatedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract validated sceneId='{contractSnapshot.ActivitySceneId}'.");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_validated",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract validated sceneId='{contractSnapshot.ActivitySceneId}'.");
        }

        private void EmitNominalNextActivitySetup(
            SessionActivityDefinition current,
            SessionActivityDefinition next,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity setupStartedIdentity = BuildIdentity(current, SessionActivityStage.NextActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(setupStartedIdentity, SessionActivityStage.NextActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.NextActivitySetupStarted,
                setupStartedIdentity,
                command.Source,
                command.Reason,
                $"next activity setup started currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}' transitionMode='{_pendingTransitionResolution.Mode}'.");
            EmitSnapshot(
                snapshots,
                "next_activity_setup_started",
                command.Source,
                command.Reason,
                $"next activity setup started currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}' transitionMode='{_pendingTransitionResolution.Mode}'.");

            SessionActivityIdentity setupSkippedIdentity = BuildIdentity(current, SessionActivityStage.NextActivitySetupSkippedNoContent, entrySequence);
            _state.SetCurrentIdentity(setupSkippedIdentity, SessionActivityStage.NextActivitySetupSkippedNoContent);
            EmitFact(
                facts,
                SessionActivityFactKind.NextActivitySetupSkippedNoContent,
                setupSkippedIdentity,
                command.Source,
                command.Reason,
                $"next activity setup skipped as no-content currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "next_activity_setup_skipped_no_content",
                command.Source,
                command.Reason,
                $"next activity setup skipped as no-content currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}'.");

            SessionActivityIdentity setupCompletedIdentity = BuildIdentity(current, SessionActivityStage.NextActivitySetupCompleted, entrySequence);
            _state.SetCurrentIdentity(setupCompletedIdentity, SessionActivityStage.NextActivitySetupCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.NextActivitySetupCompleted,
                setupCompletedIdentity,
                command.Source,
                command.Reason,
                $"next activity setup completed currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "next_activity_setup_completed",
                command.Source,
                command.Reason,
                $"next activity setup completed currentActivity='{current.ActivityId}' nextActivity='{next.ActivityId}'.");
        }

        private void EnterRunning(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (!definition.HasGameplayContent)
            {
                SessionActivityIdentity skipIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
                _state.SetCurrentIdentity(skipIdentity, SessionActivityStage.ActivityRunning);
                _state.SetExecutionState(ActivityExecutionState.Running);
                EmitFact(facts, SessionActivityFactKind.GameplayContentSkippedNoContent, skipIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                EmitSnapshot(snapshots, "gameplay_content_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                TryEmitRestartCompletedAtRunning(definition, command, facts, snapshots, entrySequence);
                return;
            }

            SessionActivityIdentity runningIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
            _state.SetCurrentIdentity(runningIdentity, SessionActivityStage.ActivityRunning);
            _state.SetExecutionState(ActivityExecutionState.Running);
            EmitFact(facts, SessionActivityFactKind.ActivityRunningEntered, runningIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            EmitSnapshot(snapshots, "activity_running_entered", command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            TryEmitRestartCompletedAtRunning(definition, command, facts, snapshots, entrySequence);
        }

        private void TryEmitRestartCompletedAtRunning(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (string.IsNullOrWhiteSpace(_pendingRestartCompletionActivityId) ||
                _pendingRestartCompletionEntrySequence <= 0)
            {
                return;
            }

            if (!_state.CurrentIdentity.IsValid ||
                _state.CurrentStage != SessionActivityStage.ActivityRunning ||
                !string.Equals(_state.CurrentIdentity.ActivityId, _pendingRestartCompletionActivityId, StringComparison.Ordinal) ||
                _state.CurrentIdentity.EntrySequence != entrySequence)
            {
                return;
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityRestartCompleted,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Restart completed for '{definition.ActivityId}' at entrySequence='{entrySequence}'.");
            EmitSnapshot(
                snapshots,
                "activity_restart_completed",
                command.Source,
                command.Reason,
                $"Restart completed for '{definition.ActivityId}' at entrySequence='{entrySequence}'.");
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;
        }

        private SessionActivityCommandResult ExecutePauseRequested(string source, string reason)
        {
            return ExecutePauseRequested(BuildPauseCommand(SessionActivityCommandKind.PauseRequested, source, reason));
        }

        private SessionActivityCommandResult ExecutePauseRequested(string source, string reason, SessionActivityIdentity identity)
        {
            SessionActivityCommand command = identity.IsValid
                ? new SessionActivityCommand(SessionActivityCommandKind.PauseRequested, identity, source, reason)
                : BuildPauseCommand(SessionActivityCommandKind.PauseRequested, source, reason);

            return ExecutePauseRequested(command);
        }

        private SessionActivityCommandResult ExecuteResumeRequested(string source, string reason)
        {
            return ExecuteResumeRequested(BuildPauseCommand(SessionActivityCommandKind.ResumeRequested, source, reason));
        }

        private SessionActivityCommandResult ExecuteResumeRequested(string source, string reason, SessionActivityIdentity identity)
        {
            SessionActivityCommand command = identity.IsValid
                ? new SessionActivityCommand(SessionActivityCommandKind.ResumeRequested, identity, source, reason)
                : BuildPauseCommand(SessionActivityCommandKind.ResumeRequested, source, reason);

            return ExecuteResumeRequested(command);
        }

        private SessionActivityCommandResult ExecutePauseRequested(SessionActivityCommand command)
        {
            return ExecutePauseOrResume(
                command,
                SessionActivityCommandKind.PauseRequested,
                ActivityExecutionState.Paused,
                ActivityExecutionBlockingCommandKind.BlockActivityExecution,
                "pause_requested",
                "Pause requested.");
        }

        private SessionActivityCommandResult ExecuteResumeRequested(SessionActivityCommand command)
        {
            return ExecutePauseOrResume(
                command,
                SessionActivityCommandKind.ResumeRequested,
                ActivityExecutionState.Running,
                ActivityExecutionBlockingCommandKind.ReleaseActivityExecution,
                "resume_requested",
                "Resume requested.");
        }

        private SessionActivityCommandResult ExecutePauseOrResume(
            SessionActivityCommand command,
            SessionActivityCommandKind expectedKind,
            ActivityExecutionState targetState,
            ActivityExecutionBlockingCommandKind gateCommandKind,
            string snapshotKind,
            string message)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(command.Kind, command.Source, command.Reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(command.Kind, command.Source, command.Reason, "pipeline_not_started");
            }

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                return RejectPauseCommand(
                    command,
                    "unexpected_stage",
                    $"Operation '{expectedKind}' requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.");
            }

            if (!command.Identity.IsValid || !_state.CurrentIdentity.IsValid || command.Identity != _state.CurrentIdentity)
            {
                return RejectPauseCommand(
                    command,
                    "stale_or_foreign_command",
                    $"Command identity '{command.Identity}' does not match the active cycle identity '{_state.CurrentIdentity}'.");
            }

            if (expectedKind == SessionActivityCommandKind.PauseRequested)
            {
                if (_state.CurrentExecutionState == ActivityExecutionState.Paused)
                {
                    return RejectPauseCommand(command, "simulation_already_paused", "Simulation is already paused.");
                }

                if (_state.CurrentExecutionState != ActivityExecutionState.Running)
                {
                    throw new InvalidOperationException("Simulation state is invalid for pause.");
                }
            }
            else
            {
                if (_state.CurrentExecutionState == ActivityExecutionState.Running)
                {
                    return RejectPauseCommand(command, "simulation_not_paused", "Simulation is not paused.");
                }

                if (_state.CurrentExecutionState != ActivityExecutionState.Paused)
                {
                    throw new InvalidOperationException("Simulation state is invalid for resume.");
                }
            }

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (expectedKind == SessionActivityCommandKind.PauseRequested)
            {
                ActivityExecutionBlockingResult gateResult = ApplyActivityGateCommand(gateCommandKind, command);
                if (gateResult.IsRejected)
                {
                    return RejectPauseCommand(
                        command,
                        gateResult.Reason,
                        $"SimulationGate rejected pause command. {gateResult.Snapshot}");
                }

                _state.SetExecutionState(targetState);
                _pauseOverlayAdapter.Show(_state.CurrentIdentity, command.Source, command.Reason);
            }
            else
            {
                _state.SetExecutionState(targetState);
                _pauseOverlayAdapter.Hide(_state.CurrentIdentity, command.Source, command.Reason);

                ActivityExecutionBlockingResult gateResult = ApplyActivityGateCommand(gateCommandKind, command);
                if (gateResult.IsRejected)
                {
                    return RejectPauseCommand(
                        command,
                        gateResult.Reason,
                        $"SimulationGate rejected resume command. {gateResult.Snapshot}");
                }
            }

            EmitPauseResolution(
                command,
                emittedFacts,
                emittedSnapshots,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseResolved
                    : SessionActivityFactKind.ResumeResolved,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_resolved"
                    : "resume_resolved",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause accepted."
                    : "Resume accepted.");

            ApplyActivityInputMode(
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityInputModeKind.PauseOverlay
                    : SessionActivityInputModeKind.ActivityGameplay,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseResolved
                    : SessionActivityFactKind.ResumeResolved,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_resolved"
                    : "resume_resolved",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause accepted."
                    : "Resume accepted.",
                command);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Accepted,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        private SessionActivityCommand BuildPauseCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (_state.CurrentIdentity.IsValid)
            {
                return new SessionActivityCommand(kind, _state.CurrentIdentity, source, reason);
            }

            return BuildNoActiveIdentityCommand(kind, source, reason);
        }

        private SessionActivityCommandResult RejectPauseCommand(
            SessionActivityCommand command,
            string reason,
            string message)
        {
            List<SessionActivityFact> rejectedFacts = new();
            List<SessionActivitySnapshot> rejectedSnapshots = new();
            EmitPauseRejection(
                command,
                rejectedFacts,
                rejectedSnapshots,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseRejected
                    : SessionActivityFactKind.ResumeRejected,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_rejected"
                    : "resume_rejected",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause rejected."
                    : "Resume rejected.",
                reason,
                message);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                reason);
        }

        private void EmitPauseResolution(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityFactKind factKind,
            string snapshotKind,
            string message)
        {
            SessionActivityFact fact = EmitFact(
                facts,
                factKind,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                message);

            EmitSnapshot(snapshots, snapshotKind, command.Source, command.Reason, message);

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' executionState='{_state.CurrentExecutionState}' gateState='{_sessionActivitySimulationGate.State}' identity='{_state.CurrentIdentity}' reason='{command.Reason}' source='{command.Source}' outcomeKind='accepted'.",
                DebugUtility.Colors.Success);

            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"{factKind} fact is invalid.");
            }
        }

        private SessionActivityFact EmitPauseRejection(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityFactKind factKind,
            string snapshotKind,
            string message,
            string reason,
            string detailMessage)
        {
            SessionActivityFact fact = EmitFact(
                facts,
                factKind,
                command.Identity,
                command.Source,
                reason,
                detailMessage);

            EmitSnapshot(snapshots, snapshotKind, command.Source, reason, message);

            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' executionState='{_state.CurrentExecutionState}' gateState='{_sessionActivitySimulationGate.State}' identity='{command.Identity}' reason='{reason}' source='{command.Source}' message='{detailMessage}' outcomeKind='rejected'.");

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' executionState='{_state.CurrentExecutionState}' gateState='{_sessionActivitySimulationGate.State}' identity='{command.Identity}' reason='{reason}' source='{command.Source}' outcomeKind='rejected'.",
                DebugUtility.Colors.Warning);

            return fact;
        }

        private void ApplyActivityInputMode(
            SessionActivityInputModeKind mode,
            SessionActivityFactKind reasonFactKind,
            string snapshotKind,
            string message,
            SessionActivityCommand command)
        {
            SessionActivityInputModeCommand inputModeCommand = new(
                mode,
                _state.CurrentIdentity,
                command.Source,
                reasonFactKind.ToString());

            SessionActivityInputModeObservation observation = _inputModeAdapter.Apply(inputModeCommand);
            if (!observation.IsValid)
            {
                throw new InvalidOperationException("InputModeAdapter returned an invalid observation.");
            }

            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][InputMode] mode='{mode}' reasonFact='{reasonFactKind}' outcomeKind='{observation.Outcome}' stage='{_state.CurrentStage}' entrySequence='{_state.CurrentEntrySequence}' activity='{_state.CurrentDefinition.ActivityId}'");
        }

        private ActivityExecutionBlockingResult ApplyActivityGateCommand(
            ActivityExecutionBlockingCommandKind gateCommandKind,
            SessionActivityCommand command)
        {
            ActivityExecutionBlockingCommand gateCommand = BuildActivityGateCommand(gateCommandKind, command);
            ActivityExecutionBlockingResult gateResult = _sessionActivitySimulationGate.Execute(gateCommand);
            RecordSimulationGateResult(gateResult);

            return gateResult;
        }

        private void ReleaseActivityGateIfBlocked(SessionActivityCommand command)
        {
            if (_state.CurrentExecutionState == ActivityExecutionState.Paused && !_sessionActivitySimulationGate.State.ActivityBlocked)
            {
                throw new InvalidOperationException("Paused activity requires a blocked simulation gate.");
            }

            if (!_sessionActivitySimulationGate.State.ActivityBlocked)
            {
                return;
            }

            SessionActivityIdentity currentIdentity = _state.CurrentIdentity;
            if (!currentIdentity.IsValid)
            {
                throw new InvalidOperationException("Activity gate release requires an active identity.");
            }

            ActivityExecutionBlockingIdentity expectedGateIdentity = BuildActivityGateIdentity(currentIdentity, command.Source, command.Reason);
            if (!_sessionActivitySimulationGate.State.ActivityIdentity.MatchesActivityScope(expectedGateIdentity))
            {
                throw new InvalidOperationException("Activity gate is blocked by a foreign identity.");
            }

            ActivityExecutionBlockingResult gateResult = _sessionActivitySimulationGate.Execute(BuildActivityGateCommand(ActivityExecutionBlockingCommandKind.ReleaseActivityExecution, command));
            RecordSimulationGateResult(gateResult);

            if (gateResult.IsRejected)
            {
                throw new InvalidOperationException($"SimulationGate rejected release command. reason='{gateResult.Reason}'.");
            }
        }

        private ActivityExecutionBlockingCommand BuildActivityGateCommand(
            ActivityExecutionBlockingCommandKind gateCommandKind,
            SessionActivityCommand command)
        {
            return new ActivityExecutionBlockingCommand(
                gateCommandKind,
                BuildActivityGateIdentity(_state.CurrentIdentity, command.Source, command.Reason),
                command.Source,
                command.Reason);
        }

        private static ActivityExecutionBlockingIdentity BuildActivityGateIdentity(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            return new ActivityExecutionBlockingIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                identity.Stage,
                source,
                reason);
        }

        private void RecordSimulationGateResult(ActivityExecutionBlockingResult gateResult)
        {
            if (!gateResult.IsValid)
            {
                throw new InvalidOperationException("SimulationGate result is invalid.");
            }

            if (gateResult.Facts.Count == 0)
            {
                throw new InvalidOperationException("SimulationGate result emitted no facts.");
            }

            SimulationGateFact fact = gateResult.Facts[gateResult.Facts.Count - 1];
            SimulationGateSnapshot snapshot = gateResult.Snapshot;
            _state.AppendTrace($"[OBS][SimulationGate][Pipeline] commandKind='{gateResult.Command.Kind}' factKind='{fact.Kind}' stage='{snapshot.CommandIdentity.Stage}' entrySequence='{snapshot.CommandIdentity.EntrySequence}' activity='{snapshot.CommandIdentity.ActivityId}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}'");
        }

        private bool EnsureExpectedStage(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            SessionActivityStage expectedStage,
            string operation)
        {
            if (_state.CurrentStage == expectedStage)
            {
                return true;
            }

            EmitRejected(command, facts, "unexpected_stage", $"Operation '{operation}' requires stage '{expectedStage}', but current stage is '{_state.CurrentStage}'.", _state.CurrentIdentity, true);
            return false;
        }

        private bool EnsureExpectedStageForContinue(
            SessionActivityCommand command,
            List<SessionActivityFact> facts)
        {
            if (_state.CurrentStage == SessionActivityStage.Deactivation ||
                _state.CurrentStage == SessionActivityStage.NextActivitySetupCompleted)
            {
                return true;
            }

            EmitRejected(
                command,
                facts,
                "unexpected_stage",
                $"Operation 'continue_to_next_activity' requires stage '{SessionActivityStage.Deactivation}' or '{SessionActivityStage.NextActivitySetupCompleted}', but current stage is '{_state.CurrentStage}'.",
                _state.CurrentIdentity,
                true);
            return false;
        }

        private bool EnsureIdentityMatches(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            SessionActivityIdentity expected,
            string operation)
        {
            if (expected.IsValid && command.Identity == expected)
            {
                return true;
            }

            EmitRejected(command, facts, "identity_mismatch", $"Operation '{operation}' received an identity that does not match the active cycle.", _state.CurrentIdentity, true);
            return false;
        }

        private SessionActivityCommandResult ExecuteNavigationCommand(SessionActivityCommandKind kind, string source, string reason, string targetActivityId = null)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(kind, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(kind, source, reason, "pipeline_not_started");
            }

            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            if (kind == SessionActivityCommandKind.GoToActivity && string.IsNullOrWhiteSpace(targetActivityId))
            {
                SessionActivityCommand invalidCommand = BuildNavigationCommand(kind, source, reason, targetActivityId);
                return RejectInvalidGoToActivityCommand(invalidCommand);
            }

            return Execute(BuildNavigationCommand(kind, source, reason, targetActivityId));
        }

        private void ClearPendingNavigationTransition()
        {
            _pendingNavigationTransition = default;
        }

        private void ClearStateForRestartTransition()
        {
            _state.ClearPendingOperation();
            _state.ClearHandoff();
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingTransitionResolution = default;
            _pendingInternalActivityTransition = default;
            _pendingRestartTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;
            ClearPendingNavigationTransition();
        }

        private SessionActivityCommandResult ExecuteSimulationCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(kind, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(kind, source, reason, "pipeline_not_started");
            }

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                return RejectStageForSimulation(kind, source, reason, "unexpected_stage");
            }

            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            if (kind == SessionActivityCommandKind.PauseSimulation && _state.CurrentExecutionState == ActivityExecutionState.Paused)
            {
                SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(command, rejectedFacts, "simulation_already_paused", "Simulation is already paused.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(SessionActivityCommandResultKind.Rejected, command, rejectedFacts, "simulation_already_paused");
            }

            if (kind == SessionActivityCommandKind.ResumeSimulation && _state.CurrentExecutionState != ActivityExecutionState.Paused)
            {
                SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(command, rejectedFacts, "simulation_not_paused", "Simulation is not paused.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(SessionActivityCommandResultKind.Rejected, command, rejectedFacts, "simulation_not_paused");
            }

            return Execute(new SessionActivityCommand(kind, _state.CurrentIdentity, source, reason));
        }

        private SessionActivityFact EmitRejected(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            string reason,
            string message,
            SessionActivityIdentity identityOverride = default,
            bool useIdentityOverride = false)
        {
            SessionActivityIdentity identity = useIdentityOverride ? identityOverride : command.Identity;
            SessionActivityFact fact = EmitFact(
                facts,
                SessionActivityFactKind.CommandRejected,
                identity,
                command.Source,
                reason,
                message);

            _state.AppendTrace($"[OBS][SessionActivityPipeline] command_rejected reason='{reason}' command='{command}' message='{message}'");
            _state.AppendTrace($"[OBS][SessionActivityPipeline] command_rejected_state executionState='{_state.CurrentExecutionState}' stage='{_state.CurrentStage}' entrySequence='{_state.CurrentEntrySequence}' identity='{_state.CurrentIdentity}'");
            if (!fact.IsValid)
            {
                throw new InvalidOperationException("CommandRejected fact is invalid.");
            }

            return fact;
        }

        private SessionActivityCommandResult RejectStartCommand(string source, string reason)
        {
            if (_state.CurrentIdentity.IsValid)
            {
                List<SessionActivityFact> rejectedFacts = new();
                SessionActivityCommand command = new(
                    SessionActivityCommandKind.StartActivity,
                    _state.CurrentIdentity,
                    source,
                    reason);

                EmitRejected(command, rejectedFacts, "pipeline_already_started", "SessionActivityPipeline already started.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    command,
                    rejectedFacts,
                    "pipeline_already_started");
            }

            return RejectWithoutActiveIdentity(SessionActivityCommandKind.StartActivity, source, reason, "pipeline_already_started");
        }

        private SessionActivityCommandResult RejectWithoutActiveIdentity(SessionActivityCommandKind kind, string source, string reason, string rejectionReason)
        {
            SessionActivityCommand command = BuildNoActiveIdentityCommand(kind, source, reason);
            if (!command.IsValid)
            {
                throw new InvalidOperationException("Rejected command cannot be constructed.");
            }

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                Array.Empty<SessionActivityFact>(),
                rejectionReason);
        }

        private SessionActivityCommandResult RejectNoHandoffAvailable(string source, string reason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("No-handoff rejection requires an active identity.");
            }

            SessionActivityCommand command = new(
                SessionActivityCommandKind.ContinueToNextActivity,
                _state.CurrentIdentity,
                source,
                reason);

            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, "no_handoff_available", "No handoff is available to continue.", _state.CurrentIdentity, true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                "no_handoff_available");
        }

        private SessionActivityCommandResult RejectPreparedHandoff(
            SessionActivityEntryHandoff handoff,
            string source,
            string reason,
            string rejectionReason,
            string message)
        {
            SessionActivityDefinition firstDefinition = ResolveFirstActivityOrFail();
            int entrySequence = ResolveNextEntrySequence();
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartActivity,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivityActivationStarted, entrySequence),
                source,
                reason);

            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, rejectionReason, message, command.Identity, true);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] SessionActivityEntryHandoffRejected handoff='{handoff}' source='{source}' reason='{reason}' rejectionReason='{rejectionReason}' message='{message}'");

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                rejectionReason);
        }

        private SessionActivityCommandResult RejectStageForSimulation(SessionActivityCommandKind kind, string source, string reason, string rejectionReason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Simulation-stage rejection requires an active identity.");
            }

            SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(
                command,
                rejectedFacts,
                rejectionReason,
                $"Operation '{kind}' requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.",
                _state.CurrentIdentity,
                true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                rejectionReason);
        }

        private SessionActivityCommandResult RejectTerminalCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Terminal rejection requires an active completed identity.");
            }

            SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, "pipeline_completed", "SessionActivityPipeline already completed.", _state.CurrentIdentity, true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                "pipeline_completed");
        }

        private bool IsTerminalCompleted()
        {
            return _state.HasCompleted || _state.CurrentStage == SessionActivityStage.Completed;
        }

        private SessionActivityCommandResult RejectInvalidGoToActivityCommand(SessionActivityCommand command)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("GoToActivity rejection requires an active identity.");
            }

            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(
                command,
                rejectedFacts,
                "target_activity_id_required",
                "Navigation 'GoToActivity' requires a non-empty target activity id.",
                _state.CurrentIdentity,
                true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                "target_activity_id_required");
        }

        private bool TryRejectStaleOrForeignCommand(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            out SessionActivityCommandResult rejectedResult)
        {
            rejectedResult = default;

            if (_state.HasStarted && !_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            SessionActivityIdentity expectedIdentity = ResolveExpectedCommandIdentity(command.Kind);
            if (expectedIdentity.IsValid && command.Identity == expectedIdentity)
            {
                return false;
            }

            if (_state.CurrentIdentity.IsValid)
            {
                List<SessionActivityFact> rejectedFacts = facts;
                EmitRejected(
                    command,
                    rejectedFacts,
                    "stale_or_foreign_command",
                    $"Command identity '{command.Identity}' does not match the active cycle identity '{_state.CurrentIdentity}'.",
                    _state.CurrentIdentity,
                    true);

                rejectedResult = new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    command,
                    rejectedFacts,
                    "stale_or_foreign_command");
                return true;
            }

            rejectedResult = new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                Array.Empty<SessionActivityFact>(),
                "stale_or_foreign_command");
            return true;
        }

        private SessionActivityIdentity ResolveExpectedCommandIdentity(SessionActivityCommandKind kind)
        {
            if (kind == SessionActivityCommandKind.StartActivity)
            {
                if (_state.HasStarted && _state.CurrentIdentity.IsValid)
                {
                    return _state.CurrentIdentity;
                }

                SessionActivityDefinition firstDefinition = ResolveFirstActivityOrFail();
                return BuildIdentity(firstDefinition, SessionActivityStage.ActivityActivationStarted, 1);
            }

            if (!_state.HasStarted)
            {
                return default;
            }

            if (kind == SessionActivityCommandKind.CompleteCurrentActivity)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.CompleteActivationWindow)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.CompleteDeactivationWindow)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.CloseForRouteExit)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.ContinueToNextActivity)
            {
                if (_state.CurrentHandoff.IsValid)
                {
                    return _state.CurrentHandoff.ToIdentity;
                }

                if (_state.CurrentIdentity.IsValid)
                {
                    return _state.CurrentIdentity;
                }
            }

            if (kind == SessionActivityCommandKind.PauseRequested ||
                kind == SessionActivityCommandKind.ResumeRequested ||
                kind == SessionActivityCommandKind.PauseSimulation ||
                kind == SessionActivityCommandKind.ResumeSimulation)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.GoToNextActivity ||
                kind == SessionActivityCommandKind.GoToPreviousActivity ||
                kind == SessionActivityCommandKind.RestartCurrentActivity ||
                kind == SessionActivityCommandKind.GoToActivity)
            {
                return _state.CurrentIdentity;
            }

            return default;
        }

        private SessionActivityCommand BuildNoActiveIdentityCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            SessionActivityDefinition firstDefinition = ResolveFirstActivityOrFail();
            return new SessionActivityCommand(
                kind,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivityActivationStarted, 1),
                source,
                reason);
        }

        private SessionActivityCommand BuildNavigationCommand(SessionActivityCommandKind kind, string source, string reason, string targetActivityId = null)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Navigation command requires an active pipeline identity.");
            }

            return new SessionActivityCommand(
                kind,
                _state.CurrentIdentity,
                source,
                reason,
                targetActivityId);
        }

        private SessionActivityDefinition ResolveActivityByOrdinalOrFail(int ordinal)
        {
            if (_catalog.TryGetByOrdinal(ordinal, out SessionActivityDefinition definition) && definition.IsValid)
            {
                return definition;
            }

            throw new InvalidOperationException($"Missing required activity ordinal '{ordinal}'.");
        }

        private SessionActivityDefinition ResolveActivityByIdOrFail(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new InvalidOperationException("activityId is required.");
            }

            for (int index = 0; index < _catalog.Definitions.Count; index++)
            {
                SessionActivityDefinition candidate = _catalog.Definitions[index];
                if (string.Equals(candidate.ActivityId, activityId, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"Missing required activity '{activityId}'.");
        }

        private bool TryResolveNavigationTarget(
            SessionActivityCommand command,
            SessionActivityDefinition current,
            out SessionActivityDefinition target,
            out bool wrapped,
            out string rejectionReason)
        {
            SessionActivityCommandKind kind = command.Kind;
            target = default;
            wrapped = false;
            rejectionReason = string.Empty;

            switch (kind)
            {
                case SessionActivityCommandKind.GoToNextActivity:
                    if (_catalog.TryGetNextOrdinal(current, out target))
                    {
                        return true;
                    }

                    EnsureCatalogAdvancePolicyOrFail();
                    if (_catalog.AdvanceAtEndMode == ActivityCatalogAdvanceAtEndMode.StopAtEnd)
                    {
                        rejectionReason = "no_next_activity";
                        return false;
                    }

                    if (_catalog.TryGetNext(current, out target, out wrapped) && target.IsValid)
                    {
                        return true;
                    }

                    rejectionReason = "activity_not_found";
                    return false;

                case SessionActivityCommandKind.GoToPreviousActivity:
                    if (current.ActivityOrdinal <= 1)
                    {
                        rejectionReason = "no_previous_activity";
                        return false;
                    }

                    return TryResolveActivityByOrdinal(current.ActivityOrdinal - 1, out target, out rejectionReason);

                case SessionActivityCommandKind.RestartCurrentActivity:
                    target = current;
                    return true;

                case SessionActivityCommandKind.GoToActivity:
                    if (string.IsNullOrWhiteSpace(command.TargetActivityId))
                    {
                        rejectionReason = "target_activity_id_required";
                        return false;
                    }

                    if (!TryResolveActivityById(command.TargetActivityId, out target, out rejectionReason))
                    {
                        return false;
                    }

                    if (string.Equals(current.ActivityId, target.ActivityId, StringComparison.OrdinalIgnoreCase))
                    {
                        rejectionReason = "already_on_activity";
                        return false;
                    }

                    return true;

                default:
                    throw new InvalidOperationException($"Unsupported navigation command kind '{kind}'.");
            }
        }

        private SessionActivityDefinition ResolveFirstActivityOrFail()
        {
            if (_catalog.TryGetFirst(out SessionActivityDefinition definition) && definition.IsValid)
            {
                return definition;
            }

            throw new InvalidOperationException("SessionActivityCatalog requires a valid first activity.");
        }

        private bool TryResolveActivityByOrdinal(int ordinal, out SessionActivityDefinition definition, out string rejectionReason)
        {
            if (_catalog.TryGetByOrdinal(ordinal, out definition) && definition.IsValid)
            {
                rejectionReason = string.Empty;
                return true;
            }

            definition = default;
            rejectionReason = "activity_not_found";
            return false;
        }

        private bool TryResolveActivityById(string activityId, out SessionActivityDefinition definition, out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                definition = default;
                rejectionReason = "target_activity_id_required";
                return false;
            }

            for (int index = 0; index < _catalog.Definitions.Count; index++)
            {
                SessionActivityDefinition candidate = _catalog.Definitions[index];
                if (string.Equals(candidate.ActivityId, activityId, StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    rejectionReason = string.Empty;
                    return true;
                }
            }

            definition = default;
            rejectionReason = "target_activity_not_found";
            return false;
        }

        private static string ResolveNavigationRejectionMessage(
            SessionActivityCommandKind kind,
            SessionActivityDefinition current,
            string rejectionReason)
        {
            return rejectionReason switch
            {
                "no_previous_activity" => $"Navigation '{kind}' has no previous activity before '{current.ActivityId}'.",
                "no_next_activity" => $"Navigation '{kind}' has no next activity after '{current.ActivityId}'.",
                "target_activity_id_required" => $"Navigation '{kind}' requires a non-empty target activity id.",
                "target_activity_not_found" => $"Navigation '{kind}' target activity id was not found in the runtime catalog.",
                "activity_not_found" => $"Navigation '{kind}' target activity was not found.",
                _ => $"Navigation '{kind}' cannot proceed from activity '{current.ActivityId}'."
            };
        }

        private SessionActivityIdentity BuildIdentity(SessionActivityDefinition definition, SessionActivityStage stage, int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            return new SessionActivityIdentity(
                PipelineId,
                _sessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                stage,
                "SessionActivityPipeline");
        }

        private static void EnsureSupportedActivationWindowOrFail(SessionActivityDefinition definition)
        {
            if (definition.ActivationWindowMode == ActivityWindowMode.None ||
                definition.ActivationWindowMode == ActivityWindowMode.AdditiveScene)
            {
                return;
            }

            throw new NotSupportedException($"Activity '{definition.ActivityId}' activationWindowMode '{definition.ActivationWindowMode}' is unsupported in Base 1.1 sandbox. Supported modes: '{ActivityWindowMode.None}', '{ActivityWindowMode.AdditiveScene}'.");
        }

        private static void EnsureSupportedDeactivationWindowOrFail(SessionActivityDefinition definition)
        {
            if (definition.DeactivationWindowMode == ActivityWindowMode.None ||
                definition.DeactivationWindowMode == ActivityWindowMode.AdditiveScene)
            {
                return;
            }

            throw new NotSupportedException($"Activity '{definition.ActivityId}' deactivationWindowMode '{definition.DeactivationWindowMode}' is unsupported in Base 1.1 sandbox. Supported modes: '{ActivityWindowMode.None}', '{ActivityWindowMode.AdditiveScene}'.");
        }

        private SessionActivityTransitionResolution ResolveNextActivityTransitionResolutionOrFail(
            SessionActivityDefinition current,
            SessionActivityDefinition next)
        {
            ActivityTransitionProfileSource source = current.NextActivityTransitionProfileSource;

            if (source == ActivityTransitionProfileSource.None)
            {
                return new SessionActivityTransitionResolution(
                    ActivityTransitionMode.None,
                    null,
                    null,
                    "None",
                    "None");
            }

            if (source == ActivityTransitionProfileSource.OverrideProfile)
            {
                if (!current.HasNextActivityTransitionProfileOverride)
                {
                    throw new InvalidOperationException(
                        $"Activity '{current.ActivityId}' requires nextActivityTransitionProfileOverride when source='{ActivityTransitionProfileSource.OverrideProfile}'. nextActivityId='{next.ActivityId}'.");
                }

                return ResolveTransitionFromProfileOrFail(
                    current,
                    next,
                    current.NextActivityTransitionProfileOverride,
                    fadeSource: "ActivityOverride",
                    loadingSource: "ActivityOverride",
                    sourceLabel: source.ToString());
            }

            if (source == ActivityTransitionProfileSource.InheritRouteProfile)
            {
                if (!_routeTransitionContext.HasRouteFadeProfile)
                {
                    throw new InvalidOperationException(
                        $"Activity '{current.ActivityId}' requires route transition profile when source='{ActivityTransitionProfileSource.InheritRouteProfile}'. nextActivityId='{next.ActivityId}' routeHasFadeProfile='{_routeTransitionContext.HasRouteFadeProfile}'.");
                }

                return ResolveTransitionFromInheritedRouteOrFail(current, next, source.ToString());
            }

            throw new NotSupportedException($"Activity '{current.ActivityId}' transition profile source '{source}' is unsupported.");
        }

        private SessionActivityTransitionResolution ResolveTransitionFromProfileOrFail(
            SessionActivityDefinition current,
            SessionActivityDefinition next,
            ActivityTransitionProfileAsset profile,
            string fadeSource,
            string loadingSource,
            string sourceLabel)
        {
            if (profile == null)
            {
                throw new InvalidOperationException($"Activity '{current.ActivityId}' has null transition profile for source='{sourceLabel}'.");
            }

            profile.ValidateOrThrow($"SessionActivityPipeline:{current.ActivityId}");

            ActivityTransitionMode mode = profile.TransitionMode;
            if (mode == ActivityTransitionMode.Seamless)
            {
                throw new NotSupportedException(
                    $"Activity '{current.ActivityId}' transition mode '{ActivityTransitionMode.Seamless}' is unsupported in Base 1.1 sandbox. source='{sourceLabel}' nextActivityId='{next.ActivityId}'.");
            }

            SceneTransitionProfile resolvedFadeProfile = profile.FadeProfileOverride;
            RuntimeLoadingProfileAsset resolvedLoadingProfile = profile.LoadingProfileOverride;
            string resolvedFadeSource = resolvedFadeProfile != null ? fadeSource : "None";
            string resolvedLoadingSource = resolvedLoadingProfile != null ? loadingSource : "None";

            if (mode == ActivityTransitionMode.CutWithCurtain && resolvedFadeProfile == null)
            {
                throw new InvalidOperationException(
                    $"Activity '{current.ActivityId}' transition mode '{ActivityTransitionMode.CutWithCurtain}' requires fade profile. source='{sourceLabel}' nextActivityId='{next.ActivityId}'.");
            }

            return new SessionActivityTransitionResolution(
                mode,
                resolvedFadeProfile,
                resolvedLoadingProfile,
                resolvedFadeSource,
                resolvedLoadingSource);
        }

        private SessionActivityTransitionResolution ResolveTransitionFromInheritedRouteOrFail(
            SessionActivityDefinition current,
            SessionActivityDefinition next,
            string sourceLabel)
        {
            SceneTransitionProfile routeFadeProfile = _routeTransitionContext.RouteFadeProfile;
            if (routeFadeProfile == null)
            {
                throw new InvalidOperationException(
                    $"Activity '{current.ActivityId}' source='{sourceLabel}' requires non-null route fade profile. nextActivityId='{next.ActivityId}'.");
            }

            ActivityTransitionMode mode = ActivityTransitionMode.CutWithCurtain;
            RuntimeLoadingProfileAsset routeLoadingProfile = _routeTransitionContext.RouteLoadingProfile;
            return new SessionActivityTransitionResolution(
                mode,
                routeFadeProfile,
                routeLoadingProfile,
                "RouteInherited",
                routeLoadingProfile != null ? "RouteInherited" : "None");
        }

        private async Task ApplyPendingTransitionBeforeNextEntryIfNeededAsync(SessionActivityIdentity identity, string source, string reason)
        {
            if (_pendingTransitionResolution.Mode != ActivityTransitionMode.CutWithCurtain || _pendingTransitionCurtainClosed)
            {
                return;
            }

            await _transitionAdapter.CloseCurtainAsync(identity, _pendingTransitionResolution, source, reason);
            _pendingTransitionCurtainClosed = true;
        }

        private bool TryResolveNextActivityForContinuation(
            SessionActivityDefinition current,
            out SessionActivityDefinition next,
            out bool wrapped)
        {
            next = default;
            wrapped = false;

            if (current.HasNextActivity)
            {
                next = ResolveActivityByIdOrFail(current.NextActivityId);
                return true;
            }

            EnsureCatalogAdvancePolicyOrFail();
            if (_catalog.AdvanceAtEndMode == ActivityCatalogAdvanceAtEndMode.StopAtEnd)
            {
                return false;
            }

            if (_catalog.TryGetNext(current, out next, out wrapped) && next.IsValid)
            {
                return true;
            }

            throw new InvalidOperationException(
                $"Activity '{current.ActivityId}' expected looping continuation, but catalog next activity resolution failed.");
        }

        private void EnsureCatalogAdvancePolicyOrFail()
        {
            if (_catalog.AdvanceAtEndMode != ActivityCatalogAdvanceAtEndMode.StopAtEnd &&
                _catalog.AdvanceAtEndMode != ActivityCatalogAdvanceAtEndMode.LoopToFirst)
            {
                throw new InvalidOperationException(
                    $"SessionActivityCatalog advanceAtEndMode '{_catalog.AdvanceAtEndMode}' is unsupported.");
            }
        }

        private async Task ReportPendingTransitionLoadingProgressIfVisibleAsync(
            SessionActivityIdentity identity,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            float normalizedProgress,
            string stageLabel)
        {
            if (!_pendingTransitionLoadingVisible || !_pendingTransitionResolution.HasLoadingProfile)
            {
                return;
            }

            string message = $"Transition loading progress stage='{stageLabel}' progress='{normalizedProgress:0.###}'.";
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionLoadingProgress,
                identity,
                command.Source,
                command.Reason,
                message);
            EmitSnapshot(
                snapshots,
                "activity_transition_loading_progress",
                command.Source,
                command.Reason,
                message);

            await _transitionLoadingAdapter.ReportProgressAsync(
                identity,
                _pendingTransitionResolution,
                normalizedProgress,
                stageLabel,
                message,
                command.Source,
                command.Reason);
        }

        private async Task ApplyPendingTransitionRevealIfNeededAsync(
            SessionActivityDefinition next,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingTransitionResolution.Mode == ActivityTransitionMode.None)
            {
                TryEmitDryTransitionCompletedAtRevealSafePoint(next, command, facts, snapshots);
                return;
            }

            if (!_pendingTransitionCurtainReveal || _pendingTransitionResolution.Mode != ActivityTransitionMode.CutWithCurtain)
            {
                return;
            }

            bool safePointReached =
                (next.ActivationWindowMode == ActivityWindowMode.None && _state.CurrentStage == SessionActivityStage.ActivityRunning) ||
                (next.ActivationWindowMode == ActivityWindowMode.AdditiveScene && _state.CurrentStage == SessionActivityStage.ActivationWindowReady);

            if (!safePointReached)
            {
                throw new InvalidOperationException(
                    $"Transition reveal-safe point not reached for activity '{next.ActivityId}'. activationWindowMode='{next.ActivationWindowMode}' currentStage='{_state.CurrentStage}'.");
            }

            if (_pendingTransitionLoadingVisible)
            {
                SessionActivityTransitionResolution loadingResolution = _pendingTransitionResolution;
                SessionActivityIdentity loadingIdentity = _state.CurrentIdentity;
                await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                    loadingIdentity,
                    command,
                    facts,
                    snapshots,
                    1f,
                    "RevealSafePoint");

                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityTransitionLoadingCompleted,
                    loadingIdentity,
                    command.Source,
                    command.Reason,
                    "Transition loading completed.");
                EmitSnapshot(
                    snapshots,
                    "activity_transition_loading_completed",
                    command.Source,
                    command.Reason,
                    "Transition loading completed.");
                await _transitionLoadingAdapter.CompleteAsync(loadingIdentity, loadingResolution, command.Source, command.Reason);

                if (loadingResolution.LoadingProfile.HideAfterCompletion)
                {
                    await _transitionLoadingAdapter.HideAsync(loadingIdentity, loadingResolution, command.Source, command.Reason);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityTransitionLoadingHidden,
                        loadingIdentity,
                        command.Source,
                        command.Reason,
                        "Transition loading hidden.");
                    EmitSnapshot(
                        snapshots,
                        "activity_transition_loading_hidden",
                        command.Source,
                        command.Reason,
                        "Transition loading hidden.");
                }
                else
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityTransitionLoadingSkippedNoContent,
                        loadingIdentity,
                        command.Source,
                        command.Reason,
                        "Transition loading hide skipped by profile.");
                    EmitSnapshot(
                        snapshots,
                        "activity_transition_loading_hidden_skipped_no_content",
                        command.Source,
                        command.Reason,
                        "Transition loading hide skipped by profile.");
                }
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionFadeOutStarted,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Transition fade-out started for next activity '{next.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_fade_out_started",
                command.Source,
                command.Reason,
                $"Transition fade-out started for next activity '{next.ActivityId}'.");

            await _transitionAdapter.OpenCurtainAsync(_state.CurrentIdentity, _pendingTransitionResolution, command.Source, command.Reason);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionFadeOutCompleted,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Transition fade-out completed for next activity '{next.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_fade_out_completed",
                command.Source,
                command.Reason,
                $"Transition fade-out completed for next activity '{next.ActivityId}'.");
            TryEmitActivityTransitionCompleted(next, command, facts, snapshots);
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingTransitionResolution = default;
        }

        private void TryEmitDryTransitionCompletedAtRevealSafePoint(
            SessionActivityDefinition next,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingTransitionResolution.Mode != ActivityTransitionMode.None)
            {
                return;
            }

            bool safePointReached =
                (next.ActivationWindowMode == ActivityWindowMode.None && _state.CurrentStage == SessionActivityStage.ActivityRunning) ||
                (next.ActivationWindowMode == ActivityWindowMode.AdditiveScene && _state.CurrentStage == SessionActivityStage.ActivationWindowReady);
            if (!safePointReached)
            {
                return;
            }

            TryEmitActivityTransitionCompleted(next, command, facts, snapshots);
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingTransitionResolution = default;
        }

        private void MarkPendingInternalTransitionContinueAccepted(SessionActivityHandoff handoff)
        {
            PendingInternalActivityTransition pending = _pendingInternalActivityTransition;
            if (!pending.IsValid)
            {
                return;
            }

            bool matches =
                string.Equals(pending.FromActivityId, handoff.FromIdentity.ActivityId, StringComparison.Ordinal) &&
                pending.FromEntrySequence == handoff.FromIdentity.EntrySequence &&
                string.Equals(pending.ToActivityId, handoff.ToIdentity.ActivityId, StringComparison.Ordinal) &&
                pending.ToEntrySequence == handoff.ToIdentity.EntrySequence;
            if (!matches)
            {
                return;
            }

            _pendingInternalActivityTransition = new PendingInternalActivityTransition(
                pending.FromActivityId,
                pending.FromEntrySequence,
                pending.ToActivityId,
                pending.ToEntrySequence,
                handoffPrepared: true,
                continueAccepted: true);
        }

        private void TryEmitActivityTransitionCompleted(
            SessionActivityDefinition next,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            PendingInternalActivityTransition pending = _pendingInternalActivityTransition;
            if (!pending.IsValid || !pending.ContinueAccepted)
            {
                return;
            }

            if (!string.Equals(pending.ToActivityId, next.ActivityId, StringComparison.Ordinal) ||
                pending.ToEntrySequence != _state.CurrentEntrySequence)
            {
                return;
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionCompleted,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                $"Transition completed from '{pending.FromActivityId}' to '{pending.ToActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_completed",
                command.Source,
                command.Reason,
                $"Transition completed from '{pending.FromActivityId}' to '{pending.ToActivityId}'.");
            _pendingInternalActivityTransition = default;
        }

        private async Task ApplyPendingTransitionFadeInBeforeNextSetupIfNeededAsync(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (_pendingTransitionResolution.Mode != ActivityTransitionMode.CutWithCurtain)
            {
                return;
            }

            SessionActivityIdentity fadeInStartedIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, entrySequence);
            _state.SetCurrentIdentity(fadeInStartedIdentity, SessionActivityStage.Deactivation);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionFadeInStarted,
                fadeInStartedIdentity,
                command.Source,
                command.Reason,
                $"Transition fade-in started currentActivity='{current.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_fade_in_started",
                command.Source,
                command.Reason,
                $"Transition fade-in started currentActivity='{current.ActivityId}'.");

            if (_pendingTransitionResolution.HasLoadingProfile)
            {
                await _transitionLoadingAdapter.StartAsync(fadeInStartedIdentity, _pendingTransitionResolution, command.Source, command.Reason);
                _pendingTransitionLoadingVisible = true;
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityTransitionLoadingStarted,
                    fadeInStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"Transition loading started currentActivity='{current.ActivityId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_transition_loading_started",
                    command.Source,
                    command.Reason,
                    $"Transition loading started currentActivity='{current.ActivityId}'.");
                await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                    fadeInStartedIdentity,
                    command,
                    facts,
                    snapshots,
                    0f,
                    "Started");
            }
            else
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityTransitionLoadingSkippedNoContent,
                    fadeInStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"Transition loading skipped as no-content currentActivity='{current.ActivityId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_transition_loading_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"Transition loading skipped as no-content currentActivity='{current.ActivityId}'.");
            }

            await ApplyPendingTransitionBeforeNextEntryIfNeededAsync(fadeInStartedIdentity, command.Source, command.Reason);

            SessionActivityIdentity fadeInCompletedIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, entrySequence);
            _state.SetCurrentIdentity(fadeInCompletedIdentity, SessionActivityStage.Deactivation);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityTransitionFadeInCompleted,
                fadeInCompletedIdentity,
                command.Source,
                command.Reason,
                $"Transition fade-in completed currentActivity='{current.ActivityId}'.");
            EmitSnapshot(
                snapshots,
                "activity_transition_fade_in_completed",
                command.Source,
                command.Reason,
                $"Transition fade-in completed currentActivity='{current.ActivityId}'.");
            await ReportPendingTransitionLoadingProgressIfVisibleAsync(
                fadeInCompletedIdentity,
                command,
                facts,
                snapshots,
                0.2f,
                "FadeInCompleted");
        }

        private void ExecuteActivationWindowAdditiveScene(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (definition.ActivationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires activationWindowAdditiveSceneKey when activationWindowMode=AdditiveScene.");
            }

            string sceneName = Normalize(definition.ActivationWindowAdditiveSceneKey.SceneName);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires activationWindowAdditiveSceneKey.SceneName when activationWindowMode=AdditiveScene. asset='{definition.ActivationWindowAdditiveSceneKey.name}'.");
            }

            SessionActivityIdentity loadStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowAdditiveSceneLoadStarted, entrySequence);
            SessionActivityIdentity loadingIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowSceneLoading, entrySequence);
            _state.SetCurrentIdentity(loadingIdentity, SessionActivityStage.ActivationWindowSceneLoading);
            _state.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.ActivationWindowAdditiveSceneLoadStarted);
            EmitFact(facts, SessionActivityFactKind.ActivationWindowAdditiveSceneLoadStarted, loadStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation additive scene load started. scene='{sceneName}'.");
            EmitSnapshot(snapshots, "activation_window_additive_scene_load_started", command.Source, command.Reason, $"'{definition.ActivityId}' activation additive scene load started. scene='{sceneName}'.");

            SessionActivityPendingOperation operation = BuildWindowPendingOperation(
                SessionActivityPendingOperationKind.ActivationWindowSceneLoad,
                SessionActivityPendingWindowKind.ActivationWindow,
                definition,
                entrySequence,
                definition.ActivationWindowAdditiveSceneKey,
                command.Source,
                command.Reason);
            _state.SetPendingOperation(operation);
            _pendingOperationRunner.RunWindowOperation(operation, definition.ActivationWindowAdditiveSceneKey, this);
        }

        private void ExecuteActivationWindowAdditiveSceneUnload(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (definition.ActivationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires activationWindowAdditiveSceneKey for additive activation window unload.");
            }

            string sceneName = Normalize(definition.ActivationWindowAdditiveSceneKey.SceneName);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires activationWindowAdditiveSceneKey.SceneName for additive activation window unload. asset='{definition.ActivationWindowAdditiveSceneKey.name}'.");
            }

            SessionActivityIdentity unloadStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowAdditiveSceneUnloadStarted, entrySequence);
            SessionActivityIdentity unloadingIdentity = BuildIdentity(definition, SessionActivityStage.ActivationWindowSceneUnloading, entrySequence);
            _state.SetCurrentIdentity(unloadingIdentity, SessionActivityStage.ActivationWindowSceneUnloading);
            _state.SetCurrentIdentity(unloadStartedIdentity, SessionActivityStage.ActivationWindowAdditiveSceneUnloadStarted);
            EmitFact(facts, SessionActivityFactKind.ActivationWindowAdditiveSceneUnloadStarted, unloadStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation additive scene unload started. scene='{sceneName}'.");
            EmitSnapshot(snapshots, "activation_window_additive_scene_unload_started", command.Source, command.Reason, $"'{definition.ActivityId}' activation additive scene unload started. scene='{sceneName}'.");

            SessionActivityPendingOperation operation = BuildWindowPendingOperation(
                SessionActivityPendingOperationKind.ActivationWindowSceneUnload,
                SessionActivityPendingWindowKind.ActivationWindow,
                definition,
                entrySequence,
                definition.ActivationWindowAdditiveSceneKey,
                command.Source,
                command.Reason);
            _state.SetPendingOperation(operation);
            _pendingOperationRunner.RunWindowOperation(operation, definition.ActivationWindowAdditiveSceneKey, this);
        }

        private void ExecuteDeactivationWindowAdditiveSceneLoad(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (definition.DeactivationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires deactivationWindowAdditiveSceneKey when deactivationWindowMode=AdditiveScene.");
            }

            string sceneName = Normalize(definition.DeactivationWindowAdditiveSceneKey.SceneName);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires deactivationWindowAdditiveSceneKey.SceneName when deactivationWindowMode=AdditiveScene. asset='{definition.DeactivationWindowAdditiveSceneKey.name}'.");
            }

            SessionActivityIdentity loadStartedIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted, entrySequence);
            SessionActivityIdentity loadingIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowSceneLoading, entrySequence);
            _state.SetCurrentIdentity(loadingIdentity, SessionActivityStage.DeactivationWindowSceneLoading);
            _state.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowAdditiveSceneLoadStarted, loadStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' deactivation additive scene load started. scene='{sceneName}'.");
            EmitSnapshot(snapshots, "deactivation_window_additive_scene_load_started", command.Source, command.Reason, $"'{definition.ActivityId}' deactivation additive scene load started. scene='{sceneName}'.");

            SessionActivityPendingOperation operation = BuildWindowPendingOperation(
                SessionActivityPendingOperationKind.DeactivationWindowSceneLoad,
                SessionActivityPendingWindowKind.DeactivationWindow,
                definition,
                entrySequence,
                definition.DeactivationWindowAdditiveSceneKey,
                command.Source,
                command.Reason);
            _state.SetPendingOperation(operation);
            _pendingOperationRunner.RunWindowOperation(operation, definition.DeactivationWindowAdditiveSceneKey, this);
        }

        private void ExecuteDeactivationWindowAdditiveSceneUnload(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (definition.DeactivationWindowAdditiveSceneKey == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires deactivationWindowAdditiveSceneKey for additive deactivation window unload.");
            }

            string sceneName = Normalize(definition.DeactivationWindowAdditiveSceneKey.SceneName);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires deactivationWindowAdditiveSceneKey.SceneName for additive deactivation window unload. asset='{definition.DeactivationWindowAdditiveSceneKey.name}'.");
            }

            SessionActivityIdentity unloadStartedIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted, entrySequence);
            SessionActivityIdentity unloadingIdentity = BuildIdentity(definition, SessionActivityStage.DeactivationWindowSceneUnloading, entrySequence);
            _state.SetCurrentIdentity(unloadingIdentity, SessionActivityStage.DeactivationWindowSceneUnloading);
            _state.SetCurrentIdentity(unloadStartedIdentity, SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted);
            EmitFact(facts, SessionActivityFactKind.DeactivationWindowAdditiveSceneUnloadStarted, unloadStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' deactivation additive scene unload started. scene='{sceneName}'.");
            EmitSnapshot(snapshots, "deactivation_window_additive_scene_unload_started", command.Source, command.Reason, $"'{definition.ActivityId}' deactivation additive scene unload started. scene='{sceneName}'.");

            SessionActivityPendingOperation operation = BuildWindowPendingOperation(
                SessionActivityPendingOperationKind.DeactivationWindowSceneUnload,
                SessionActivityPendingWindowKind.DeactivationWindow,
                definition,
                entrySequence,
                definition.DeactivationWindowAdditiveSceneKey,
                command.Source,
                command.Reason);
            _state.SetPendingOperation(operation);
            _pendingOperationRunner.RunWindowOperation(operation, definition.DeactivationWindowAdditiveSceneKey, this);
        }

        private int ResolveNextEntrySequence()
        {
            return _state.CurrentEntrySequence > 0 ? _state.CurrentEntrySequence + 1 : 1;
        }

        private SessionActivityFact EmitFact(
            List<SessionActivityFact> emittedFacts,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message,
            SessionActivityHandoff handoff = default)
        {
            SessionActivityFact fact = new(kind, identity, source, reason, message, handoff);
            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid fact '{kind}'.");
            }

            emittedFacts.Add(fact);
            _state.AppendFact(fact);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' entrySequence='{fact.Identity.EntrySequence}' activity='{fact.Identity.ActivityId}' executionState='{_state.CurrentExecutionState}'");
            return fact;
        }

        private SessionActivitySnapshot EmitSnapshot(
            List<SessionActivitySnapshot> emittedSnapshots,
            string snapshotKind,
            string source,
            string reason,
            string message)
        {
            SessionActivitySnapshot snapshot = new SessionActivitySnapshot(
                _state.CurrentIdentity,
                _state.CurrentDefinition,
                _state.CurrentHandoff,
                source,
                reason,
                $"{snapshotKind}: {message}");

            if (!snapshot.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid snapshot '{snapshotKind}'.");
            }

            emittedSnapshots.Add(snapshot);
            _state.AppendSnapshot(snapshot);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] snapshot='{snapshotKind}' stage='{snapshot.Identity.Stage}' entrySequence='{snapshot.Identity.EntrySequence}' activity='{snapshot.Definition.ActivityId}'");
            return snapshot;
        }

        // Ponte MVP/default bridge: converte temporariamente PlayerPreparation payload em PlayerSelectionSnapshot.
        // Contrato alvo futuro: snapshot explícito vindo de Menu/CharacterSelection/route request.
        private static PlayerSelectionSnapshot BuildMvpPlayerSelectionSnapshotFromPlayerPreparation(
            SessionActivityEntryHandoff handoff,
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("Cannot build PlayerSelectionSnapshot from invalid handoff.");
            }

            List<PlayerSelectionEntry> entries = new();
            HashSet<string> dedupe = new(StringComparer.Ordinal);
            string rawIds = Normalize(handoff.PlayerPreparation.PlayerIds);
            int requiredPlayersToSelect = handoff.PlayerPreparation.RequiredPlayers;
            int selectedRequiredPlayers = 0;

            if (!string.IsNullOrWhiteSpace(rawIds))
            {
                string[] split = rawIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int index = 0; index < split.Length; index++)
                {
                    string playerId = Normalize(split[index]);
                    if (string.IsNullOrWhiteSpace(playerId))
                    {
                        continue;
                    }

                    if (!dedupe.Add(playerId))
                    {
                        throw new InvalidOperationException($"Duplicate playerId detected in handoff player selection payload. playerId='{playerId}'.");
                    }

                    if (selectedRequiredPlayers < requiredPlayersToSelect)
                    {
                        entries.Add(new PlayerSelectionEntry(playerId, required: true));
                        selectedRequiredPlayers += 1;
                    }
                }
            }

            if (requiredPlayersToSelect > 0 && selectedRequiredPlayers == 0)
            {
                throw new InvalidOperationException("MVP player selection bridge could not select any required player from PlayerPreparation payload.");
            }

            if (selectedRequiredPlayers < requiredPlayersToSelect)
            {
                throw new InvalidOperationException(
                    $"MVP player selection bridge selected fewer required players than expected. selected='{selectedRequiredPlayers}' expected='{requiredPlayersToSelect}'.");
            }

            int ignoredOptionalPlayersCount = handoff.PlayerPreparation.OptionalPlayers;
            return new PlayerSelectionSnapshot(
                identity,
                entries,
                PlayerSelectionSnapshotSource.MvpDefaultFromPlayerPreparation,
                ignoredOptionalPlayersCount,
                source,
                reason);
        }

        private static PlayerSelectionSnapshot RebindPlayerSelectionSnapshotIdentity(
            PlayerSelectionSnapshot snapshot,
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Cannot rebind PlayerSelectionSnapshot with invalid activity identity.");
            }

            IReadOnlyList<PlayerSelectionEntry> entries = snapshot.IsValid
                ? snapshot.Entries
                : Array.Empty<PlayerSelectionEntry>();

            PlayerSelectionSnapshotSource selectionSource = snapshot.IsValid
                ? snapshot.SelectionSource
                : PlayerSelectionSnapshotSource.ExplicitPayload;
            int ignoredOptionalPlayersCount = snapshot.IsValid ? snapshot.IgnoredOptionalPlayersCount : 0;

            return new PlayerSelectionSnapshot(identity, entries, selectionSource, ignoredOptionalPlayersCount, source, reason);
        }

        private static string FormatPlayerSelectionSource(PlayerSelectionSnapshotSource source)
        {
            return source switch
            {
                PlayerSelectionSnapshotSource.ExplicitPayload => "explicit_payload",
                PlayerSelectionSnapshotSource.MvpDefaultFromPlayerPreparation => "mvp_from_player_preparation",
                _ => "unknown",
            };
        }

        private void EnsureStartedOrFail(string operation)
        {
            if (!_state.HasStarted)
            {
                throw new InvalidOperationException($"Operation '{operation}' requires the pipeline to be started.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

    }
}

