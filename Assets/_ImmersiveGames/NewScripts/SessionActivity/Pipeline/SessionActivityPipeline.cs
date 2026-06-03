using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerActivityParticipantRequirementId = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantRequirementId;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;
using PlayerSessionParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantBinding;
using PlayerSessionParticipantId = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantId;
using PlayerSessionParticipantRole = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantRole;
using PlayerSessionParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipationContext;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityPipeline : ISessionActivityEntryHandoffReceiver, ISessionActivityPendingOperationCallback, ISessionActivitySnapshotPayloadProvider, IActivityEntryRuntimeBridge, IActivityEntryObjectSetupRuntimeBridge, IActivityEntryParticipantBindingRuntimeBridge, IActivityEntryActorInventoryRuntimeBridge, IActivityEntryActorPresentationRuntimeBridge, IActivityEntryActorAttributeRuntimeBridge, IActivityEntryActorParticipationRuntimeBridge, IActivityEntryPermissionTargetRuntimeBridge, IActivityEntryMovementBindingRuntimeBridge, IActivityEntryCameraBindingRuntimeBridge, IActivityExitActorTeardownRuntimeBridge
    {
        private const string PipelineId = "SessionActivityPipeline.Base11.Sandbox";
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";
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
        private readonly IActorResetAdapter _actorResetAdapter;
        private readonly IPlayerInputBindingAdapter _playerInputBindingAdapter;
        private readonly IMovementBindingAdapter _movementBindingAdapter;
        private readonly IPlayerMovementControlAdapter _playerMovementControlAdapter;
        private readonly IActorPresentationMaterializationAdapter _actorPresentationMaterializationAdapter;
        private readonly ActivityPlayerActorRegistry _activityPlayerActorRegistry;
        private readonly ActivitySceneActorRegistry _activitySceneActorRegistry;
        private readonly SessionActorRuntimeStore _sessionActorRuntimeStore;
        private readonly ActivityActorExitRuntimeState _activityActorExitRuntimeState = new();
        private readonly IActivityCapabilityPermissionRuntime _permissionRuntime;
        private readonly IActivityEntryPipeline _activityEntryPipeline;
        private readonly string _sessionId;
        private PendingNavigationTransition _pendingNavigationTransition;
        private SessionActivityRouteTransitionContext _routeTransitionContext;
        private SessionActivityTransitionResolution _pendingTransitionResolution;
        private bool _pendingTransitionCurtainReveal;
        private bool _pendingTransitionCurtainClosed;
        private bool _pendingTransitionLoadingVisible;
        private PendingInternalActivityTransition _pendingInternalActivityTransition;
        private PendingRestartTransition _pendingRestartTransition;
        private readonly ActivityContentReleaseRuntimeState _activityContentReleaseRuntimeState = new();
        private readonly ActivityObjectExitRuntimeState _activityObjectExitRuntimeState = new();
        private ActivityContentReleaseContinuationTelemetry _lastActivityContentReleaseContinuationTelemetry;
        private bool _pendingContinuationExitTeardownCompleted;
        private bool _routeExitActorTeardownCompleted;
        private string _pendingRestartCompletionActivityId;
        private int _pendingRestartCompletionEntrySequence;
        private PlayerSessionParticipationContext _lastSessionParticipationContext;
        private IReadOnlyList<SessionActivityActorMaterializationPlanEntry> _lastActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
        private PlayerActivityParticipationContext _lastActivityParticipationContext;
        private SessionActivityRailKind _activeRailKind;
        private IReadOnlyList<PlayerActorIdentityRecord> _movementControlTargetsForCurrentEntry = Array.Empty<PlayerActorIdentityRecord>();
        private bool _movementControlEnableAllowedForCurrentEntry;
        private string _lastMovementDisableEmissionKey;
        private VisualReadinessSignal _lastVisualReadinessSignal;
        private PendingVisualReadinessCompletion _pendingVisualReadinessCompletion;
        private PendingRouteExitTeardownCompletion _pendingRouteExitTeardownCompletion;

        private readonly struct VisualReadinessSignal
        {
            public VisualReadinessSignal(
                SessionActivityIdentity identity,
                string routeOperationId,
                string source,
                string reason)
            {
                Identity = identity;
                RouteOperationId = Normalize(routeOperationId);
                Source = Normalize(source);
                Reason = Normalize(reason);
            }

            public SessionActivityIdentity Identity { get; }
            public string RouteOperationId { get; }
            public string Source { get; }
            public string Reason { get; }

            public bool IsValid =>
                Identity.IsValid &&
                !string.IsNullOrWhiteSpace(RouteOperationId) &&
                !string.IsNullOrWhiteSpace(Source);
        }

        private sealed class PendingVisualReadinessCompletion
        {
            public PendingVisualReadinessCompletion(
                SessionActivityVisualReadinessRequest request,
                TaskCompletionSource<SessionActivityVisualReadinessResult> completion)
            {
                Request = request;
                Completion = completion;
            }

            public SessionActivityVisualReadinessRequest Request { get; }
            public TaskCompletionSource<SessionActivityVisualReadinessResult> Completion { get; }

            public bool IsValid => Request.IsValid && Completion != null;
        }



        private sealed class PendingRouteExitTeardownCompletion
        {
            public PendingRouteExitTeardownCompletion(
                string sessionStateId,
                string source,
                string reason,
                TaskCompletionSource<SessionActivityRouteExitTeardownResult> completion)
            {
                SessionStateId = Normalize(sessionStateId);
                Source = Normalize(source);
                Reason = Normalize(reason);
                Completion = completion;
            }

            public string SessionStateId { get; }
            public string Source { get; }
            public string Reason { get; }
            public TaskCompletionSource<SessionActivityRouteExitTeardownResult> Completion { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(SessionStateId) && Completion != null;
        }

        internal enum ActorPresentationReleaseRail
        {
            Unknown = 0,
            BeforeRematerialization = 1,
            ActivityExit = 2,
            RouteExit = 3
        }

        internal readonly struct ActorPresentationCapabilityState
        {
            public ActorPresentationCapabilityState(
                ActorInstanceId actorInstanceRuntimeId,
                string actorId,
                ActorPresentationEndpoint endpoint,
                ActorPresentationRuntimeHandle runtimeHandle,
                string pipelineIdentity,
                string activityIdentity)
            {
                ActorInstanceRuntimeId = actorInstanceRuntimeId;
                ActorId = Normalize(actorId);
                Endpoint = endpoint;
                RuntimeHandle = runtimeHandle;
                PipelineIdentity = Normalize(pipelineIdentity);
                ActivityIdentity = Normalize(activityIdentity);
            }

            public ActorInstanceId ActorInstanceRuntimeId { get; }
            public string ActorId { get; }
            public ActorPresentationEndpoint Endpoint { get; }
            public ActorPresentationRuntimeHandle RuntimeHandle { get; }
            public string PipelineIdentity { get; }
            public string ActivityIdentity { get; }
            public bool IsValid =>
                ActorInstanceRuntimeId.IsValid &&
                !string.IsNullOrWhiteSpace(ActorId) &&
                Endpoint != null &&
                RuntimeHandle.IsValid &&
                !string.IsNullOrWhiteSpace(PipelineIdentity) &&
                !string.IsNullOrWhiteSpace(ActivityIdentity);
        }


        internal enum ActivityParticipantReadinessStageOutcome
        {
            Unknown = 0,
            Ready = 1,
            SkippedNoRequiredParticipant = 2,
            Failed = 3
        }

        internal readonly struct ActivityParticipantReadinessStageResult
        {
            public ActivityParticipantReadinessStageResult(
                ActivityParticipantReadinessStageOutcome outcome,
                int requiredRequirements,
                int requiredResolvedRequirements,
                int activeActorsCount,
                string reasonCode)
            {
                Outcome = outcome;
                RequiredRequirements = requiredRequirements;
                RequiredResolvedRequirements = requiredResolvedRequirements;
                ActiveActorsCount = activeActorsCount;
                ReasonCode = Normalize(reasonCode);
            }

            public ActivityParticipantReadinessStageOutcome Outcome { get; }
            public int RequiredRequirements { get; }
            public int RequiredResolvedRequirements { get; }
            public int ActiveActorsCount { get; }
            public string ReasonCode { get; }
            public bool IsReady => Outcome == ActivityParticipantReadinessStageOutcome.Ready;
            public bool IsSkipped => Outcome == ActivityParticipantReadinessStageOutcome.SkippedNoRequiredParticipant;
            public bool IsFailed => Outcome == ActivityParticipantReadinessStageOutcome.Failed;
        }


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

        internal readonly struct ActorAttributeCapabilityState
        {
            public ActorAttributeCapabilityState(
                ActorInstanceId actorInstanceRuntimeId,
                string actorId,
                ActorAttributeEndpoint endpoint)
            {
                ActorInstanceRuntimeId = actorInstanceRuntimeId;
                ActorId = Normalize(actorId);
                Endpoint = endpoint;
            }

            public ActorInstanceId ActorInstanceRuntimeId { get; }
            public string ActorId { get; }
            public ActorAttributeEndpoint Endpoint { get; }
            public bool IsValid =>
                ActorInstanceRuntimeId.IsValid &&
                !string.IsNullOrWhiteSpace(ActorId) &&
                Endpoint != null &&
                Endpoint.IsInitialized;
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

        internal sealed class PendingActivityContentReleaseContext
        {
            public PendingActivityContentReleaseContext(
                SessionActivityDefinition definition,
                int entrySequence,
                ActivityContentLoadedSet loadedSet,
                string source,
                string reason)
            {
                Definition = definition;
                EntrySequence = entrySequence;
                LoadedSet = loadedSet;
                Source = Normalize(source);
                Reason = Normalize(reason);
                NextSceneIndex = 0;
            }

            public SessionActivityDefinition Definition { get; }
            public int EntrySequence { get; }
            public ActivityContentLoadedSet LoadedSet { get; }
            public string Source { get; }
            public string Reason { get; }
            public int NextSceneIndex { get; set; }

            public bool IsValid =>
                Definition.IsValid &&
                EntrySequence > 0 &&
                LoadedSet.IsValid &&
                !string.IsNullOrWhiteSpace(Source);
        }

        private readonly struct ActivityContentReleaseContinuationTelemetry
        {
            public ActivityContentReleaseContinuationTelemetry(
                SessionActivityDefinition definition,
                int entrySequence,
                string source,
                string reason,
                string continuationKind,
                string continuationTargetActivityId,
                int continuationTargetEntrySequence,
                bool hasPendingRestartTransition,
                bool hasPendingRouteExit,
                bool hasNextActivity,
                bool routeExitRequested,
                bool deactivationCompleted,
                bool activityCompletionRequested,
                string releaseStatus,
                bool skippedNoContent,
                int loadedSceneCount,
                int releasedSceneCount,
                string previousStage,
                string nextStage)
            {
                Definition = definition;
                EntrySequence = entrySequence;
                Source = Normalize(source);
                Reason = Normalize(reason);
                ContinuationKind = Normalize(continuationKind);
                ContinuationTargetActivityId = Normalize(continuationTargetActivityId);
                ContinuationTargetEntrySequence = continuationTargetEntrySequence;
                HasPendingRestartTransition = hasPendingRestartTransition;
                HasPendingRouteExit = hasPendingRouteExit;
                HasNextActivity = hasNextActivity;
                RouteExitRequested = routeExitRequested;
                DeactivationCompleted = deactivationCompleted;
                ActivityCompletionRequested = activityCompletionRequested;
                ReleaseStatus = Normalize(releaseStatus);
                SkippedNoContent = skippedNoContent;
                LoadedSceneCount = loadedSceneCount;
                ReleasedSceneCount = releasedSceneCount;
                PreviousStage = Normalize(previousStage);
                NextStage = Normalize(nextStage);
            }

            public SessionActivityDefinition Definition { get; }
            public int EntrySequence { get; }
            public string Source { get; }
            public string Reason { get; }
            public string ContinuationKind { get; }
            public string ContinuationTargetActivityId { get; }
            public int ContinuationTargetEntrySequence { get; }
            public bool HasPendingRestartTransition { get; }
            public bool HasPendingRouteExit { get; }
            public bool HasNextActivity { get; }
            public bool RouteExitRequested { get; }
            public bool DeactivationCompleted { get; }
            public bool ActivityCompletionRequested { get; }
            public string ReleaseStatus { get; }
            public bool SkippedNoContent { get; }
            public int LoadedSceneCount { get; }
            public int ReleasedSceneCount { get; }
            public string PreviousStage { get; }
            public string NextStage { get; }

            public bool IsValid =>
                Definition.IsValid &&
                EntrySequence > 0 &&
                !string.IsNullOrWhiteSpace(Source) &&
                !string.IsNullOrWhiteSpace(ContinuationKind);
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
            _permissionRuntime = new ActivityCapabilityPermissionRuntime();
            _playerActorParticipationAdapter = new PlayerActorParticipationAdapter(_permissionRuntime);
            _activityPlayerActorRegistry = new ActivityPlayerActorRegistry();
            _activitySceneActorRegistry = new ActivitySceneActorRegistry();
            _sessionActorRuntimeStore = new SessionActorRuntimeStore();
            _actorResetAdapter = new ActorResetAdapter(
                new PlayerActorResetEndpointResolver(_activityPlayerActorRegistry, _sessionActorRuntimeStore));
            _playerInputBindingAdapter = new PlayerInputBindingAdapter();
            _movementBindingAdapter = new MovementBindingAdapter(_permissionRuntime);
            _playerMovementControlAdapter = new PlayerMovementControlAdapter(_permissionRuntime);
            _actorPresentationMaterializationAdapter = new UnityActorPresentationMaterializationAdapter();
            _activityEntryPipeline = new ActivityEntryPipeline(this, this, this, this, this, this, this, this, this);
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
        public bool AwaitingContinuationAfterActivityContentRelease => _activityContentReleaseRuntimeState.IsAwaitingContinuation;
        public int PendingActivityContentReleaseEntrySequence => _activityContentReleaseRuntimeState.HasPendingReleaseContext
            ? _activityContentReleaseRuntimeState.PendingReleaseContext.EntrySequence
            : 0;
        public string PendingActivityContentReleaseSummary => _activityContentReleaseRuntimeState.HasPendingReleaseContext
            ? $"activityId='{_activityContentReleaseRuntimeState.PendingReleaseContext.Definition.ActivityId}', entrySequence='{_activityContentReleaseRuntimeState.PendingReleaseContext.EntrySequence}', nextSceneIndex='{_activityContentReleaseRuntimeState.PendingReleaseContext.NextSceneIndex}', totalScenes='{_activityContentReleaseRuntimeState.PendingReleaseContext.LoadedSet.Scenes.Count}'"
            : "<none>";
        public string SessionId => _sessionId;
        public SessionActivityRailKind ActiveRailKind => _activeRailKind;
        string IActivityEntryPipelineBoundary.PipelineId => PipelineId;
        string IActivityEntryPipelineBoundary.SessionId => _sessionId;

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

        public SessionActivityCommand BuildResetSessionCommand(string source, string reason)
        {
            EnsureStartedOrFail("ResetSessionCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.ResetSession,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommandResult ResetSession(string source, string reason)
        {
            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.ResetSession,
                    source,
                    reason,
                    "session_reset_requires_started_pipeline");
            }

            return Execute(BuildResetSessionCommand(source, reason));
        }

        public SessionActivitySessionResetResult ResetSessionAfterRouteExit(string sessionStateId, string source, string reason)
        {
            string normalizedSessionStateId = Normalize(sessionStateId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (!string.Equals(normalizedSessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return new SessionActivitySessionResetResult(
                    SessionActivitySessionResetKind.Failed,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    _sessionActorRuntimeStore.Count,
                    _sessionActorRuntimeStore.Count,
                    "session_reset_session_mismatch",
                    $"requestedSessionStateId='{normalizedSessionStateId}' activeSessionStateId='{_sessionId}'");
            }

            if (!_state.HasStarted)
            {
                return new SessionActivitySessionResetResult(
                    SessionActivitySessionResetKind.NotRequired,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    0,
                    0,
                    "session_reset_not_required",
                    "pipeline_not_started");
            }

            if (_state.CurrentPendingOperation.IsValid)
            {
                return new SessionActivitySessionResetResult(
                    SessionActivitySessionResetKind.Failed,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    _sessionActorRuntimeStore.Count,
                    _sessionActorRuntimeStore.Count,
                    "session_reset_blocked_pending_operation",
                    $"pendingOperation='{_state.CurrentPendingOperation.OperationKind}'");
            }

            int beforeCount = _sessionActorRuntimeStore.Count;
            if (_state.HasCompleted && beforeCount == 0)
            {
                return new SessionActivitySessionResetResult(
                    SessionActivitySessionResetKind.NotRequired,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    0,
                    0,
                    "session_reset_not_required",
                    "pipeline_already_completed_and_session_store_empty");
            }

            SessionActivityCommandResult result = ResetSession(normalizedSource, normalizedReason);
            int afterCount = _sessionActorRuntimeStore.Count;
            bool completed = result.Kind == SessionActivityCommandResultKind.Completed && afterCount == 0;
            return new SessionActivitySessionResetResult(
                completed ? SessionActivitySessionResetKind.Completed : SessionActivitySessionResetKind.Failed,
                _sessionId,
                _state.CurrentStage,
                _state.CurrentDefinition.ActivityId,
                beforeCount,
                afterCount,
                completed ? "session_reset_completed" : "session_reset_failed",
                completed ? "session_scoped_actors_released" : $"commandResult='{result.Kind}' remaining='{afterCount}' reason='{result.Reason}'");
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
            ActivityHandoffRuntimeResetStage.Execute(
                new ActivityHandoffRuntimeResetStageCommand(activationIdentity, entrySequence, source, reason),
                _activityEntryPipeline,
                _activityContentReleaseRuntimeState,
                _activityObjectExitRuntimeState,
                _activityActorExitRuntimeState,
                _activityPlayerActorRegistry,
                _activitySceneActorRegistry,
                _sessionActorRuntimeStore);
            _pendingContinuationExitTeardownCompleted = false;
            _routeExitActorTeardownCompleted = false;
            FailPendingVisualReadinessCompletion(
                "pipeline_reset_for_new_handoff",
                "SessionActivity started a new prepared handoff before the previous visual readiness request completed.");
            FailPendingRouteExitTeardownCompletion(
                "pipeline_reset_for_new_handoff",
                "SessionActivity started a new prepared handoff before the previous route-exit teardown request completed.");
            _lastVisualReadinessSignal = default;
            _lastSessionParticipationContext = handoff.SessionParticipationContext;
            _lastActorMaterializationPlanEntries = handoff.ActorMaterializationPlanEntries ?? Array.Empty<SessionActivityActorMaterializationPlanEntry>();
            _lastActivityParticipationContext = null;
            _activeRailKind = SessionActivityRailKind.ActivityEntryRail;
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
                $"[OBS][SessionActivityPipeline][Handoff] SessionActivityEntryHandoffAccepted pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{initialDefinition.ActivityId}' activityOrdinal='{initialDefinition.ActivityOrdinal}' entrySequence='{entrySequence}' source='{source}' reason='{reason}' sessionParticipationContext='{(handoff.HasSessionParticipationContext ? "present" : "absent")}' sessionParticipationRevision='{handoff.SessionParticipationRevision}' sessionSlotReservations='{handoff.SessionParticipationSlotReservationCount}' sessionSelections='{handoff.SessionParticipationSelectionCount}' sessionParticipants='{handoff.SessionParticipationParticipantCount}' actorMaterializationPlanEntries='{handoff.ActorMaterializationPlanEntryCount}'.",
                DebugUtility.Colors.Success);
            EmitFact(emittedFacts, SessionActivityFactKind.PipelineStarted, activationIdentity, source, reason, "SessionActivityPipeline started from prepared handoff.");
            EmitSnapshot(emittedSnapshots, "pipeline_started_from_handoff", source, reason, "Pipeline started from prepared handoff.");

            EnterActivity(initialDefinition, command, emittedFacts, emittedSnapshots, entrySequence);

            SessionActivityCommandResult result = new(
                SessionActivityCommandResultKind.Started,
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

            if (IsTerminalCompleted() && command.Kind != SessionActivityCommandKind.ResetSession)
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
                case SessionActivityCommandKind.ResetSession:
                    EmitSessionReset(command, emittedFacts, emittedSnapshots);
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
                ? SessionActivityCommandResultKind.Started
                : emittedFacts[emittedFacts.Count - 1].Kind switch
                {
                    SessionActivityFactKind.CommandRejected => SessionActivityCommandResultKind.Rejected,
                    SessionActivityFactKind.PipelineCompleted => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.ActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.ActivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.GameplayContentSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.DeactivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.NextActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.ActivityContentLoadSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.SimulationPaused => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.SimulationResumed => SessionActivityCommandResultKind.Completed,
                    _ => SessionActivityCommandResultKind.Started,
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

        private SessionActivityPendingOperation BuildActivityContentPendingOperation(
            ActivityContentLoadPlan plan,
            int entrySequence,
            ActivityContentSceneLoadCommand command)
        {
            return new SessionActivityPendingOperation(
                command.OperationId,
                _state.PipelineId,
                _state.SessionId,
                plan.ActivityId,
                plan.ActivityOrdinal,
                entrySequence,
                SessionActivityPendingWindowKind.None,
                SessionActivityPendingOperationKind.ActivityContentSceneLoad,
                command.SceneKey,
                command.SceneName,
                command.Source,
                command.Reason);
        }

        private bool TryStartActivityContentReleaseForContinuation(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            if (_activityContentReleaseRuntimeState.HasPendingReleaseContext)
            {
                return true;
            }

            SessionActivityIdentity dematerializationStartedIdentity = BuildIdentity(current, SessionActivityStage.ActivityContentReleaseStarted, currentEntrySequence);
            LogPhaseBoundary("SessionActivityDematerializationStarted", dematerializationStartedIdentity, command.Source, command.Reason, detail: "phase='dematerialization'");

            EmitObjectSnapshotCaptureStage(current, command, facts, snapshots, currentEntrySequence);
            EmitObjectReleaseStage(current, command, facts, snapshots, currentEntrySequence);

            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
            _activityContentReleaseRuntimeState.StoreCurrentLoadedSet(loadedSet, current.ActivityId, currentEntrySequence, command.Source, command.Reason);
            if (!loadedSet.HasScenes)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(current, SessionActivityStage.ActivityContentReleaseSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityContentReleaseSkippedNoContent);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentReleaseSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' activity content release skipped as no-content.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_release_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' activity content release skipped as no-content.");

                FinalizeActivityContentReleaseCompleted(
                    current,
                    command,
                    facts,
                    snapshots,
                    currentEntrySequence,
                    loadedSceneCount: 0,
                    releasedSceneCount: 0,
                    skippedNoContent: true,
                    status: "SkippedNoContent");
                return false;
            }

            if (!loadedSet.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity '{current.ActivityId}' has non-empty CurrentActivityContentLoadedSet but it is invalid for release.");
            }

            SessionActivityIdentity releaseStartedIdentity = BuildIdentity(current, SessionActivityStage.ActivityContentReleaseStarted, currentEntrySequence);
            _state.SetCurrentIdentity(releaseStartedIdentity, SessionActivityStage.ActivityContentReleaseStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentReleaseStarted,
                releaseStartedIdentity,
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' activity content release started scenes='{loadedSet.Scenes.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_release_started",
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' activity content release started scenes='{loadedSet.Scenes.Count}'.");

            SessionActivityIdentity retentionPlanIdentity = BuildIdentity(current, SessionActivityStage.ActivityContentRetentionPlanResolved, currentEntrySequence);
            _state.SetCurrentIdentity(retentionPlanIdentity, SessionActivityStage.ActivityContentRetentionPlanResolved);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentRetentionPlanResolved,
                retentionPlanIdentity,
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' activity content retention plan resolved policy='ReleaseByDefault' scenes='{loadedSet.Scenes.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_retention_plan_resolved",
                command.Source,
                command.Reason,
                $"'{current.ActivityId}' activity content retention plan resolved policy='ReleaseByDefault' scenes='{loadedSet.Scenes.Count}'.");

            PendingActivityContentReleaseContext releaseContext = new(
                current,
                currentEntrySequence,
                loadedSet,
                command.Source,
                command.Reason);
            _activityContentReleaseRuntimeState.SetPendingReleaseContext(releaseContext, current.ActivityId, currentEntrySequence, command.Source, command.Reason);
            _activityContentReleaseRuntimeState.SetAwaitingContinuation(true, current.ActivityId, currentEntrySequence, command.Source, command.Reason);

            ExecuteNextActivityContentSceneRelease(_activityContentReleaseRuntimeState.PendingReleaseContext, command, facts, snapshots);
            return true;
        }

        private void EmitObjectReleaseStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectReleaseStage.Execute(
                new ActivityObjectReleaseStageCommand(command.Identity, command, entrySequence),
                definition,
                this,
                _activityObjectExitRuntimeState,
                facts,
                snapshots);
        }

        private void EmitObjectSnapshotCaptureStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectSnapshotCaptureStage.Execute(
                new ActivityObjectSnapshotCaptureStageCommand(
                    command.Identity,
                    command,
                    entrySequence,
                    RouteActivitySnapshotSchemaId),
                definition,
                this,
                _activityObjectExitRuntimeState,
                facts,
                snapshots);
        }

        private void ExecuteNextActivityContentSceneRelease(
            PendingActivityContentReleaseContext context,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (context == null || !context.IsValid)
            {
                throw new InvalidOperationException("Pending activity content release context is invalid.");
            }

            ActivityContentSceneUnloadDispatchStageResult result = ActivityContentSceneUnloadDispatchStage.Execute(
                new ActivityContentSceneUnloadDispatchStageCommand(command),
                this,
                _activityContentReleaseRuntimeState,
                _pendingOperationRunner,
                this,
                facts,
                snapshots);

            if (result.CompletedNoMoreScenes)
            {
                FinalizeActivityContentReleaseCompleted(context, command, facts, snapshots);
            }
        }

        private ActivityContentReleaseContinuationTelemetry FinalizeActivityContentReleaseCompleted(
            PendingActivityContentReleaseContext context,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (context == null || !context.IsValid)
            {
                throw new InvalidOperationException("Pending activity content release context is invalid for finalization.");
            }

            int sceneCount = context.LoadedSet.Scenes?.Count ?? 0;
            return FinalizeActivityContentReleaseCompleted(
                context.Definition,
                command,
                facts,
                snapshots,
                context.EntrySequence,
                loadedSceneCount: sceneCount,
                releasedSceneCount: sceneCount,
                skippedNoContent: false,
                status: "Unloaded");
        }

        private ActivityContentReleaseContinuationTelemetry FinalizeActivityContentReleaseCompleted(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            int loadedSceneCount,
            int releasedSceneCount,
            bool skippedNoContent,
            string status)
        {
            string continuationKind = ResolveActivityContentReleaseContinuationKind(definition, entrySequence);
            ActivityContentReleaseContinuationTelemetry telemetry = BuildActivityContentReleaseContinuationTelemetry(
                definition,
                command,
                entrySequence,
                continuationKind,
                loadedSceneCount,
                releasedSceneCount,
                skippedNoContent,
                status,
                previousStage: nameof(SessionActivityStage.ActivityContentReleaseCompleted),
                nextStage: continuationKind);

            ActivityContentReleaseFinalizationStage.Execute(
                new ActivityContentReleaseFinalizationStageCommand(
                    command,
                    entrySequence,
                    loadedSceneCount,
                    releasedSceneCount,
                    skippedNoContent,
                    completionKind: "Completed",
                    status: status,
                    continuationKind: telemetry.ContinuationKind),
                definition,
                this,
                _activityContentReleaseRuntimeState,
                _activityObjectExitRuntimeState,
                facts,
                snapshots);

            _lastActivityContentReleaseContinuationTelemetry = telemetry;
            EmitActivityContentReleaseContinuationEvent(
                "ActivityContentReleaseContinuationResolved",
                telemetry);
            return telemetry;
        }

        private string ResolveActivityContentReleaseContinuationKind(SessionActivityDefinition definition, int entrySequence)
        {
            if (_activeRailKind == SessionActivityRailKind.ActivityRouteExitRail &&
                definition.IsValid &&
                string.Equals(definition.ActivityId, _state.CurrentDefinition.ActivityId, StringComparison.Ordinal) &&
                entrySequence == _state.CurrentEntrySequence)
            {
                return "RouteExit";
            }

            PendingRestartTransition restart = _pendingRestartTransition;
            if (restart.IsValid &&
                definition.IsValid &&
                string.Equals(restart.Activity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                restart.FromEntrySequence == entrySequence)
            {
                return "RestartCurrentActivity";
            }

            PendingInternalActivityTransition transition = _pendingInternalActivityTransition;
            if (transition.IsValid &&
                definition.IsValid &&
                string.Equals(transition.FromActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                transition.FromEntrySequence == entrySequence)
            {
                return "NextActivity";
            }

            if (definition.IsValid && definition.HasNextActivity)
            {
                return "NextActivity";
            }

            return "CompleteActivity";
        }

        private ActivityContentReleaseContinuationTelemetry BuildActivityContentReleaseContinuationTelemetry(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            int entrySequence,
            string continuationKind,
            int loadedSceneCount,
            int releasedSceneCount,
            bool skippedNoContent,
            string releaseStatus,
            string previousStage,
            string nextStage)
        {
            string normalizedContinuationKind = Normalize(continuationKind);
            string targetActivityId = string.Empty;
            int targetEntrySequence = 0;

            PendingRestartTransition restart = _pendingRestartTransition;
            PendingInternalActivityTransition transition = _pendingInternalActivityTransition;
            bool hasPendingRouteExit = _activeRailKind == SessionActivityRailKind.ActivityRouteExitRail;
            bool hasPendingRestart = restart.IsValid &&
                definition.IsValid &&
                string.Equals(restart.Activity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                restart.FromEntrySequence == entrySequence;
            bool hasInternalTransition = transition.IsValid &&
                definition.IsValid &&
                string.Equals(transition.FromActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                transition.FromEntrySequence == entrySequence;
            bool hasNextActivity = definition.IsValid && definition.HasNextActivity;

            if (string.Equals(normalizedContinuationKind, "RestartCurrentActivity", StringComparison.Ordinal) && hasPendingRestart)
            {
                targetActivityId = restart.Activity.ActivityId;
                targetEntrySequence = restart.NextEntrySequence;
            }
            else if (string.Equals(normalizedContinuationKind, "NextActivity", StringComparison.Ordinal))
            {
                if (hasInternalTransition)
                {
                    targetActivityId = transition.ToActivityId;
                    targetEntrySequence = transition.ToEntrySequence;
                }
                else if (hasNextActivity)
                {
                    targetActivityId = definition.NextActivityId;
                    targetEntrySequence = 0;
                }
            }

            return new ActivityContentReleaseContinuationTelemetry(
                definition,
                entrySequence,
                command.Source,
                command.Reason,
                normalizedContinuationKind,
                targetActivityId,
                targetEntrySequence,
                hasPendingRestart,
                hasPendingRouteExit,
                hasNextActivity || hasInternalTransition,
                hasPendingRouteExit,
                _state.CurrentStage == SessionActivityStage.Deactivation || _state.CurrentStage == SessionActivityStage.ActivityContentReleaseCompleted,
                string.Equals(normalizedContinuationKind, "CompleteActivity", StringComparison.Ordinal),
                releaseStatus,
                skippedNoContent,
                loadedSceneCount,
                releasedSceneCount,
                previousStage,
                nextStage);
        }

        private bool TryBeginActivityContentReleaseContinuation(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            int entrySequence,
            string continuationKindOverride,
            string targetActivityIdOverride,
            int targetEntrySequenceOverride,
            out ActivityContentReleaseContinuationTelemetry telemetry)
        {
            telemetry = _lastActivityContentReleaseContinuationTelemetry;
            if (!telemetry.IsValid ||
                !definition.IsValid ||
                !string.Equals(telemetry.Definition.ActivityId, definition.ActivityId, StringComparison.Ordinal) ||
                telemetry.EntrySequence != entrySequence)
            {
                telemetry = default;
                return false;
            }

            string continuationKind = string.IsNullOrWhiteSpace(continuationKindOverride)
                ? telemetry.ContinuationKind
                : Normalize(continuationKindOverride);
            string targetActivityId = string.IsNullOrWhiteSpace(targetActivityIdOverride)
                ? telemetry.ContinuationTargetActivityId
                : Normalize(targetActivityIdOverride);
            int targetEntrySequence = targetEntrySequenceOverride > 0
                ? targetEntrySequenceOverride
                : telemetry.ContinuationTargetEntrySequence;

            telemetry = new ActivityContentReleaseContinuationTelemetry(
                definition,
                entrySequence,
                command.Source,
                command.Reason,
                continuationKind,
                targetActivityId,
                targetEntrySequence,
                telemetry.HasPendingRestartTransition,
                telemetry.HasPendingRouteExit,
                telemetry.HasNextActivity,
                telemetry.RouteExitRequested,
                telemetry.DeactivationCompleted,
                telemetry.ActivityCompletionRequested,
                telemetry.ReleaseStatus,
                telemetry.SkippedNoContent,
                telemetry.LoadedSceneCount,
                telemetry.ReleasedSceneCount,
                telemetry.PreviousStage,
                continuationKind);

            EmitActivityContentReleaseContinuationEvent(
                "ActivityContentReleaseContinuationStarted",
                telemetry);
            return true;
        }

        private void CompleteActivityContentReleaseContinuationIfStarted(
            bool started,
            ActivityContentReleaseContinuationTelemetry telemetry,
            string nextStage)
        {
            if (!started || !telemetry.IsValid)
            {
                return;
            }

            ActivityContentReleaseContinuationTelemetry completedTelemetry = new(
                telemetry.Definition,
                telemetry.EntrySequence,
                telemetry.Source,
                telemetry.Reason,
                telemetry.ContinuationKind,
                telemetry.ContinuationTargetActivityId,
                telemetry.ContinuationTargetEntrySequence,
                telemetry.HasPendingRestartTransition,
                telemetry.HasPendingRouteExit,
                telemetry.HasNextActivity,
                telemetry.RouteExitRequested,
                telemetry.DeactivationCompleted,
                telemetry.ActivityCompletionRequested,
                telemetry.ReleaseStatus,
                telemetry.SkippedNoContent,
                telemetry.LoadedSceneCount,
                telemetry.ReleasedSceneCount,
                telemetry.PreviousStage,
                nextStage);

            EmitActivityContentReleaseContinuationEvent(
                "ActivityContentReleaseContinuationCompleted",
                completedTelemetry);
            _lastActivityContentReleaseContinuationTelemetry = default;
        }

        private void EmitActivityContentReleaseContinuationEvent(
            string eventName,
            ActivityContentReleaseContinuationTelemetry telemetry)
        {
            if (!telemetry.IsValid)
            {
                return;
            }

            string normalizedEventName = Normalize(eventName);
            string message =
                $"[OBS][ActivityContentReleaseContinuationStage][ActivityContentReleaseContinuation] event='{normalizedEventName}' owner='{ActivityContentReleaseContinuationStage.Owner}' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{telemetry.Definition.ActivityId}' entrySequence='{telemetry.EntrySequence}' stage='{_state.CurrentStage}' source='{telemetry.Source}' reason='{telemetry.Reason}' continuationKind='{telemetry.ContinuationKind}' continuationTargetActivityId='{(string.IsNullOrWhiteSpace(telemetry.ContinuationTargetActivityId) ? "<none>" : telemetry.ContinuationTargetActivityId)}' continuationTargetEntrySequence='{telemetry.ContinuationTargetEntrySequence}' hasPendingRestartTransition='{telemetry.HasPendingRestartTransition.ToString().ToLowerInvariant()}' hasPendingRouteExit='{telemetry.HasPendingRouteExit.ToString().ToLowerInvariant()}' hasNextActivity='{telemetry.HasNextActivity.ToString().ToLowerInvariant()}' routeExitRequested='{telemetry.RouteExitRequested.ToString().ToLowerInvariant()}' deactivationCompleted='{telemetry.DeactivationCompleted.ToString().ToLowerInvariant()}' activityCompletionRequested='{telemetry.ActivityCompletionRequested.ToString().ToLowerInvariant()}' releaseStatus='{telemetry.ReleaseStatus}' skippedNoContent='{telemetry.SkippedNoContent.ToString().ToLowerInvariant()}' loadedSceneCount='{telemetry.LoadedSceneCount}' releasedSceneCount='{telemetry.ReleasedSceneCount}' previousStage='{telemetry.PreviousStage}' nextStage='{telemetry.NextStage}'.";

            ActivityContentReleaseContinuationStage.LogEvent(
                message,
                string.Equals(normalizedEventName, "ActivityContentReleaseContinuationCompleted", StringComparison.Ordinal));

            _state.AppendTrace(message);
        }

        private void EmitObjectContributorUnregisterStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityStage stage)
        {
            ActivityObjectContributorUnregisterStage.Execute(
                new ActivityObjectContributorUnregisterStageCommand(command.Identity, command, entrySequence),
                definition,
                this,
                _activityObjectExitRuntimeState,
                facts,
                snapshots);
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
                    LogPhaseBoundary("SessionActivityMaterializationCompleted", readyIdentity, source, reason, completed: true, detail: "phase='materialization' readiness='activation_window_ready'");
                    EmitPredefinedVisualSetupReadyFactIfApplicable(
                        definition,
                        facts,
                        readyIdentity,
                        source,
                        reason,
                        "activation_window_ready");
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

                    if (_activeRailKind == SessionActivityRailKind.ActivityRouteExitRail)
                    {
                        FinalizeDeactivationForRouteExit(definition, syntheticCommand, facts, snapshots, entrySequence);
                    }
                    else if (_pendingRestartTransition.IsValid)
                    {
                        _ = FinalizePendingRestartTransition(
                            definition,
                            syntheticCommand,
                            facts,
                            snapshots,
                            entrySequence);
                    }
                    else
                    {
                        _ = FinalizeDeactivationAndContinuation(definition, syntheticCommand, facts, snapshots, entrySequence);
                    }

                    break;
                }
                case SessionActivityPendingOperationKind.ActivityContentSceneLoad:
                {
                    ActivityEntryContentLoadResult result = _activityEntryPipeline.CompleteContentLoad(
                        new ActivityEntryContentLoadCompletionCommand(
                            _state.CurrentIdentity,
                            _state.CurrentIdentity.ActivityId,
                            _state.CurrentIdentity.ActivityOrdinal,
                            operation,
                            source,
                            reason),
                        definition,
                        facts,
                        snapshots);
                    if (result.ShouldContinueEntry)
                    {
                        ContinueAfterActivityContentLoadedSetReady(definition, syntheticCommand, facts, snapshots, entrySequence);
                    }
                    break;
                }
                case SessionActivityPendingOperationKind.ActivityContentSceneUnload:
                {
                    throw new InvalidOperationException("ActivityContentSceneUnload completion requires typed unload result callback.");
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
            if (active.OperationKind == SessionActivityPendingOperationKind.ActivityContentSceneLoad)
            {
                List<SessionActivityFact> contentLoadFacts = new();
                _activityEntryPipeline.FailContentLoad(
                    new ActivityEntryContentLoadFailureCommand(
                        _state.CurrentIdentity,
                        _state.CurrentIdentity.ActivityId,
                        _state.CurrentIdentity.ActivityOrdinal,
                        active,
                        source,
                        reason,
                        error),
                    _state.CurrentDefinition,
                    contentLoadFacts);
            }
            else if (active.OperationKind == SessionActivityPendingOperationKind.ActivityContentSceneUnload)
            {
                SessionActivityIdentity identity = BuildIdentity(_state.CurrentDefinition, SessionActivityStage.ActivityContentReleaseFailed, active.EntrySequence);
                _state.SetCurrentIdentity(identity, SessionActivityStage.ActivityContentReleaseFailed);
                EmitFact(
                    new List<SessionActivityFact>(),
                    SessionActivityFactKind.ActivityContentReleaseFailed,
                    identity,
                    source,
                    reason,
                    $"Activity content scene unload failed operationId='{active.OperationId}' scene='{active.SceneName}' error='{error}'.");
            }

            _state.ClearPendingOperation();
            _activityEntryPipeline.ResetState();
            _activityContentReleaseRuntimeState.ClearPendingReleaseContext(_state.CurrentDefinition.ActivityId, _state.CurrentEntrySequence, source, reason);
            _activityContentReleaseRuntimeState.SetAwaitingContinuation(false, _state.CurrentDefinition.ActivityId, _state.CurrentEntrySequence, source, reason);

            throw new InvalidOperationException(
                $"[FATAL][SessionActivityPipeline] Pending operation failed operationId='{active.OperationId}' operationKind='{active.OperationKind}' activityId='{active.ActivityId}' currentStage='{_state.CurrentStage}' sceneName='{active.SceneName}' reason='{error}'.");
        }

        public void CompleteActivityContentSceneUnloadOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneUnloadResult unloadResult)
        {
            if (!operation.IsValid)
            {
                throw new InvalidOperationException("Pending operation is invalid.");
            }

            if (!unloadResult.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneUnloadResult is invalid.");
            }

            if (!TryValidatePendingOperationCompletion(operation, unloadResult.Source, unloadResult.Reason))
            {
                return;
            }

            if (operation.OperationKind != SessionActivityPendingOperationKind.ActivityContentSceneUnload)
            {
                throw new InvalidOperationException($"Unsupported pending unload completion kind '{operation.OperationKind}'.");
            }

            _state.ClearPendingOperation();

            SessionActivityDefinition definition = _state.CurrentDefinition;
            int entrySequence = _state.CurrentEntrySequence;
            List<SessionActivityFact> facts = new();
            List<SessionActivitySnapshot> snapshots = new();
            PendingActivityContentReleaseContext context = _activityContentReleaseRuntimeState.PendingReleaseContext;
            if (context == null || !context.IsValid)
            {
                throw new InvalidOperationException("Pending activity content release context is missing for unload completion.");
            }
            SessionActivityCommand syntheticCommand = new(
                SessionActivityCommandKind.CompleteCurrentActivity,
                _state.CurrentIdentity,
                unloadResult.Source,
                unloadResult.Reason);

            HandleActivityContentSceneUnloadCompleted(
                definition,
                syntheticCommand,
                facts,
                snapshots,
                entrySequence,
                operation,
                unloadResult.Kind);

            context.NextSceneIndex += 1;
            if (context.NextSceneIndex < context.LoadedSet.Scenes.Count)
            {
                ExecuteNextActivityContentSceneRelease(context, syntheticCommand, facts, snapshots);
                return;
            }

            FinalizeActivityContentReleaseCompleted(context, syntheticCommand, facts, snapshots);
            if (_activeRailKind == SessionActivityRailKind.ActivityRouteExitRail &&
                string.Equals(context.Definition.ActivityId, _state.CurrentDefinition.ActivityId, StringComparison.Ordinal) &&
                context.EntrySequence == _state.CurrentEntrySequence)
            {
                CompleteRouteExitClosure(context.Definition, syntheticCommand, facts, snapshots, context.EntrySequence);
                return;
            }

            PendingRestartTransition restart = _pendingRestartTransition;
            if (restart.IsValid &&
                string.Equals(restart.Activity.ActivityId, context.Definition.ActivityId, StringComparison.Ordinal) &&
                restart.FromEntrySequence == context.EntrySequence)
            {
                StartPendingRestartEntry(restart, syntheticCommand, facts, snapshots);
                return;
            }

            SessionActivityIdentity deactivationIdentity = BuildIdentity(context.Definition, SessionActivityStage.Deactivation, context.EntrySequence);
            _ = ContinueAfterDeactivationAsync(context.Definition, syntheticCommand, facts, snapshots, context.EntrySequence, deactivationIdentity);
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
            SessionActivityFactKind rejectionKind = active.OperationKind switch
            {
                SessionActivityPendingOperationKind.ActivityContentSceneLoad => SessionActivityFactKind.ActivityContentSceneLoadRejected,
                SessionActivityPendingOperationKind.ActivityContentSceneUnload => SessionActivityFactKind.ActivityContentSceneUnloadRejected,
                _ => SessionActivityFactKind.CommandRejected,
            };
            EmitFact(
                facts,
                rejectionKind,
                _state.CurrentIdentity,
                source,
                reason,
                $"Pending operation completion rejected as stale/foreign. active='{active}' incoming='{operation}'.");
            return false;
        }

        private void HandleActivityContentSceneUnloadCompleted(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityPendingOperation operation,
            ActivityContentUnloadResultKind unloadKind)
        {
            SessionActivityIdentity unloadedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentSceneUnloaded, entrySequence);
            _state.SetCurrentIdentity(unloadedIdentity, SessionActivityStage.ActivityContentSceneUnloaded);
            string releaseStatus = unloadKind == ActivityContentUnloadResultKind.SkippedNoContent
                ? "SkippedNoContent"
                : "Unloaded";
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneUnloaded,
                unloadedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unloaded operationId='{operation.OperationId}' sceneName='{operation.SceneName}' releaseStatus='{releaseStatus}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_scene_unloaded",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unloaded operationId='{operation.OperationId}' sceneName='{operation.SceneName}' releaseStatus='{releaseStatus}'.");
        }

        private void EmitActivityContentSceneUnloadCommandIssued(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityPendingOperation operation)
        {
            SessionActivityIdentity unloadingIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentSceneUnloading, entrySequence);
            _state.SetCurrentIdentity(unloadingIdentity, SessionActivityStage.ActivityContentSceneUnloading);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneUnloadCommandIssued,
                unloadingIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unload command issued operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_scene_unload_command_issued",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unload command issued operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");
        }

        private static SessionActivityCommandResult BuildCommandResult(SessionActivityCommand command, List<SessionActivityFact> emittedFacts)
        {
            SessionActivityCommandResultKind resultKind = emittedFacts.Count == 0
                ? SessionActivityCommandResultKind.Started
                : emittedFacts[emittedFacts.Count - 1].Kind switch
                {
                    SessionActivityFactKind.CommandRejected => SessionActivityCommandResultKind.Rejected,
                    SessionActivityFactKind.PipelineCompleted => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.ActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.ActivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.GameplayContentSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.DeactivationWindowSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.NextActivitySetupSkippedNoContent => SessionActivityCommandResultKind.SkippedNoContent,
                    SessionActivityFactKind.SimulationPaused => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.SimulationResumed => SessionActivityCommandResultKind.Completed,
                    _ => SessionActivityCommandResultKind.Started,
                };

            return new SessionActivityCommandResult(
                resultKind,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        private void EmitSessionReset(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!_state.HasStarted)
            {
                EmitRejected(
                    command,
                    facts,
                    "session_reset_requires_started_pipeline",
                    "Session reset requires a started pipeline.",
                    command.Identity,
                    true);
                return;
            }

            if (_state.CurrentPendingOperation.IsValid)
            {
                EmitRejected(
                    command,
                    facts,
                    "session_reset_blocked_pending_operation",
                    $"Session reset blocked while pending operation '{_state.CurrentPendingOperation.OperationKind}' is active.",
                    _state.CurrentIdentity,
                    true);
                return;
            }

            SessionActivityDefinition currentDefinition = _state.CurrentDefinition.IsValid
                ? _state.CurrentDefinition
                : ResolveFirstActivityOrFail();
            int entrySequence = _state.CurrentEntrySequence > 0 ? _state.CurrentEntrySequence : 1;
            SessionActivityIdentity resetIdentity = BuildIdentity(currentDefinition, SessionActivityStage.Completed, entrySequence);

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][SessionReset] event='SessionResetStarted' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{resetIdentity.ActivityId}' entrySequence='{resetIdentity.EntrySequence}' source='{command.Source}' reason='{command.Reason}' sessionActorCount='{_sessionActorRuntimeStore.Count}'.",
                DebugUtility.Colors.Info);

            _state.SetCurrentIdentity(resetIdentity, SessionActivityStage.Completed);

            ReleaseSessionScopedActors(resetIdentity, facts, command.Source, command.Reason);
            ReleaseIndexedRouteScopedPlayerActors();
            _sessionActorRuntimeStore.Clear();
            _activityPlayerActorRegistry.ClearAllRouteScopedIndexes();
            _activitySceneActorRegistry.ClearAllRouteRetained();
            _activityEntryPipeline.ResetState();
            _activityContentReleaseRuntimeState.ClearCurrentLoadedSet(resetIdentity.ActivityId, resetIdentity.EntrySequence, "SessionActivityPipeline", "session_reset");
            _activityContentReleaseRuntimeState.ClearPendingReleaseContext(resetIdentity.ActivityId, resetIdentity.EntrySequence, "SessionActivityPipeline", "session_reset");
            _activityContentReleaseRuntimeState.SetAwaitingContinuation(false, resetIdentity.ActivityId, resetIdentity.EntrySequence, "SessionActivityPipeline", "session_reset");
            _activityObjectExitRuntimeState.ClearAll(resetIdentity.ActivityId, resetIdentity.EntrySequence, "SessionActivityPipeline", "session_reset");
            _activityActorExitRuntimeState.ClearAll(resetIdentity.ActivityId, resetIdentity.EntrySequence, "SessionActivityPipeline", "session_reset");
            _lastSessionParticipationContext = null;
            _lastActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
            _lastActivityParticipationContext = null;
            _pendingContinuationExitTeardownCompleted = false;
            _routeExitActorTeardownCompleted = false;
            _pendingTransitionResolution = default;
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingInternalActivityTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;
            _activeRailKind = SessionActivityRailKind.None;
            _state.SetExecutionState(ActivityExecutionState.Stopped);
            _state.MarkCompleted();

            EmitFact(
                facts,
                SessionActivityFactKind.PipelineCompleted,
                resetIdentity,
                command.Source,
                command.Reason,
                $"Session reset completed. sessionScopedActorsRemaining='{_sessionActorRuntimeStore.Count}'.");
            EmitSnapshot(snapshots, "session_reset_completed", command.Source, command.Reason, "Session reset completed.");

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][SessionReset] event='SessionResetCompleted' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{resetIdentity.ActivityId}' entrySequence='{resetIdentity.EntrySequence}' source='{command.Source}' reason='{command.Reason}' sessionActorCount='{_sessionActorRuntimeStore.Count}'.",
                DebugUtility.Colors.Success);
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
            _activityEntryPipeline.ResetState();
            _activityContentReleaseRuntimeState.ClearCurrentLoadedSet(firstDefinition.ActivityId, entrySequence, "SessionActivityPipeline", "pipeline_start_reset");
            _activityContentReleaseRuntimeState.ClearPendingReleaseContext(firstDefinition.ActivityId, entrySequence, "SessionActivityPipeline", "pipeline_start_reset");
            _activityContentReleaseRuntimeState.SetAwaitingContinuation(false, firstDefinition.ActivityId, entrySequence, "SessionActivityPipeline", "pipeline_start_reset");
            _pendingContinuationExitTeardownCompleted = false;
            _activityObjectExitRuntimeState.ClearAll(firstDefinition.ActivityId, entrySequence, "SessionActivityPipeline", "pipeline_start_reset");
            _lastSessionParticipationContext = null;
            _lastActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
            _lastActivityParticipationContext = null;
            ReleaseIndexedRouteScopedPlayerActors();
            ReleaseSessionScopedActors(activationIdentity, facts, command.Source, "pipeline_start_reset");
            _activityPlayerActorRegistry.ClearAllRouteScopedIndexes();
            _activitySceneActorRegistry.ClearAllRouteRetained();
            _activityActorExitRuntimeState.ClearAll(firstDefinition.ActivityId, entrySequence, "SessionActivityPipeline", "pipeline_start_reset");
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
            _activeRailKind = SessionActivityRailKind.ActivityCompletionRail;
            ClearPendingNavigationTransition();
            _pendingContinuationExitTeardownCompleted = false;

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
            EmitMovementControlDisableForCurrentTargets(current, command, facts, snapshots, currentEntrySequence, reasonCode: "deactivation_window_started");

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
            _activeRailKind = SessionActivityRailKind.ActivityRestartRail;
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
            ExecuteActivityExitActorTeardown(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);

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
            _activeRailKind = SessionActivityRailKind.ActivityRouteExitRail;
            _routeExitActorTeardownCompleted = false;
            ActivityExitOrderingPolicy exitOrderingPolicy = ActivityExitOrderingPolicy.ForScenario(
                _state.CurrentStage == SessionActivityStage.DeactivationWindowReady
                    ? ActivityExitOrderingScenario.RouteExitFromDeactivationWindowReady
                    : ActivityExitOrderingScenario.RouteExitFromActivityRunning);
            ClearPendingNavigationTransition();
            _state.ClearHandoff();
            _pendingInternalActivityTransition = default;
            _pendingTransitionResolution = default;
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;

            if (exitOrderingPolicy.IsValid)
            {
                DebugUtility.Log(typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][ExitOrderingPolicy] event='ActivityExitOrderingPolicyApplied' scenario='{exitOrderingPolicy.Scenario}' policyId='{exitOrderingPolicy.PolicyId}' deactivationWindowRule='{exitOrderingPolicy.DeactivationWindowRule}' continuation='{exitOrderingPolicy.Continuation}' snapshotBeforeTeardown='{exitOrderingPolicy.SnapshotBeforeTeardown}' releaseAfterDeactivationWindow='{exitOrderingPolicy.ReleaseAfterDeactivationWindow}' operationalMayContinueBeforeRouteExitCompleted='{exitOrderingPolicy.OperationalMayContinueBeforeRouteExitCompleted}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
            }

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
                EmitMovementControlDisableForCurrentTargets(current, command, facts, snapshots, currentEntrySequence, reasonCode: "route_exit_requested");
                DebugUtility.Log(typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][ExitOrderingPolicy] event='RouteExitActorTeardownDeferredUntilAfterDeactivation' scenario='{ActivityExitOrderingScenario.RouteExitFromActivityRunning}' policyId='{ActivityExitOrderingPolicyIds.RouteExitFromActivityRunning}' activityId='{current.ActivityId}' entrySequence='{currentEntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

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
                ExecuteActivityExitActorTeardown(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.RouteExit);
                _routeExitActorTeardownCompleted = true;

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

            await ContinueAfterDeactivationAsync(current, command, facts, snapshots, currentEntrySequence, deactivationIdentity);
        }

        private bool EnsureContinuationExitTeardownAfterBlackoutOrStartRelease(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            if (_pendingContinuationExitTeardownCompleted)
            {
                return true;
            }

            if (_pendingTransitionResolution.Mode == ActivityTransitionMode.CutWithCurtain && !_pendingTransitionCurtainClosed)
            {
                throw new InvalidOperationException(
                    $"Activity '{current.ActivityId}' cannot execute continuation teardown before transition blackout completed.");
            }

            ExecuteActivityExitActorTeardown(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);

            _pendingContinuationExitTeardownCompleted = true;

            if (TryStartActivityContentReleaseForContinuation(current, command, facts, snapshots, currentEntrySequence))
            {
                return false;
            }

            return true;
        }

        private ActivityExitActorTeardownResult ExecuteActivityExitActorTeardown(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ActorPresentationReleaseRail releaseRail)
        {
            if (releaseRail == ActorPresentationReleaseRail.RouteExit &&
                _activeRailKind == SessionActivityRailKind.ActivityRouteExitRail &&
                !CanExecuteRouteExitActorTeardownAfterDeactivation())
            {
                SessionActivityIdentity blockedIdentity = BuildIdentity(definition, _state.CurrentStage, entrySequence);
                DebugUtility.Log(typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][ExitOrderingPolicy] event='RouteExitActorTeardownGuardDeferred' scenario='{ActivityExitOrderingScenario.RouteExitFromActivityRunning}' policyId='{ActivityExitOrderingPolicyIds.RouteExitFromActivityRunning}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' currentStage='{_state.CurrentStage}' rail='{releaseRail}' source='{command.Source}' reason='{command.Reason}' detail='RouteExit actor teardown is deferred until DeactivationWindowCompleted or DeactivationWindowSkippedNoContent.'.",
                    DebugUtility.Colors.Warning);
                return new ActivityExitActorTeardownResult(
                    false,
                    blockedIdentity,
                    "route_exit_teardown_deferred_until_after_deactivation");
            }

            return ActivityExitActorTeardownStage.Execute(
                new ActivityExitActorTeardownCommand(command.Identity, command, entrySequence, releaseRail),
                definition,
                this,
                _activityActorExitRuntimeState,
                this,
                _sessionActorRuntimeStore,
                facts,
                snapshots);
        }

        private bool CanExecuteRouteExitActorTeardownAfterDeactivation()
        {
            return _state.CurrentStage == SessionActivityStage.DeactivationWindowCompleted ||
                _state.CurrentStage == SessionActivityStage.DeactivationWindowSkippedNoContent ||
                _state.CurrentStage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                _state.CurrentStage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                _state.CurrentStage == SessionActivityStage.Deactivation ||
                _state.CurrentStage == SessionActivityStage.ActivityContentReleaseStarted ||
                _state.CurrentStage == SessionActivityStage.ActivityContentReleaseCompleted ||
                _state.CurrentStage == SessionActivityStage.ClosedForRouteExit;
        }

        private async Task ContinueAfterDeactivationAsync(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence,
            SessionActivityIdentity deactivationIdentity)
        {
            if (_activityContentReleaseRuntimeState.IsAwaitingContinuation ||
                _activityContentReleaseRuntimeState.HasPendingReleaseContext)
            {
                return;
            }

            if (!deactivationIdentity.IsValid)
            {
                deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            }

            if (_pendingNavigationTransition.IsValid)
            {
                bool continuationStarted = TryBeginActivityContentReleaseContinuation(
                    current,
                    command,
                    currentEntrySequence,
                    "NextActivity",
                    _pendingNavigationTransition.Target.IsValid ? _pendingNavigationTransition.Target.ActivityId : string.Empty,
                    _pendingNavigationTransition.TargetEntrySequence,
                    out ActivityContentReleaseContinuationTelemetry continuationTelemetry);

                await FinalizePendingNavigationTransition(current, command, facts, snapshots, deactivationIdentity);
                CompleteActivityContentReleaseContinuationIfStarted(
                    continuationStarted,
                    continuationTelemetry,
                    "ActivityHandoffPrepared");
                return;
            }

            if (!TryResolveNextActivityForContinuation(current, out SessionActivityDefinition next, out bool wrapped))
            {
                if (!EnsureContinuationExitTeardownAfterBlackoutOrStartRelease(current, command, facts, snapshots, currentEntrySequence))
                {
                    return;
                }

                bool continuationStarted = TryBeginActivityContentReleaseContinuation(
                    current,
                    command,
                    currentEntrySequence,
                    "CompleteActivity",
                    string.Empty,
                    0,
                    out ActivityContentReleaseContinuationTelemetry continuationTelemetry);

                _state.SetCurrentIdentity(BuildIdentity(current, SessionActivityStage.Completed, currentEntrySequence), SessionActivityStage.Completed);
                _state.MarkCompleted();
                _activeRailKind = SessionActivityRailKind.None;
                _pendingInternalActivityTransition = default;
                _pendingContinuationExitTeardownCompleted = false;
                EmitFact(facts, SessionActivityFactKind.PipelineCompleted, _state.CurrentIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                EmitSnapshot(snapshots, "pipeline_completed", command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                CompleteActivityContentReleaseContinuationIfStarted(
                    continuationStarted,
                    continuationTelemetry,
                    nameof(SessionActivityStage.Completed));
                return;
            }

            int nextEntrySequence = ResolveNextEntrySequence();
            SessionActivityIdentity nextActivationIdentity = BuildIdentity(next, SessionActivityStage.ActivityActivationStarted, nextEntrySequence);

            if (!_pendingContinuationExitTeardownCompleted)
            {
                SessionActivityTransitionResolution transitionResolution = ResolveNextActivityTransitionResolutionOrFail(current, next);
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

                if (!EnsureContinuationExitTeardownAfterBlackoutOrStartRelease(current, command, facts, snapshots, currentEntrySequence))
                {
                    return;
                }
            }

            bool nextContinuationStarted = TryBeginActivityContentReleaseContinuation(
                current,
                command,
                currentEntrySequence,
                "NextActivity",
                next.ActivityId,
                nextEntrySequence,
                out ActivityContentReleaseContinuationTelemetry nextContinuationTelemetry);

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
            _pendingContinuationExitTeardownCompleted = false;
            await ApplyContinuePolicyAfterHandoffPreparedAsync(current, command, facts, snapshots);
            CompleteActivityContentReleaseContinuationIfStarted(
                nextContinuationStarted,
                nextContinuationTelemetry,
                "ActivityHandoffPrepared");
        }

        private void FinalizeDeactivationForRouteExit(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            _activeRailKind = SessionActivityRailKind.ActivityRouteExitRail;
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

            if (!_routeExitActorTeardownCompleted)
            {
                DebugUtility.Log(typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][ExitOrderingPolicy] event='RouteExitActorTeardownAfterDeactivationStarted' scenario='{ActivityExitOrderingScenario.RouteExitFromActivityRunning}' policyId='{ActivityExitOrderingPolicyIds.RouteExitFromActivityRunning}' activityId='{current.ActivityId}' entrySequence='{currentEntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                ActivityExitActorTeardownResult routeExitTeardownResult = ExecuteActivityExitActorTeardown(
                    current,
                    command,
                    facts,
                    snapshots,
                    currentEntrySequence,
                    ActorPresentationReleaseRail.RouteExit);
                _routeExitActorTeardownCompleted = true;
                DebugUtility.Log(typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][ExitOrderingPolicy] event='RouteExitActorTeardownAfterDeactivationCompleted' scenario='{ActivityExitOrderingScenario.RouteExitFromActivityRunning}' policyId='{ActivityExitOrderingPolicyIds.RouteExitFromActivityRunning}' activityId='{current.ActivityId}' entrySequence='{currentEntrySequence}' result='{routeExitTeardownResult}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }

            if (TryStartActivityContentReleaseForContinuation(current, command, facts, snapshots, currentEntrySequence))
            {
                return;
            }

            CompleteRouteExitClosure(current, command, facts, snapshots, currentEntrySequence);
        }

        private void CompleteRouteExitClosure(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence)
        {
            bool continuationStarted = TryBeginActivityContentReleaseContinuation(
                current,
                command,
                currentEntrySequence,
                "RouteExit",
                string.Empty,
                0,
                out ActivityContentReleaseContinuationTelemetry continuationTelemetry);

            _state.ClearHandoff();
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;
            _pendingTransitionResolution = default;
            _pendingInternalActivityTransition = default;
            _pendingRestartCompletionActivityId = string.Empty;
            _pendingRestartCompletionEntrySequence = 0;

            SessionActivityIdentity routeExitClosedIdentity = BuildIdentity(current, SessionActivityStage.ClosedForRouteExit, currentEntrySequence);
            _state.SetCurrentIdentity(routeExitClosedIdentity, SessionActivityStage.ClosedForRouteExit);
            EmitMovementControlDisableForCurrentTargets(current, command, facts, snapshots, currentEntrySequence, reasonCode: "closed_for_route_exit");
            _state.MarkCompleted();
            EmitFact(facts, SessionActivityFactKind.ActivityRouteExitCompleted, routeExitClosedIdentity, command.Source, command.Reason, $"'{current.ActivityId}' route-exit closed.");
            EmitSnapshot(snapshots, "activity_route_exit_completed", command.Source, command.Reason, $"'{current.ActivityId}' route-exit closed.");
            CompletePendingRouteExitTeardownIfAny(routeExitClosedIdentity, command.Source, command.Reason);
            _activeRailKind = SessionActivityRailKind.None;
            _routeExitActorTeardownCompleted = false;
            CompleteActivityContentReleaseContinuationIfStarted(
                continuationStarted,
                continuationTelemetry,
                nameof(SessionActivityStage.ClosedForRouteExit));
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

            ReleaseActivityGateIfBlocked(command);
            _state.SetExecutionState(ActivityExecutionState.Stopped);

            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            if (TryStartActivityContentReleaseForContinuation(current, command, facts, snapshots, currentEntrySequence))
            {
                return;
            }

            StartPendingRestartEntry(restart, command, facts, snapshots);

            await Task.CompletedTask;
        }

        private void StartPendingRestartEntry(
            PendingRestartTransition restart,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            bool continuationStarted = TryBeginActivityContentReleaseContinuation(
                restart.Activity,
                command,
                restart.FromEntrySequence,
                "RestartCurrentActivity",
                restart.Activity.ActivityId,
                restart.NextEntrySequence,
                out ActivityContentReleaseContinuationTelemetry continuationTelemetry);

            _pendingRestartTransition = default;
            SessionActivityIdentity restartSetupIdentity = BuildIdentity(restart.Activity, SessionActivityStage.ActivitySetupStarted, restart.NextEntrySequence);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityRestartSetupStarted,
                restartSetupIdentity,
                command.Source,
                command.Reason,
                $"Restart setup started for '{restart.Activity.ActivityId}' at entrySequence='{restart.NextEntrySequence}'.");
            EmitSnapshot(
                snapshots,
                "activity_restart_setup_started",
                command.Source,
                command.Reason,
                $"Restart setup started for '{restart.Activity.ActivityId}' at entrySequence='{restart.NextEntrySequence}'.");

            _state.SetCurrentDefinition(restart.Activity);
            _pendingRestartCompletionActivityId = restart.Activity.ActivityId;
            _pendingRestartCompletionEntrySequence = restart.NextEntrySequence;
            EnterActivity(restart.Activity, command, facts, snapshots, restart.NextEntrySequence);
            CompleteActivityContentReleaseContinuationIfStarted(
                continuationStarted,
                continuationTelemetry,
                nameof(SessionActivityStage.ActivitySetupStarted));
        }

        private async Task EmitContinueAsync(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (_activeRailKind == SessionActivityRailKind.ActivityRouteExitRail)
            {
                EmitRejected(
                    command,
                    facts,
                    "route_exit_rail_disallows_continue",
                    "ContinueToNextActivity is not allowed while ActivityRouteExitRail is active.",
                    _state.CurrentIdentity,
                    true);
                return;
            }

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
            _activeRailKind = SessionActivityRailKind.ActivityNavigationRail;
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
            EmitMovementControlDisableForCurrentTargets(current, command, facts, snapshots, currentEntrySequence, reasonCode: "activity_navigation_exit");
            ExecuteActivityExitActorTeardown(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);

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

        private static ActivityContentLoadPlan BuildActivityContentLoadPlan(
            SessionActivityDefinition definition,
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid while building ActivityContentLoadPlan.");
            }

            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Activity setup identity is invalid while building ActivityContentLoadPlan.");
            }

            if (definition.ActivityContentMode == ActivityContentMode.None)
            {
                return new ActivityContentLoadPlan(
                    identity,
                    definition.ActivityId,
                    definition.ActivityOrdinal,
                    definition.ActivityContentMode,
                    string.Empty,
                    Array.Empty<ActivityContentLoadPlanScene>(),
                    source,
                    reason);
            }

            if (definition.ActivityContentMode != ActivityContentMode.Profile)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' has unsupported ActivityContentMode='{definition.ActivityContentMode}' while building ActivityContentLoadPlan.");
            }

            ActivityContentProfileAsset profile = definition.ActivityContentProfile;
            if (profile == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires ActivityContentProfile when ActivityContentMode=Profile.");
            }

            profile.ValidateOrThrow($"SessionActivityPipeline:{definition.ActivityId}");

            IReadOnlyList<ActivityContentSceneEntry> entries = profile.ContentScenes ?? Array.Empty<ActivityContentSceneEntry>();
            List<ActivityContentLoadPlanScene> scenes = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                ActivityContentSceneEntry entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException($"Activity '{definition.ActivityId}' has null content scene entry at index '{index}'.");
                }

                scenes.Add(new ActivityContentLoadPlanScene(
                    index + 1,
                    ActivityContentSceneRuntimeReference.FromSceneKeyAsset(entry.SceneKey),
                    entry.Requiredness));
            }

            return new ActivityContentLoadPlan(
                identity,
                definition.ActivityId,
                definition.ActivityOrdinal,
                definition.ActivityContentMode,
                profile.ContentProfileId,
                scenes,
                source,
                reason);
        }

        private void EnterActivity(SessionActivityDefinition definition, SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots, int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            SessionActivityIdentity setupBoundaryIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            ActivityEntryPreparationCommand entryCommand = new(
                setupBoundaryIdentity,
                command.Source,
                command.Reason);
            ActivityEntryPreparationResult entryResult = _activityEntryPipeline.PrepareEntry(entryCommand);
            if (entryResult.Kind != ActivityEntryPreparationResultKind.Prepared || !entryResult.IsValid)
            {
                throw new InvalidOperationException($"ActivityEntryPipeline failed entry preparation. kind='{entryResult.Kind}' reason='{entryResult.Reason}' identity='{entryResult.Identity}'.");
            }

            ActivityContentLoadPlan contentLoadPlan = BuildActivityContentLoadPlan(
                definition,
                setupBoundaryIdentity,
                command.Source,
                command.Reason);
            ActivityEntryContentLoadResult contentLoadResult = _activityEntryPipeline.BeginContentLoad(
                new ActivityEntryContentLoadCommand(contentLoadPlan),
                facts,
                snapshots);
            if (!contentLoadResult.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPipeline returned invalid content load result.");
            }

            if (!contentLoadResult.ShouldContinueEntry)
            {
                return;
            }

            ActivityEntrySetupReadinessResult setupReadinessResult = _activityEntryPipeline.ExecuteSetupAndReadiness(
                new ActivityEntryCommand(
                    setupBoundaryIdentity,
                    setupBoundaryIdentity.ActivityId,
                    setupBoundaryIdentity.ActivityOrdinal,
                    command.Source,
                    command.Reason),
                definition,
                facts,
                snapshots);
            if (setupReadinessResult.Kind != ActivityEntrySetupReadinessResultKind.Completed || !setupReadinessResult.IsValid)
            {
                throw new InvalidOperationException($"ActivityEntryPipeline setup/readiness failed. kind='{setupReadinessResult.Kind}' reason='{setupReadinessResult.Reason}' identity='{setupReadinessResult.Identity}'.");
            }

            EnterActivationFlow(definition, command, facts, snapshots, entrySequence);
        }

        private void EnterActivationFlow(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
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

        private void ContinueAfterActivityContentLoadedSetReady(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (_state.CurrentStage != SessionActivityStage.ActivityContentLoadedSetReady)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' cannot continue after content loaded set because current stage is '{_state.CurrentStage}' instead of '{SessionActivityStage.ActivityContentLoadedSetReady}'.");
            }

            SessionActivityIdentity setupBoundaryIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            ActivityEntrySetupReadinessResult setupReadinessResult = _activityEntryPipeline.ExecuteSetupAndReadiness(
                new ActivityEntryCommand(
                    setupBoundaryIdentity,
                    setupBoundaryIdentity.ActivityId,
                    setupBoundaryIdentity.ActivityOrdinal,
                    command.Source,
                    command.Reason),
                definition,
                facts,
                snapshots);
            if (setupReadinessResult.Kind != ActivityEntrySetupReadinessResultKind.Completed || !setupReadinessResult.IsValid)
            {
                throw new InvalidOperationException($"ActivityEntryPipeline setup/readiness failed. kind='{setupReadinessResult.Kind}' reason='{setupReadinessResult.Reason}' identity='{setupReadinessResult.Identity}'.");
            }

            EnterActivationFlow(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitPredefinedVisualSetupReadyFactIfApplicable(
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            SessionActivityIdentity readinessIdentity,
            string source,
            string reason,
            string readinessPoint)
        {
            string routeOperationId = _lastSessionParticipationContext != null && _lastSessionParticipationContext.IsValid
                ? _lastSessionParticipationContext.RouteOperationId
                : string.Empty;
            if (string.IsNullOrWhiteSpace(routeOperationId))
            {
                return;
            }

            _lastVisualReadinessSignal = new VisualReadinessSignal(
                readinessIdentity,
                routeOperationId,
                source,
                reason);
            EmitFact(
                facts,
                SessionActivityFactKind.PredefinedVisualSetupReady,
                readinessIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' predefined visual setup ready routeOperationId='{routeOperationId}' entrySequence='{readinessIdentity.EntrySequence}' readinessPoint='{readinessPoint}'.");

            CompletePendingVisualReadinessIfMatching(
                routeOperationId,
                readinessIdentity,
                "visual_readiness_completed",
                $"SessionActivity visual readiness completed at readinessPoint='{readinessPoint}'.");
        }

        private ActivityCapabilityInventory RequireCurrentActivityCapabilityInventoryForEntry(
            SessionActivityIdentity identity,
            string consumerName)
        {
            ActivityCapabilityInventory inventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult validation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool isCurrentEntryInventory =
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == identity.EntrySequence;

            if (!isCurrentEntryInventory)
            {
                throw new InvalidOperationException(
                    $"[FATAL][SessionActivityPipeline][CapabilityInventory] Missing current entry capability inventory for consumer='{Normalize(consumerName)}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}'.");
            }

            return inventory;
        }

        private bool HasCurrentActivityCapabilityInventoryForEntry(
            SessionActivityIdentity identity,
            out ActivityCapabilityInventory inventory,
            out ActivityCapabilityInventoryValidationResult validation)
        {
            inventory = _state.CurrentActivityCapabilityInventoryPreview;
            validation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            return
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == identity.EntrySequence;
        }

        private ActorInventoryFeedResult BuildActorInventoryFeedForCurrentEntry(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            return _activityActorExitRuntimeState.GetActorInventoryFeedForExit(identity, source, reason);
        }

        private static Dictionary<ActorInstanceId, ActorInstanceRecord> BuildActorInstanceIndex(IReadOnlyList<ActorInstanceRecord> instances)
        {
            Dictionary<ActorInstanceId, ActorInstanceRecord> byId = new();
            if (instances == null)
            {
                return byId;
            }

            for (int index = 0; index < instances.Count; index++)
            {
                ActorInstanceRecord instance = instances[index];
                if (!instance.IsValid)
                {
                    continue;
                }

                byId[instance.ActorInstanceId] = instance;
            }

            return byId;
        }

        private bool TryResolveActorInstanceIdForActor(
            SessionActivityIdentity identity,
            string actorId,
            string source,
            string reason,
            out ActorInstanceId actorInstanceId)
        {
            actorInstanceId = default;
            if (!identity.IsValid || string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            ActorInventoryFeedResult feedResult = BuildActorInventoryFeedForCurrentEntry(identity, source, reason);
            for (int index = 0; index < feedResult.ActorInstances.Count; index++)
            {
                ActorInstanceRecord instance = feedResult.ActorInstances[index];
                if (!instance.IsValid)
                {
                    continue;
                }

                if (string.Equals(instance.ActorId, actorId, StringComparison.Ordinal))
                {
                    actorInstanceId = instance.ActorInstanceId;
                    return actorInstanceId.IsValid;
                }
            }

            return false;
        }

        private bool TryResolveActorInstanceMetadata(
            SessionActivityIdentity identity,
            ActorInstanceId actorInstanceId,
            out ActorInstanceRecord instance)
        {
            instance = default;
            if (!identity.IsValid || !actorInstanceId.IsValid)
            {
                return false;
            }

            ActorInventoryFeedResult feedResult = BuildActorInventoryFeedForCurrentEntry(identity, "actor_presentation_release", "resolve_actor_instance_metadata");
            for (int index = 0; index < feedResult.ActorInstances.Count; index++)
            {
                ActorInstanceRecord current = feedResult.ActorInstances[index];
                if (!current.IsValid)
                {
                    continue;
                }

                if (current.ActorInstanceId == actorInstanceId)
                {
                    instance = current;
                    return true;
                }
            }

            return false;
        }

        private bool IsActorParticipationEligibleFromPolicy(
            SessionActivityIdentity identity,
            string activityId,
            ActorParticipationRecord participation,
            ActorInstanceRecord instance,
            out string reasonCode)
        {
            if (!participation.IsValid || !instance.IsValid)
            {
                reasonCode = "participation_record_invalid";
                return false;
            }

            if (!participation.ParticipatesInCurrentEntry)
            {
                reasonCode = "entry_not_participating";
                return false;
            }

            switch (participation.Policy)
            {
                case ActorParticipationRecord.ActorParticipationPolicy.AllActivitiesInRoute:
                    reasonCode = "eligible";
                    return true;
                case ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds:
                    {
                        IReadOnlyList<string> activityIds = participation.ExplicitActivityIds;
                        for (int index = 0; index < activityIds.Count; index++)
                        {
                            if (string.Equals(Normalize(activityIds[index]), Normalize(activityId), StringComparison.Ordinal))
                            {
                                reasonCode = "eligible";
                                return true;
                            }
                        }

                        reasonCode = "activity_not_listed";
                        return false;
                    }
                case ActorParticipationRecord.ActorParticipationPolicy.None:
                default:
                    reasonCode = "policy_disabled";
                    return false;
            }
        }

        private void ReleaseIndexedRouteScopedPlayerActors()
        {
            IReadOnlyList<PlayerActorRuntimeHandle> handles = _activityPlayerActorRegistry.GetIndexedRouteScopedHandles();
            for (int index = 0; index < handles.Count; index++)
            {
                PlayerActorRuntimeHandle handle = handles[index];
                if (handle.Instance != null)
                {
                    Object.Destroy(handle.Instance);
                }
            }
        }

        private void ReleaseSessionScopedActors(
            SessionActivityIdentity identity,
            List<SessionActivityFact> facts,
            string source,
            string reason)
        {
            IReadOnlyList<SessionActorRuntimeEntry> entries = _sessionActorRuntimeStore.GetEntriesForSession(identity);
            for (int index = 0; index < entries.Count; index++)
            {
                SessionActorRuntimeEntry entry = entries[index];
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActorLifetime] event='ActorLifetimeDecisionResolved' owner='SessionActivityPipeline' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' decision='Release' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeDecisionResolved,
                    identity,
                    source,
                    reason,
                    $"Actor lifetime decision resolved actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='SessionReset' decision='Release'.");

                if (entry.Instance != null)
                {
                    Object.Destroy(entry.Instance);
                }

                _sessionActorRuntimeStore.Remove(entry.ActorInstanceRuntimeId);
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActorLifetime] event='ActorLifetimeReleased' owner='SessionActivityPipeline' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeReleased,
                    identity,
                    source,
                    reason,
                    $"Actor lifetime released actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='SessionReset'.");
            }
        }

        private bool TryGetActivePresentationHandle(ActorPresentationEndpointReference presentationReference, out ActorPresentationRuntimeHandle handle)
        {
            handle = default;
            if (presentationReference == null || !presentationReference.IsValid)
            {
                return false;
            }

            return _activityActorExitRuntimeState.TryGetActivePresentationHandle(presentationReference, out handle);
        }

        private void StoreActivePresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle)
        {
            if (presentationReference == null || !presentationReference.IsValid || !handle.IsValid)
            {
                return;
            }

            _activityActorExitRuntimeState.StoreActiveActorPresentation(
                new ActorPresentationCapabilityState(
                    presentationReference.ActorInstanceRuntimeId,
                    presentationReference.ActorId,
                    presentationReference.Endpoint,
                    handle,
                    identity.PipelineId,
                    BuildActorAttributeActivityIdentity(identity)),
                identity.ActivityId,
                identity.EntrySequence,
                "ActivityEntryActorPresentationStage",
                "store_active_actor_presentation");
            SyncActorPresentationRegistryHandle(identity, presentationReference, handle);
        }

        private void SyncActorPresentationRegistryHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle)
        {
            if (!identity.IsValid || presentationReference == null || !presentationReference.IsValid || !handle.IsValid)
            {
                return;
            }

            try
            {
                _activitySceneActorRegistry.SetPresentationHandle(identity, presentationReference.ActorId, handle);
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationRegistryHandleSynced' owner='ActivitySceneActorRegistry' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' actorId='{Normalize(presentationReference.ActorId)}' source='ActivityEntryActorPresentationStage' reason='actor_presentation_registry_handle_sync'.",
                    DebugUtility.Colors.Info);
            }
            catch (InvalidOperationException)
            {
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationRegistryHandleSyncSkipped' owner='ActivitySceneActorRegistry' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' actorId='{Normalize(presentationReference.ActorId)}' source='ActivityEntryActorPresentationStage' reason='actor_not_registered_in_presentation_registry'.",
                    DebugUtility.Colors.Info);
            }
        }



        private void EmitActorPresentationReleaseGenericStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ActorPresentationReleaseRail rail,
            ActorInstanceId targetActorInstanceId = default)
        {
            ActivityExitActorTeardownStage.ExecuteActorPresentationRelease(
                new ActivityExitActorTeardownCommand(command.Identity, command, entrySequence, rail, targetActorInstanceId),
                definition,
                this,
                _activityActorExitRuntimeState,
                this,
                facts,
                snapshots);
        }

        private void EmitActorParticipationExitFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityExitActorTeardownStage.ExecuteActorParticipationExit(
                new ActivityExitActorTeardownCommand(command.Identity, command, entrySequence, ActorPresentationReleaseRail.ActivityExit),
                definition,
                this,
                _activityActorExitRuntimeState,
                this,
                _sessionActorRuntimeStore,
                facts,
                snapshots);
        }

        private void EmitActorAttributeReleaseFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityExitActorTeardownStage.ExecuteActorAttributeRelease(
                new ActivityExitActorTeardownCommand(command.Identity, command, entrySequence, ActorPresentationReleaseRail.ActivityExit),
                definition,
                this,
                _activityActorExitRuntimeState,
                this,
                facts,
                snapshots);
        }

        private static string BuildActorAttributeActivityIdentity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                return string.Empty;
            }

            return $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.ActivityOrdinal}|{identity.EntrySequence}";
        }

        private static ActorAttributeCommand BuildActorAttributeCommand(
            SessionActivityIdentity activityIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue,
            string source,
            string reason)
        {
            return operation switch
            {
                ActorAttributeOperation.Set => ActorAttributeCommand.Set(activityIdentity, actorInstanceRuntimeId, attributeId, setValue, source, reason),
                ActorAttributeOperation.Add => ActorAttributeCommand.Add(activityIdentity, actorInstanceRuntimeId, attributeId, amount, source, reason),
                ActorAttributeOperation.Subtract => ActorAttributeCommand.Subtract(activityIdentity, actorInstanceRuntimeId, attributeId, amount, source, reason),
                ActorAttributeOperation.ResetToInitial => ActorAttributeCommand.ResetToInitial(activityIdentity, actorInstanceRuntimeId, attributeId, source, reason),
                ActorAttributeOperation.RestoreToMax => ActorAttributeCommand.RestoreToMax(activityIdentity, actorInstanceRuntimeId, attributeId, source, reason),
                _ => new ActorAttributeCommand(activityIdentity, actorInstanceRuntimeId, attributeId, operation, amount, setValue, source, reason)
            };
        }

        private void LogActorAttributeCommandRejected(
            ActorAttributeOperation operation,
            string actorId,
            ActorAttributeId attributeId,
            string rejectionReason,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeCommandRejected' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' actorId='{actorId}' attributeId='{attributeId}' operation='{operation}' rejectionReason='{Normalize(rejectionReason)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Error);
        }


        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                left.CycleKey == right.CycleKey;
        }

        private void EmitMovementControlEnableAtRunning(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (!_movementControlEnableAllowedForCurrentEntry ||
                _movementControlTargetsForCurrentEntry == null ||
                _movementControlTargetsForCurrentEntry.Count == 0)
            {
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][MovementControl] event='MovementControlEnableSkippedNoTarget' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementControlEnableSkippedNoTarget,
                    _state.CurrentIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement control enable skipped because no target is available for current entry.");
                EmitSnapshot(
                    snapshots,
                    "movement_control_enable_skipped_no_target",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement control enable skipped because no target is available for current entry.");
                return;
            }

            EmitMovementControlState(
                definition,
                command,
                facts,
                snapshots,
                entrySequence,
                _movementControlTargetsForCurrentEntry,
                enable: true,
                reasonCode: "activity_running_entered");
        }

        private void EmitMovementControlDisableForCurrentTargets(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            string reasonCode)
        {
            if (_movementControlTargetsForCurrentEntry == null || _movementControlTargetsForCurrentEntry.Count == 0)
            {
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][MovementControl] event='MovementControlDisableSkippedNoTarget' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementControlDisableSkippedNoTarget,
                    _state.CurrentIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement control disable skipped because no target is available for current entry.");
                EmitSnapshot(
                    snapshots,
                    "movement_control_disable_skipped_no_target",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement control disable skipped because no target is available for current entry.");
                return;
            }

            EmitMovementControlState(
                definition,
                command,
                facts,
                snapshots,
                entrySequence,
                _movementControlTargetsForCurrentEntry,
                enable: false,
                reasonCode: reasonCode);
        }

        private void EmitMovementControlState(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            IReadOnlyList<PlayerActorIdentityRecord> targets,
            bool enable,
            string reasonCode)
        {
            SessionActivityIdentity scopeIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorParticipationExitStageStarted, entrySequence);
            IReadOnlyList<MovementControlRecord> records = PlayerMovementControlStage.Execute(
                scopeIdentity,
                targets,
                enable,
                _playerMovementControlAdapter,
                _activityPlayerActorRegistry,
                command.Source,
                $"{command.Reason}|{reasonCode}");

            SessionActivityFactKind factKind = enable
                ? SessionActivityFactKind.MovementControlEnabled
                : SessionActivityFactKind.MovementControlDisabled;
            string snapshotKey = enable ? "movement_control_enabled" : "movement_control_disabled";
            string stateValue = enable ? "true" : "false";
            string eventName = enable ? "MovementControlEnabled" : "MovementControlDisabled";
            if (!enable)
            {
                string disableKey = $"{definition.ActivityId}|{entrySequence}|{command.Source}|{command.Reason}";
                if (string.Equals(_lastMovementDisableEmissionKey, disableKey, StringComparison.Ordinal))
                {
                    DebugUtility.Log(
                        typeof(SessionActivityPipeline),
                        $"[OBS][SessionActivityPipeline][MovementControl] event='MovementControlDisableSkippedDuplicate' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _lastMovementDisableEmissionKey = disableKey;
            }
            else
            {
                _lastMovementDisableEmissionKey = string.Empty;
            }

            for (int index = 0; index < records.Count; index++)
            {
                MovementControlRecord record = records[index];
                if (!record.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementControl] Invalid record activityId='{definition.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                EmitFact(
                    facts,
                    factKind,
                    _state.CurrentIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement control state changed controlEnabled='{stateValue}' playerSlotId='{record.ActorIdentity.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' endpoint='{record.ObservedEndpoint}'.");
            }

            EmitSnapshot(
                snapshots,
                snapshotKey,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' movement control state changed controlEnabled='{stateValue}' affectedActors='{records.Count}'.");
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][MovementControl] event='{eventName}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' controlEnabled='{stateValue}' affectedActors='{records.Count}' source='{command.Source}' reason='{command.Reason}'.",
                enable ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }

        private IReadOnlyList<PlayerActorIdentityRecord> ResolveRetainedMovementTargetsOrEmpty(SessionActivityIdentity identity)
        {
            if (_activityPlayerActorRegistry.TryGetActiveActorIdentities(identity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors) &&
                activeActors != null &&
                activeActors.Count > 0)
            {
                List<PlayerActorIdentityRecord> resolvedActive = new();
                for (int index = 0; index < activeActors.Count; index++)
                {
                    PlayerActorIdentityRecord candidate = activeActors[index];
                    if (!candidate.IsValid)
                    {
                        continue;
                    }

                    if (!_activityPlayerActorRegistry.TryResolveHandleForParticipant(identity, candidate.ParticipantId, out PlayerActorRuntimeHandle handle) ||
                        !handle.IsValid)
                    {
                        continue;
                    }

                    if (handle.PlayerSlotId != candidate.PlayerSlotId || handle.ParticipantId != candidate.ParticipantId)
                    {
                        continue;
                    }

                    PlayerActorMovementBindingState movementState = handle.Instance.GetComponent<PlayerActorMovementBindingState>();
                    if (movementState == null || !movementState.IsValid)
                    {
                        continue;
                    }

                    resolvedActive.Add(new PlayerActorIdentityRecord(
                        identity,
                        candidate.ParticipantBinding,
                        candidate.PlayerActorId));
                }

                if (resolvedActive.Count > 0)
                {
                    return resolvedActive;
                }
            }

            IReadOnlyList<PlayerActorIdentityRecord> retained = _activityPlayerActorRegistry.GetRouteRetainedActorIdentitiesForSession(identity);
            if (retained == null || retained.Count == 0)
            {
                return Array.Empty<PlayerActorIdentityRecord>();
            }

            List<PlayerActorIdentityRecord> resolved = new();
            for (int index = 0; index < retained.Count; index++)
            {
                PlayerActorIdentityRecord candidate = retained[index];
                if (!candidate.IsValid)
                {
                    continue;
                }

                if (!_activityPlayerActorRegistry.TryResolveHandleForParticipant(identity, candidate.ParticipantId, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
                {
                    continue;
                }

                if (handle.PlayerSlotId != candidate.PlayerSlotId || handle.ParticipantId != candidate.ParticipantId)
                {
                    continue;
                }

                GameObject instance = handle.Instance;
                PlayerActorMovementBindingState movementState = instance.GetComponent<PlayerActorMovementBindingState>();
                if (movementState == null || !movementState.IsValid)
                {
                    continue;
                }

                PlayerActorInputBindingState inputState = instance.GetComponent<PlayerActorInputBindingState>();
                if (inputState == null || !inputState.IsValid)
                {
                    continue;
                }

                if (inputState.PlayerSlotId != candidate.PlayerSlotId ||
                    inputState.PlayerActorId != candidate.PlayerActorId)
                {
                    continue;
                }

                resolved.Add(new PlayerActorIdentityRecord(
                    identity,
                    candidate.ParticipantBinding,
                    candidate.PlayerActorId));
            }

            return resolved.Count == 0 ? Array.Empty<PlayerActorIdentityRecord>() : resolved;
        }


        public void ObserveActivitySceneContractOrSkip(
            SessionActivityDefinition definition,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            Scene routeScene = SceneManager.GetActiveScene();
            if (!routeScene.IsValid() || !routeScene.isLoaded)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity setup requires valid loaded route scene for ActivitySceneContract observation.");
            }

            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
            bool hasLoadedSetForEntry = IsLoadedSetForCurrentEntry(loadedSet, definition, entrySequence);
            bool hasActivityContentScenes = hasLoadedSetForEntry && loadedSet.HasScenes;

            if (definition.ActivityContentMode == ActivityContentMode.Profile && !hasActivityContentScenes)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' declared ActivityContentMode.Profile but has no valid ActivityContentLoadedSet for ActivitySceneContract observation. routeScene='{routeScene.name}' entrySequence='{entrySequence}'.");
            }

            List<ActivitySceneContractCandidate> contentCandidates = new();
            List<string> contentSceneNames = new();

            if (hasActivityContentScenes)
            {
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    ActivityContentLoadedSceneRecord record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' has invalid ActivityContentLoadedSceneRecord at index '{sceneIndex}' for ActivitySceneContract observation.");
                    }

                    Scene contentScene = SceneManager.GetSceneByName(record.SceneName);
                    if (!contentScene.IsValid() || !contentScene.isLoaded)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' ActivityContentLoadedSet references scene='{record.SceneName}' but the scene is not loaded for ActivitySceneContract observation.");
                    }

                    contentSceneNames.Add(contentScene.name);
                    AddContractCandidates(
                        contentCandidates,
                        contentScene,
                        "ActivityContentScene",
                        record.SceneOrdinal);
                }
            }

            List<ActivitySceneContractCandidate> routeCandidates = new();
            AddContractCandidates(routeCandidates, routeScene, "RouteScene", 0);

            if (contentCandidates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity content scope must have at most one ActivitySceneContractAuthoring in v0. contentScenes=[{FormatList(contentSceneNames)}] count='{contentCandidates.Count}'.");
            }

            ActivitySceneContractCandidate selectedCandidate = default;
            bool hasSelectedCandidate = false;
            string resolutionScope;
            string resolutionDetail;

            if (contentCandidates.Count == 1)
            {
                selectedCandidate = contentCandidates[0];
                hasSelectedCandidate = true;
                resolutionScope = selectedCandidate.ScopeKind;
                resolutionDetail =
                    $"routeScene='{routeScene.name}' contentScenes=[{FormatList(contentSceneNames)}] contentContracts='{contentCandidates.Count}' routeContracts='{routeCandidates.Count}' priority='activity_content'.";
            }
            else if (hasActivityContentScenes)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupStarted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySceneContractSkippedNoContent,
                    skippedIdentity,
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity scene contract skipped because ActivityContentScenes had no ActivitySceneContractAuthoring. routeScene='{routeScene.name}' contentScenes=[{FormatList(contentSceneNames)}] routeContracts='{routeCandidates.Count}' fallback='disabled_for_activity_content'.");
                EmitSnapshot(
                    snapshots,
                    "activity_scene_contract_skipped_no_content",
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity scene contract skipped because ActivityContentScenes had no ActivitySceneContractAuthoring. routeScene='{routeScene.name}' contentScenes=[{FormatList(contentSceneNames)}] routeContracts='{routeCandidates.Count}' fallback='disabled_for_activity_content'.");
                return;
            }
            else
            {
                if (routeCandidates.Count == 0)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupStarted);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivitySceneContractSkippedNoContent,
                        skippedIdentity,
                        source,
                        reason,
                        $"'{definition.ActivityId}' activity scene contract skipped as no-content in routeScene='{routeScene.name}'.");
                    EmitSnapshot(
                        snapshots,
                        "activity_scene_contract_skipped_no_content",
                        source,
                        reason,
                        $"'{definition.ActivityId}' activity scene contract skipped as no-content in routeScene='{routeScene.name}'.");
                    return;
                }

                if (routeCandidates.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"Activity '{definition.ActivityId}' route scene scope must have at most one ActivitySceneContractAuthoring in v0. routeScene='{routeScene.name}' count='{routeCandidates.Count}'.");
                }

                selectedCandidate = routeCandidates[0];
                hasSelectedCandidate = true;
                resolutionScope = selectedCandidate.ScopeKind;
                resolutionDetail = $"routeScene='{routeScene.name}' contentScenes=[<none>] contentContracts='0' routeContracts='{routeCandidates.Count}' priority='route_scene'.";
            }

            if (!hasSelectedCandidate || selectedCandidate.Contract == null)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity scene contract resolution returned no selected candidate.");
            }

            ActivitySceneContractSnapshot contractSnapshot = selectedCandidate.Contract.BuildSnapshotOrThrow();
            if (!contractSnapshot.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity '{definition.ActivityId}' activity scene contract snapshot is invalid. scope='{resolutionScope}' scene='{selectedCandidate.SceneName}' component='{selectedCandidate.Contract.name}'.");
            }

            SessionActivityIdentity observedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(observedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySceneContractObserved,
                observedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity scene contract observed scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}' {resolutionDetail}");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_observed",
                source,
                reason,
                $"'{definition.ActivityId}' activity scene contract observed scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}' {resolutionDetail}");

            SessionActivityIdentity validatedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySceneContractValidated,
                validatedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity scene contract validated scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}'.");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_validated",
                source,
                reason,
                $"'{definition.ActivityId}' activity scene contract validated scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}'.");

            bool IsLoadedSetForCurrentEntry(
                ActivityContentLoadedSet currentLoadedSet,
                SessionActivityDefinition currentDefinition,
                int currentEntrySequence)
            {
                return currentLoadedSet.IsValid &&
                       currentLoadedSet.Identity.Stage == SessionActivityStage.ActivityContentLoadedSetReady &&
                       string.Equals(currentLoadedSet.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                       string.Equals(currentLoadedSet.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                       string.Equals(currentLoadedSet.Identity.ActivityId, currentDefinition.ActivityId, StringComparison.Ordinal) &&
                       currentLoadedSet.Identity.ActivityOrdinal == currentDefinition.ActivityOrdinal &&
                       currentLoadedSet.Identity.EntrySequence == currentEntrySequence;
            }

            void AddContractCandidates(
                List<ActivitySceneContractCandidate> candidates,
                Scene scene,
                string scopeKind,
                int contentSceneOrdinal)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    ActivitySceneContractAuthoring[] contracts =
                        roots[rootIndex].GetComponentsInChildren<ActivitySceneContractAuthoring>(true);

                    for (int contractIndex = 0; contractIndex < contracts.Length; contractIndex++)
                    {
                        ActivitySceneContractAuthoring contract = contracts[contractIndex];
                        if (contract == null)
                        {
                            continue;
                        }

                        candidates.Add(new ActivitySceneContractCandidate(
                            contract,
                            scene.name,
                            scopeKind,
                            contentSceneOrdinal));
                    }
                }
            }

            string FormatList(IReadOnlyList<string> values)
            {
                if (values == null || values.Count == 0)
                {
                    return "<none>";
                }

                return string.Join(", ", values);
            }
        }

        private readonly struct ActivitySceneContractCandidate
        {
            public ActivitySceneContractCandidate(
                ActivitySceneContractAuthoring contract,
                string sceneName,
                string scopeKind,
                int contentSceneOrdinal)
            {
                Contract = contract;
                SceneName = Normalize(sceneName);
                ScopeKind = Normalize(scopeKind);
                ContentSceneOrdinal = contentSceneOrdinal < 0 ? 0 : contentSceneOrdinal;
            }

            public ActivitySceneContractAuthoring Contract { get; }
            public string SceneName { get; }
            public string ScopeKind { get; }
            public int ContentSceneOrdinal { get; }
        }

        private static bool TryGetPolicyValue(IReadOnlyList<ActivityCapabilityPolicyEntry> metadata, string key, out string value)
        {
            value = string.Empty;
            if (metadata == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            for (int index = 0; index < metadata.Count; index++)
            {
                ActivityCapabilityPolicyEntry entry = metadata[index];
                if (string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    value = entry.Value;
                    return !string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }

        public bool TryApplyActorAttributeCommand(
            SessionActivityIdentity commandIdentity,
            string actorId,
            ActorAttributeOperation operation,
            string attributeId,
            float amount,
            float setValue,
            string source,
            string reason,
            out ActorAttributeApplyResult result)
        {
            result = default;
            string normalizedActorId = Normalize(actorId);
            ActorAttributeId runtimeAttributeId = new(Normalize(attributeId));
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            string activityId = _state.CurrentDefinition.ActivityId;
            int entrySequence = _state.CurrentEntrySequence;

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeCommandRequested' activityId='{activityId}' entrySequence='{entrySequence}' actorId='{normalizedActorId}' attributeId='{runtimeAttributeId}' operation='{operation}' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (!_state.CurrentIdentity.IsValid || !commandIdentity.IsValid || !IsSameActivityCycle(commandIdentity, _state.CurrentIdentity))
            {
                result = ActorAttributeApplyResult.Reject(default, runtimeAttributeId, "stale_or_foreign_activity_identity");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                result = ActorAttributeApplyResult.Reject(default, runtimeAttributeId, "actor_id_missing");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!TryResolveActorInstanceIdForActor(commandIdentity, normalizedActorId, normalizedSource, normalizedReason, out ActorInstanceId actorInstanceId))
            {
                result = ActorAttributeApplyResult.Reject(default, runtimeAttributeId, "actor_attribute_target_not_found");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorInstanceRuntimeId actorInstanceRuntimeId = new(actorInstanceId.Value);

            if (!_activityActorExitRuntimeState.TryGetActiveActorAttributeCapability(actorInstanceId, out ActorAttributeCapabilityState capabilityState) || !capabilityState.IsValid)
            {
                result = ActorAttributeApplyResult.Reject(actorInstanceRuntimeId, runtimeAttributeId, "actor_attribute_capability_not_ready");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!runtimeAttributeId.IsValid || !capabilityState.Endpoint.TryGetState(runtimeAttributeId, out _))
            {
                result = ActorAttributeApplyResult.Reject(actorInstanceRuntimeId, runtimeAttributeId, "actor_attribute_not_found");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeCommand command = BuildActorAttributeCommand(
                commandIdentity,
                actorInstanceRuntimeId,
                runtimeAttributeId,
                operation,
                amount,
                setValue,
                normalizedSource,
                normalizedReason);

            bool applied = capabilityState.Endpoint.TryApplyCommand(command, out result);
            if (!applied || result.Rejected || result.Failed)
            {
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!result.HasFact)
            {
                result = ActorAttributeApplyResult.Fail(actorInstanceRuntimeId, runtimeAttributeId, "attribute_changed_fact_missing");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeChangedFact fact = result.Fact;
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeChanged' activityId='{activityId}' entrySequence='{entrySequence}' actorId='{normalizedActorId}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' operation='{fact.Operation}' clamped='{fact.Clamped}' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
            return true;
        }

        public bool TryQaResetCurrentPlayerActor(
            SessionActivityIdentity commandIdentity,
            string source,
            string reason,
            out string outcomeReason)
        {
            outcomeReason = "unknown";
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRequested' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' selectionMode='CurrentSinglePlayerActor' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning || !_state.CurrentIdentity.IsValid)
            {
                outcomeReason = "actor_reset_qa_invalid_stage";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' stage='{_state.CurrentStage}' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!commandIdentity.IsValid || !IsSameActivityCycle(commandIdentity, _state.CurrentIdentity))
            {
                outcomeReason = "actor_reset_qa_stale_or_foreign_identity";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' commandIdentity='{commandIdentity}' currentIdentity='{_state.CurrentIdentity}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            IReadOnlyList<PlayerActorIdentityRecord> targets = ActivityEntryActorInventoryStage.ResolvePlayerActorCapabilityTargetsForCurrentEntry(
                (IActivityEntryActorInventoryRuntimeBridge)this,
                commandIdentity);
            if (targets == null || targets.Count == 0)
            {
                outcomeReason = "actor_reset_qa_no_active_player_actor";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (targets.Count != 1 || !targets[0].IsValid)
            {
                List<string> availableActorInstances = new();
                List<string> availableParticipants = new();
                for (int index = 0; index < targets.Count; index++)
                {
                    PlayerActorIdentityRecord candidate = targets[index];
                    if (!candidate.IsValid)
                    {
                        continue;
                    }

                    if (_activityPlayerActorRegistry.TryResolveHandleForParticipant(commandIdentity, candidate.ParticipantId, out PlayerActorRuntimeHandle candidateHandle) &&
                        candidateHandle.IsValid &&
                        candidateHandle.ActorInstanceRuntimeId.IsValid)
                    {
                        availableActorInstances.Add(candidateHandle.ActorInstanceRuntimeId.ToString());
                    }
                    else
                    {
                        availableActorInstances.Add($"unresolved_runtime_for_participant:{candidate.ParticipantId}");
                    }

                    availableParticipants.Add(candidate.ParticipantId.ToString());
                }

                outcomeReason = "actor_reset_qa_current_player_ambiguous";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' targetCount='{targets.Count}' availableActorInstances='{(availableActorInstances.Count == 0 ? "<none>" : string.Join(",", availableActorInstances))}' availableParticipantIds='{(availableParticipants.Count == 0 ? "<none>" : string.Join(",", availableParticipants))}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            PlayerActorIdentityRecord selected = targets[0];

            ActorInventoryFeedResult qaFeed = BuildActorInventoryFeedForCurrentEntry(commandIdentity, normalizedSource, normalizedReason);
            if (!TryResolvePlayerActorInstanceFromFeed(
                    qaFeed,
                    selected,
                    out GameObject instance,
                    out ActorInstanceRecord selectedPlayerInstance,
                    out PlayerActorIdentity observedIdentity,
                    out string resolutionDetail))
            {
                outcomeReason = "actor_reset_qa_player_actor_not_found";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' playerSlotId='{selected.PlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' resolutionDetail='{resolutionDetail}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            Vector3 placementPosition = instance.transform.localPosition;
            Vector3 placementEulerAngles = instance.transform.localEulerAngles;
            ActorResetTargetRef target = new(
                new ActorResetActorRef(
                    commandIdentity,
                    new ActorId(selectedPlayerInstance.ActorId),
                    new ActorInstanceRuntimeId(selectedPlayerInstance.ActorInstanceId.Value),
                    selectedPlayerInstance.Kind,
                    observedIdentity.PlayerActorId,
                    observedIdentity.PlayerSlotId),
                new[] { ActorResetGroup.Placement, ActorResetGroup.ActivityParticipation },
                placementId: string.Empty,
                placementDeclared: true,
                placementRequired: true,
                placementOptional: false,
                hasPlacement: true,
                placementPosition,
                placementEulerAngles);
            ActorResetCommand resetCommand = new(commandIdentity, new[] { target }, normalizedSource, normalizedReason);
            IReadOnlyList<ActorResetResult> results;
            try
            {
                results = _actorResetAdapter.Execute(resetCommand, commandIdentity);
            }
            catch (Exception exception)
            {
                outcomeReason = "actor_reset_qa_failed";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaFailed' reason='{outcomeReason}' playerSlotId='{selected.PlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' error='{exception.Message}'.",
                    DebugUtility.Colors.Error);
                return false;
            }

            if (results == null || results.Count != 1 || !results[0].IsValid)
            {
                outcomeReason = "actor_reset_qa_invalid_result";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' playerSlotId='{selected.PlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            outcomeReason = "actor_reset_qa_applied";
            ActorResetResult result = results[0];
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaApplied' reason='{outcomeReason}' playerSlotId='{selected.PlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' appliedGroups='{result.AppliedGroups.Count}' skippedGroups='{result.SkippedGroups.Count}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                DebugUtility.Colors.Success);
            return true;
        }

        public bool TryQaResetCurrentActivityObjects(
            SessionActivityIdentity commandIdentity,
            string source,
            string reason,
            out string outcomeReason)
        {
            outcomeReason = "unknown";
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaRequested' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning || !_state.CurrentIdentity.IsValid)
            {
                outcomeReason = "activity_object_reset_qa_invalid_stage";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaRejected' reason='{outcomeReason}' stage='{_state.CurrentStage}' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            if (!commandIdentity.IsValid || !IsSameActivityCycle(commandIdentity, _state.CurrentIdentity))
            {
                outcomeReason = "activity_object_reset_qa_stale_or_foreign_identity";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaRejected' reason='{outcomeReason}' commandIdentity='{commandIdentity}' currentIdentity='{_state.CurrentIdentity}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            SessionActivityDefinition definition = _state.CurrentDefinition;
            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            ActivityCapabilityInventory inventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult validation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool hasCanonicalInventoryForCurrentEntry =
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, commandIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, commandIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, commandIdentity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == commandIdentity.EntrySequence;

            if (!hasCanonicalInventoryForCurrentEntry)
            {
                outcomeReason = "activity_object_reset_qa_inventory_missing_or_invalid";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaRejected' reason='{outcomeReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' inventoryValid='{inventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{validation.IsValid.ToString().ToLowerInvariant()}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            ActivityResetResult resetResult;
            try
            {
                resetResult = ActivityResetStage.Execute(
                    new ActivityResetCommand(
                        new ActivityResetActivityReference(
                            commandIdentity,
                            definition.ActivityId,
                            definition.ActivityOrdinal),
                        normalizedSource,
                        normalizedReason),
                    new ActivityResetContext(discoveryResult, inventory, validation),
                    IsDiscoveryResultForCurrentResetEntry,
                    IsReportForCurrentResetEntry,
                    HasRequiredResetContributorForCurrentResetEntry,
                    Stages.ActivityEntryObjectSetupStageUtility.ResolveObjectResetEndpointsFromInventory,
                    Stages.ActivityEntryObjectSetupStageUtility.ExecuteObjectResetCommand,
                    IsObjectResetResultForCurrentResetEntry,
                    (kind, message) => DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][QA][ObjectResetStage] fact='{kind}' detail='{message}' source='{normalizedSource}' reason='{normalizedReason}'.", DebugUtility.Colors.Info),
                    (snapshotKind, message) => DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][QA][ObjectResetStage] snapshot='{snapshotKind}' detail='{message}' source='{normalizedSource}' reason='{normalizedReason}'.", DebugUtility.Colors.Info));
            }
            catch (Exception exception)
            {
                outcomeReason = "activity_object_reset_qa_failed";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaFailed' reason='{outcomeReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' error='{exception.Message}'.",
                    DebugUtility.Colors.Error);
                return false;
            }

            if (resetResult.CompletionKind == ActivityResetCompletionKind.Applied)
            {
                outcomeReason = "activity_object_reset_qa_applied";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaApplied' reason='{outcomeReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' commandCount='{resetResult.CommandCount}' appliedCount='{resetResult.AppliedCount}' skippedCount='{resetResult.SkippedCount}' failedCount='{resetResult.FailedCount}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Success);
                return true;
            }

            if (resetResult.CompletionKind == ActivityResetCompletionKind.NoCommands ||
                resetResult.CompletionKind == ActivityResetCompletionKind.NoApplicableGroups ||
                resetResult.CompletionKind == ActivityResetCompletionKind.SkippedOptional)
            {
                outcomeReason = "activity_object_reset_qa_no_commands";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaSkipped' reason='{outcomeReason}' completionKind='{resetResult.CompletionKind}' completionReason='{resetResult.CompletionReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' commandCount='{resetResult.CommandCount}' appliedCount='{resetResult.AppliedCount}' skippedCount='{resetResult.SkippedCount}' failedCount='{resetResult.FailedCount}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            outcomeReason = "activity_object_reset_qa_rejected";
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActivityObjectResetQaRejected' reason='{outcomeReason}' completionKind='{resetResult.CompletionKind}' completionReason='{resetResult.CompletionReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                DebugUtility.Colors.Warning);
            return false;
        }

        private static bool TryResolvePlayerActorInstanceFromFeed(
            ActorInventoryFeedResult feed,
            PlayerActorIdentityRecord selected,
            out GameObject actorRoot,
            out ActorInstanceRecord actorInstance,
            out PlayerActorIdentity observedIdentity,
            out string resolutionDetail)
        {
            actorRoot = null;
            actorInstance = default;
            observedIdentity = null;
            if (!selected.IsValid || feed.ActorInstances == null)
            {
                resolutionDetail = BuildActorResetQaResolutionDetail(selected, feed.ActorInstances, Array.Empty<string>());
                return false;
            }

            List<string> availableActorIds = new();
            List<string> availablePlayerSlots = new();
            for (int index = 0; index < feed.ActorInstances.Count; index++)
            {
                ActorInstanceRecord instance = feed.ActorInstances[index];
                if (!instance.IsValid || instance.ActorRoot == null)
                {
                    continue;
                }

                PlayerActorIdentity identity = instance.ActorRoot.GetComponent<PlayerActorIdentity>();
                if (identity == null || !identity.IsValid)
                {
                    continue;
                }

                availableActorIds.Add(instance.ActorId);

                availablePlayerSlots.Add(identity.PlayerSlotId.ToString());
                if (identity.PlayerSlotId != selected.PlayerSlotId)
                {
                    continue;
                }

                actorRoot = instance.ActorRoot;
                actorInstance = instance;
                observedIdentity = identity;
                resolutionDetail = BuildActorResetQaResolutionDetail(selected, availableActorIds, availablePlayerSlots);
                return true;
            }

            resolutionDetail = BuildActorResetQaResolutionDetail(selected, availableActorIds, availablePlayerSlots);
            return false;
        }

        private static string BuildActorResetQaResolutionDetail(
            PlayerActorIdentityRecord requestedIdentity,
            IReadOnlyList<ActorInstanceRecord> instances,
            IReadOnlyList<string> availablePlayerSlots)
        {
            List<string> actorIds = new();
            if (instances != null)
            {
                for (int index = 0; index < instances.Count; index++)
                {
                    ActorInstanceRecord instance = instances[index];
                    if (instance.IsValid && instance.ActorRoot != null && instance.ActorRoot.GetComponent<PlayerActorIdentity>() != null)
                    {
                        actorIds.Add(instance.ActorId);
                    }
                }
            }

            return BuildActorResetQaResolutionDetail(requestedIdentity, actorIds, availablePlayerSlots);
        }

        private static string BuildActorResetQaResolutionDetail(
            PlayerActorIdentityRecord requestedIdentity,
            IReadOnlyList<string> availableActorIds,
            IReadOnlyList<string> availablePlayerSlots)
        {
            string requestedPlayerSlotId = requestedIdentity.IsValid ? requestedIdentity.PlayerSlotId.ToString() : string.Empty;
            string requestedPlayerActorId = requestedIdentity.IsValid ? requestedIdentity.PlayerActorId.ToString() : string.Empty;
            string joinedActorIds = availableActorIds == null || availableActorIds.Count == 0 ? "<none>" : string.Join(",", availableActorIds);
            string joinedPlayerSlots = availablePlayerSlots == null || availablePlayerSlots.Count == 0 ? "<none>" : string.Join(",", availablePlayerSlots);
            return $"requestedPlayerSlotId='{requestedPlayerSlotId}' requestedPlayerActorId='{requestedPlayerActorId}' availableActorIds='{joinedActorIds}' availablePlayerSlots='{joinedPlayerSlots}'";
        }

        private bool IsObjectResetResultForCurrentEntry(
            ActivityObjectResetResult result,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            SessionActivityIdentity identity = result.Command.Identity;
            return result.IsValid &&
                   identity.IsValid &&
                   string.Equals(identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ResetGroup != ActivityStateResetGroup.Unknown;
        }

        private bool IsObjectResetResultForCurrentResetEntry(
            ActivityObjectResetResult result,
            string activityId,
            int activityOrdinal,
            int entrySequence)
        {
            SessionActivityIdentity identity = result.Command.Identity;
            return result.IsValid &&
                   identity.IsValid &&
                   string.Equals(identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, activityId, StringComparison.Ordinal) &&
                   identity.ActivityOrdinal == activityOrdinal &&
                   identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ResetGroup != ActivityStateResetGroup.Unknown;
        }

        private static IActivityObjectReleaseEndpoint[] ResolveObjectReleaseEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectReleaseEndpoint>();
            }

            List<IActivityObjectReleaseEndpoint> endpoints = new();
            HashSet<IActivityObjectReleaseEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.ReleaseEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectReleaseEndpointReference>(capability.CapabilityId, out ActivityObjectReleaseEndpointReference runtimeReference) ||
                    runtimeReference.Endpoint == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Endpoint))
                {
                    endpoints.Add(runtimeReference.Endpoint);
                }
            }

            return endpoints.ToArray();
        }

        private static IReadOnlyList<ActivityCapabilityPermissionReceiverReference> ResolvePermissionReceiversFromInventory(
            ActivityCapabilityInventory inventory,
            SessionActivityIdentity activeIdentity)
        {
            List<ActivityCapabilityPermissionReceiverReference> receivers = new();
            if (!inventory.IsValid)
            {
                return receivers;
            }

            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.PermissionTarget)
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityCapabilityPermissionReceiverReference>(capability.CapabilityId, out ActivityCapabilityPermissionReceiverReference runtimeReference) ||
                    runtimeReference == null ||
                    !runtimeReference.IsValid ||
                    runtimeReference.Receiver == null)
                {
                    if (capability.Required)
                    {
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CapabilityPermission] Missing runtime reference for required capabilityId='{capability.CapabilityId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
                    }

                    continue;
                }

                receivers.Add(runtimeReference);
            }

            return receivers;
        }

        private IActivityObjectSnapshotProvider[] ResolveObjectSnapshotProviders(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return Array.Empty<IActivityObjectSnapshotProvider>();
            }

            List<IActivityObjectSnapshotProvider> providers = new();
            ActivityObjectContributor contributor = targetObject.GetComponent<ActivityObjectContributor>();
            bool includeChildren = contributor != null && contributor.IncludeChildrenForEndpointDiscovery;
            MonoBehaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : targetObject.GetComponents<MonoBehaviour>();

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActivityObjectSnapshotProvider provider)
                {
                    providers.Add(provider);
                }
            }

            return providers.ToArray();
        }

        private IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpoints(GameObject targetObject, ActivityObjectContributionReport report)
        {
            if (targetObject == null)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            ActivityObjectContributor contributor = targetObject.GetComponent<ActivityObjectContributor>();
            bool includeChildren = contributor != null && contributor.IncludeChildrenForEndpointDiscovery;
            MonoBehaviour[] behaviours = includeChildren
                ? targetObject.GetComponentsInChildren<MonoBehaviour>(true)
                : targetObject.GetComponents<MonoBehaviour>();

            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActivityObjectSnapshotRestoreEndpoint endpoint)
                {
                    endpoints.Add(endpoint);
                }
            }

            return endpoints.ToArray();
        }

        private static bool TryResolveSupportingSnapshotProvider(
            string targetId,
            IActivityObjectSnapshotProvider[] providers,
            out IActivityObjectSnapshotProvider resolvedProvider)
        {
            resolvedProvider = null;
            if (providers == null || providers.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < providers.Length; index++)
            {
                IActivityObjectSnapshotProvider provider = providers[index];
                if (provider == null || !provider.Supports(targetId))
                {
                    continue;
                }

                resolvedProvider = provider;
                return true;
            }

            return false;
        }

        private static bool IsSamePermissionScope(
            SessionActivityIdentity activityIdentity,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity)
        {
            return activityIdentity.IsValid &&
                   receiverIdentity.IsValid &&
                   string.Equals(activityIdentity.PipelineId, receiverIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(activityIdentity.SessionId, receiverIdentity.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(activityIdentity.ActivityId, receiverIdentity.ActivityId, StringComparison.Ordinal) &&
                   activityIdentity.EntrySequence == receiverIdentity.EntrySequence;
        }

        private static bool TryResolveSupportingSnapshotRestoreEndpoint(
            string targetId,
            IActivityObjectSnapshotRestoreEndpoint[] endpoints,
            out IActivityObjectSnapshotRestoreEndpoint resolvedEndpoint)
        {
            resolvedEndpoint = null;
            if (endpoints == null || endpoints.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < endpoints.Length; index++)
            {
                IActivityObjectSnapshotRestoreEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(targetId))
                {
                    continue;
                }

                resolvedEndpoint = endpoint;
                return true;
            }

            return false;
        }

        private static string ResolveSnapshotContractFailureReason(
            string providerFailureReason,
            string restoreFailureReason,
            bool providerFound,
            bool restoreFound)
        {
            if (!providerFound && !restoreFound)
            {
                return "snapshot_provider_and_restore_endpoint_missing";
            }

            if (!providerFound)
            {
                return "snapshot_provider_missing";
            }

            if (!restoreFound)
            {
                return "snapshot_restore_endpoint_missing";
            }

            if (!string.Equals(providerFailureReason, "resolved", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(providerFailureReason) ? "snapshot_provider_contract_invalid" : providerFailureReason;
            }

            if (!string.Equals(restoreFailureReason, "resolved", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(restoreFailureReason) ? "snapshot_restore_contract_invalid" : restoreFailureReason;
            }

            return "<none>";
        }

        private static string ToCoordinateSpaceToken(ActivityObjectSnapshotCoordinateSpace coordinateSpace)
        {
            return coordinateSpace == ActivityObjectSnapshotCoordinateSpace.LocalTransform
                ? "local_transform"
                : "world_transform";
        }

        private bool IsObjectSnapshotRestoreResultForCurrentEntry(
            ActivityObjectSnapshotRestoreResult result,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            ActivityObjectSnapshotRestoreCommand command = result.Command;
            SessionActivityIdentity identity = command.Identity;
            return result.IsValid &&
                   identity.IsValid &&
                   string.Equals(identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(command.TargetId);
        }

        private ActivityObjectReleaseResult ExecuteObjectReleaseCommand(
            ActivityObjectReleaseCommand command,
            IActivityObjectReleaseEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                if (command.IsRequired)
                {
                    return new ActivityObjectReleaseResult(
                        ActivityObjectReleaseResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "required_release_endpoint_missing");
                }

                return new ActivityObjectReleaseResult(
                    ActivityObjectReleaseResultKind.SkippedOptional,
                    command,
                    command.Source,
                    command.Reason,
                    "optional_release_endpoint_missing");
            }

            bool hasSupportingEndpoint = false;
            for (int index = 0; index < endpoints.Length; index++)
            {
                IActivityObjectReleaseEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.ReleaseKind))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                ActivityObjectReleaseResult result = endpoint.ApplyRelease(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectReleaseResult(
                        ActivityObjectReleaseResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "invalid_release_result");
                }

                return result;
            }

            if (command.IsRequired)
            {
                return new ActivityObjectReleaseResult(
                    ActivityObjectReleaseResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    hasSupportingEndpoint ? "required_release_not_applied" : "required_release_kind_not_supported");
            }

            return new ActivityObjectReleaseResult(
                ActivityObjectReleaseResultKind.SkippedOptional,
                command,
                command.Source,
                command.Reason,
                hasSupportingEndpoint ? "optional_release_not_applied" : "optional_release_kind_not_supported");
        }

        private bool IsObjectReleaseResultForCurrentEntry(
            ActivityObjectReleaseResult result,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            SessionActivityIdentity identity = result.Command.Identity;
            return result.IsValid &&
                   identity.IsValid &&
                   string.Equals(identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   identity.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(result.Command.TargetId) &&
                   result.Command.ReleaseKind != ActivityReleaseRequirementKind.Unknown;
        }

        private bool IsObjectReleaseResultAcceptedForIssuedCommand(
            ActivityObjectReleaseResult result,
            ActivityObjectReleaseCommand issuedCommand,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return IsObjectReleaseResultForCurrentEntry(result, definition, entrySequence) &&
                   issuedCommand.IsValid &&
                   string.Equals(result.Command.TargetId, issuedCommand.TargetId, StringComparison.Ordinal) &&
                   result.Command.ReleaseKind == issuedCommand.ReleaseKind;
        }

        private bool IsDiscoveryResultForCurrentEntry(
            ActivityObjectContributorDiscoveryResult result,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return result.IsValid &&
                   result.Identity.IsValid &&
                   string.Equals(result.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Identity.EntrySequence == entrySequence;
        }

        private bool IsDiscoveryResultForCurrentResetEntry(
            ActivityObjectContributorDiscoveryResult result,
            string activityId,
            int activityOrdinal,
            int entrySequence)
        {
            return result.IsValid &&
                   result.Identity.IsValid &&
                   string.Equals(result.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(result.Identity.ActivityId, activityId, StringComparison.Ordinal) &&
                   result.Identity.ActivityOrdinal == activityOrdinal &&
                   result.Identity.EntrySequence == entrySequence;
        }

        private bool IsReportForCurrentEntry(
            ActivityObjectContributionReport report,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return report.IsValid &&
                   report.Identity.IsValid &&
                   string.Equals(report.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   report.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   report.Identity.EntrySequence == entrySequence;
        }

        private bool IsReportForCurrentResetEntry(
            ActivityObjectContributionReport report,
            string activityId,
            int activityOrdinal,
            int entrySequence)
        {
            return report.IsValid &&
                   report.Identity.IsValid &&
                   string.Equals(report.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(report.Identity.ActivityId, activityId, StringComparison.Ordinal) &&
                   report.Identity.ActivityOrdinal == activityOrdinal &&
                   report.Identity.EntrySequence == entrySequence;
        }

        private bool HasRequiredResetContributorForCurrentResetEntry(
            ActivityObjectContributorDiscoveryResult result,
            string activityId,
            int activityOrdinal,
            int entrySequence)
        {
            if (!IsDiscoveryResultForCurrentResetEntry(result, activityId, activityOrdinal, entrySequence) || result.Reports.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < result.Reports.Count; index++)
            {
                ActivityObjectContributionReport report = result.Reports[index];
                if (!IsReportForCurrentResetEntry(report, activityId, activityOrdinal, entrySequence))
                {
                    continue;
                }

                if (report.Requiredness == ActivitySetupRequirementRequiredness.Required &&
                    report.SupportedResetGroups != null &&
                    report.SupportedResetGroups.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject ResolveContributorObjectOrFail(SessionActivityDefinition definition, ActivityObjectContributionReport report)
        {
            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
            for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
            {
                ActivityContentLoadedSceneRecord sceneRecord = loadedSet.Scenes[sceneIndex];
                if (!sceneRecord.IsValid || !string.Equals(sceneRecord.SceneName, report.SceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByName(sceneRecord.SceneName);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    ActivityObjectContributor[] contributors = roots[rootIndex].GetComponentsInChildren<ActivityObjectContributor>(true);
                    for (int contributorIndex = 0; contributorIndex < contributors.Length; contributorIndex++)
                    {
                        ActivityObjectContributor contributor = contributors[contributorIndex];
                        if (contributor == null)
                        {
                            continue;
                        }

                        if (string.Equals(contributor.TargetId, report.TargetId, StringComparison.Ordinal))
                        {
                            return contributor.gameObject;
                        }
                    }
                }
            }

            throw new InvalidOperationException(
                $"Activity '{definition.ActivityId}' could not resolve contributor object for targetId='{report.TargetId}' scene='{report.SceneName}'.");
        }

        private bool HasLoadedSetForCurrentEntry(
            ActivityContentLoadedSet loadedSet,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            return loadedSet.IsValid &&
                   loadedSet.Identity.Stage == SessionActivityStage.ActivityContentLoadedSetReady &&
                   string.Equals(loadedSet.Identity.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.SessionId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   loadedSet.Identity.ActivityOrdinal == definition.ActivityOrdinal &&
                   loadedSet.Identity.EntrySequence == entrySequence;
        }

        private static string FormatCapabilityKindsSummary(IReadOnlyList<ActivityCapabilityDescriptor> capabilities)
        {
            if (capabilities == null || capabilities.Count == 0)
            {
                return "<none>";
            }

            Dictionary<ActivityCapabilityKind, int> countsByKind = new();
            for (int index = 0; index < capabilities.Count; index++)
            {
                ActivityCapabilityKind kind = capabilities[index].CapabilityKind;
                countsByKind.TryGetValue(kind, out int count);
                countsByKind[kind] = count + 1;
            }

            List<ActivityCapabilityKind> kinds = new(countsByKind.Keys);
            kinds.Sort();
            List<string> segments = new(kinds.Count);
            for (int index = 0; index < kinds.Count; index++)
            {
                ActivityCapabilityKind kind = kinds[index];
                segments.Add($"{kind}:{countsByKind[kind]}");
            }

            return string.Join(",", segments);
        }

        private static string FormatValidationIssueCodes(IReadOnlyList<ActivityCapabilityInventoryValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return "<none>";
            }

            Dictionary<string, int> countsByCode = new(StringComparer.Ordinal);
            for (int index = 0; index < issues.Count; index++)
            {
                string code = string.IsNullOrWhiteSpace(issues[index].Code) ? "unknown" : issues[index].Code;
                countsByCode.TryGetValue(code, out int count);
                countsByCode[code] = count + 1;
            }

            List<string> codes = new(countsByCode.Keys);
            codes.Sort(StringComparer.Ordinal);

            List<string> segments = new(codes.Count);
            for (int index = 0; index < codes.Count; index++)
            {
                string code = codes[index];
                segments.Add($"{code}:{countsByCode[code]}");
            }

            return string.Join(",", segments);
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
                if (definition.ActivationWindowMode == ActivityWindowMode.None)
                {
                    LogPhaseBoundary("SessionActivityMaterializationCompleted", skipIdentity, command.Source, command.Reason, completed: true, detail: "phase='materialization' readiness='activity_running_no_gameplay_content'");
                }
                EmitSnapshot(snapshots, "gameplay_content_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                EmitMovementControlEnableAtRunning(definition, command, facts, snapshots, entrySequence);
                TryEmitRestartCompletedAtRunning(definition, command, facts, snapshots, entrySequence);
                return;
            }

            SessionActivityIdentity runningIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
            _state.SetCurrentIdentity(runningIdentity, SessionActivityStage.ActivityRunning);
            _state.SetExecutionState(ActivityExecutionState.Running);
            EmitFact(facts, SessionActivityFactKind.ActivityRunningEntered, runningIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            if (definition.ActivationWindowMode == ActivityWindowMode.None)
            {
                LogPhaseBoundary("SessionActivityMaterializationCompleted", runningIdentity, command.Source, command.Reason, completed: true, detail: "phase='materialization' readiness='activity_running'");
            }
            EmitSnapshot(snapshots, "activity_running_entered", command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            EmitMovementControlEnableAtRunning(definition, command, facts, snapshots, entrySequence);
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

            if (!command.Identity.IsValid || !_state.CurrentIdentity.IsValid || command.Identity.StageKey != _state.CurrentIdentity.StageKey)
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
                SessionActivityCommandResultKind.Completed,
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
            if (expected.IsValid && command.Identity.StageKey == expected.StageKey)
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
            _pendingContinuationExitTeardownCompleted = false;
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
            if (expectedIdentity.IsValid && command.Identity.StageKey == expectedIdentity.StageKey)
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

            if (kind == SessionActivityCommandKind.ResetSession)
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

        private SessionActivityIdentity BuildIdentity(string activityId, int activityOrdinal, SessionActivityStage stage, int entrySequence)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new InvalidOperationException("Activity id is invalid.");
            }

            return new SessionActivityIdentity(
                PipelineId,
                _sessionId,
                activityId,
                activityOrdinal,
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
                LogPhaseBoundary("SessionActivityRevealSkipped", _state.CurrentIdentity, command.Source, command.Reason, completed: true, detail: "transitionMode='None'");
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

            LogPhaseBoundary("SessionActivityRevealStarted", _state.CurrentIdentity, command.Source, command.Reason, detail: $"transitionMode='{_pendingTransitionResolution.Mode}' nextActivity='{next.ActivityId}'");
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
            LogPhaseBoundary("SessionActivityRevealCompleted", _state.CurrentIdentity, command.Source, command.Reason, completed: true, detail: $"transitionMode='{_pendingTransitionResolution.Mode}' nextActivity='{next.ActivityId}'");
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
                SessionActivityIdentity skippedIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, entrySequence);
                LogPhaseBoundary("SessionActivityTransitionBlackoutSkipped", skippedIdentity, command.Source, command.Reason, completed: true, detail: $"transitionMode='{_pendingTransitionResolution.Mode}'");
                return;
            }

            SessionActivityIdentity fadeInStartedIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, entrySequence);
            LogPhaseBoundary("SessionActivityTransitionBlackoutStarted", fadeInStartedIdentity, command.Source, command.Reason, detail: $"transitionMode='{_pendingTransitionResolution.Mode}'");
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
            LogPhaseBoundary("SessionActivityTransitionBlackoutCompleted", fadeInCompletedIdentity, command.Source, command.Reason, completed: true, detail: $"transitionMode='{_pendingTransitionResolution.Mode}'");
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

        private void LogPhaseBoundary(
            string phaseName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            bool completed = false,
            string detail = "")
        {
            string normalizedDetail = string.IsNullOrWhiteSpace(detail)
                ? string.Empty
                : $" {detail}";
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Phase] {phaseName} pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' stage='{identity.Stage}' source='{source}' reason='{reason}'.{normalizedDetail}",
                completed ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
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
            _state.AppendTrace($"[OBS][SessionActivityPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' entrySequence='{fact.Identity.EntrySequence}' activity='{fact.Identity.ActivityId}' executionState='{_state.CurrentExecutionState}' message=\"{fact.Message}\"");
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

        private PlayerSessionParticipationContext ResolveSessionParticipationContextOrFail(
            string activityId,
            int activityOrdinal,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            PlayerSessionParticipationContext context = _lastSessionParticipationContext;
            if (context == null || !context.IsValid)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(activityId, activityOrdinal, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{activityId}' participant binding failed missing SessionParticipationContext sessionStateId='{_sessionId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{activityId}' participant binding failed missing SessionParticipationContext.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Missing valid SessionParticipationContext activityId='{activityId}' entrySequence='{entrySequence}' sessionStateId='{_sessionId}'.");
            }

            if (context.HasSessionId && !string.Equals(context.SessionId, _sessionId, StringComparison.Ordinal))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(activityId, activityOrdinal, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{activityId}' participant binding failed foreign SessionParticipationContext sessionStateId='{_sessionId}' contextSessionId='{context.SessionId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{activityId}' participant binding failed foreign SessionParticipationContext.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Foreign SessionParticipationContext activityId='{activityId}' entrySequence='{entrySequence}' sessionStateId='{_sessionId}' contextSessionId='{context.SessionId}'.");
            }

            return context;
        }



        private void StoreActivityParticipationContext(
            string activityId,
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActivityParticipantBinding> participants,
            string source,
            string reason,
            string status)
        {
            IReadOnlyList<PlayerActivityParticipantBinding> safeParticipants = participants ?? Array.Empty<PlayerActivityParticipantBinding>();
            PlayerActivityParticipationContext context = new(
                identity,
                safeParticipants,
                source,
                reason);
            _lastActivityParticipationContext = context;
            _activityActorExitRuntimeState.StoreActivityParticipationContext(
                context,
                activityId,
                identity.EntrySequence,
                source,
                reason);
            string sessionParticipationState = _lastSessionParticipationContext != null && _lastSessionParticipationContext.IsValid
                ? "present"
                : "absent";
            int sessionRevision = _lastSessionParticipationContext?.Revision ?? 0;
            int sessionParticipants = _lastSessionParticipationContext?.ParticipantCount ?? 0;
            int activityParticipants = context.ParticipantCount;
            string bindings = FormatActivityParticipantBindings(safeParticipants);
            _state.AppendTrace(
                $"[OBS][ActivityEntryPipeline][ActivityParticipation] ActivityParticipationContextPrepared activityId='{activityId}' entrySequence='{identity.EntrySequence}' sessionParticipationContext='{sessionParticipationState}' sessionParticipationRevision='{sessionRevision}' activityParticipants='{activityParticipants}' status='{status}' source='{source}' reason='{reason}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipationContextPrepared' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{activityId}' entrySequence='{identity.EntrySequence}' stage='{identity.Stage}' sessionParticipationContext='{sessionParticipationState}' sessionParticipationRevision='{sessionRevision}' sessionParticipants='{sessionParticipants}' activityParticipationContext='{(context.IsValid ? "present" : "invalid")}' activityParticipants='{activityParticipants}' status='{status}' materializationOwner='ActivityEntryPipeline' inputBindingOwner='ActivityEntryPipeline' bindings=\"{bindings}\" source='{source}' reason='{reason}'.",
                context.IsValid ? DebugUtility.Colors.Info : DebugUtility.Colors.Warning);
        }



private bool TryBuildActivityParticipantBinding(
            ParticipantRequirement requirement,
            string source,
            string reason,
            out PlayerActivityParticipantBinding binding,
            out string resolutionReason)
        {
            binding = default;
            resolutionReason = "unknown";
            if (string.IsNullOrWhiteSpace(requirement.Requirement.RequirementId))
            {
                resolutionReason = "invalid_requirement_id";
                return false;
            }

            if (!TryResolveSessionParticipantBinding(requirement, out PlayerSessionParticipantBinding sessionParticipant, out resolutionReason))
            {
                return false;
            }

            binding = new PlayerActivityParticipantBinding(
                new PlayerActivityParticipantRequirementId(requirement.Requirement.RequirementId),
                sessionParticipant.ParticipantId,
                sessionParticipant.Role,
                sessionParticipant.PlayerSlotId,
                sessionParticipant.PlayerSelectionId,
                sessionParticipant.ActorDefinitionId,
                sessionParticipant.ActorId,
                sessionParticipant.ActorScope,
                sessionParticipant.MaterializationPolicy,
                requirement.Requirement.IsRequired,
                sessionParticipant.RequiresPlayerActor,
                sessionParticipant.RequiresPlayerInput,
                source,
                reason);
            if (!binding.IsValid)
            {
                resolutionReason = "activity_participant_binding_invalid";
                return false;
            }

            return true;
        }

        private bool TryResolveSessionParticipantBinding(
            ParticipantRequirement requirement,
            out PlayerSessionParticipantBinding binding,
            out string resolutionReason)
        {
            binding = default;
            resolutionReason = "unknown";
            if (_lastSessionParticipationContext == null ||
                !_lastSessionParticipationContext.IsValid ||
                _lastSessionParticipationContext.Participants == null)
            {
                resolutionReason = "session_participation_context_missing";
                return false;
            }

            if (!requirement.SessionParticipantId.IsValid)
            {
                resolutionReason = "session_participant_id_missing";
                return false;
            }

            if (requirement.ExpectedSessionRole == PlayerSessionParticipantRole.Unknown)
            {
                resolutionReason = "expected_session_role_missing";
                return false;
            }

            IReadOnlyList<PlayerSessionParticipantBinding> participants = _lastSessionParticipationContext.Participants;
            for (int index = 0; index < participants.Count; index++)
            {
                PlayerSessionParticipantBinding candidate = participants[index];
                if (!candidate.IsValid || candidate.ParticipantId != requirement.SessionParticipantId)
                {
                    continue;
                }

                if (candidate.Role != requirement.ExpectedSessionRole)
                {
                    resolutionReason = "session_participant_role_mismatch";
                    return false;
                }

                binding = candidate;
                resolutionReason = "resolved_by_session_participant_id";
                return true;
            }

            resolutionReason = "session_participant_id_missing";
            return false;
        }

        private static string FormatSessionParticipantId(PlayerSessionParticipantId participantId)
        {
            return participantId.IsValid ? participantId.ToString() : "<none>";
        }

        private void LogActivityParticipationBindingSkipped(
            string activityId,
            SessionActivityIdentity identity,
            string requirementId,
            PlayerSessionParticipantId participantId,
            string skipReason,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][ActivityEntryPipeline][ActivityParticipation] event='ActivityParticipationBindingSkipped' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{FormatSessionParticipantId(participantId)}' sessionParticipationContext='{(_lastSessionParticipationContext != null && _lastSessionParticipationContext.IsValid ? "present" : "absent")}' skipReason='{skipReason}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Warning);
        }

        private static string FormatActivityParticipantBindings(IReadOnlyList<PlayerActivityParticipantBinding> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return "<none>";
            }

            List<string> segments = new(participants.Count);
            for (int index = 0; index < participants.Count; index++)
            {
                PlayerActivityParticipantBinding participant = participants[index];
                segments.Add($"requirementId='{participant.RequirementId}' participantId='{participant.ParticipantId}' role='{participant.Role}' playerSlotId='{participant.PlayerSlotId}' actorDefinitionId='{participant.ActorDefinitionId}' actorId='{participant.ActorId}' materializationPolicy='{participant.MaterializationPolicy}' requiresPlayerInput='{participant.RequiresPlayerInput}'");
            }

            return string.Join(" | ", segments);
        }



        public bool TryGetSnapshotPayloadForSaveOnExit(
            string sessionStateId,
            out SessionActivitySnapshotPayload payload,
            out string failureReason)
        {
            payload = default;
            string normalizedSessionStateId = Normalize(sessionStateId);
            if (string.IsNullOrWhiteSpace(normalizedSessionStateId))
            {
                failureReason = "session_state_id_missing";
                return false;
            }

            if (!string.Equals(normalizedSessionStateId, _sessionId, StringComparison.Ordinal))
            {
                failureReason = "stale_or_foreign_session_state";
                return false;
            }

            return _activityObjectExitRuntimeState.TryGetSnapshotPayloadForSaveOnExit(
                out payload,
                out failureReason,
                _state.CurrentDefinition.ActivityId,
                _state.CurrentEntrySequence,
                "SessionActivityPipeline",
                "route_activity_save_payload_requested");
        }

        public Task<SessionActivityRouteExitTeardownResult> AwaitRouteExitTeardownAsync(
            string requestedSessionStateId,
            string source,
            string reason,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<SessionActivityRouteExitTeardownResult>(cancellationToken);
            }

            string normalizedSessionStateId = Normalize(requestedSessionStateId);
            if (!string.Equals(normalizedSessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return Task.FromResult(new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Failed,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentHandoff.IsValid,
                    "route_exit_failed_stale_or_foreign_session_activity_teardown_request",
                    $"teardown request sessionStateId='{normalizedSessionStateId}' does not match pipeline sessionStateId='{_sessionId}'."));
            }

            if (TryBuildImmediateRouteExitTeardownResult(out SessionActivityRouteExitTeardownResult immediateResult))
            {
                return Task.FromResult(immediateResult);
            }

            SessionActivityCommandResult closeResult = CloseForRouteExit(source, reason);
            if (!closeResult.IsValid || closeResult.IsRejected || closeResult.IsFailed)
            {
                return Task.FromResult(new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Failed,
                    _sessionId,
                    _state.CurrentStage,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentHandoff.IsValid,
                    "route_exit_close_for_route_exit_rejected",
                    $"CloseForRouteExit rejected. resultKind='{closeResult.Kind}' reason='{closeResult.Reason}'."));
            }

            if (TryBuildImmediateRouteExitTeardownResult(out immediateResult))
            {
                return Task.FromResult(immediateResult);
            }

            TaskCompletionSource<SessionActivityRouteExitTeardownResult> completion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRouteExitTeardownCompletion = new PendingRouteExitTeardownCompletion(
                normalizedSessionStateId,
                source,
                reason,
                completion);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    PendingRouteExitTeardownCompletion pending = _pendingRouteExitTeardownCompletion;
                    if (pending != null && ReferenceEquals(pending.Completion, completion))
                    {
                        _pendingRouteExitTeardownCompletion = null;
                    }

                    completion.TrySetCanceled(cancellationToken);
                });
            }

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][RouteExit] SessionActivityRouteExitAwaitRegistered sessionStateId='{normalizedSessionStateId}' source='{Normalize(source)}' reason='{Normalize(reason)}' stage='{_state.CurrentStage}' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}'.");
            return completion.Task;
        }

        public SessionActivityRouteExitTeardownResult RequestRouteExitTeardown(
            string requestedSessionStateId,
            string source,
            string reason)
        {
            Task<SessionActivityRouteExitTeardownResult> task = AwaitRouteExitTeardownAsync(
                requestedSessionStateId,
                source,
                reason,
                CancellationToken.None);

            if (task.IsCompletedSuccessfully)
            {
                return task.Result;
            }

            string normalizedSessionStateId = Normalize(requestedSessionStateId);
            return new SessionActivityRouteExitTeardownResult(
                SessionActivityRouteExitTeardownKind.Started,
                normalizedSessionStateId,
                _state.CurrentStage,
                _state.CurrentDefinition.ActivityId,
                _state.CurrentHandoff.IsValid,
                "route_exit_started_close_for_route_exit_accepted",
                $"Route-exit teardown started and is pending completion. stage='{_state.CurrentStage}' pendingOperation='{_state.CurrentPendingOperation}'.");
        }

        private bool TryBuildImmediateRouteExitTeardownResult(out SessionActivityRouteExitTeardownResult result)
        {
            SessionActivityStage stage = _state.CurrentStage;
            bool hasPendingHandoff = _state.CurrentHandoff.IsValid;

            if (!_state.HasStarted)
            {
                result = new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.NotRequired,
                    _sessionId,
                    stage,
                    _state.CurrentDefinition.ActivityId,
                    hasPendingHandoff,
                    "route_exit_not_required_no_active_session_activity",
                    "Pipeline is not started.");
                return true;
            }

            if ((stage == SessionActivityStage.Deactivation ||
                 stage == SessionActivityStage.Completed ||
                 stage == SessionActivityStage.ClosedForRouteExit) &&
                !hasPendingHandoff)
            {
                result = new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Completed,
                    _sessionId,
                    stage,
                    _state.CurrentDefinition.ActivityId,
                    false,
                    "route_exit_completed",
                    "SessionActivity route-exit teardown is already completed.");
                return true;
            }

            result = default;
            return false;
        }

        private void CompletePendingRouteExitTeardownIfAny(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            if (_pendingRouteExitTeardownCompletion == null || !_pendingRouteExitTeardownCompletion.IsValid)
            {
                _pendingRouteExitTeardownCompletion = null;
                return;
            }

            PendingRouteExitTeardownCompletion pending = _pendingRouteExitTeardownCompletion;
            _pendingRouteExitTeardownCompletion = null;
            SessionActivityRouteExitTeardownResult result = new(
                SessionActivityRouteExitTeardownKind.Completed,
                _sessionId,
                SessionActivityStage.ClosedForRouteExit,
                identity.ActivityId,
                _state.CurrentHandoff.IsValid,
                "route_exit_completed",
                "SessionActivity route-exit teardown completed by pipeline closure.");
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][RouteExit] SessionActivityRouteExitAwaitCompleted result='{result}' source='{Normalize(source)}' reason='{Normalize(reason)}'.");
            pending.Completion.TrySetResult(result);
        }

        private void FailPendingRouteExitTeardownCompletion(string reason, string detail)
        {
            if (_pendingRouteExitTeardownCompletion == null || !_pendingRouteExitTeardownCompletion.IsValid)
            {
                _pendingRouteExitTeardownCompletion = null;
                return;
            }

            PendingRouteExitTeardownCompletion pending = _pendingRouteExitTeardownCompletion;
            _pendingRouteExitTeardownCompletion = null;
            SessionActivityRouteExitTeardownResult result = new(
                SessionActivityRouteExitTeardownKind.Failed,
                _sessionId,
                _state.CurrentStage,
                _state.CurrentDefinition.ActivityId,
                _state.CurrentHandoff.IsValid,
                reason,
                detail);
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][RouteExit] SessionActivityRouteExitAwaitFailed result='{result}'.");
            pending.Completion.TrySetResult(result);
        }

        public Task<SessionActivityVisualReadinessResult> AwaitVisualReadinessAsync(
            SessionActivityVisualReadinessRequest request,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<SessionActivityVisualReadinessResult>(cancellationToken);
            }

            if (TryBuildImmediateVisualReadinessResult(request, out SessionActivityVisualReadinessResult immediateResult))
            {
                return Task.FromResult(immediateResult);
            }

            TaskCompletionSource<SessionActivityVisualReadinessResult> completion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingVisualReadinessCompletion = new PendingVisualReadinessCompletion(request, completion);
            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][VisualReadiness] SessionActivityVisualReadinessAwaitRegistered request='{request}'.");
            return completion.Task;
        }

        private bool TryBuildImmediateVisualReadinessResult(
            SessionActivityVisualReadinessRequest request,
            out SessionActivityVisualReadinessResult result)
        {
            result = default;

            if (!request.IsValid)
            {
                result = new SessionActivityVisualReadinessResult(
                    SessionActivityVisualReadinessResultKind.Failed,
                    request.SessionStateId,
                    request.ExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    "visual_readiness_request_invalid",
                    $"SessionActivity visual readiness request is invalid. request='{request}'.");
                return true;
            }

            if (!string.Equals(request.SessionStateId, _sessionId, StringComparison.Ordinal))
            {
                result = new SessionActivityVisualReadinessResult(
                    SessionActivityVisualReadinessResultKind.RejectedForeignOrStale,
                    request.SessionStateId,
                    request.ExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    "stale_or_foreign_session_state",
                    $"Requested sessionStateId='{request.SessionStateId}' does not match pipeline sessionStateId='{_sessionId}'.");
                return true;
            }

            if (_lastVisualReadinessSignal.IsValid)
            {
                if (!string.Equals(_lastVisualReadinessSignal.RouteOperationId, request.ExpectedRouteOperationId, StringComparison.Ordinal))
                {
                    result = new SessionActivityVisualReadinessResult(
                        SessionActivityVisualReadinessResultKind.RejectedForeignOrStale,
                        request.SessionStateId,
                        request.ExpectedRouteOperationId,
                        _lastVisualReadinessSignal.Identity.ActivityId,
                        _lastVisualReadinessSignal.Identity.EntrySequence,
                        "stale_or_foreign_route_operation",
                        $"Readiness routeOperationId='{_lastVisualReadinessSignal.RouteOperationId}' does not match expectedRouteOperationId='{request.ExpectedRouteOperationId}'.");
                    return true;
                }

                if (_state.CurrentIdentity.CycleKey != _lastVisualReadinessSignal.Identity.CycleKey)
                {
                    result = new SessionActivityVisualReadinessResult(
                        SessionActivityVisualReadinessResultKind.RejectedForeignOrStale,
                        request.SessionStateId,
                        request.ExpectedRouteOperationId,
                        _state.CurrentIdentity.ActivityId,
                        _state.CurrentIdentity.EntrySequence,
                        "stale_or_foreign_activity_cycle",
                        $"Current cycle='{_state.CurrentIdentity.CycleSignature}' diverged from readiness cycle='{_lastVisualReadinessSignal.Identity.CycleSignature}'.");
                    return true;
                }

                _state.AppendTrace(
                    $"[OBS][SessionActivityPipeline][VisualReadiness] SessionActivityVisualReadinessAlreadyCompleted routeOperationId='{request.ExpectedRouteOperationId}' activityId='{_lastVisualReadinessSignal.Identity.ActivityId}' entrySequence='{_lastVisualReadinessSignal.Identity.EntrySequence}' source='{request.Source}' reason='{request.Reason}'.");

                result = new SessionActivityVisualReadinessResult(
                    SessionActivityVisualReadinessResultKind.Ready,
                    request.SessionStateId,
                    request.ExpectedRouteOperationId,
                    _lastVisualReadinessSignal.Identity.ActivityId,
                    _lastVisualReadinessSignal.Identity.EntrySequence,
                    "visual_readiness_already_completed",
                    "SessionActivity visual readiness had already completed for this route operation.");
                return true;
            }

            string observedRouteOperationId = _lastSessionParticipationContext != null && _lastSessionParticipationContext.IsValid
                ? _lastSessionParticipationContext.RouteOperationId
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(observedRouteOperationId) &&
                !string.Equals(observedRouteOperationId, request.ExpectedRouteOperationId, StringComparison.Ordinal))
            {
                result = new SessionActivityVisualReadinessResult(
                    SessionActivityVisualReadinessResultKind.RejectedForeignOrStale,
                    request.SessionStateId,
                    request.ExpectedRouteOperationId,
                    _state.CurrentIdentity.ActivityId,
                    _state.CurrentIdentity.EntrySequence,
                    "stale_or_foreign_route_operation",
                    $"Current handoff routeOperationId='{observedRouteOperationId}' does not match expectedRouteOperationId='{request.ExpectedRouteOperationId}'.");
                return true;
            }

            return false;
        }

        private void CompletePendingVisualReadinessIfMatching(
            string routeOperationId,
            SessionActivityIdentity readinessIdentity,
            string reason,
            string detail)
        {
            if (_pendingVisualReadinessCompletion == null || !_pendingVisualReadinessCompletion.IsValid)
            {
                return;
            }

            PendingVisualReadinessCompletion pending = _pendingVisualReadinessCompletion;
            if (!string.Equals(pending.Request.ExpectedRouteOperationId, Normalize(routeOperationId), StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(pending.Request.SessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return;
            }

            _pendingVisualReadinessCompletion = null;
            SessionActivityVisualReadinessResult result = new(
                SessionActivityVisualReadinessResultKind.Ready,
                pending.Request.SessionStateId,
                pending.Request.ExpectedRouteOperationId,
                readinessIdentity.ActivityId,
                readinessIdentity.EntrySequence,
                reason,
                detail);
            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][VisualReadiness] SessionActivityVisualReadinessCompleted result='{result}'.");
            LogPhaseBoundary("SessionActivityReadinessCompleted", readinessIdentity, pending.Request.Source, pending.Request.Reason, completed: true, detail: "phase='readiness' readiness='visual_ready'");
            pending.Completion.TrySetResult(result);
        }

        private void FailPendingVisualReadinessCompletion(string reason, string detail)
        {
            if (_pendingVisualReadinessCompletion == null || !_pendingVisualReadinessCompletion.IsValid)
            {
                _pendingVisualReadinessCompletion = null;
                return;
            }

            PendingVisualReadinessCompletion pending = _pendingVisualReadinessCompletion;
            _pendingVisualReadinessCompletion = null;
            SessionActivityVisualReadinessResult result = new(
                SessionActivityVisualReadinessResultKind.Failed,
                pending.Request.SessionStateId,
                pending.Request.ExpectedRouteOperationId,
                _state.CurrentDefinition.ActivityId,
                _state.CurrentEntrySequence,
                reason,
                detail);
            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][VisualReadiness] SessionActivityVisualReadinessFailed result='{result}'.");
            pending.Completion.TrySetResult(result);
        }

        private static string JoinValues(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "<none>";
            }

            List<string> sorted = new(values);
            sorted.Sort(StringComparer.Ordinal);
            return string.Join(",", sorted);
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

        void IActivityEntryLogRuntimeBridge.LogPhaseBoundary(
            string phaseName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            bool completed,
            string detail)
        {
            LogPhaseBoundary(phaseName, identity, source, reason, completed, detail);
        }

        void IActivityEntryLogRuntimeBridge.LogEntryOwnerEvent(
            string eventName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string detail)
        {
            string normalizedEventName = Normalize(eventName);
            if (string.IsNullOrWhiteSpace(normalizedEventName))
            {
                throw new InvalidOperationException("ActivityEntry owner eventName is required.");
            }

            string normalizedDetail = Normalize(detail);
            string detailSuffix = string.IsNullOrWhiteSpace(normalizedDetail)
                ? string.Empty
                : $" {normalizedDetail}";

            string message =
                $"[OBS][ActivityEntryPipeline] event='{normalizedEventName}' pipelineId='{PipelineId}' sessionStateId='{_sessionId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' stage='{identity.Stage}' source='{Normalize(source)}' reason='{Normalize(reason)}'{detailSuffix}.";

            DebugUtility.Log(typeof(SessionActivityPipeline), message, DebugUtility.Colors.Info);
            _state.AppendTrace(message);
        }

        SessionActivityIdentity IActivityEntryIdentityRuntimeBridge.BuildIdentity(SessionActivityDefinition definition, SessionActivityStage stage, int entrySequence)
        {
            return BuildIdentity(definition, stage, entrySequence);
        }

        void IActivityEntryIdentityRuntimeBridge.SetCurrentIdentity(SessionActivityIdentity identity, SessionActivityStage stage)
        {
            _state.SetCurrentIdentity(identity, stage);
        }

        void IActivityEntryFactRuntimeBridge.EmitFact(
            List<SessionActivityFact> emittedFacts,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message)
        {
            EmitFact(emittedFacts, kind, identity, source, reason, message);
        }

        void IActivityEntryFactRuntimeBridge.EmitSnapshot(
            List<SessionActivitySnapshot> emittedSnapshots,
            string snapshotKind,
            string source,
            string reason,
            string message)
        {
            EmitSnapshot(emittedSnapshots, snapshotKind, source, reason, message);
        }

        void IActivityEntryContentLoadedSetRuntimeBridge.SetCurrentActivityContentLoadedSet(ActivityContentLoadedSet loadedSet)
        {
            _state.SetCurrentActivityContentLoadedSet(loadedSet);
            string activityId = _state.CurrentDefinition.ActivityId;
            int entrySequence = _state.CurrentEntrySequence;
            _activityContentReleaseRuntimeState.StoreCurrentLoadedSet(loadedSet, activityId, entrySequence, "ActivityEntryContentLoadedSetStore", "activity_content_loaded_set_ready");
        }

        SessionActivityPendingOperation IActivityEntryContentPendingOperationRuntimeBridge.BuildActivityContentPendingOperation(
            ActivityContentLoadPlan plan,
            int entrySequence,
            ActivityContentSceneLoadCommand command)
        {
            return BuildActivityContentPendingOperation(plan, entrySequence, command);
        }

        void IActivityEntryContentPendingOperationRuntimeBridge.SetPendingOperation(SessionActivityPendingOperation operation)
        {
            _state.SetPendingOperation(operation);
        }

        void IActivityEntryContentPendingOperationRuntimeBridge.RunActivityContentOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command)
        {
            _pendingOperationRunner.RunActivityContentOperation(operation, command, this);
        }


        void IActivityEntryContentLoadedSetRuntimeBridge.ClearCurrentActivityContentLoadedSet()
        {
            _activityContentReleaseRuntimeState.ClearCurrentLoadedSet(_state.CurrentDefinition.ActivityId, _state.CurrentEntrySequence, "ActivityEntryContentLoadedSetStore", "clear_current_activity_content_loaded_set");
            _state.ClearCurrentActivityContentLoadedSet();
        }

        void IActivityEntryPreparationRuntimeBridge.ClearCurrentActivityObjectContributorDiscoveryResult()
        {
            _activityObjectExitRuntimeState.ClearContributorDiscoveryResult(
                _state.CurrentDefinition.ActivityId,
                _state.CurrentEntrySequence,
                "ActivityEntryPreparationRuntimeBridge",
                "clear_current_activity_object_contributor_discovery_result");
            _state.ClearCurrentActivityObjectContributorDiscoveryResult();
        }

        void IActivityEntryPreparationRuntimeBridge.ClearCurrentActivitySetupInventory()
        {
            _state.ClearCurrentActivitySetupInventory();
        }

        void IActivityEntryPreparationRuntimeBridge.ClearCurrentActorInventoryFeedResult()
        {
            _state.ClearCurrentActorInventoryFeedResult();
        }

        void IActivityEntryPreparationRuntimeBridge.ResetMovementControlStateForEntry()
        {
            _movementControlTargetsForCurrentEntry = Array.Empty<PlayerActorIdentityRecord>();
            _movementControlEnableAllowedForCurrentEntry = false;
            _lastMovementDisableEmissionKey = string.Empty;
        }

        void IActivityEntryPreparationRuntimeBridge.BeginActivityActorScope(SessionActivityIdentity identity)
        {
            _activitySceneActorRegistry.BeginActivityScope(identity);
        }

        void IActivityEntryPreparationRuntimeBridge.ClearActiveActorParticipations(string activityId, int entrySequence, string source, string reason)
        {
            _activityActorExitRuntimeState.ClearActiveActorParticipations(activityId, entrySequence, source, reason);
        }

        bool IActivityEntryPreparationRuntimeBridge.TryGetActivePlayerActorIdentities(SessionActivityIdentity identity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors)
        {
            return _activityPlayerActorRegistry.TryGetActiveActorIdentities(identity, out activeActors);
        }

        void IActivityEntryPreparationRuntimeBridge.EmitPredefinedVisualSetupReadyFactIfApplicable(
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            SessionActivityIdentity readinessIdentity,
            string source,
            string reason,
            string readinessPoint)
        {
            EmitPredefinedVisualSetupReadyFactIfApplicable(definition, facts, readinessIdentity, source, reason, readinessPoint);
        }

        ActivityContentLoadedSet IActivityEntryObjectSetupRuntimeBridge.GetCurrentActivityContentLoadedSet()
        {
            return _state.CurrentActivityContentLoadedSet;
        }

        ActivityObjectContributorDiscoveryResult IActivityEntryObjectSetupRuntimeBridge.GetCurrentActivityObjectContributorDiscoveryResult()
        {
            return _activityObjectExitRuntimeState.CurrentContributorDiscoveryResult;
        }

        void IActivityEntryObjectSetupRuntimeBridge.SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result)
        {
            _activityObjectExitRuntimeState.StoreContributorDiscoveryResult(
                result,
                result.IsValid ? result.Identity.ActivityId : _state.CurrentDefinition.ActivityId,
                result.IsValid ? result.Identity.EntrySequence : _state.CurrentEntrySequence,
                "ActivityEntryObjectContributorDiscoveryStage",
                "activity_object_contributor_discovery_result_stored");
            _state.SetCurrentActivityObjectContributorDiscoveryResult(result);
        }

        void IActivityEntryObjectSetupRuntimeBridge.SetCurrentActivitySetupInventory(ActivitySetupInventory inventory)
        {
            _state.SetCurrentActivitySetupInventory(inventory);
        }

        void IActivityEntryObjectSetupRuntimeBridge.SetCurrentActivityCapabilityInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation)
        {
            _activityObjectExitRuntimeState.StoreInventoryPreview(
                inventory,
                validation,
                inventory.IsValid ? inventory.Id.ActivityId : _state.CurrentDefinition.ActivityId,
                inventory.IsValid ? inventory.Id.EntrySequence : _state.CurrentEntrySequence,
                "ActivityEntryCapabilityInventoryPreviewStage",
                "activity_capability_inventory_preview_stored");
            _state.SetCurrentActivityCapabilityInventoryPreview(inventory, validation);
        }

        void IActivityEntryObjectSetupRuntimeBridge.ClearCurrentActivityCapabilityInventoryPreview()
        {
            _activityObjectExitRuntimeState.ClearInventoryState(
                _state.CurrentDefinition.ActivityId,
                _state.CurrentEntrySequence,
                "ActivityEntryCapabilityInventoryPreviewStage",
                "activity_capability_inventory_preview_cleared");
            _state.ClearCurrentActivityCapabilityInventoryPreview();
        }

        ActivitySetupInventory IActivityEntryParticipantBindingRuntimeBridge.GetCurrentActivitySetupInventory()
        {
            return _state.CurrentActivitySetupInventory;
        }

        PlayerSessionParticipationContext IActivityEntryParticipantBindingRuntimeBridge.ResolveSessionParticipationContextOrFail(
            string activityId,
            int activityOrdinal,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartActivity,
                BuildIdentity(activityId, activityOrdinal, SessionActivityStage.ActivityParticipantBindingStarted, entrySequence),
                source,
                reason);
            return ResolveSessionParticipationContextOrFail(activityId, activityOrdinal, command, facts, snapshots, entrySequence);
        }

        IReadOnlyList<SessionActivityActorMaterializationPlanEntry> IActivityEntryParticipantBindingRuntimeBridge.GetActorMaterializationPlanEntries()
        {
            return _lastActorMaterializationPlanEntries ?? Array.Empty<SessionActivityActorMaterializationPlanEntry>();
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryBuildActivityParticipantBinding(
            ParticipantRequirement requirement,
            string source,
            string reason,
            out PlayerActivityParticipantBinding binding,
            out string resolutionReason)
        {
            return TryBuildActivityParticipantBinding(requirement, source, reason, out binding, out resolutionReason);
        }

        void IActivityEntryParticipantBindingRuntimeBridge.LogActivityParticipationBindingSkipped(
            string activityId,
            SessionActivityIdentity identity,
            string requirementId,
            PlayerSessionParticipantId participantId,
            string skipReason,
            string source,
            string reason)
        {
            LogActivityParticipationBindingSkipped(activityId, identity, requirementId, participantId, skipReason, source, reason);
        }

        void IActivityEntryParticipantBindingRuntimeBridge.StoreActivityParticipationContext(
            string activityId,
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActivityParticipantBinding> participants,
            string source,
            string reason,
            string status)
        {
            StoreActivityParticipationContext(activityId, identity, participants, source, reason, status);
        }

        void IActivityEntryParticipantBindingRuntimeBridge.BeginPlayerActorActivityScope(SessionActivityIdentity identity)
        {
            _activityPlayerActorRegistry.BeginActivityScope(identity);
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryGetActivePlayerActorIdentities(
            SessionActivityIdentity identity,
            out IReadOnlyList<PlayerActorIdentityRecord> activeActors)
        {
            return _activityPlayerActorRegistry.TryGetActiveActorIdentities(identity, out activeActors);
        }

        IReadOnlyList<PlayerActivityParticipantBinding> IActivityEntryParticipantBindingRuntimeBridge.GetRetainedPlayerExitBindings(SessionActivityIdentity identity)
        {
            return _activityActorExitRuntimeState.GetActivePlayerParticipantBindings();
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryGetRetainedPlayerActorForParticipant(
            SessionActivityIdentity identity,
            PlayerSessionParticipantId participantId,
            out PlayerActorRuntimeHandle handle)
        {
            return _activityPlayerActorRegistry.TryGetRetainedForParticipant(identity, participantId, out handle);
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryGetSessionScopedPlayerActorForParticipant(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding participant,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!participant.IsValid ||
                !_sessionActorRuntimeStore.TryGetByParticipantId(identity, participant.ParticipantId, out SessionActorRuntimeEntry entry) ||
                !entry.IsValid)
            {
                return false;
            }

            PlayerActorIdentityRecord actorIdentity = new(identity, participant, PlayerActorIdentityRecord.BuildPlayerActorId(identity, participant.ActorId));
            handle = new PlayerActorRuntimeHandle(actorIdentity, entry.Instance, entry.Actor);
            return handle.IsValid;
        }

        void IActivityEntryParticipantBindingRuntimeBridge.RegisterRetainedPlayerActorParticipation(SessionActivityIdentity identity, PlayerActorRuntimeHandle handle)
        {
            _activityPlayerActorRegistry.RegisterRetainedParticipation(identity, handle);
            TrackSessionScopedHandle(handle);
        }

        void IActivityEntryParticipantBindingRuntimeBridge.RegisterMaterializedPlayerActor(PlayerActorRuntimeHandle handle)
        {
            _activityPlayerActorRegistry.RegisterMaterialized(handle);
            TrackSessionScopedHandle(handle);
        }

        IReadOnlyList<PlayerActorMaterializationRecord> IActivityEntryParticipantBindingRuntimeBridge.ExecutePlayerActorMaterialization(
            PlayerActorMaterializationCommand command,
            SessionActivityIdentity identity)
        {
            return _playerActorMaterializationAdapter.Execute(command, identity);
        }

        IReadOnlyList<PlayerActorParticipationEnterRecord> IActivityEntryParticipantBindingRuntimeBridge.ExecutePlayerActorParticipationEnter(
            PlayerActorParticipationEnterCommand command,
            SessionActivityIdentity identity)
        {
            return _playerActorParticipationAdapter.Execute(command, identity, _activityPlayerActorRegistry);
        }

        IReadOnlyList<ActorResetResult> IActivityEntryParticipantBindingRuntimeBridge.ExecuteActorReset(ActorResetCommand command, SessionActivityIdentity identity)
        {
            return _actorResetAdapter.Execute(command, identity);
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryResolvePlayerActorHandleForParticipant(
            SessionActivityIdentity identity,
            PlayerSessionParticipantId participantId,
            out PlayerActorRuntimeHandle handle)
        {
            return _activityPlayerActorRegistry.TryResolveHandleForParticipant(identity, participantId, out handle);
        }

        bool IActivityEntryParticipantBindingRuntimeBridge.TryResolvePlacementMarkerFromCurrentEntry(
            SessionActivityIdentity identity,
            string placementId,
            out Vector3 position,
            out Vector3 eulerAngles,
            out string resolutionReason)
        {
            position = Vector3.zero;
            eulerAngles = Vector3.zero;
            resolutionReason = string.Empty;

            string normalizedPlacementId = Normalize(placementId);
            if (!identity.IsValid)
            {
                resolutionReason = "invalid_activity_identity";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedPlacementId))
            {
                resolutionReason = "placement_id_missing";
                return false;
            }

            int matchCount = 0;
            string resolvedSources = string.Empty;
            string missingScenes = string.Empty;
            string searchedSources = string.Empty;

            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
            if (loadedSet.IsValid && loadedSet.Identity.CycleKey == identity.CycleKey)
            {
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    ActivityContentLoadedSceneRecord record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid || string.IsNullOrWhiteSpace(record.SceneName))
                    {
                        continue;
                    }

                    searchedSources = AppendCsv(searchedSources, $"ActivityContent:{record.SceneName}");
                    Scene scene = SceneManager.GetSceneByName(record.SceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        missingScenes = AppendCsv(missingScenes, record.SceneName);
                        continue;
                    }

                    if (TryResolvePlacementMarkerInScene(
                        scene,
                        normalizedPlacementId,
                        "ActivityContent",
                        ref matchCount,
                        ref position,
                        ref eulerAngles,
                        ref resolvedSources,
                        out resolutionReason))
                    {
                        return false;
                    }
                }
            }
            else
            {
                searchedSources = AppendCsv(searchedSources, "ActivityContent:<missing_or_stale>");
            }

            Scene routeScene = SceneManager.GetActiveScene();
            if (routeScene.IsValid() && routeScene.isLoaded)
            {
                searchedSources = AppendCsv(searchedSources, $"RouteScene:{routeScene.name}");
                if (TryResolvePlacementMarkerInScene(
                    routeScene,
                    normalizedPlacementId,
                    "RouteScene",
                    ref matchCount,
                    ref position,
                    ref eulerAngles,
                    ref resolvedSources,
                    out resolutionReason))
                {
                    return false;
                }
            }
            else
            {
                searchedSources = AppendCsv(searchedSources, "RouteScene:<invalid_or_unloaded>");
            }

            if (matchCount == 1)
            {
                resolutionReason = $"resolved_from_authorized_placement_sources;source={resolvedSources}";
                return true;
            }

            string missingDetail = string.IsNullOrWhiteSpace(missingScenes)
                ? string.Empty
                : $";missingScenes={missingScenes}";
            resolutionReason = $"no_placement_marker_found_in_authorized_sources;searchedSources={searchedSources}{missingDetail}";
            return false;
        }

        private static bool TryResolvePlacementMarkerInScene(
            Scene scene,
            string normalizedPlacementId,
            string sourceKind,
            ref int matchCount,
            ref Vector3 position,
            ref Vector3 eulerAngles,
            ref string resolvedSources,
            out string failureReason)
        {
            failureReason = string.Empty;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                PlayerActorPlacementMarker[] markers = root.GetComponentsInChildren<PlayerActorPlacementMarker>(true);
                for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                {
                    PlayerActorPlacementMarker marker = markers[markerIndex];
                    if (marker == null || !marker.IsValid)
                    {
                        continue;
                    }

                    string markerId = Normalize(marker.PlacementId);
                    if (!string.Equals(markerId, normalizedPlacementId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matchCount++;
                    if (matchCount > 1)
                    {
                        failureReason = $"duplicate_placement_marker_in_authorized_sources;placementId={normalizedPlacementId};sources={resolvedSources},{sourceKind}:{scene.name}";
                        return true;
                    }

                    position = marker.transform.position;
                    eulerAngles = marker.transform.rotation.eulerAngles;
                    resolvedSources = AppendCsv(resolvedSources, $"{sourceKind}:{scene.name}:{marker.name}");
                }
            }

            return false;
        }

        private static string AppendCsv(string current, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return current ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(current)
                ? value
                : $"{current},{value}";
        }

        ActivitySceneActorRegistry IActivityEntryActorInventoryRuntimeBridge.GetActivitySceneActorRegistry()
        {
            return _activitySceneActorRegistry;
        }

        ActivityPlayerActorRegistry IActivityEntryActorInventoryRuntimeBridge.GetActivityPlayerActorRegistry()
        {
            return _activityPlayerActorRegistry;
        }

        SessionActorRuntimeStore IActivityEntryActorInventoryRuntimeBridge.GetSessionActorRuntimeStore()
        {
            return _sessionActorRuntimeStore;
        }

        PlayerActivityParticipationContext IActivityEntryActorInventoryRuntimeBridge.GetCurrentActivityParticipationContext()
        {
            return _lastActivityParticipationContext;
        }

        ActorInventoryFeedResult IActivityEntryActorInventoryRuntimeBridge.GetCurrentActorInventoryFeedResult()
        {
            return _state.CurrentActorInventoryFeedResult;
        }

        private void TrackSessionScopedHandle(PlayerActorRuntimeHandle handle)
        {
            SessionActorRuntimeEntry entry = SessionActorRuntimeStore.FromPlayerHandle(handle);
            if (entry.IsValid)
            {
                _sessionActorRuntimeStore.Register(entry);
            }
        }

        void IActivityEntryActorInventoryRuntimeBridge.SetCurrentActorInventoryFeedResult(ActorInventoryFeedResult result)
        {
            _state.SetCurrentActorInventoryFeedResult(result);
            _activityActorExitRuntimeState.StoreActorInventoryFeedResult(
                result,
                result.Identity.ActivityId,
                result.Identity.EntrySequence,
                "ActivityEntryActorInventoryStage",
                "actor_inventory_feed_result_stored");
        }

        void IActivityEntryActorInventoryRuntimeBridge.ClearCurrentActorInventoryFeedResult()
        {
            SessionActivityIdentity currentIdentity = _state.CurrentIdentity;
            _state.ClearCurrentActorInventoryFeedResult();
            _activityActorExitRuntimeState.ClearActorInventoryFeedResult(
                currentIdentity.ActivityId,
                currentIdentity.EntrySequence,
                "ActivityEntryActorInventoryStage",
                "clear_current_actor_inventory_feed_result");
        }


        ActivityCapabilityInventory IActivityEntryPermissionTargetRuntimeBridge.GetCurrentActivityCapabilityInventoryPreview()
        {
            return _state.CurrentActivityCapabilityInventoryPreview;
        }

        void IActivityEntryPermissionTargetRuntimeBridge.BeginPermissionScope(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Permission scope identity is invalid.");
            }

            _permissionRuntime.BeginPermissionScope(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.EntrySequence);
        }

        void IActivityEntryPermissionTargetRuntimeBridge.ReplacePermissionReceivers(IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers)
        {
            _permissionRuntime.ReplaceReceivers(receivers ?? Array.Empty<ActivityCapabilityPermissionReceiverReference>());
        }

        ActivityPlayerActorRegistry IActivityEntryMovementBindingRuntimeBridge.GetActivityPlayerActorRegistry()
        {
            return _activityPlayerActorRegistry;
        }

        IMovementBindingAdapter IActivityEntryMovementBindingRuntimeBridge.GetMovementBindingAdapter()
        {
            return _movementBindingAdapter;
        }

        IReadOnlyList<PlayerActorIdentityRecord> IActivityEntryMovementBindingRuntimeBridge.ResolveRetainedMovementTargets(SessionActivityIdentity identity)
        {
            return ResolveRetainedMovementTargetsOrEmpty(identity);
        }

        void IActivityEntryMovementBindingRuntimeBridge.SetMovementControlTargets(IReadOnlyList<PlayerActorIdentityRecord> targets, bool enableAllowed)
        {
            _movementControlTargetsForCurrentEntry = targets ?? Array.Empty<PlayerActorIdentityRecord>();
            _movementControlEnableAllowedForCurrentEntry = enableAllowed && _movementControlTargetsForCurrentEntry.Count > 0;
        }

        ActivitySetupInventory IActivityEntryCameraBindingRuntimeBridge.GetCurrentActivitySetupInventory()
        {
            return _state.CurrentActivitySetupInventory;
        }

        bool IActivityEntryCameraBindingRuntimeBridge.TryGetCurrentActivityCapabilityInventory(
            SessionActivityIdentity identity,
            out ActivityCapabilityInventory inventory,
            out ActivityCapabilityInventoryValidationResult validation)
        {
            inventory = _state.CurrentActivityCapabilityInventoryPreview;
            validation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            return
                identity.IsValid &&
                inventory.IsValid &&
                validation.IsValid &&
                string.Equals(inventory.Id.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.SessionStateId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(inventory.Id.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                inventory.Id.EntrySequence == identity.EntrySequence;
        }

        IReadOnlyList<PlayerActivityParticipantBinding> IActivityEntryCameraBindingRuntimeBridge.GetActivityParticipantBindings()
        {
            if (_lastActivityParticipationContext == null ||
                !_lastActivityParticipationContext.IsValid ||
                _lastActivityParticipationContext.Participants == null ||
                _lastActivityParticipationContext.Participants.Count == 0)
            {
                return Array.Empty<PlayerActivityParticipantBinding>();
            }

            return _lastActivityParticipationContext.Participants;
        }

        bool IActivityEntryCameraBindingRuntimeBridge.TryResolvePlayerActorHandle(
            SessionActivityIdentity identity,
            PlayerActivityParticipantBinding binding,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!identity.IsValid || !binding.IsValid || !binding.RequiresPlayerActor)
            {
                return false;
            }

            try
            {
                handle = _activityPlayerActorRegistry.ResolveActiveHandleOrFail(identity, binding.ParticipantId);
                return handle.IsValid;
            }
            catch (InvalidOperationException)
            {
                handle = default;
                return false;
            }
        }

        bool IActivityEntryCameraBindingRuntimeBridge.TryGetActivityCameraPreparationExecutor(out IActivityCameraPreparationExecutor executor)
        {
            return DependencyManager.Provider.TryGetGlobal<IActivityCameraPreparationExecutor>(out executor) && executor != null;
        }





        ActorPresentationResult IActivityExitActorTeardownRuntimeBridge.ReleaseActorPresentation(ActorPresentationRuntimeHandle handle, string source, string reason)
        {
            return _actorPresentationMaterializationAdapter.Release(new ActorPresentationReleaseCommand(handle, source, reason));
        }

        void IActivityExitActorTeardownRuntimeBridge.ClearActorPresentationRegistryHandle(SessionActivityIdentity identity, ActorPresentationCapabilityState state)
        {
            if (!state.IsValid)
            {
                return;
            }

            try
            {
                _activitySceneActorRegistry.ClearPresentationHandle(identity, state.ActorId);
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationRegistryHandleClearRequested' owner='ActivitySceneActorRegistry' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' actorId='{Normalize(state.ActorId)}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' source='ActivityExitActorTeardownStage' reason='actor_presentation_registry_handle_clear'.",
                    DebugUtility.Colors.Info);
            }
            catch (InvalidOperationException)
            {
                DebugUtility.LogVerbose<SessionActivityPipeline>(
                    $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationRegistryHandleClearSkipped' owner='ActivitySceneActorRegistry' activityId='{Normalize(identity.ActivityId)}' entrySequence='{identity.EntrySequence}' actorId='{Normalize(state.ActorId)}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' source='ActivityExitActorTeardownStage' reason='actor_not_registered_in_presentation_registry'.",
                    DebugUtility.Colors.Info);
            }
        }


        IReadOnlyList<PlayerActorParticipationExitRecord> IActivityExitActorTeardownRuntimeBridge.ExecutePlayerActorParticipationExit(
            PlayerActorParticipationExitCommand command,
            SessionActivityIdentity identity)
        {
            return _playerActorParticipationAdapter.Execute(command, identity, _activityPlayerActorRegistry);
        }

        ActivityCapabilityInventory IActivityEntryActorPresentationRuntimeBridge.GetCurrentActivityCapabilityInventoryPreview()
        {
            return _state.CurrentActivityCapabilityInventoryPreview;
        }

        bool IActivityEntryActorPresentationRuntimeBridge.TryGetActiveActorPresentationHandle(
            ActorPresentationEndpointReference presentationReference,
            out ActorPresentationRuntimeHandle handle)
        {
            return TryGetActivePresentationHandle(presentationReference, out handle);
        }

        void IActivityEntryActorPresentationRuntimeBridge.StoreActiveActorPresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle)
        {
            StoreActivePresentationHandle(identity, presentationReference, handle);
        }

        void IActivityEntryActorPresentationRuntimeBridge.SyncActiveActorPresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle)
        {
            SyncActorPresentationRegistryHandle(identity, presentationReference, handle);
        }

        void IActivityEntryActorPresentationRuntimeBridge.ReleaseActorPresentationBeforeRematerialization(
            SessionActivityIdentity identity,
            string source,
            string reason,
            ActorInstanceId actorInstanceRuntimeId,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            SessionActivityCommand releaseCommand = new(
                SessionActivityCommandKind.StartActivity,
                identity,
                source,
                reason);
            EmitActorPresentationReleaseGenericStage(
                _state.CurrentDefinition,
                releaseCommand,
                facts,
                snapshots,
                identity.EntrySequence,
                ActorPresentationReleaseRail.BeforeRematerialization,
                actorInstanceRuntimeId);
        }

        ActivityCapabilityInventory IActivityEntryActorAttributeRuntimeBridge.GetCurrentActivityCapabilityInventoryPreview()
        {
            return _state.CurrentActivityCapabilityInventoryPreview;
        }

        void IActivityEntryActorAttributeRuntimeBridge.StoreActiveActorAttributeCapability(
            SessionActivityIdentity identity,
            ActorAttributeEndpointReference attributeReference,
            ActorAttributeEndpoint endpoint)
        {
            if (attributeReference == null || !attributeReference.IsValid || endpoint == null)
            {
                return;
            }

            _activityActorExitRuntimeState.StoreActiveActorAttributeCapability(
                new ActorAttributeCapabilityState(
                    attributeReference.ActorInstanceRuntimeId,
                    attributeReference.ActorId,
                    endpoint),
                identity.ActivityId,
                identity.EntrySequence,
                "ActivityEntryActorAttributeStage",
                "store_active_actor_attribute_capability");
        }

        void IActivityEntryActorAttributeRuntimeBridge.RemoveActiveActorAttributeCapability(ActorInstanceId actorInstanceRuntimeId)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activityActorExitRuntimeState.RemoveActiveActorAttributeCapability(actorInstanceRuntimeId, _state.CurrentDefinition.ActivityId, _state.CurrentEntrySequence, "ActivityEntryActorAttributeStage", "remove_active_actor_attribute_capability");
        }

        ActorParticipationReadinessEvaluation IActivityEntryActorParticipationRuntimeBridge.EvaluateActorParticipationReadiness(
            SessionActivityIdentity identity,
            ActorInstanceRecord instance)
        {
            if (!instance.IsValid || instance.ActorRoot == null || instance.RuntimeActor == null)
            {
                return new ActorParticipationReadinessEvaluation(
                    isReady: false,
                    isFailure: true,
                    "actor_instance_invalid");
            }

            if (instance.CapabilitySurface == null)
            {
                return new ActorParticipationReadinessEvaluation(
                    isReady: false,
                    isFailure: true,
                    "actor_capability_surface_missing");
            }

            ActorPresentationEndpoint presentationEndpoint = instance.CapabilitySurface.PresentationEndpoint;
            if (presentationEndpoint != null)
            {
                ActorPresentationProfileAsset profile = presentationEndpoint.Profile;
                if (profile == null)
                {
                    return new ActorParticipationReadinessEvaluation(
                        isReady: false,
                        isFailure: true,
                        "presentation_profile_missing");
                }

                if (profile.IsRequired)
                {
                    if (!_activityActorExitRuntimeState.TryGetActivePresentationState(instance.ActorInstanceId, out ActorPresentationCapabilityState presentationState) || !presentationState.IsValid)
                    {
                        return new ActorParticipationReadinessEvaluation(
                            isReady: false,
                            isFailure: true,
                            "required_presentation_not_ready");
                    }
                }
            }

            ActorAttributeEndpoint attributeEndpoint = instance.CapabilitySurface.AttributeEndpoint;
            if (attributeEndpoint != null)
            {
                if (!_activityActorExitRuntimeState.TryGetActiveActorAttributeCapability(instance.ActorInstanceId, out ActorAttributeCapabilityState capabilityState))
                {
                    return new ActorParticipationReadinessEvaluation(
                        isReady: false,
                        isFailure: true,
                        "required_attribute_not_ready");
                }

                if (!capabilityState.IsValid || capabilityState.Endpoint != attributeEndpoint)
                {
                    return new ActorParticipationReadinessEvaluation(
                        isReady: false,
                        isFailure: true,
                        "required_attribute_capability_invalid");
                }
            }

            return new ActorParticipationReadinessEvaluation(
                isReady: true,
                isFailure: false,
                "ready");
        }

        void IActivityEntryActorParticipationRuntimeBridge.StoreActiveActorParticipation(ActorInstanceId actorInstanceRuntimeId)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                return;
            }

            _activityActorExitRuntimeState.StoreActiveActorParticipation(actorInstanceRuntimeId, _state.CurrentDefinition.ActivityId, _state.CurrentEntrySequence, "ActivityEntryActorParticipationStage", "store_active_actor_participation");
        }

    }
}
