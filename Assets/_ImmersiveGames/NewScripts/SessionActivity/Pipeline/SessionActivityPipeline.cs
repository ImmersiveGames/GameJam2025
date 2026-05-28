using System;
using System.Collections.Generic;
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
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityPipeline : ISessionActivityEntryHandoffReceiver, ISessionActivityPendingOperationCallback, ISessionActivitySnapshotPayloadProvider
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
        private readonly ActorPresentationPlanResolver _actorPresentationPlanResolver;
        private readonly IActorPresentationMaterializationAdapter _actorPresentationMaterializationAdapter;
        private readonly ActivityPlayerActorRegistry _activityPlayerActorRegistry;
        private readonly ActivityNonPlayerActorRegistry _activityNonPlayerActorRegistry;
        private readonly HashSet<ActorInstanceId> _activeActorParticipationsByActorInstanceId = new();
        private readonly Dictionary<ActorInstanceId, ActorPresentationCapabilityState> _activeActorPresentationByActorInstanceId = new();
        private readonly Dictionary<ActorInstanceId, ActorAttributeCapabilityState> _activeActorAttributeCapabilitiesByActorInstanceId = new();
        private readonly ActivitySetupInventoryBuilder _activitySetupInventoryBuilder;
        private readonly ActivitySetupInventoryValidator _activitySetupInventoryValidator;
        private readonly ActivityCapabilityInventoryCoordinator _activityCapabilityInventoryCoordinator;
        private readonly IActivityCapabilityPermissionRuntime _permissionRuntime;
        private readonly string _sessionId;
        private PendingNavigationTransition _pendingNavigationTransition;
        private SessionActivityRouteTransitionContext _routeTransitionContext;
        private SessionActivityTransitionResolution _pendingTransitionResolution;
        private bool _pendingTransitionCurtainReveal;
        private bool _pendingTransitionCurtainClosed;
        private bool _pendingTransitionLoadingVisible;
        private PendingInternalActivityTransition _pendingInternalActivityTransition;
        private PendingRestartTransition _pendingRestartTransition;
        private PendingActivityContentReleaseContext _pendingActivityContentReleaseContext;
        private bool _awaitingContinuationAfterActivityContentRelease;
        private string _pendingRestartCompletionActivityId;
        private int _pendingRestartCompletionEntrySequence;
        private SessionActivityPlayerPreparationHandoff _lastRouteSessionPlayerPreparationHandoff;
        private SessionActivityRailKind _activeRailKind;
        private PendingActivityContentLoadContext _pendingActivityContentLoadContext;
        private SessionActivitySnapshotPayload _lastSnapshotPayloadForSaveOnExit;
        private bool _lastSnapshotCaptureFailedForSaveOnExit;
        private string _lastSnapshotCaptureFailureDetail;
        private IReadOnlyList<PlayerActorIdentityRecord> _movementControlTargetsForCurrentEntry = Array.Empty<PlayerActorIdentityRecord>();
        private bool _movementControlEnableAllowedForCurrentEntry;
        private string _lastMovementDisableEmissionKey;
        private PredefinedVisualReadinessSignal _lastPredefinedVisualReadinessSignal;

        private sealed class PendingActivityContentLoadContext
        {
            public PendingActivityContentLoadContext(
                SessionActivityIdentity identity,
                string contentProfileId,
                IReadOnlyList<ActivityContentSceneEntry> entries)
            {
                Identity = identity;
                ContentProfileId = Normalize(contentProfileId);
                Entries = entries ?? Array.Empty<ActivityContentSceneEntry>();
                LoadedRecords = new List<ActivityContentLoadedSceneRecord>(Entries.Count);
                NextSceneOrdinal = 1;
            }

            public SessionActivityIdentity Identity { get; }
            public string ContentProfileId { get; }
            public IReadOnlyList<ActivityContentSceneEntry> Entries { get; }
            public List<ActivityContentLoadedSceneRecord> LoadedRecords { get; }
            public int NextSceneOrdinal { get; set; }
            public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(ContentProfileId) && Entries != null;
        }

        private readonly struct PredefinedVisualReadinessSignal
        {
            public PredefinedVisualReadinessSignal(
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


        internal readonly struct NonPlayerActorDiscoveredRecord
        {
            public NonPlayerActorDiscoveredRecord(NonPlayerActorIdentityRecord identity)
            {
                Identity = identity;
            }

            public NonPlayerActorIdentityRecord Identity { get; }
            public bool IsValid => Identity.IsValid;
        }

        internal readonly struct NonPlayerActorDiscoveryStageResult
        {
            public NonPlayerActorDiscoveryStageResult(
                bool hasAuthorizedSource,
                IReadOnlyList<NonPlayerActorDiscoveredRecord> discoveredRecords)
            {
                HasAuthorizedSource = hasAuthorizedSource;
                DiscoveredRecords = discoveredRecords ?? Array.Empty<NonPlayerActorDiscoveredRecord>();
            }

            public bool HasAuthorizedSource { get; }
            public IReadOnlyList<NonPlayerActorDiscoveredRecord> DiscoveredRecords { get; }
            public int DiscoveredCount => DiscoveredRecords?.Count ?? 0;
        }

        internal enum PlayerActorReadinessStageOutcome
        {
            Unknown = 0,
            Ready = 1,
            SkippedNoRequiredParticipant = 2,
            Failed = 3
        }

        internal readonly struct PlayerActorReadinessStageResult
        {
            public PlayerActorReadinessStageResult(
                PlayerActorReadinessStageOutcome outcome,
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

            public PlayerActorReadinessStageOutcome Outcome { get; }
            public int RequiredRequirements { get; }
            public int RequiredResolvedRequirements { get; }
            public int ActiveActorsCount { get; }
            public string ReasonCode { get; }
            public bool IsReady => Outcome == PlayerActorReadinessStageOutcome.Ready;
            public bool IsSkipped => Outcome == PlayerActorReadinessStageOutcome.SkippedNoRequiredParticipant;
            public bool IsFailed => Outcome == PlayerActorReadinessStageOutcome.Failed;
        }


        internal readonly struct PlayerInputBindingStageResult
        {
            public PlayerInputBindingStageResult(
                IReadOnlyList<PlayerInputBindingRequirement> requirements,
                int requiredCount,
                IReadOnlyList<PlayerInputBindingRecord> records,
                string reasonCode)
            {
                Requirements = requirements ?? Array.Empty<PlayerInputBindingRequirement>();
                RequiredCount = requiredCount;
                Records = records ?? Array.Empty<PlayerInputBindingRecord>();
                ReasonCode = Normalize(reasonCode);
            }

            public IReadOnlyList<PlayerInputBindingRequirement> Requirements { get; }
            public int RequiredCount { get; }
            public IReadOnlyList<PlayerInputBindingRecord> Records { get; }
            public string ReasonCode { get; }
            public bool IsSkipped => RequiredCount <= 0;
        }


        internal readonly struct MovementBindingStageResult
        {
            public MovementBindingStageResult(
                IReadOnlyList<MovementBindingRequirement> requirements,
                int requiredCount,
                IReadOnlyList<MovementBindingRecord> records,
                IReadOnlyList<PlayerActorIdentityRecord> retainedTargets,
                bool usedRetainedTargets,
                bool skippedNoRequiredMovement)
            {
                Requirements = requirements ?? Array.Empty<MovementBindingRequirement>();
                RequiredCount = requiredCount;
                Records = records ?? Array.Empty<MovementBindingRecord>();
                RetainedTargets = retainedTargets ?? Array.Empty<PlayerActorIdentityRecord>();
                UsedRetainedTargets = usedRetainedTargets;
                SkippedNoRequiredMovement = skippedNoRequiredMovement;
            }

            public IReadOnlyList<MovementBindingRequirement> Requirements { get; }
            public int RequiredCount { get; }
            public IReadOnlyList<MovementBindingRecord> Records { get; }
            public IReadOnlyList<PlayerActorIdentityRecord> RetainedTargets { get; }
            public bool UsedRetainedTargets { get; }
            public bool SkippedNoRequiredMovement { get; }
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
                ActorAttributeEndpoint endpoint,
                string pipelineIdentity,
                string activityIdentity)
            {
                ActorInstanceRuntimeId = actorInstanceRuntimeId;
                ActorId = Normalize(actorId);
                Endpoint = endpoint;
                PipelineIdentity = Normalize(pipelineIdentity);
                ActivityIdentity = Normalize(activityIdentity);
            }

            public ActorInstanceId ActorInstanceRuntimeId { get; }
            public string ActorId { get; }
            public ActorAttributeEndpoint Endpoint { get; }
            public string PipelineIdentity { get; }
            public string ActivityIdentity { get; }
            public bool IsValid =>
                ActorInstanceRuntimeId.IsValid &&
                !string.IsNullOrWhiteSpace(ActorId) &&
                Endpoint != null &&
                Endpoint.IsInitialized &&
                !string.IsNullOrWhiteSpace(PipelineIdentity) &&
                !string.IsNullOrWhiteSpace(ActivityIdentity);
        }

        private sealed class ActorParticipationReadinessPolicy : IActorParticipationReadinessPolicy
        {
            private readonly SessionActivityPipeline _owner;

            public ActorParticipationReadinessPolicy(SessionActivityPipeline owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public ActorParticipationReadinessEvaluation Evaluate(
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
                        if (!_owner._activeActorPresentationByActorInstanceId.TryGetValue(instance.ActorInstanceId, out ActorPresentationCapabilityState presentationState) || !presentationState.IsValid)
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
                    if (!_owner._activeActorAttributeCapabilitiesByActorInstanceId.TryGetValue(instance.ActorInstanceId, out ActorAttributeCapabilityState capabilityState))
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

        private sealed class PendingActivityContentReleaseContext
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
            _activityNonPlayerActorRegistry = new ActivityNonPlayerActorRegistry();
            _actorResetAdapter = new ActorResetAdapter(
                new PlayerActorResetEndpointResolver(_activityPlayerActorRegistry));
            _playerInputBindingAdapter = new PlayerInputBindingAdapter();
            _movementBindingAdapter = new MovementBindingAdapter(_permissionRuntime);
            _playerMovementControlAdapter = new PlayerMovementControlAdapter(_permissionRuntime);
            _actorPresentationPlanResolver = new ActorPresentationPlanResolver();
            _actorPresentationMaterializationAdapter = new UnityActorPresentationMaterializationAdapter();
            _activitySetupInventoryBuilder = new ActivitySetupInventoryBuilder();
            _activitySetupInventoryValidator = new ActivitySetupInventoryValidator();
            _activityCapabilityInventoryCoordinator = new ActivityCapabilityInventoryCoordinator();
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
        public bool AwaitingContinuationAfterActivityContentRelease => _awaitingContinuationAfterActivityContentRelease;
        public int PendingActivityContentReleaseEntrySequence => _pendingActivityContentReleaseContext != null && _pendingActivityContentReleaseContext.IsValid
            ? _pendingActivityContentReleaseContext.EntrySequence
            : 0;
        public string PendingActivityContentReleaseSummary => _pendingActivityContentReleaseContext != null && _pendingActivityContentReleaseContext.IsValid
            ? $"activityId='{_pendingActivityContentReleaseContext.Definition.ActivityId}', entrySequence='{_pendingActivityContentReleaseContext.EntrySequence}', nextSceneIndex='{_pendingActivityContentReleaseContext.NextSceneIndex}', totalScenes='{_pendingActivityContentReleaseContext.LoadedSet.Scenes.Count}'"
            : "<none>";
        public string SessionId => _sessionId;
        public SessionActivityRailKind ActiveRailKind => _activeRailKind;

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
            _pendingActivityContentLoadContext = null;
            _pendingActivityContentReleaseContext = null;
            _awaitingContinuationAfterActivityContentRelease = false;
            _lastSnapshotPayloadForSaveOnExit = default;
            _lastSnapshotCaptureFailedForSaveOnExit = false;
            _lastSnapshotCaptureFailureDetail = string.Empty;
            _lastPredefinedVisualReadinessSignal = default;
            _lastRouteSessionPlayerPreparationHandoff = handoff.PlayerPreparation;
            _activeRailKind = SessionActivityRailKind.ActivityEntryRail;
            _activityPlayerActorRegistry.ClearAllRouteRetained();
            _activityNonPlayerActorRegistry.ClearAllRouteRetained();
            _activeActorParticipationsByActorInstanceId.Clear();
            _activeActorPresentationByActorInstanceId.Clear();            _activeActorAttributeCapabilitiesByActorInstanceId.Clear();
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
            SessionActivityDefinition definition,
            int entrySequence,
            ActivityContentSceneLoadCommand command)
        {
            return new SessionActivityPendingOperation(
                command.OperationId,
                _state.PipelineId,
                _state.SessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                SessionActivityPendingWindowKind.None,
                SessionActivityPendingOperationKind.ActivityContentSceneLoad,
                command.SceneKey != null ? command.SceneKey.name : string.Empty,
                command.SceneName,
                command.Source,
                command.Reason);
        }

        private SessionActivityPendingOperation BuildActivityContentReleasePendingOperation(
            SessionActivityDefinition definition,
            int entrySequence,
            ActivityContentSceneUnloadCommand command)
        {
            return new SessionActivityPendingOperation(
                command.OperationId,
                _state.PipelineId,
                _state.SessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                SessionActivityPendingWindowKind.None,
                SessionActivityPendingOperationKind.ActivityContentSceneUnload,
                command.SceneKey != null ? command.SceneKey.name : string.Empty,
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
            if (_pendingActivityContentReleaseContext != null && _pendingActivityContentReleaseContext.IsValid)
            {
                return true;
            }

            EmitObjectSnapshotCaptureStage(current, command, facts, snapshots, currentEntrySequence);
            EmitObjectReleaseStage(current, command, facts, snapshots, currentEntrySequence);

            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
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
                EmitObjectContributorUnregisterStage(
                    current,
                    command,
                    facts,
                    snapshots,
                    currentEntrySequence,
                    SessionActivityStage.ActivityContentReleaseSkippedNoContent);
                SessionActivityIdentity completedIdentity = BuildIdentity(current, SessionActivityStage.ActivityContentReleaseCompleted, currentEntrySequence);
                _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityContentReleaseCompleted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentReleaseCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' activity content release completed scenes='0' status='SkippedNoContent'.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_release_completed",
                    command.Source,
                    command.Reason,
                    $"'{current.ActivityId}' activity content release completed scenes='0' status='SkippedNoContent'.");
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

            _pendingActivityContentReleaseContext = new PendingActivityContentReleaseContext(
                current,
                currentEntrySequence,
                loadedSet,
                command.Source,
                command.Reason);
            _awaitingContinuationAfterActivityContentRelease = true;

            ExecuteNextActivityContentSceneRelease(_pendingActivityContentReleaseContext, command, facts, snapshots);
            return true;
        }

        private void EmitObjectReleaseStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectExitStage().Release(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitObjectReleaseStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            SessionActivityIdentity releaseIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentReleaseStarted, entrySequence);
            _state.SetCurrentIdentity(releaseIdentity, SessionActivityStage.ActivityContentReleaseStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ObjectReleaseStarted,
                releaseIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release started.");
            EmitSnapshot(
                snapshots,
                "object_release_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release started.");

            if (!discoveryResult.IsValid ||
                !IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence) ||
                discoveryResult.Reports.Count == 0)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectReleaseCompleted,
                    releaseIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release completed with no contributors for current entry.");
                EmitSnapshot(
                    snapshots,
                    "object_release_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release completed with no contributors for current entry.");
                return;
            }

            int commandCount = 0;
            int appliedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            ActivityCapabilityInventory releaseInventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult releaseInventoryValidation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool hasValidReleaseInventory =
                releaseInventory.IsValid &&
                releaseInventoryValidation.IsValid &&
                string.Equals(releaseInventory.Id.PipelineId, releaseIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(releaseInventory.Id.SessionStateId, releaseIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(releaseInventory.Id.ActivityId, releaseIdentity.ActivityId, StringComparison.Ordinal) &&
                releaseInventory.Id.EntrySequence == releaseIdentity.EntrySequence;

            if (!hasValidReleaseInventory)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ObjectReleaseFailed,
                    releaseIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' object release failed reason='release_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{releaseInventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{releaseInventoryValidation.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"release_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, definition, entrySequence))
                {
                    continue;
                }

                if (report.SupportedReleaseKinds == null || report.SupportedReleaseKinds.Count == 0)
                {
                    skippedCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseSkippedOptional,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release skipped targetId='{report.TargetId}' reason='no_supported_release_kinds'.");
                    continue;
                }

                IActivityObjectReleaseEndpoint[] endpoints = ResolveObjectReleaseEndpointsFromInventory(releaseInventory, report);

                for (int kindIndex = 0; kindIndex < report.SupportedReleaseKinds.Count; kindIndex++)
                {
                    ActivityReleaseRequirementKind releaseKind = report.SupportedReleaseKinds[kindIndex];
                    if (releaseKind == ActivityReleaseRequirementKind.Unknown)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' release kind cannot be Unknown targetId='{report.TargetId}'.");
                    }

                    ActivityObjectReleaseCommand releaseCommand = new(
                        releaseIdentity,
                        report.TargetId,
                        report.RoleId,
                        report.ContributorKind,
                        report.Requiredness,
                        releaseKind,
                        command.Source,
                        command.Reason);
                    if (!releaseCommand.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' produced invalid object release command targetId='{report.TargetId}' releaseKind='{releaseKind}'.");
                    }

                    commandCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseCommandIssued,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release command issued targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' releaseKind='{releaseKind}'.");

                    ActivityObjectReleaseResult result = ExecuteObjectReleaseCommand(releaseCommand, endpoints);
                    if (!result.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' object release returned invalid result targetId='{report.TargetId}' releaseKind='{releaseKind}'.");
                    }

                    if (!IsObjectReleaseResultAcceptedForIssuedCommand(result, releaseCommand, definition, entrySequence))
                    {
                        EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseRejectedForeignOrStale,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release rejected foreign/stale targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='stale_or_foreign_release_result'.");
                        continue;
                    }

                    if (result.IsApplied)
                    {
                        appliedCount += 1;
                        EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseApplied,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release applied targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' releaseKind='{releaseKind}'.");
                        continue;
                    }

                    if (result.IsSkippedOptional)
                    {
                        skippedCount += 1;
                        EmitFact(
                            facts,
                            SessionActivityFactKind.ObjectReleaseSkippedOptional,
                            releaseIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' object release skipped optional targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                        continue;
                    }

                    failedCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ObjectReleaseFailed,
                        releaseIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' object release failed targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                    throw new InvalidOperationException(
                        $"object_release_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' releaseKind='{releaseKind}' reason='{result.Message}'.");
                }
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ObjectReleaseCompleted,
                releaseIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}'.");
            EmitSnapshot(
                snapshots,
                "object_release_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' object release completed commandCount='{commandCount}' appliedCount='{appliedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}'.");
        }

        private void EmitObjectSnapshotCaptureStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectExitStage().CaptureSnapshot(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitObjectSnapshotCaptureStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            SessionActivityIdentity captureIdentity = BuildIdentity(definition, SessionActivityStage.Deactivation, entrySequence);
            _state.SetCurrentIdentity(captureIdentity, SessionActivityStage.Deactivation);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotCaptureStarted,
                captureIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture started.");
            EmitSnapshot(
                snapshots,
                "activity_object_snapshot_capture_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture started.");

            if (!discoveryResult.IsValid ||
                !IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence) ||
                discoveryResult.Reports.Count == 0)
            {
                _lastSnapshotPayloadForSaveOnExit = default;
                _lastSnapshotCaptureFailedForSaveOnExit = false;
                _lastSnapshotCaptureFailureDetail = string.Empty;
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                    captureIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture skipped reason='no_discovery_result'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted,
                    captureIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='0' targetIds='<none>' hasTransformPayload='false'.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_snapshot_capture_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='0'.");
                return;
            }

            int capturedCount = 0;
            int failedCount = 0;
            bool hasTransformPayload = false;
            string captureFailureDetail = string.Empty;
            HashSet<string> capturedTargetIds = new(StringComparer.Ordinal);
            List<SessionActivitySnapshotPayloadObject> capturedObjects = new();
            ActivityCapabilityInventory snapshotInventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult snapshotInventoryValidation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool hasValidSnapshotInventory =
                snapshotInventory.IsValid &&
                snapshotInventoryValidation.IsValid &&
                string.Equals(snapshotInventory.Id.PipelineId, captureIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(snapshotInventory.Id.SessionStateId, captureIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(snapshotInventory.Id.ActivityId, captureIdentity.ActivityId, StringComparison.Ordinal) &&
                snapshotInventory.Id.EntrySequence == captureIdentity.EntrySequence;

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, definition, entrySequence))
                {
                    continue;
                }

                if (!hasValidSnapshotInventory)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='snapshot_inventory_missing_or_invalid'.");
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "snapshot_inventory_missing_or_invalid";
                    }
                    continue;
                }

                IActivityObjectSnapshotProvider[] providers = ResolveObjectSnapshotProvidersFromInventory(snapshotInventory, report);
                if (providers.Length == 0)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture skipped targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='no_snapshot_providers'.");
                    continue;
                }

                ActivityObjectSnapshotCaptureCommand captureCommand = new(
                    captureIdentity,
                    report.ContentProfileId,
                    report.TargetId,
                    command.Source,
                    command.Reason);
                if (!captureCommand.IsValid)
                {
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "invalid_capture_command";
                    }
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='invalid_capture_command'.");
                    continue;
                }

                ActivityObjectSnapshotCaptureResult captureResult = ExecuteObjectSnapshotCaptureCommand(captureCommand, providers);
                if (!captureResult.IsValid || !IsObjectSnapshotCaptureResultForCurrentEntry(captureResult, definition, entrySequence))
                {
                    failedCount += 1;
                    if (string.IsNullOrWhiteSpace(captureFailureDetail))
                    {
                        captureFailureDetail = "invalid_or_foreign_capture_result";
                    }
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{report.TargetId}' contentProfileId='{report.ContentProfileId}' reason='invalid_or_foreign_capture_result'.");
                    continue;
                }

                if (captureResult.IsCaptured)
                {
                    capturedCount += 1;
                    hasTransformPayload |= captureResult.HasTransformPayload;
                    capturedTargetIds.Add(captureResult.Command.TargetId);
                    ActivityObjectSnapshot snapshotData = captureResult.Snapshot;
                    capturedObjects.Add(new SessionActivitySnapshotPayloadObject(
                        captureResult.Command.TargetId,
                        captureResult.Command.ContentProfileId,
                        snapshotData.PositionX,
                        snapshotData.PositionY,
                        snapshotData.PositionZ,
                        snapshotData.RotationX,
                        snapshotData.RotationY,
                        snapshotData.RotationZ,
                        snapshotData.RotationW,
                        snapshotData.ScaleX,
                        snapshotData.ScaleY,
                        snapshotData.ScaleZ));
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptured,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot captured targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' coordinateSpace='{ToCoordinateSpaceToken(snapshotData.CoordinateSpace)}' hasTransformPayload='{captureResult.HasTransformPayload.ToString().ToLowerInvariant()}' capturedPosition='({snapshotData.PositionX:0.###},{snapshotData.PositionY:0.###},{snapshotData.PositionZ:0.###})' position='({snapshotData.PositionX:0.###},{snapshotData.PositionY:0.###},{snapshotData.PositionZ:0.###})' rotation='({snapshotData.RotationX:0.###},{snapshotData.RotationY:0.###},{snapshotData.RotationZ:0.###},{snapshotData.RotationW:0.###})' scale='({snapshotData.ScaleX:0.###},{snapshotData.ScaleY:0.###},{snapshotData.ScaleZ:0.###})' detail='{captureResult.Detail}'.");
                    continue;
                }

                if (captureResult.IsSkippedOptional)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders,
                        captureIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot capture skipped targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' reason='{captureResult.Detail}'.");
                    continue;
                }

                failedCount += 1;
                if (string.IsNullOrWhiteSpace(captureFailureDetail))
                {
                    captureFailureDetail = string.IsNullOrWhiteSpace(captureResult.Detail)
                        ? "snapshot_capture_failed"
                        : Normalize(captureResult.Detail);
                }
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed,
                    captureIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot capture failed targetId='{captureResult.Command.TargetId}' contentProfileId='{captureResult.Command.ContentProfileId}' reason='{captureResult.Detail}'.");
            }

            string capturedTargetIdsText = capturedTargetIds.Count > 0 ? string.Join(",", capturedTargetIds) : "<none>";
            if (capturedObjects.Count > 0)
            {
                _lastSnapshotPayloadForSaveOnExit = new SessionActivitySnapshotPayload(
                    RouteActivitySnapshotSchemaId,
                    PipelineId,
                    _sessionId,
                    definition.ActivityId,
                    definition.ActivityOrdinal,
                    entrySequence,
                    capturedObjects);
                _lastSnapshotCaptureFailedForSaveOnExit = false;
                _lastSnapshotCaptureFailureDetail = string.Empty;
            }
            else
            {
                _lastSnapshotPayloadForSaveOnExit = default;
                _lastSnapshotCaptureFailedForSaveOnExit = failedCount > 0;
                _lastSnapshotCaptureFailureDetail = failedCount > 0
                    ? (string.IsNullOrWhiteSpace(captureFailureDetail) ? "snapshot_capture_failed" : Normalize(captureFailureDetail))
                    : string.Empty;
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted,
                captureIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='{capturedCount}' failedCount='{failedCount}' targetIds='{capturedTargetIdsText}' hasTransformPayload='{hasTransformPayload.ToString().ToLowerInvariant()}'.");
            EmitSnapshot(
                snapshots,
                "activity_object_snapshot_capture_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot capture completed capturedCount='{capturedCount}' failedCount='{failedCount}' targetIds='{capturedTargetIdsText}' hasTransformPayload='{hasTransformPayload.ToString().ToLowerInvariant()}'.");
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

            if (context.NextSceneIndex >= context.LoadedSet.Scenes.Count)
            {
                FinalizeActivityContentReleaseCompleted(context, command, facts, snapshots);
                return;
            }

            ActivityContentLoadedSceneRecord record = context.LoadedSet.Scenes[context.NextSceneIndex];
            if (!record.IsValid)
            {
                throw new InvalidOperationException($"Activity content loaded scene record is invalid at index='{context.NextSceneIndex}'.");
            }

            ActivityContentSceneUnloadCommand unloadCommand = new ActivityContentSceneUnloadCommand(
                Guid.NewGuid().ToString("N"),
                record.Identity,
                record.ContentProfileId,
                record.SceneOrdinal,
                record.SceneKey,
                record.Requiredness,
                command.Source,
                command.Reason,
                command.Source,
                command.Reason);
            if (!unloadCommand.IsValid)
            {
                throw new InvalidOperationException($"ActivityContentSceneUnloadCommand is invalid for scene='{record.SceneName}'.");
            }

            SessionActivityPendingOperation pendingOperation = BuildActivityContentReleasePendingOperation(
                context.Definition,
                context.EntrySequence,
                unloadCommand);
            _state.SetPendingOperation(pendingOperation);
            EmitActivityContentSceneUnloadCommandIssued(
                context.Definition,
                command,
                facts,
                snapshots,
                context.EntrySequence,
                pendingOperation);

            _pendingOperationRunner.RunActivityContentReleaseOperation(pendingOperation, unloadCommand, this);
        }

        private void FinalizeActivityContentReleaseCompleted(
            PendingActivityContentReleaseContext context,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            SessionActivityIdentity completedIdentity = BuildIdentity(context.Definition, SessionActivityStage.ActivityContentReleaseCompleted, context.EntrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityContentReleaseCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentReleaseCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{context.Definition.ActivityId}' activity content release completed scenes='{context.LoadedSet.Scenes.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_release_completed",
                command.Source,
                command.Reason,
                $"'{context.Definition.ActivityId}' activity content release completed scenes='{context.LoadedSet.Scenes.Count}'.");

            EmitObjectContributorUnregisterStage(
                context.Definition,
                command,
                facts,
                snapshots,
                context.EntrySequence,
                SessionActivityStage.ActivityContentReleaseCompleted);

            _state.ClearCurrentActivityContentLoadedSet();
            _pendingActivityContentReleaseContext = null;
            _awaitingContinuationAfterActivityContentRelease = false;
        }

        private void EmitObjectContributorUnregisterStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityStage stage)
        {
            BuildActivityObjectExitStage().UnregisterContributors(definition, command, facts, snapshots, entrySequence, stage);
        }

        private void EmitObjectContributorUnregisterStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityStage stage)
        {
            SessionActivityIdentity unregisterIdentity = BuildIdentity(definition, stage, entrySequence);
            _state.SetCurrentIdentity(unregisterIdentity, stage);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterStarted,
                unregisterIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister started.");
            EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister started.");

            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            if (!discoveryResult.IsValid || discoveryResult.Reports == null || discoveryResult.Reports.Count == 0)
            {
                _state.ClearCurrentActivityObjectContributorDiscoveryResult();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    unregisterIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister skipped reason='no_discovery_result'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                    unregisterIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='0'.");
                return;
            }

            if (!IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence))
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterFailed,
                    unregisterIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result' discoveryIdentity='{discoveryResult.Identity}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_unregister_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister failed reason='stale_or_foreign_discovery_result'.");
                throw new InvalidOperationException(
                    $"stale_or_foreign_contributor_discovery_result: activityId='{definition.ActivityId}' entrySequence='{entrySequence}' discoveryIdentity='{discoveryResult.Identity}'.");
            }

            int unregisteredCount = 0;
            bool hasCurrentEntryContributors = false;
            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid)
                {
                    continue;
                }

                if (!IsReportForCurrentEntry(report, definition, entrySequence))
                {
                    continue;
                }

                hasCurrentEntryContributors = true;
                unregisteredCount += 1;
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregistered,
                    unregisterIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregistered contentProfileId='{report.ContentProfileId}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}'.");
            }

            if (!hasCurrentEntryContributors)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors,
                    unregisterIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor unregister skipped reason='no_contributors_for_entry'.");
            }

            _state.ClearCurrentActivityObjectContributorDiscoveryResult();
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted,
                unregisterIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");
            EmitSnapshot(
                snapshots,
                "activity_object_contributor_unregister_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor unregister completed unregisteredCount='{unregisteredCount}'.");
        }

        private sealed class ActivityObjectExitStage
        {
            private readonly SessionActivityPipeline _owner;

            public ActivityObjectExitStage(SessionActivityPipeline owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void CaptureSnapshot(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitObjectSnapshotCaptureStageCore(definition, command, facts, snapshots, entrySequence);
            }

            public void Release(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitObjectReleaseStageCore(definition, command, facts, snapshots, entrySequence);
            }

            public void UnregisterContributors(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence,
                SessionActivityStage stage)
            {
                _owner.EmitObjectContributorUnregisterStageCore(definition, command, facts, snapshots, entrySequence, stage);
            }
        }

        private ActivityObjectExitStage BuildActivityObjectExitStage()
        {
            return new ActivityObjectExitStage(this);
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
                    HandleActivityContentSceneLoadCompleted(definition, syntheticCommand, facts, snapshots, entrySequence, operation);
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
                SessionActivityIdentity identity = BuildIdentity(_state.CurrentDefinition, SessionActivityStage.ActivityContentLoadFailed, active.EntrySequence);
                _state.SetCurrentIdentity(identity, SessionActivityStage.ActivityContentLoadFailed);
                EmitFact(
                    new List<SessionActivityFact>(),
                    SessionActivityFactKind.ActivityContentLoadFailed,
                    identity,
                    source,
                    reason,
                    $"Activity content scene load failed operationId='{active.OperationId}' scene='{active.SceneName}' error='{error}'.");
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
            _pendingActivityContentLoadContext = null;
            _pendingActivityContentReleaseContext = null;
            _awaitingContinuationAfterActivityContentRelease = false;

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
            PendingActivityContentReleaseContext context = _pendingActivityContentReleaseContext;
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
            _pendingActivityContentLoadContext = null;
            _pendingActivityContentReleaseContext = null;
            _lastSnapshotPayloadForSaveOnExit = default;
            _lastSnapshotCaptureFailedForSaveOnExit = false;
            _lastSnapshotCaptureFailureDetail = string.Empty;
            _lastRouteSessionPlayerPreparationHandoff = default;
            _activityPlayerActorRegistry.ClearAllRouteRetained();
            _activityNonPlayerActorRegistry.ClearAllRouteRetained();
            _activeActorParticipationsByActorInstanceId.Clear();
            _activeActorPresentationByActorInstanceId.Clear();            _activeActorAttributeCapabilitiesByActorInstanceId.Clear();
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
            EmitActorPresentationReleaseGenericStage(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);
            EmitActorAttributeReleaseFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);
            EmitActorParticipationExitFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);

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
            EmitActorPresentationReleaseGenericStage(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);
            EmitActorAttributeReleaseFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);
            EmitActorParticipationExitFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);

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
            ClearPendingNavigationTransition();
            _state.ClearHandoff();
            _pendingInternalActivityTransition = default;
            _pendingTransitionResolution = default;
            _pendingTransitionCurtainReveal = false;
            _pendingTransitionCurtainClosed = false;
            _pendingTransitionLoadingVisible = false;

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
                EmitMovementControlDisableForCurrentTargets(current, command, facts, snapshots, currentEntrySequence, reasonCode: "route_exit_requested");
                EmitActorPresentationReleaseGenericStage(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.RouteExit);
                EmitActorAttributeReleaseFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);
                EmitActorParticipationExitFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);

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
                EmitActorPresentationReleaseGenericStage(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.RouteExit);
                EmitActorAttributeReleaseFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);
                EmitActorParticipationExitFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);

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

            if (TryStartActivityContentReleaseForContinuation(current, command, facts, snapshots, currentEntrySequence))
            {
                return;
            }

            await ContinueAfterDeactivationAsync(current, command, facts, snapshots, currentEntrySequence, deactivationIdentity);
        }

        private async Task ContinueAfterDeactivationAsync(
            SessionActivityDefinition current,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int currentEntrySequence,
            SessionActivityIdentity deactivationIdentity)
        {
            if (_awaitingContinuationAfterActivityContentRelease ||
                (_pendingActivityContentReleaseContext != null && _pendingActivityContentReleaseContext.IsValid))
            {
                return;
            }

            if (!deactivationIdentity.IsValid)
            {
                deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            }

            if (_pendingNavigationTransition.IsValid)
            {
                await FinalizePendingNavigationTransition(current, command, facts, snapshots, deactivationIdentity);
                return;
            }

            if (!TryResolveNextActivityForContinuation(current, out SessionActivityDefinition next, out bool wrapped))
            {
                _state.SetCurrentIdentity(BuildIdentity(current, SessionActivityStage.Completed, currentEntrySequence), SessionActivityStage.Completed);
                _state.MarkCompleted();
                _activeRailKind = SessionActivityRailKind.None;
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
            _activeRailKind = SessionActivityRailKind.None;
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
            EmitActorPresentationReleaseGenericStage(current, command, facts, snapshots, currentEntrySequence, ActorPresentationReleaseRail.ActivityExit);
            EmitActorAttributeReleaseFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);
            EmitActorParticipationExitFromInventoryStage(current, command, facts, snapshots, currentEntrySequence);

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
            int currentEntrySequence,
            IReadOnlyList<PlayerActorIdentityRecord> exitedPlayerActors)
        {
            SessionActivityIdentity scopeIdentity = BuildIdentity(current, SessionActivityStage.PlayerActorParticipationExitStageStarted, currentEntrySequence);
            IReadOnlyList<PlayerActorIdentityRecord> actors = exitedPlayerActors ?? Array.Empty<PlayerActorIdentityRecord>();
            if (actors.Count == 0)
            {
                return;
            }

            SessionActivityIdentity stageStartedIdentity = scopeIdentity;
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

            _state.ClearCurrentActivityContentLoadedSet();
            _state.ClearCurrentActivityObjectContributorDiscoveryResult();
            _state.ClearCurrentActivitySetupInventory();

            if (!ResolveAndPrepareActivityContent(definition, command, facts, snapshots, entrySequence))
            {
                return;
            }

            EmitNominalActivitySetup(definition, command, facts, snapshots, entrySequence);
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

        private bool ResolveAndPrepareActivityContent(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity profileResolvedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentProfileResolved, entrySequence);
            _state.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActivityContentProfileResolved);

            if (definition.ActivityContentMode == ActivityContentMode.None)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentProfileResolved,
                    profileResolvedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_profile_resolved_none",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");

                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentLoadSkippedNoContent, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityContentLoadSkippedNoContent);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentLoadSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_load_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                return true;
            }

            if (definition.ActivityContentMode != ActivityContentMode.Profile)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' has unsupported ActivityContentMode='{definition.ActivityContentMode}'.");
            }

            if (!definition.HasActivityContentProfile)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' requires ActivityContentProfile when ActivityContentMode=Profile.");
            }

            ActivityContentProfileAsset profile = definition.ActivityContentProfile;
            ValidateActivityContentProfileForLoadOrThrow(profile, definition.ActivityId);
            string profileId = Normalize(profile.ContentProfileId);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentProfileResolved,
                profileResolvedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_profile_resolved_profile",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");

            SessionActivityIdentity loadStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentLoadStarted, entrySequence);
            _state.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.ActivityContentLoadStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentLoadStarted,
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_load_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");

            IReadOnlyList<ActivityContentSceneEntry> entries = profile.ContentScenes ?? Array.Empty<ActivityContentSceneEntry>();
            _pendingActivityContentLoadContext = new PendingActivityContentLoadContext(loadStartedIdentity, profileId, entries);

            ExecuteNextActivityContentSceneLoad(definition, command, facts, snapshots, entrySequence);
            return false;
        }

        private void ExecuteNextActivityContentSceneLoad(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (_pendingActivityContentLoadContext == null || !_pendingActivityContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' has no valid pending activity content load context.");
            }

            int sceneOrdinal = _pendingActivityContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingActivityContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' next content scene ordinal is out of range. next='{sceneOrdinal}' total='{_pendingActivityContentLoadContext.Entries.Count}'.");
            }

            ActivityContentSceneEntry entry = _pendingActivityContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' content scene entry is null at ordinal='{sceneOrdinal}'.");
            }

            if (entry.Requiredness == ActivityContentRequiredness.Required &&
                (entry.SceneKey == null || string.IsNullOrWhiteSpace(entry.SceneKey.SceneName)))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentLoadFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_load_failed_required_scene_invalid",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' required content scene at ordinal='{sceneOrdinal}' is invalid.");
            }

            if (entry.SceneKey == null || string.IsNullOrWhiteSpace(entry.SceneKey.SceneName))
            {
                SessionActivityIdentity rejectedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _state.SetCurrentIdentity(rejectedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityContentSceneLoadRejected,
                    rejectedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_content_scene_load_rejected",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
                _pendingActivityContentLoadContext.NextSceneOrdinal += 1;
                if (_pendingActivityContentLoadContext.NextSceneOrdinal > _pendingActivityContentLoadContext.Entries.Count)
                {
                    FinalizeActivityContentLoadedSet(definition, command, facts, snapshots, entrySequence);
                }
                else
                {
                    ExecuteNextActivityContentSceneLoad(definition, command, facts, snapshots, entrySequence);
                }

                return;
            }

            SessionActivityIdentity loadingIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoading, entrySequence);
            _state.SetCurrentIdentity(loadingIdentity, SessionActivityStage.ActivityContentSceneLoading);

            ActivityContentSceneLoadCommand loadCommand = new ActivityContentSceneLoadCommand(
                Guid.NewGuid().ToString("N"),
                loadingIdentity,
                _pendingActivityContentLoadContext.ContentProfileId,
                sceneOrdinal,
                entry.SceneKey,
                entry.Requiredness,
                command.Source,
                command.Reason);

            SessionActivityPendingOperation pendingOperation = BuildActivityContentPendingOperation(definition, entrySequence, loadCommand);
            _state.SetPendingOperation(pendingOperation);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneLoadCommandIssued,
                loadingIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' contentProfileId='{loadCommand.ContentProfileId}' sceneOrdinal='{loadCommand.SceneOrdinal}' sceneKey='{loadCommand.SceneKey.name}' sceneName='{loadCommand.SceneName}' requiredness='{loadCommand.Requiredness}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_scene_load_command_issued",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' sceneName='{loadCommand.SceneName}'.");

            _pendingOperationRunner.RunActivityContentOperation(pendingOperation, loadCommand, this);
        }

        private void HandleActivityContentSceneLoadCompleted(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityPendingOperation operation)
        {
            if (_pendingActivityContentLoadContext == null || !_pendingActivityContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' missing pending activity content load context on completion.");
            }

            SessionActivityIdentity loadedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoaded, entrySequence);
            _state.SetCurrentIdentity(loadedIdentity, SessionActivityStage.ActivityContentSceneLoaded);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneLoaded,
                loadedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene loaded operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_scene_loaded",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene loaded operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");

            ActivityContentLoadedSceneRecord record = new(
                loadedIdentity,
                _pendingActivityContentLoadContext.ContentProfileId,
                _pendingActivityContentLoadContext.NextSceneOrdinal,
                ResolveSceneKeyForLoadedRecordOrFail(operation),
                operation.OperationId,
                ResolveRequirednessForCurrentLoadedSceneOrFail(),
                command.Source,
                command.Reason);
            _pendingActivityContentLoadContext.LoadedRecords.Add(record);
            _pendingActivityContentLoadContext.NextSceneOrdinal += 1;

            if (_pendingActivityContentLoadContext.NextSceneOrdinal > _pendingActivityContentLoadContext.Entries.Count)
            {
                FinalizeActivityContentLoadedSet(definition, command, facts, snapshots, entrySequence);
                return;
            }

            ExecuteNextActivityContentSceneLoad(definition, command, facts, snapshots, entrySequence);
        }

        private void FinalizeActivityContentLoadedSet(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (_pendingActivityContentLoadContext == null || !_pendingActivityContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' missing pending activity content load context to finalize loaded set.");
            }

            SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.ActivityContentLoadedSetReady, entrySequence);
            _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivityContentLoadedSetReady);

            ActivityContentLoadedSet loadedSet = new(
                readyIdentity,
                _pendingActivityContentLoadContext.ContentProfileId,
                _pendingActivityContentLoadContext.LoadedRecords,
                command.Source,
                command.Reason);

            if (!loadedSet.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' produced invalid ActivityContentLoadedSet.");
            }

            _state.SetCurrentActivityContentLoadedSet(loadedSet);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentLoadedSetReady,
                readyIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content loaded set ready profileId='{_pendingActivityContentLoadContext.ContentProfileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_content_loaded_set_ready",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content loaded set ready profileId='{_pendingActivityContentLoadContext.ContentProfileId}' loadedScenes='{loadedSet.Scenes.Count}'.");

            _pendingActivityContentLoadContext = null;
            ContinueAfterActivityContentLoadedSetReady(definition, command, facts, snapshots, entrySequence);
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

            EmitNominalActivitySetup(definition, command, facts, snapshots, entrySequence);
            EnterActivationFlow(definition, command, facts, snapshots, entrySequence);
        }

        private ActivityContentRequiredness ResolveRequirednessForCurrentLoadedSceneOrFail()
        {
            if (_pendingActivityContentLoadContext == null || !_pendingActivityContentLoadContext.IsValid)
            {
                throw new InvalidOperationException("Pending activity content load context is invalid while resolving requiredness.");
            }

            int sceneOrdinal = _pendingActivityContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingActivityContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Pending activity content scene ordinal '{sceneOrdinal}' is out of range while resolving requiredness.");
            }

            ActivityContentSceneEntry entry = _pendingActivityContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null || entry.Requiredness == ActivityContentRequiredness.Unknown)
            {
                throw new InvalidOperationException($"Pending activity content scene requiredness is invalid at ordinal='{sceneOrdinal}'.");
            }

            return entry.Requiredness;
        }

        private static void ValidateActivityContentProfileForLoadOrThrow(ActivityContentProfileAsset profile, string activityId)
        {
            if (profile == null)
            {
                throw new InvalidOperationException($"Activity '{activityId}' requires ActivityContentProfileAsset.");
            }

            string profileId = Normalize(profile.ContentProfileId);
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new InvalidOperationException($"Activity '{activityId}' has invalid ActivityContentProfileAsset '{profile.name}': contentProfileId is required.");
            }

            if (profile.DiscoveryMode == ActivitySceneDiscoveryMode.None)
            {
                throw new InvalidOperationException($"Activity '{activityId}' has invalid ActivityContentProfileAsset '{profile.name}': discoveryMode cannot be None.");
            }

            if (profile.PreparationPolicy == ActivityContentPreparationPolicy.Unknown)
            {
                throw new InvalidOperationException($"Activity '{activityId}' has invalid ActivityContentProfileAsset '{profile.name}': preparationPolicy cannot be Unknown.");
            }

            IReadOnlyList<ActivityContentSceneEntry> entries = profile.ContentScenes ?? Array.Empty<ActivityContentSceneEntry>();
            if (entries.Count == 0)
            {
                throw new InvalidOperationException($"Activity '{activityId}' has invalid ActivityContentProfileAsset '{profile.name}': at least one content scene entry is required for v0.");
            }

            for (int index = 0; index < entries.Count; index++)
            {
                ActivityContentSceneEntry entry = entries[index];
                if (entry == null)
                {
                    throw new InvalidOperationException($"Activity '{activityId}' has null content scene entry at index '{index}'.");
                }

                if (entry.Requiredness == ActivityContentRequiredness.Unknown)
                {
                    throw new InvalidOperationException($"Activity '{activityId}' has content scene entry with unknown requiredness at index '{index}'.");
                }

                if (entry.SceneKey != null && string.IsNullOrWhiteSpace(entry.SceneKey.SceneName))
                {
                    throw new InvalidOperationException($"Activity '{activityId}' has content scene entry with empty SceneName at index '{index}'.");
                }
            }
        }

        private Foundation.Platform.SceneReferences.SceneKeyAsset ResolveSceneKeyForLoadedRecordOrFail(SessionActivityPendingOperation operation)
        {
            if (_pendingActivityContentLoadContext == null || !_pendingActivityContentLoadContext.IsValid)
            {
                throw new InvalidOperationException("Pending activity content load context is invalid while resolving scene key.");
            }

            int sceneOrdinal = _pendingActivityContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingActivityContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Pending activity content scene ordinal '{sceneOrdinal}' is out of range while resolving scene key.");
            }

            ActivityContentSceneEntry entry = _pendingActivityContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null || entry.SceneKey == null)
            {
                throw new InvalidOperationException($"Pending activity content scene key is missing at ordinal='{sceneOrdinal}' operationId='{operation.OperationId}'.");
            }

            return entry.SceneKey;
        }

        private void EmitNominalActivitySetup(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            _movementControlTargetsForCurrentEntry = Array.Empty<PlayerActorIdentityRecord>();
            _movementControlEnableAllowedForCurrentEntry = false;
            _lastMovementDisableEmissionKey = string.Empty;
            SessionActivityIdentity setupStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(setupStartedIdentity, SessionActivityStage.ActivitySetupStarted);
            _activityNonPlayerActorRegistry.BeginActivityScope(setupStartedIdentity);
            _activeActorParticipationsByActorInstanceId.Clear();
            EmitFact(facts, SessionActivityFactKind.ActivitySetupStarted, setupStartedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity setup started.");
            EmitSnapshot(snapshots, "activity_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' activity setup started.");
            ObserveActivitySceneContractOrSkip(definition, command, facts, snapshots, entrySequence);
            DiscoverActivityObjectContributorsOrSkip(definition, command, facts, snapshots, entrySequence);
            EmitNonPlayerActorDiscoveryStage(definition, command, facts, snapshots, entrySequence);
            BuildAndValidateActivitySetupInventory(definition, command, facts, snapshots, entrySequence);
            EmitObjectSnapshotContractValidationStage(definition, command, facts, snapshots, entrySequence);
            EmitObjectResetStage(definition, command, facts, snapshots, entrySequence);
            EmitObjectSnapshotRestoreStage(definition, command, facts, snapshots, entrySequence);
            ParticipantBindingStageResult participantBindingResult = EmitParticipantBindingStage(definition, command, facts, snapshots, entrySequence);
            EmitPlayerActorReadinessStage(definition, command, facts, snapshots, entrySequence, participantBindingResult);
            EmitActivityCapabilityInventoryPreviewStage(definition, command, facts, snapshots, entrySequence);
            EmitActorPresentationSetupFromInventoryStage(definition, command, facts, snapshots, entrySequence);
            EmitActorAttributeSetupFromInventoryStage(definition, command, facts, snapshots, entrySequence);
            EmitActorParticipationEnterFromInventoryStage(definition, command, facts, snapshots, entrySequence);
            EmitPlayerInputBindingStage(definition, command, facts, snapshots, entrySequence, participantBindingResult);
            EmitMovementBindingStage(definition, command, facts, snapshots, entrySequence, participantBindingResult);
            EmitCameraBindingStage(definition, command, facts, snapshots, entrySequence);
            SessionActivityIdentity setupCompletedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupCompleted, entrySequence);
            _state.SetCurrentIdentity(setupCompletedIdentity, SessionActivityStage.ActivitySetupCompleted);
            EmitFact(facts, SessionActivityFactKind.ActivitySetupCompleted, setupCompletedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity setup completed.");
            if (definition.ActivationWindowMode == ActivityWindowMode.None)
            {
                EmitPredefinedVisualSetupReadyFactIfApplicable(
                    definition,
                    facts,
                    setupCompletedIdentity,
                    command.Source,
                    command.Reason,
                    "activity_setup_completed");
            }
            EmitSnapshot(snapshots, "activity_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' activity setup completed.");
        }

        private void EmitPredefinedVisualSetupReadyFactIfApplicable(
            SessionActivityDefinition definition,
            List<SessionActivityFact> facts,
            SessionActivityIdentity readinessIdentity,
            string source,
            string reason,
            string readinessPoint)
        {
            string routeOperationId = _lastRouteSessionPlayerPreparationHandoff.IsValid
                ? _lastRouteSessionPlayerPreparationHandoff.RouteOperationId
                : string.Empty;
            if (string.IsNullOrWhiteSpace(routeOperationId))
            {
                return;
            }

            _lastPredefinedVisualReadinessSignal = new PredefinedVisualReadinessSignal(
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
        }

        private ParticipantBindingStageResult EmitParticipantBindingStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivitySetupInventory inventory = _state.CurrentActivitySetupInventory;
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActivityParticipantBindingStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding stage started. routeSessionParticipantOwnership='external' activityOwnership='false'.");
            EmitSnapshot(
                snapshots,
                "activity_participant_binding_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding stage started. routeSessionParticipantOwnership='external' activityOwnership='false'.");

            SessionActivityIdentity expectedInventoryIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            if (!inventory.IsValid || inventory.Identity.CycleKey != expectedInventoryIdentity.CycleKey)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding failed because ActivitySetupInventory is missing or foreign/stale.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding failed because ActivitySetupInventory is missing or foreign/stale.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Missing valid ActivitySetupInventory for activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ParticipantRequirement> participantRequirements = inventory.ParticipantRequirements;
            if (participantRequirements == null || participantRequirements.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding skipped because no participant requirements were declared. inventoryId='{inventory.InventoryId}' totalRequirements='0' routeSessionParticipantPreparationConsumed='false'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_skipped_no_requirements",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding skipped because no participant requirements were declared totalRequirements='0'.");

                SessionActivityIdentity completedAfterSkipIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence);
                _state.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.ActivityParticipantBindingCompleted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingCompleted,
                    completedAfterSkipIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding completed resolved='0' skipped='0' totalRequirements='0' status='SkippedNoRequirements'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding completed resolved='0' skipped='0' totalRequirements='0' status='SkippedNoRequirements'.");
                return new ParticipantBindingStageResult(
                    completedAfterSkipIdentity,
                    totalRequirements: 0,
                    requiredRequirements: 0,
                    resolvedRequirements: 0,
                    skippedRequirements: 0,
                    requiredResolvedRequirements: 0,
                    resolvedParticipants: Array.Empty<ParticipantBindingResolvedRecord>());
            }

            SessionActivityPlayerPreparationHandoff routeSessionPlayerPreparation = ResolveRouteSessionPlayerPreparationOrFail(
                definition,
                command,
                facts,
                snapshots,
                entrySequence);
            HashSet<string> plannedRouteSessionParticipants = BuildPlannedRouteSessionParticipantSet(routeSessionPlayerPreparation.ParticipantIds);
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> technicalPlanByParticipantId =
                BuildTechnicalPlanMap(routeSessionPlayerPreparation.TechnicalPlanEntries);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingResolutionStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding resolution started routeSessionParticipantOwnership='external' activityOwnership='false' participantOwnership='RouteSession' playerPreparationOutcome='{routeSessionPlayerPreparation.Outcome}' participationKind='{routeSessionPlayerPreparation.ParticipationKind}' plannedParticipants='{plannedRouteSessionParticipants.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_participant_binding_resolution_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding resolution started plannedParticipants='{plannedRouteSessionParticipants.Count}'.");

            int resolvedCount = 0;
            int skippedCount = 0;
            int requiredRequirementCount = 0;
            int requiredResolvedCount = 0;
            List<ParticipantBindingResolvedRecord> resolvedParticipants = new(participantRequirements.Count);
            List<ActivityParticipantBindCommand> bindCommands = new(participantRequirements.Count);
            List<ActivityParticipantMaterializationCommand> materializationCommands = new(participantRequirements.Count);
            List<ActivityParticipantPlacementCommand> placementCommands = new(participantRequirements.Count);
            List<ActivityParticipantResetCommand> resetCommands = new(participantRequirements.Count);

            for (int index = 0; index < participantRequirements.Count; index++)
            {
                ParticipantRequirement requirement = participantRequirements[index];
                if (!requirement.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant binding failed invalid participant requirement index='{index}'.");
                    EmitSnapshot(
                        snapshots,
                        "activity_participant_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant binding failed invalid participant requirement index='{index}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Invalid ParticipantRequirement at index='{index}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantRequirementDeclared,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant requirement declared requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' participantId='{requirement.ParticipantId}' roleId='{requirement.RoleId}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='RouteSession' activityOwnership='false' status='Declared'.");

                string requestedParticipantId = Normalize(requirement.ParticipantId);
                bool requirementRequired = requirement.Requirement.IsRequired;
                if (requirementRequired)
                {
                    requiredRequirementCount += 1;
                }
                bool hasRequestedHint = !string.IsNullOrWhiteSpace(requestedParticipantId);
                bool resolvedFromHint = false;
                bool resolved = false;
                string resolvedParticipantId = string.Empty;
                string resolutionReason = "no_route_session_participant_available";

                if (hasRequestedHint)
                {
                    if (plannedRouteSessionParticipants.Contains(requestedParticipantId))
                    {
                        resolved = true;
                        resolvedFromHint = true;
                        resolvedParticipantId = requestedParticipantId;
                        resolutionReason = "resolved_from_requested_participant_id";
                    }
                    else
                    {
                        resolutionReason = "requested_participant_id_not_found_in_route_session";
                    }
                }
                else if (TryResolveSingleParticipantHint(plannedRouteSessionParticipants, out string inferredParticipantId))
                {
                    resolved = true;
                    resolvedParticipantId = inferredParticipantId;
                    resolutionReason = "resolved_from_route_session_single_participant";
                }

                if (resolved)
                {
                    bool hasTechnicalPlan = technicalPlanByParticipantId.TryGetValue(resolvedParticipantId, out SessionActivityPlayerTechnicalPlanEntry technicalPlanEntry) &&
                        technicalPlanEntry.IsValid;
                    if (!hasTechnicalPlan)
                    {
                        if (!requirementRequired)
                        {
                            skippedCount += 1;
                            EmitFact(
                                facts,
                                SessionActivityFactKind.ActivityParticipantBindingResolved,
                                startedIdentity,
                                command.Source,
                                command.Reason,
                                $"'{definition.ActivityId}' optional participant requirement unresolved technical plan requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{(hasRequestedHint ? requestedParticipantId : "<none>")}' resolvedParticipantId='{resolvedParticipantId}' participantOwnership='RouteSession' activityOwnership='false' activityBinding='skipped' status='OptionalTechnicalPlanMissingSkipped' resolutionReason='missing_route_session_participant_technical_plan'.");
                            continue;
                        }

                        SessionActivityIdentity missingTechnicalPlanIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                        _state.SetCurrentIdentity(missingTechnicalPlanIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                        EmitFact(
                            facts,
                            SessionActivityFactKind.ActivityParticipantBindingFailed,
                            missingTechnicalPlanIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' required participant technical plan missing requirementId='{requirement.Requirement.RequirementId}' participantId='{resolvedParticipantId}' operation='materialization' routeIdentity='{routeSessionPlayerPreparation.RouteIdentity}' routeOperationId='{routeSessionPlayerPreparation.RouteOperationId}' transitionId='{routeSessionPlayerPreparation.TransitionId}' routeSequence='{routeSessionPlayerPreparation.RouteSequence}' error='missing_route_session_participant_technical_plan'.");
                        EmitSnapshot(
                            snapshots,
                            "activity_participant_binding_failed",
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' required participant technical plan missing requirementId='{requirement.Requirement.RequirementId}' participantId='{resolvedParticipantId}'.");
                        throw new InvalidOperationException(
                            $"missing_route_session_participant_technical_plan: activityId='{definition.ActivityId}' participantId='{resolvedParticipantId}' operation='materialization' routeIdentity='{routeSessionPlayerPreparation.RouteIdentity}' routeOperationId='{routeSessionPlayerPreparation.RouteOperationId}' transitionId='{routeSessionPlayerPreparation.TransitionId}' routeSequence='{routeSessionPlayerPreparation.RouteSequence}'.");
                    }

                    resolvedCount += 1;
                    if (requirementRequired)
                    {
                        requiredResolvedCount += 1;
                    }
                    resolvedParticipants.Add(new ParticipantBindingResolvedRecord(
                        requirement.Requirement.RequirementId,
                        requirement.ParticipantKind,
                        resolvedParticipantId,
                        requirementRequired));
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingResolved,
                        startedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant requirement resolved requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{(hasRequestedHint ? requestedParticipantId : "<none>")}' resolvedParticipantId='{resolvedParticipantId}' resolvedFromHint='{resolvedFromHint}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='RouteSession' activityOwnership='false' status='ResolvedNominally' resolutionReason='{resolutionReason}'.");

                    ActivityParticipantBindCommand bindCommand = new(
                        startedIdentity,
                        requirement.Requirement.RequirementId,
                        requirement.ParticipantKind,
                        requestedParticipantId,
                        resolvedParticipantId,
                        requirement.RoleId,
                        command.Source,
                        command.Reason);
                    ActivityParticipantMaterializationCommand materializationCommand = new(
                        startedIdentity,
                        requirement.Requirement.RequirementId,
                        requirement.ParticipantKind,
                        resolvedParticipantId,
                        ActivityParticipantMaterializationNeedKind.EnsureRouteSessionParticipantAvailable,
                        command.Source,
                        command.Reason);
                    ActivityParticipantPlacementCommand placementCommand = new(
                        startedIdentity,
                        requirement.Requirement.RequirementId,
                        resolvedParticipantId,
                        requirement.PlacementRequirementId,
                        command.Source,
                        command.Reason);
                    ActivityParticipantResetCommand resetCommand = new(
                        startedIdentity,
                        requirement.Requirement.RequirementId,
                        resolvedParticipantId,
                        BuildDefaultParticipantResetGroups(),
                        command.Source,
                        command.Reason);

                    if (!bindCommand.IsValid || !materializationCommand.IsValid || !placementCommand.IsValid || !resetCommand.IsValid)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                        EmitFact(
                            facts,
                            SessionActivityFactKind.ActivityParticipantBindingFailed,
                            failedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' participant command plan failed invalid command requirementId='{requirement.Requirement.RequirementId}' resolvedParticipantId='{resolvedParticipantId}'.");
                        EmitSnapshot(
                            snapshots,
                            "activity_participant_binding_failed",
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' participant command plan failed invalid command requirementId='{requirement.Requirement.RequirementId}'.");
                        throw new InvalidOperationException(
                            $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Invalid participant command plan requirementId='{requirement.Requirement.RequirementId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    bindCommands.Add(bindCommand);
                    materializationCommands.Add(materializationCommand);
                    placementCommands.Add(placementCommand);
                    resetCommands.Add(resetCommand);
                    continue;
                }

                if (!requirementRequired)
                {
                    skippedCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindingResolved,
                        startedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' optional participant requirement unresolved requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{(hasRequestedHint ? requestedParticipantId : "<none>")}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='RouteSession' activityOwnership='false' activityBinding='skipped' status='OptionalUnresolvedSkipped' resolutionReason='{resolutionReason}'.");
                    continue;
                }

                SessionActivityIdentity requiredFailedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                _state.SetCurrentIdentity(requiredFailedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    requiredFailedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' required participant requirement unresolved requirementId='{requirement.Requirement.RequirementId}' participantKind='{requirement.ParticipantKind}' requestedParticipantId='{(hasRequestedHint ? requestedParticipantId : "<none>")}' requiredness='{requirement.Requirement.Requiredness}' participantOwnership='RouteSession' activityOwnership='false' resolutionReason='{resolutionReason}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' required participant requirement unresolved requirementId='{requirement.Requirement.RequirementId}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Required participant requirement unresolved requirementId='{requirement.Requirement.RequirementId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' resolutionReason='{resolutionReason}'.");
            }

            if (bindCommands.Count > 0)
            {
                EmitParticipantCommandPlan(
                    definition,
                    command,
                    facts,
                    snapshots,
                    startedIdentity,
                    technicalPlanByParticipantId,
                    bindCommands,
                    materializationCommands,
                    placementCommands,
                    resetCommands);
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityParticipantBindingCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding completed resolved='{resolvedCount}' skipped='{skippedCount}' totalRequirements='{participantRequirements.Count}' participantOwnership='RouteSession' activityOwnership='false' status='ResolvedNominally'.");
            EmitSnapshot(
                snapshots,
                "activity_participant_binding_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant binding completed resolved='{resolvedCount}' skipped='{skippedCount}' totalRequirements='{participantRequirements.Count}'.");
            return new ParticipantBindingStageResult(
                completedIdentity,
                participantRequirements.Count,
                requiredRequirementCount,
                resolvedCount,
                skippedCount,
                requiredResolvedCount,
                resolvedParticipants);
        }

        private void EmitPlayerActorReadinessStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ParticipantBindingStageResult participantBindingResult)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.PlayerActorReadinessStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorReadinessStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor readiness started.");
            EmitSnapshot(
                snapshots,
                "player_actor_readiness_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor readiness started.");

            SessionActivityIdentity expectedBindingIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence);
            PlayerActorReadinessStageResult readinessResult = PlayerActorReadinessStage.Execute(
                startedIdentity,
                expectedBindingIdentity,
                participantBindingResult,
                _activityPlayerActorRegistry);

            if (readinessResult.IsSkipped)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessSkippedNoRequiredParticipant, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.PlayerActorReadinessSkippedNoRequiredParticipant);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorReadinessSkippedNoRequiredParticipant,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness skipped because no required participant was declared.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_readiness_skipped_no_required_participant",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness skipped because no required participant was declared.");

                SessionActivityIdentity skippedCompletedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessCompleted, entrySequence);
                _state.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.PlayerActorReadinessCompleted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorReadinessCompleted,
                    skippedCompletedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness completed with skip.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_readiness_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness completed with skip.");
                return;
            }

            if (readinessResult.IsFailed)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerActorReadinessFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerActorReadinessFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness failed reason='{readinessResult.ReasonCode}' requiredResolved='{readinessResult.RequiredResolvedRequirements}' required='{readinessResult.RequiredRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
                EmitSnapshot(
                    snapshots,
                    "player_actor_readiness_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player actor readiness failed reason='{readinessResult.ReasonCode}' requiredResolved='{readinessResult.RequiredResolvedRequirements}' required='{readinessResult.RequiredRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][PlayerActorReadiness] Failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='{readinessResult.ReasonCode}'.");
            }

            SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessValidatedMaterializedOnly, entrySequence);
            _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.PlayerActorReadinessValidatedMaterializedOnly);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorReadyMaterializedOnly,
                readyIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor ready materialized-only requiredReady='{readinessResult.RequiredResolvedRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
            EmitSnapshot(
                snapshots,
                "player_actor_ready_materialized_only",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor ready materialized-only requiredReady='{readinessResult.RequiredResolvedRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerActorReadinessCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.PlayerActorReadinessCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorReadinessCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor readiness completed.");
            EmitSnapshot(
                snapshots,
                "player_actor_readiness_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor readiness completed.");
        }
        private ActivityCapabilityInventory BuildAndStoreActivityCapabilityInventoryForEntry(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> playerActorTargets,
            string source,
            string reason)
        {
            IReadOnlyList<ActorScanTarget> actorTargets = BuildActorScanTargetsForCurrentEntry(identity, playerActorTargets, source, reason);
            ActivityCapabilityInventoryBuildResult buildResult = _activityCapabilityInventoryCoordinator.BuildForEntry(
                identity,
                _state.CurrentActivityObjectContributorDiscoveryResult,
                actorTargets,
                source,
                reason);
            _state.SetCurrentActivityCapabilityInventoryPreview(buildResult.Inventory, buildResult.Validation);
            return buildResult.Inventory;
        }

        private IReadOnlyList<ActorScanTarget> BuildActorScanTargetsForCurrentEntry(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> playerActorTargets,
            string source,
            string reason)
        {
            ActorInventoryFeed feed = new();
            ActorInventoryFeedResult feedResult = feed.BuildFromSources(
                identity,
                BuildActorInstanceSources(identity, playerActorTargets),
                source,
                reason);
            return feedResult.BuildScanTargets(source);
        }

        private ActorInventoryFeedResult BuildActorInventoryFeedForCurrentEntry(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            IReadOnlyList<PlayerActorIdentityRecord> playerActors = ResolvePlayerActorCapabilityTargetsForCurrentEntry(identity);
            ActorInventoryFeed feed = new();
            return feed.BuildFromSources(
                identity,
                BuildActorInstanceSources(identity, playerActors),
                source,
                reason);
        }

        private IReadOnlyList<IActivityActorInstanceSource> BuildActorInstanceSources(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> playerActorTargets)
        {
            IReadOnlyList<NonPlayerActorRuntimeEntry> nonPlayerActors = Array.Empty<NonPlayerActorRuntimeEntry>();
            try
            {
                nonPlayerActors = _activityNonPlayerActorRegistry.GetActiveEntries(identity);
            }
            catch (InvalidOperationException)
            {
                nonPlayerActors = Array.Empty<NonPlayerActorRuntimeEntry>();
            }

            return new IActivityActorInstanceSource[]
            {
                new PlayerActorInstanceSource(playerActorTargets ?? Array.Empty<PlayerActorIdentityRecord>(), _activityPlayerActorRegistry),
                new NonPlayerActorInstanceSource(nonPlayerActors),
            };
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

        private void SyncPermissionReceiversForMovementTargets(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> movementTargets,
            string source,
            string reason)
        {
            if (movementTargets == null || movementTargets.Count == 0)
            {
                _permissionRuntime.ReplaceReceivers(Array.Empty<ActivityCapabilityPermissionReceiverReference>());
                return;
            }

            ActivityCapabilityInventory inventory = BuildAndStoreActivityCapabilityInventoryForEntry(
                identity,
                movementTargets,
                source,
                reason);
            IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers = ResolvePermissionReceiversFromInventory(inventory, identity);
            for (int index = 0; index < receivers.Count; index++)
            {
                ActivityCapabilityPermissionReceiverReference receiver = receivers[index];
                if (receiver == null || !receiver.IsValid || !IsSamePermissionScope(identity, receiver.Identity))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionActivityPipeline][MovementBinding] permission_receiver_identity_mismatch activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' receiverId='{receiver?.ReceiverId ?? "<null>"}' receiverIdentity='{receiver?.Identity.ToString() ?? "<null>"}'.");
                }
            }

            _permissionRuntime.ReplaceReceivers(receivers);
        }

        private IReadOnlyList<PlayerActorIdentityRecord> ResolvePlayerActorCapabilityTargetsForCurrentEntry(SessionActivityIdentity identity)
        {
            if (_activityPlayerActorRegistry.TryGetActiveActorIdentities(identity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors) &&
                activeActors != null &&
                activeActors.Count > 0)
            {
                return activeActors;
            }

            return ResolveRouteRetainedPlayerActorIdentitiesOrEmpty(identity);
        }

        private IReadOnlyList<PlayerActorIdentityRecord> ResolveRouteRetainedPlayerActorIdentitiesOrEmpty(SessionActivityIdentity identity)
        {
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

                if (!_activityPlayerActorRegistry.TryResolveInstanceForControl(identity, candidate.PlayerActorId, out GameObject instance, out PlayerActorIdentityRecord observedIdentity) ||
                    instance == null ||
                    !observedIdentity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(observedIdentity.PlayerSlotId, candidate.PlayerSlotId, StringComparison.Ordinal))
                {
                    continue;
                }

                resolved.Add(new PlayerActorIdentityRecord(
                    identity,
                    candidate.PlayerSlotId,
                    candidate.PlayerActorId));
            }

            return resolved.Count == 0 ? Array.Empty<PlayerActorIdentityRecord>() : resolved;
        }

        internal readonly struct ParticipantBindingStageResult
        {
            public ParticipantBindingStageResult(
                SessionActivityIdentity identity,
                int totalRequirements,
                int requiredRequirements,
                int resolvedRequirements,
                int skippedRequirements,
                int requiredResolvedRequirements,
                IReadOnlyList<ParticipantBindingResolvedRecord> resolvedParticipants)
            {
                Identity = identity;
                TotalRequirements = totalRequirements < 0 ? 0 : totalRequirements;
                RequiredRequirements = requiredRequirements < 0 ? 0 : requiredRequirements;
                ResolvedRequirements = resolvedRequirements < 0 ? 0 : resolvedRequirements;
                SkippedRequirements = skippedRequirements < 0 ? 0 : skippedRequirements;
                RequiredResolvedRequirements = requiredResolvedRequirements < 0 ? 0 : requiredResolvedRequirements;
                ResolvedParticipants = resolvedParticipants ?? Array.Empty<ParticipantBindingResolvedRecord>();
            }

            public SessionActivityIdentity Identity { get; }
            public int TotalRequirements { get; }
            public int RequiredRequirements { get; }
            public int ResolvedRequirements { get; }
            public int SkippedRequirements { get; }
            public int RequiredResolvedRequirements { get; }
            public IReadOnlyList<ParticipantBindingResolvedRecord> ResolvedParticipants { get; }

            public bool IsValid =>
                Identity.IsValid &&
                Identity.Stage == SessionActivityStage.ActivityParticipantBindingCompleted &&
                RequiredRequirements >= 0 &&
                ResolvedRequirements >= 0 &&
                SkippedRequirements >= 0 &&
                RequiredResolvedRequirements >= 0 &&
                RequiredResolvedRequirements <= RequiredRequirements &&
                ResolvedParticipants != null;
        }

        private void EmitActorPresentationSetupFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationSetupStarted);
            EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup started mode='inventory_references'.");
            EmitSnapshot(snapshots, "actor_presentation_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup started.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationSetupFromInventoryStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}' mode='InventoryReferences'.", DebugUtility.Colors.Info);

            ActivityCapabilityInventory inventory = _state.CurrentActivityCapabilityInventoryPreview;
            if (!inventory.IsValid ||
                !string.Equals(inventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) ||
                inventory.Id.EntrySequence != startedIdentity.EntrySequence)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed reason='inventory_missing_or_foreign'.");
                EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed reason='inventory_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationSetup] Missing or foreign ActivityCapabilityInventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ActorPresentationEndpointReference> presentationReferences = ResolveActorPresentationReferencesFromInventory(inventory);
            int totalResolved = 0;
            int totalMaterialized = 0;
            int totalRetained = 0;
            int totalReady = 0;
            int totalSkipped = 0;
            if (presentationReferences.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped reason='no_presentation_endpoint_references'.");
                EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped reason='no_presentation_endpoint_references'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='no_presentation_endpoint_references' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);

                SessionActivityIdentity completedAfterSkipIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupCompleted, entrySequence);
                _state.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedAfterSkipIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed with skip.");
                EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed with skip.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' mode='SkippedNoReferences' total='0' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                return;
            }

            for (int index = 0; index < presentationReferences.Count; index++)
            {
                ActorPresentationEndpointReference presentationReference = presentationReferences[index];
                ActorPresentationEndpoint endpoint = presentationReference.Endpoint;
                if (endpoint == null)
                {
                    continue;
                }

                endpoint.ValidateOrThrow($"{nameof(SessionActivityPipeline)}/ActorPresentationSetupFromInventory");
                if (endpoint.Profile == null)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='actor_presentation_profile_missing'.");
                    EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='actor_presentation_profile_missing'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationSetup] Missing ActorPresentationProfileAsset actorId='{presentationReference.ActorId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                ActorPresentationPlanResolutionResult planResult = ActorPresentationSetupStage.ResolvePlan(
                    _actorPresentationPlanResolver,
                    endpoint.Profile,
                    endpoint,
                    startedIdentity.ActivityId,
                    presentationReference.ActorId,
                    presentationReference.ActorKind.ToString(),
                    nameof(SessionActivityPipeline),
                    command.Reason);

                if (planResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{planResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='{planResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationSetup] Plan resolution failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='{planResult.ReasonCode}' message='{planResult.Message}'.");
                }

                if (planResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{planResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' reason='{planResult.ReasonCode}'.");
                    continue;
                }

                SessionActivityIdentity planResolvedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationPlanResolved, entrySequence);
                _state.SetCurrentIdentity(planResolvedIdentity, SessionActivityStage.ActorPresentationPlanResolved);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationPlanResolved, planResolvedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation plan resolved actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}'.");
                EmitSnapshot(snapshots, "actor_presentation_plan_resolved", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation plan resolved actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' profileId='{planResult.ResolvedPlan.ProfileId}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationPlanResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}' mode='Planned' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                totalResolved += 1;

                if (TryGetActivePresentationHandle(presentationReference, out ActorPresentationRuntimeHandle activeHandle))
                {
                    if (CanRetainPresentationHandle(presentationReference, activeHandle, planResult.ResolvedPlan))
                    {
                        SessionActivityIdentity retainedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationRetained, entrySequence);
                        _state.SetCurrentIdentity(retainedIdentity, SessionActivityStage.ActorPresentationRetained);
                        EmitFact(facts, SessionActivityFactKind.ActorPresentationRetained, retainedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation retained actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' policy='{activeHandle.ResolvedPlan.ReleasePolicy}' profileId='{activeHandle.ResolvedPlan.ProfileId}'.");
                        DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationRetained' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        totalRetained += 1;

                        SessionActivityIdentity readyRetainedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReady, entrySequence);
                        _state.SetCurrentIdentity(readyRetainedIdentity, SessionActivityStage.ActorPresentationReady);
                        EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyRetainedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready retained actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' instance='{activeHandle.PresentationInstance.name}'.");
                        DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        SyncNonPlayerPresentationHandle(startedIdentity, presentationReference, activeHandle);
                        totalReady += 1;
                        continue;
                    }

                    EmitActorPresentationReleaseGenericStage(
                        definition,
                        command,
                        facts,
                        snapshots,
                        entrySequence,
                        ActorPresentationReleaseRail.BeforeRematerialization,
                        presentationReference.ActorInstanceRuntimeId);
                }

                ActorPresentationResult materializationResult = _actorPresentationMaterializationAdapter.Materialize(new ActorPresentationMaterializationCommand(planResult.ResolvedPlan, command.Source, command.Reason));
                if (materializationResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationSetup] Materialization failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='{materializationResult.ReasonCode}' message='{materializationResult.Message}'.");
                }

                if (materializationResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' mode='SkippedOptional' reasonCode='{materializationResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                    totalSkipped += 1;
                    continue;
                }

                SessionActivityIdentity materializedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationMaterialized, entrySequence);
                _state.SetCurrentIdentity(materializedIdentity, SessionActivityStage.ActorPresentationMaterialized);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationMaterialized, materializedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation materialized actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationMaterialized' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' componentPath='{presentationReference.ComponentPath}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                totalMaterialized += 1;

                SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReady, entrySequence);
                _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorPresentationReady);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' instance='{materializationResult.ReadyFact.PresentationInstance.name}'.");
                EmitSnapshot(snapshots, "actor_presentation_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                StoreActivePresentationHandle(startedIdentity, presentationReference, materializationResult.ReadyFact.RuntimeHandle);
                totalReady += 1;
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' mode='inventory_references'.");
            EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private static IReadOnlyList<ActorPresentationEndpointReference> ResolveActorPresentationReferencesFromInventory(ActivityCapabilityInventory inventory)
        {
            List<ActorPresentationEndpointReference> references = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.PresentationEndpoint)
                {
                    continue;
                }

                if (inventory.TryGetRuntimeReference(capability.CapabilityId, out ActorPresentationEndpointReference reference) &&
                    reference != null &&
                    reference.IsValid)
                {
                    references.Add(reference);
                }
            }

            return references;
        }

        private static IReadOnlyList<ActorAttributeEndpointReference> ResolveActorAttributeReferencesFromInventory(ActivityCapabilityInventory inventory)
        {
            List<ActorAttributeEndpointReference> references = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.AttributeEndpoint)
                {
                    continue;
                }

                if (inventory.TryGetRuntimeReference(capability.CapabilityId, out ActorAttributeEndpointReference reference) &&
                    reference != null &&
                    reference.IsValid)
                {
                    references.Add(reference);
                }
            }

            return references;
        }

        private bool TryGetActivePresentationHandle(ActorPresentationEndpointReference presentationReference, out ActorPresentationRuntimeHandle handle)
        {
            handle = default;
            if (presentationReference == null || !presentationReference.IsValid)
            {
                return false;
            }

            if (!_activeActorPresentationByActorInstanceId.TryGetValue(presentationReference.ActorInstanceRuntimeId, out ActorPresentationCapabilityState state) || !state.IsValid)
            {
                return false;
            }

            handle = state.RuntimeHandle;
            return handle.IsValid;
        }

        private bool CanRetainPresentationHandle(
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle activeHandle,
            ActorPresentationResolvedPlan resolvedPlan)
        {
            if (!resolvedPlan.IsValid || !activeHandle.IsValid || !activeHandle.ResolvedPlan.IsValid)
            {
                return false;
            }

            if (resolvedPlan.ReleasePolicy == ActorPresentationReleasePolicy.ReleaseOnActivityExit)
            {
                return false;
            }

            return activeHandle.IsValid &&
                activeHandle.ResolvedPlan.IsValid &&
                activeHandle.PresentationInstance != null &&
                activeHandle.ResolvedPlan.ReleasePolicy == resolvedPlan.ReleasePolicy &&
                string.Equals(activeHandle.ResolvedPlan.ProfileId, resolvedPlan.ProfileId, StringComparison.Ordinal) &&
                string.Equals(activeHandle.ResolvedPlan.ActorId, presentationReference.ActorId, StringComparison.Ordinal);
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

            _activeActorPresentationByActorInstanceId[presentationReference.ActorInstanceRuntimeId] = new ActorPresentationCapabilityState(
                presentationReference.ActorInstanceRuntimeId,
                presentationReference.ActorId,
                presentationReference.Endpoint,
                handle,
                identity.PipelineId,
                BuildActorAttributeActivityIdentity(identity));
            SyncNonPlayerPresentationHandle(identity, presentationReference, handle);
        }

        private void RemoveActivePresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference)
        {
            if (presentationReference == null || !presentationReference.IsValid)
            {
                return;
            }

            _activeActorPresentationByActorInstanceId.Remove(presentationReference.ActorInstanceRuntimeId);
            if (IsNonPlayerPresentationReference(presentationReference))
            {
                _activityNonPlayerActorRegistry.ClearPresentationHandle(identity, presentationReference.ActorId);
            }
        }

        private void SyncNonPlayerPresentationHandle(
            SessionActivityIdentity identity,
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle handle)
        {
            if (presentationReference == null || !presentationReference.IsValid || !handle.IsValid)
            {
                return;
            }

            if (!IsNonPlayerPresentationReference(presentationReference))
            {
                return;
            }

            _activityNonPlayerActorRegistry.SetPresentationHandle(identity, presentationReference.ActorId, handle);
        }

        private static bool IsNonPlayerPresentationReference(ActorPresentationEndpointReference presentationReference)
        {
            if (presentationReference == null || presentationReference.Endpoint == null)
            {
                return false;
            }

            return presentationReference.Endpoint.GetComponentInParent<_ImmersiveGames.NewScripts.Actors.Runtime.NonPlayerActor>(true) != null;
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
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationReleaseStarted);
            EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}' target='{(targetActorInstanceId.IsValid ? targetActorInstanceId.Value : "all")}'.");
            EmitSnapshot(snapshots, "actor_presentation_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceId.IsValid ? targetActorInstanceId.Value : "all")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            List<ActorPresentationCapabilityState> activeStates = new();
            if (targetActorInstanceId.IsValid)
            {
                if (_activeActorPresentationByActorInstanceId.TryGetValue(targetActorInstanceId, out ActorPresentationCapabilityState targetedState) &&
                    targetedState.IsValid)
                {
                    activeStates.Add(targetedState);
                }
            }
            else
            {
                foreach (ActorPresentationCapabilityState state in _activeActorPresentationByActorInstanceId.Values)
                {
                    if (state.IsValid)
                    {
                        activeStates.Add(state);
                    }
                }
            }

            if (activeStates.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                EmitSnapshot(snapshots, "actor_presentation_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceId.IsValid ? targetActorInstanceId.Value : "all")}' mode='Skipped' reason='no_active_actor_presentation_handle' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
            }
            else
            {
                for (int index = 0; index < activeStates.Count; index++)
                {
                    ActorPresentationCapabilityState state = activeStates[index];
                    ActorPresentationRuntimeHandle handle = state.RuntimeHandle;
                    if (!handle.IsValid)
                    {
                        continue;
                    }

                    ActorPresentationReleasePolicy policy = handle.ResolvedPlan.ReleasePolicy;
                    bool shouldRelease = rail switch
                    {
                        ActorPresentationReleaseRail.BeforeRematerialization => true,
                        ActorPresentationReleaseRail.ActivityExit => policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit,
                        ActorPresentationReleaseRail.RouteExit => policy == ActorPresentationReleasePolicy.ReleaseOnRouteExit,
                        _ => false,
                    };

                    if (!shouldRelease)
                    {
                        SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                        _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                        EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='policy_mismatch' policy='{policy}' rail='{rail}' mode='Retained'.");
                        DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' policy='{policy}' mode='Retained' reason='policy_mismatch' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                        continue;
                    }

                    ActorPresentationResult releaseResult = _actorPresentationMaterializationAdapter.Release(
                        new ActorPresentationReleaseCommand(handle, command.Source, command.Reason));
                    if (!releaseResult.IsSuccess)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                        EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='{releaseResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationRelease] Release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}'.");
                    }

                    bool keptBound = policy == ActorPresentationReleasePolicy.KeepBound;
                    SessionActivityIdentity releasedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleased, entrySequence);
                    _state.SetCurrentIdentity(releasedIdentity, SessionActivityStage.ActorPresentationReleased);
                    EmitFact(facts, SessionActivityFactKind.ActorPresentationReleased, releasedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}' mode='{(keptBound ? "Retained" : "Released")}'.");
                    EmitSnapshot(snapshots, "actor_presentation_released", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleased' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' policy='{policy}' mode='{(keptBound ? "Retained" : "Released")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                    if (keptBound)
                    {
                        if (rail == ActorPresentationReleaseRail.BeforeRematerialization)
                        {
                            SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                            _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                            EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='keep_bound_blocks_rematerialization'.");
                            EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='keep_bound_blocks_rematerialization'.");
                            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='keep_bound_blocks_rematerialization' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Error);
                            throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorPresentationRelease] KeepBound blocks rematerialization actorId='{state.ActorId}'.");
                        }

                        continue;
                    }

                    _activeActorPresentationByActorInstanceId.Remove(state.ActorInstanceRuntimeId);
                    if (state.Endpoint != null &&
                        state.Endpoint.GetComponentInParent<_ImmersiveGames.NewScripts.Actors.Runtime.NonPlayerActor>(true) != null)
                    {
                        try
                        {
                            _activityNonPlayerActorRegistry.ClearPresentationHandle(startedIdentity, state.ActorId);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorPresentationReleaseCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release completed rail='{rail}'.");
            EmitSnapshot(snapshots, "actor_presentation_release_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release completed rail='{rail}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][ActorPresentation] event='ActorPresentationReleaseCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceId.IsValid ? targetActorInstanceId.Value : "all")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private void EmitActorAttributeSetupFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeSetupStarted);
            EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup started mode='inventory_references'.");
            EmitSnapshot(snapshots, "actor_attribute_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup started.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeSetupStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}' mode='InventoryReferences'.", DebugUtility.Colors.Info);

            ActivityCapabilityInventory inventory = _state.CurrentActivityCapabilityInventoryPreview;
            if (!inventory.IsValid ||
                !string.Equals(inventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) ||
                inventory.Id.EntrySequence != startedIdentity.EntrySequence)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed reason='inventory_missing_or_foreign'.");
                EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed reason='inventory_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorAttributeSetup] Missing or foreign ActivityCapabilityInventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ActorAttributeEndpointReference> attributeReferences = ResolveActorAttributeReferencesFromInventory(inventory);
            int totalCount = attributeReferences.Count;
            int resolvedCount = 0;
            int readyCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            if (attributeReferences.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_references'.");
                EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_references'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeSetupSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='no_attribute_endpoint_references' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                skippedCount += 1;
            }
            else
            {
                string pipelineIdentity = startedIdentity.PipelineId;
                string activityIdentity = BuildActorAttributeActivityIdentity(startedIdentity);
                for (int index = 0; index < attributeReferences.Count; index++)
                {
                    ActorAttributeEndpointReference attributeReference = attributeReferences[index];
                    if (attributeReference == null || !attributeReference.IsValid || attributeReference.Endpoint == null)
                    {
                        continue;
                    }

                    resolvedCount += 1;
                    ActorAttributeEndpoint endpoint = attributeReference.Endpoint;
                    ActorAttributeProfileAsset profile = endpoint.AttributeProfile;
                    SessionActivityIdentity profileResolvedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeProfileResolved, entrySequence);
                    _state.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActorAttributeProfileResolved);
                    EmitFact(facts, SessionActivityFactKind.ActorAttributeProfileResolved, profileResolvedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute profile resolved actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    EmitSnapshot(snapshots, "actor_attribute_profile_resolved", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute profile resolved actorId='{attributeReference.ActorId}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeProfileResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

                    if (profile == null)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='attribute_profile_missing'.");
                        EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' reason='attribute_profile_missing'.");
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorAttributeSetup] Missing ActorAttributeProfileAsset actorId='{attributeReference.ActorId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!endpoint.TryInitialize(attributeReference.ActorId, pipelineIdentity, activityIdentity, out ActorAttributeSetupResult setupResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                        EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' reason='{setupResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorAttributeSetup] Initialization failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                    }

                    if (setupResult.IsSkippedNoContent)
                    {
                        skippedCount += 1;
                        SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                        _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                        EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                        EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped actorId='{attributeReference.ActorId}' reason='{setupResult.Reason}'.");
                        _activeActorAttributeCapabilitiesByActorInstanceId.Remove(attributeReference.ActorInstanceRuntimeId);
                        continue;
                    }

                    _activeActorAttributeCapabilitiesByActorInstanceId[attributeReference.ActorInstanceRuntimeId] =
                        new ActorAttributeCapabilityState(
                            attributeReference.ActorInstanceRuntimeId,
                            attributeReference.ActorId,
                            endpoint,
                            pipelineIdentity,
                            activityIdentity);

                    SessionActivityIdentity readyIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReady, entrySequence);
                    _state.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorAttributeReady);
                    EmitFact(facts, SessionActivityFactKind.ActorAttributeReady, readyIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute ready actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(endpoint)}'.");
                    EmitSnapshot(snapshots, "actor_attribute_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute ready actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(endpoint)}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                    readyCount += 1;
                }

                if (resolvedCount == 0 || readyCount == 0)
                {
                    skippedCount += 1;
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                    EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                    EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                }
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeSetupCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' mode='inventory_references'.");
            EmitSnapshot(snapshots, "actor_attribute_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private void EmitActorParticipationEnterFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationEnterStarted);
            EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter started mode='inventory_feed'.");
            EmitSnapshot(snapshots, "actor_participation_enter_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter started.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEnterStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            ActorInventoryFeedResult feedResult = BuildActorInventoryFeedForCurrentEntry(startedIdentity, command.Source, command.Reason);
            ActorParticipationStageExecutor executor = new(new ActorParticipationReadinessPolicy(this));
            ActorParticipationCommand participationCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            ActorParticipationResult participationResult = executor.Execute(participationCommand);

            int total = participationResult.Total;
            int entered = participationResult.Entered;
            int skipped = participationResult.Skipped;
            int failed = participationResult.Failed;

            for (int index = 0; index < participationResult.ActorResults.Count; index++)
            {
                ActorParticipationActorResult actorResult = participationResult.ActorResults[index];
                if (!actorResult.IsValid)
                {
                    continue;
                }

                ActorInstanceRecord instance = actorResult.Instance;
                if (actorResult.IsSkipped)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterSkipped, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                    EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEnterSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' skipKind='{actorResult.SkipKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
                    continue;
                }

                if (actorResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                    EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation failed actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEnterFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' failureKind='{actorResult.SkipKind}' failureReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorParticipationEnter] readiness failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                }

                _activeActorParticipationsByActorInstanceId.Add(instance.ActorInstanceId);
                EmitFact(facts, SessionActivityFactKind.ActorParticipationEntered, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation entered actorId='{instance.ActorId}' actorRole='{instance.Role}' actorScope='{instance.Scope}'.");
                EmitSnapshot(snapshots, "actor_participation_entered", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation entered actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEntered' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                EmitFact(facts, SessionActivityFactKind.ActorReady, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor ready actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
                EmitSnapshot(snapshots, "actor_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor ready actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            }

            if (entered == 0)
            {
                skipped += 1;
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterSkipped, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEnterSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_entered_actors' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationEnterCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            EmitSnapshot(snapshots, "actor_participation_enter_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationEnterCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private void EmitActorParticipationExitFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationExitStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationExitStarted);
            EmitFact(facts, SessionActivityFactKind.ActorParticipationExitStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started mode='inventory_feed'.");
            EmitSnapshot(snapshots, "actor_participation_exit_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            ActorInventoryFeedResult feedResult = BuildActorInventoryFeedForCurrentEntry(startedIdentity, command.Source, command.Reason);
            ActorParticipationExitStageExecutor exitExecutor = new();
            ActorParticipationExitCommand exitCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            ActorParticipationExitResult exitResult = exitExecutor.Execute(exitCommand, _activeActorParticipationsByActorInstanceId);
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitResultBuilt' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{exitResult.Total}' exited='{exitResult.Exited}' skipped='{exitResult.Skipped}' failed='{exitResult.Failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            int total = exitResult.Total;
            int exited = exitResult.Exited;
            int skipped = exitResult.Skipped;
            int failed = exitResult.Failed;
            List<PlayerActorIdentityRecord> exitedPlayerActors = new();
            List<string> exitedPlayerActorDetails = new();

            for (int index = 0; index < exitResult.ActorResults.Count; index++)
            {
                ActorParticipationExitActorResult actorResult = exitResult.ActorResults[index];
                if (!actorResult.IsValid)
                {
                    continue;
                }

                ActorInstanceRecord instance = actorResult.Instance;
                if (actorResult.IsSkipped)
                {
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' skipKind='{actorResult.SkipOrFailureKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
                    continue;
                }

                if (actorResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationExitFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationExitFailed);
                    EmitFact(facts, SessionActivityFactKind.ActorParticipationExitFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    EmitSnapshot(snapshots, "actor_participation_exit_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' failureKind='{actorResult.SkipOrFailureKind}' failureReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorParticipationExit] failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                }

                _activeActorParticipationsByActorInstanceId.Remove(instance.ActorInstanceId);
                EmitFact(facts, SessionActivityFactKind.ActorParticipationExited, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
                EmitSnapshot(snapshots, "actor_participation_exited", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExited' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                if (instance.Kind == ActorKind.Player)
                {
                    if (!actorResult.HasResolvedPlayerIdentity)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][SessionActivityPipeline][ActorParticipationExit] player_identity_resolution_failed reason='player_identity_missing_in_actor_participation_record' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    PlayerActorIdentityRecord resolvedIdentity = new(
                        startedIdentity,
                        actorResult.PlayerSlotId,
                        actorResult.PlayerActorId);
                    exitedPlayerActors.Add(resolvedIdentity);
                    exitedPlayerActorDetails.Add(
                        $"actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{actorResult.PlayerSlotId}'");
                }
            }

            try
            {
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitBridgeStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerCount='{exitedPlayerActors.Count}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                EmitPlayerActorParticipationExitIfNeeded(definition, command, facts, snapshots, entrySequence, exitedPlayerActors);
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitBridgeCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerCount='{exitedPlayerActors.Count}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                failed += 1;
                string actorDetails = exitedPlayerActorDetails.Count == 0
                    ? "actorId='<none>' actorInstanceRuntimeId='<none>' playerActorId='<none>' playerSlotId='<none>'"
                    : string.Join(" | ", exitedPlayerActorDetails);
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitBridgeFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='player_side_effect_bridge_failed' details='{actorDetails}' error='{ex.Message}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                    DebugUtility.Colors.Error);
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationExitFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationExitFailed);
                EmitFact(facts, SessionActivityFactKind.ActorParticipationExitFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed reason='player_side_effect_bridge_failed:{ex.Message}'.");
                EmitSnapshot(snapshots, "actor_participation_exit_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed reason='player_side_effect_bridge_failed'.");
                throw;
            }

            if (exited == 0)
            {
                skipped += 1;
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
                EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
                EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_exited_actors' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorParticipationExitCompleted, entrySequence);
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitCompletionAboutToEmit' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationExitCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorParticipationExitCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            EmitSnapshot(snapshots, "actor_participation_exit_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorParticipationExitCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private void EmitActorAttributeReleaseFromInventoryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeReleaseStarted);
            EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            EmitSnapshot(snapshots, "actor_attribute_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeReleaseStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            int totalCount = _activeActorAttributeCapabilitiesByActorInstanceId.Count;
            int releasedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            if (_activeActorAttributeCapabilitiesByActorInstanceId.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeReleaseSkipped);
                EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                EmitSnapshot(snapshots, "actor_attribute_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                skippedCount += 1;
            }
            else
            {
                int releasedOrSkippedCount = 0;
                ActorInstanceId[] activeActorInstanceIds = new ActorInstanceId[_activeActorAttributeCapabilitiesByActorInstanceId.Keys.Count];
                _activeActorAttributeCapabilitiesByActorInstanceId.Keys.CopyTo(activeActorInstanceIds, 0);
                for (int index = 0; index < activeActorInstanceIds.Length; index++)
                {
                    ActorInstanceId actorInstanceId = activeActorInstanceIds[index];
                    if (!_activeActorAttributeCapabilitiesByActorInstanceId.TryGetValue(actorInstanceId, out ActorAttributeCapabilityState capabilityState) || capabilityState.Endpoint == null)
                    {
                        continue;
                    }

                    string actorId = capabilityState.ActorId;

                    if (!capabilityState.Endpoint.TryRelease(capabilityState.PipelineIdentity, capabilityState.ActivityIdentity, out ActorAttributeReleaseResult releaseResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    if (releaseResult.Rejected || releaseResult.Failed)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    SessionActivityStage stage = releaseResult.SkippedNoContent
                        ? SessionActivityStage.ActorAttributeReleaseSkipped
                        : SessionActivityStage.ActorAttributeReleased;
                    SessionActivityFactKind factKind = releaseResult.SkippedNoContent
                        ? SessionActivityFactKind.ActorAttributeReleaseSkipped
                        : SessionActivityFactKind.ActorAttributeReleased;
                    string snapshotKind = releaseResult.SkippedNoContent ? "actor_attribute_release_skipped" : "actor_attribute_released";
                    string eventName = releaseResult.SkippedNoContent ? "ActorAttributeReleaseSkipped" : "ActorAttributeReleased";

                    SessionActivityIdentity outcomeIdentity = BuildIdentity(definition, stage, entrySequence);
                    _state.SetCurrentIdentity(outcomeIdentity, stage);
                    EmitFact(facts, factKind, outcomeIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    EmitSnapshot(snapshots, snapshotKind, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='{eventName}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}' source='{command.Source}' reasonDetail='{command.Reason}'.", releaseResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

                    if (releaseResult.SkippedNoContent)
                    {
                        skippedCount += 1;
                    }
                    else
                    {
                        releasedCount += 1;
                    }

                    _activeActorAttributeCapabilitiesByActorInstanceId.Remove(actorInstanceId);
                    releasedOrSkippedCount += 1;
                }

                if (releasedOrSkippedCount == 0)
                {
                    skippedCount += 1;
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeReleaseSkipped);
                    EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                    EmitSnapshot(snapshots, "actor_attribute_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                }
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeReleaseCompleted);
            EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            EmitSnapshot(snapshots, "actor_attribute_release_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeReleaseCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private static string BuildActorAttributeActivityIdentity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                return string.Empty;
            }

            return $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.ActivityOrdinal}|{identity.EntrySequence}";
        }

        private static string BuildActorAttributeIdList(ActorAttributeEndpoint endpoint)
        {
            if (endpoint == null || endpoint.RuntimeStates == null || endpoint.RuntimeStates.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new(endpoint.RuntimeStates.Count);
            for (int index = 0; index < endpoint.RuntimeStates.Count; index++)
            {
                ActorAttributeState state = endpoint.RuntimeStates[index];
                if (state == null || !state.AttributeId.IsValid)
                {
                    continue;
                }

                ids.Add(state.AttributeId.ToString());
            }

            if (ids.Count == 0)
            {
                return "<none>";
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join(",", ids);
        }

        private static ActorAttributeCommand BuildActorAttributeCommand(
            string pipelineIdentity,
            string activityIdentity,
            string actorInstanceId,
            ActorAttributeId attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue,
            string source,
            string reason)
        {
            return operation switch
            {
                ActorAttributeOperation.Set => ActorAttributeCommand.Set(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, setValue, source, reason),
                ActorAttributeOperation.Add => ActorAttributeCommand.Add(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, amount, source, reason),
                ActorAttributeOperation.Subtract => ActorAttributeCommand.Subtract(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, amount, source, reason),
                ActorAttributeOperation.ResetToInitial => ActorAttributeCommand.ResetToInitial(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, source, reason),
                ActorAttributeOperation.RestoreToMax => ActorAttributeCommand.RestoreToMax(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, source, reason),
                _ => new ActorAttributeCommand(pipelineIdentity, activityIdentity, actorInstanceId, attributeId, operation, amount, setValue, source, reason)
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


        internal readonly struct ParticipantBindingResolvedRecord
        {
            public ParticipantBindingResolvedRecord(
                string requirementId,
                ActivityParticipantRequirementKind participantKind,
                string resolvedParticipantId,
                bool required)
            {
                RequirementId = Normalize(requirementId);
                ParticipantKind = participantKind;
                ResolvedParticipantId = Normalize(resolvedParticipantId);
                Required = required;
            }

            public string RequirementId { get; }
            public ActivityParticipantRequirementKind ParticipantKind { get; }
            public string ResolvedParticipantId { get; }
            public bool Required { get; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(RequirementId) &&
                ParticipantKind != ActivityParticipantRequirementKind.Unknown &&
                !string.IsNullOrWhiteSpace(ResolvedParticipantId);
        }

        private void EmitPlayerInputBindingStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ParticipantBindingStageResult participantBindingResult)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.PlayerInputBindingStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player input binding started.");
            EmitSnapshot(
                snapshots,
                "player_input_binding_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player input binding started.");

            if (!participantBindingResult.IsValid || !IsSameActivityCycle(participantBindingResult.Identity, BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence)))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed because participant binding result is missing or foreign/stale.");
                EmitSnapshot(
                    snapshots,
                    "player_input_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed because participant binding result is missing or foreign/stale.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][PlayerInputBinding] Missing or foreign participant binding result activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            PlayerInputBindingStageResult stageResult;
            try
            {
                stageResult = PlayerInputBindingStage.Execute(
                    startedIdentity,
                    participantBindingResult,
                    _playerInputBindingAdapter,
                    _activityPlayerActorRegistry,
                    command.Source,
                    command.Reason);
            }
            catch (Exception exception)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed error='{exception.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "player_input_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed error='{exception.Message}'.");
                throw;
            }

            if (stageResult.IsSkipped)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingSkippedNoRequiredInput, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.PlayerInputBindingSkippedNoRequiredInput);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingSkippedNoRequiredInput,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding skipped because no required controllable participant was resolved.");
                EmitSnapshot(
                    snapshots,
                    "player_input_binding_skipped_no_required_input",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding skipped because no required controllable participant was resolved.");

                SessionActivityIdentity skippedCompletedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingCompleted, entrySequence);
                _state.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.PlayerInputBindingCompleted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingCompleted,
                    skippedCompletedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding completed with skip.");
                EmitSnapshot(
                    snapshots,
                    "player_input_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding completed with skip.");
                return;
            }

            IReadOnlyList<PlayerInputBindingRequirement> requirements = stageResult.Requirements;
            for (int index = 0; index < requirements.Count; index++)
            {
                PlayerInputBindingRequirement requirement = requirements[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingCommandIssued,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding command issued requirementId='{requirement.RequirementId}' playerSlotId='{requirement.PlayerSlotId}' playerActorId='{requirement.PlayerActorId}' required='{requirement.Required}'.");
            }

            int requiredCount = stageResult.RequiredCount;
            IReadOnlyList<PlayerInputBindingRecord> records = stageResult.Records;
            int requiredBoundCount = 0;
            for (int index = 0; index < records.Count; index++)
            {
                PlayerInputBindingRecord record = records[index];
                if (!record.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerInputBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player input binding failed because adapter produced invalid record index='{index}'.");
                    EmitSnapshot(
                        snapshots,
                        "player_input_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' player input binding failed because adapter produced invalid record index='{index}'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][PlayerInputBinding] Invalid binding record activityId='{definition.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                if (record.Requirement.Required)
                {
                    requiredBoundCount += 1;
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBound,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input bound requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.Requirement.PlayerActorId}' observedInput='{record.ObservedInputId}'.");
            }

            if (requiredBoundCount < requiredCount)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                EmitSnapshot(
                    snapshots,
                    "player_input_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][PlayerInputBinding] Required binding incomplete activityId='{definition.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.PlayerInputBindingCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.PlayerInputBindingCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");
            EmitSnapshot(
                snapshots,
                "player_input_binding_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");
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

        private void EmitMovementBindingStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ParticipantBindingStageResult participantBindingResult)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingStarted, entrySequence);
            _permissionRuntime.BeginPermissionScope(
                startedIdentity.PipelineId,
                startedIdentity.SessionId,
                startedIdentity.ActivityId,
                startedIdentity.EntrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.MovementBindingStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' movement binding started.");
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            EmitSnapshot(
                snapshots,
                "movement_binding_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' movement binding started.");

            if (!participantBindingResult.IsValid || !IsSameActivityCycle(participantBindingResult.Identity, BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence)))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed because participant binding result is missing or foreign/stale.");
                EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed because participant binding result is missing or foreign/stale.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] Missing or foreign participant binding result activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            MovementBindingStageResult stageResult;
            try
            {
                stageResult = PlayerMovementBindingStage.Execute(
                    startedIdentity,
                    participantBindingResult,
                    _movementBindingAdapter,
                    _activityPlayerActorRegistry,
                    ResolveRetainedMovementTargetsOrEmpty(startedIdentity),
                    command.Source,
                    command.Reason);
            }
            catch (Exception exception)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed error='{exception.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed error='{exception.Message}'.");
                throw;
            }

            int requiredCount = stageResult.RequiredCount;
            if (requiredCount <= 0)
            {
                IReadOnlyList<PlayerActorIdentityRecord> retainedTargets = stageResult.RetainedTargets;
                if (stageResult.UsedRetainedTargets && retainedTargets.Count > 0)
                {
                    _movementControlTargetsForCurrentEntry = retainedTargets;
                    _movementControlEnableAllowedForCurrentEntry = true;
                    try
                    {
                        SyncPermissionReceiversForMovementTargets(startedIdentity, retainedTargets, command.Source, command.Reason);
                    }
                    catch (Exception exception)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                        _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                        EmitFact(
                            facts,
                            SessionActivityFactKind.MovementBindingFailed,
                            failedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' movement binding failed reason='permission_receiver_sync_failed' error='{exception.Message}'.");
                        EmitSnapshot(
                            snapshots,
                            "movement_binding_failed",
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' movement binding failed reason='permission_receiver_sync_failed' error='{exception.Message}'.");
                        throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] permission_receiver_sync_failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' detail='{exception.Message}'.", exception);
                    }

                    for (int index = 0; index < retainedTargets.Count; index++)
                    {
                        PlayerActorIdentityRecord retained = retainedTargets[index];
                        EmitFact(
                            facts,
                            SessionActivityFactKind.MovementBindingRetained,
                            startedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{definition.ActivityId}' movement binding retained playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}'.");
                        DebugUtility.Log(
                            typeof(SessionActivityPipeline),
                            $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingRetained' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Info);
                    }

                    SessionActivityIdentity retainedCompletedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingCompleted, entrySequence);
                    _state.SetCurrentIdentity(retainedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingCompleted,
                        retainedCompletedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' movement binding completed status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false'.");
                    DebugUtility.Log(
                        typeof(SessionActivityPipeline),
                        $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                    EmitSnapshot(
                        snapshots,
                        "movement_binding_completed",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' movement binding completed status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false'.");
                    return;
                }

                _movementControlTargetsForCurrentEntry = Array.Empty<PlayerActorIdentityRecord>();
                _movementControlEnableAllowedForCurrentEntry = false;
                _permissionRuntime.ReplaceReceivers(Array.Empty<ActivityCapabilityPermissionReceiverReference>());

                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingSkippedNoRequiredMovement, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.MovementBindingSkippedNoRequiredMovement);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingSkippedNoRequiredMovement,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding skipped because no movement capability target is required or retained.");
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingSkippedNoRequiredMovement' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                EmitSnapshot(
                    snapshots,
                    "movement_binding_skipped_no_required_movement",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding skipped because no movement capability target is required or retained.");

                SessionActivityIdentity skippedCompletedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingCompleted, entrySequence);
                _state.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingCompleted,
                    skippedCompletedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding completed status='NoMovementCapabilityRequired' controlEnabled='false'.");
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' status='NoMovementCapabilityRequired' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                EmitSnapshot(
                    snapshots,
                    "movement_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding completed status='NoMovementCapabilityRequired' controlEnabled='false'.");
                return;
            }

            IReadOnlyList<MovementBindingRequirement> requirements = stageResult.Requirements;
            for (int index = 0; index < requirements.Count; index++)
            {
                MovementBindingRequirement requirement = requirements[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingCommandIssued,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding command issued requirementId='{requirement.RequirementId}' playerSlotId='{requirement.PlayerSlotId}' playerActorId='{requirement.PlayerActorId}' required='{requirement.Required}'.");
            }

            IReadOnlyList<MovementBindingRecord> records = stageResult.Records;
            int requiredBoundCount = 0;
            for (int index = 0; index < records.Count; index++)
            {
                MovementBindingRecord record = records[index];
                if (!record.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                    EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' movement binding failed because adapter produced invalid record index='{index}'.");
                    EmitSnapshot(
                        snapshots,
                        "movement_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' movement binding failed because adapter produced invalid record index='{index}'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] Invalid binding record activityId='{definition.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                if (record.Requirement.Required)
                {
                    requiredBoundCount += 1;
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerMovementBound,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement bound requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.Requirement.PlayerActorId}' endpoint='{record.ObservedEndpoint}'.");
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][MovementBinding] event='PlayerMovementBound' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.Requirement.PlayerActorId}' endpoint='{record.ObservedEndpoint}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }

            if (requiredBoundCount < requiredCount)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] Required binding incomplete activityId='{definition.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            List<PlayerActorIdentityRecord> boundTargets = new(records.Count);
            for (int index = 0; index < records.Count; index++)
            {
                MovementBindingRecord record = records[index];
                boundTargets.Add(new PlayerActorIdentityRecord(
                    startedIdentity,
                    record.Requirement.PlayerSlotId,
                    record.Requirement.PlayerActorId));
            }
            _movementControlTargetsForCurrentEntry = boundTargets;
            _movementControlEnableAllowedForCurrentEntry = true;
            try
            {
                SyncPermissionReceiversForMovementTargets(startedIdentity, boundTargets, command.Source, command.Reason);
            }
            catch (Exception exception)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed reason='permission_receiver_sync_failed' error='{exception.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' movement binding failed reason='permission_receiver_sync_failed' error='{exception.Message}'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] permission_receiver_sync_failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' detail='{exception.Message}'.", exception);
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.MovementBindingCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.MovementBindingCompleted);
            EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' movement binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false'.");
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            EmitSnapshot(
                snapshots,
                "movement_binding_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' movement binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false'.");
        }

        private void EmitCameraBindingStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.CameraBindingStarted);
            EmitFact(facts, SessionActivityFactKind.CameraBindingStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding stage started.");

            ActivitySetupInventory inventory = _state.CurrentActivitySetupInventory;
            IReadOnlyList<CameraBindingRequirement> cameraRequirements = inventory.CameraBindingRequirements;
            IReadOnlyList<PlayerActorIdentityRecord> cameraTargets = ResolvePlayerActorCapabilityTargetsForCurrentEntry(startedIdentity);
            if (cameraTargets.Count > 0)
            {
                try
                {
                    BuildAndStoreActivityCapabilityInventoryForEntry(startedIdentity, cameraTargets, command.Source, command.Reason);
                }
                catch (Exception exception)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_build_failed' error='{exception.Message}'.");
                    EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_build_failed' error='{exception.Message}'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CameraBinding] camera_inventory_build_failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' detail='{exception.Message}'.", exception);
                }
            }

            ActivityCapabilityInventory cameraInventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult cameraValidation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool hasValidCameraInventory =
                cameraInventory.IsValid &&
                cameraValidation.IsValid &&
                string.Equals(cameraInventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(cameraInventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(cameraInventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) &&
                cameraInventory.Id.EntrySequence == startedIdentity.EntrySequence;

            int requiredCameraCount = 0;
            bool hasActivityCameraRequirement = false;
            for (int index = 0; index < cameraRequirements.Count; index++)
            {
                CameraBindingRequirement requirement = cameraRequirements[index];
                if (!requirement.IsValid || requirement.CameraBindingKind != ActivityCameraBindingRequirementKind.ActivityCamera)
                {
                    continue;
                }

                hasActivityCameraRequirement = true;
                if (requirement.Requirement.IsRequired)
                {
                    requiredCameraCount += 1;
                }
            }

            if (!hasActivityCameraRequirement)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reasonCode='no_activity_camera_requirement' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                return;
            }

            if (!hasValidCameraInventory)
            {
                if (requiredCameraCount > 0)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CameraBinding] Missing valid camera inventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reasonCode='camera_inventory_missing_or_invalid' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                return;
            }

            if (!TryResolveCameraTargetReferenceFromInventory(cameraInventory, cameraRequirements, out ActivityCameraTargetReference selectedCameraTarget))
            {
                if (requiredCameraCount > 0)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                    _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_target_missing_in_inventory'.");
                    EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_target_missing_in_inventory'.");
                    throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CameraBinding] Missing camera target reference activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_target_missing_in_inventory'.");
                EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_target_missing_in_inventory'.");
                DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reasonCode='camera_target_missing_in_inventory' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                return;
            }

            EmitFact(facts, SessionActivityFactKind.PlayerCameraEndpointResolved, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera endpoint resolved playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='PlayerCameraEndpointResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' capabilityId='{selectedCameraTarget.CapabilityId}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<IActivityCameraPreparationExecutor>(out IActivityCameraPreparationExecutor cameraExecutor) || cameraExecutor == null)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='activity_camera_preparation_executor_missing'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CameraBinding] IActivityCameraPreparationExecutor missing activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            ActivityCameraRebindTargetsCommand rebindCommand = new(startedIdentity.SessionId, selectedCameraTarget.TrackingTarget, selectedCameraTarget.LookAtTarget, command.Source, command.Reason);
            if (!cameraExecutor.TryRebindTargets(rebindCommand, out ActivityCameraRebindTargetsResult rebindResult, out string rebindReason) || rebindResult == null || !rebindResult.Success)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][CameraBinding] Rebind failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='{Normalize(rebindReason)}'.");
            }

            EmitFact(facts, SessionActivityFactKind.ActivityCameraTargetBound, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity camera target bound playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}'.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='ActivityCameraTargetBound' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

            SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.CameraBindingCompleted, entrySequence);
            _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.CameraBindingCompleted);
            EmitFact(facts, SessionActivityFactKind.CameraBindingCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding completed.");
            EmitSnapshot(snapshots, "camera_binding_completed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding completed.");
            DebugUtility.Log(typeof(SessionActivityPipeline), $"[OBS][SessionActivityPipeline][CameraBinding] event='CameraBindingCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        private IReadOnlyList<PlayerActorIdentityRecord> ResolveRetainedMovementTargetsOrEmpty(SessionActivityIdentity identity)
        {
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

                if (!_activityPlayerActorRegistry.TryResolveInstanceForControl(identity, candidate.PlayerActorId, out GameObject instance, out PlayerActorIdentityRecord observedIdentity) ||
                    instance == null ||
                    !observedIdentity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(observedIdentity.PlayerSlotId, candidate.PlayerSlotId, StringComparison.Ordinal))
                {
                    continue;
                }

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

                if (!string.Equals(inputState.PlayerSlotId, candidate.PlayerSlotId, StringComparison.Ordinal) ||
                    !string.Equals(inputState.PlayerActorId, candidate.PlayerActorId, StringComparison.Ordinal))
                {
                    continue;
                }

                resolved.Add(new PlayerActorIdentityRecord(
                    identity,
                    candidate.PlayerSlotId,
                    candidate.PlayerActorId));
            }

            return resolved.Count == 0 ? Array.Empty<PlayerActorIdentityRecord>() : resolved;
        }


        private void EmitParticipantCommandPlan(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity identity,
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> technicalPlanByParticipantId,
            IReadOnlyList<ActivityParticipantBindCommand> bindCommands,
            IReadOnlyList<ActivityParticipantMaterializationCommand> materializationCommands,
            IReadOnlyList<ActivityParticipantPlacementCommand> placementCommands,
            IReadOnlyList<ActivityParticipantResetCommand> resetCommands)
        {
            ActivityParticipantCommandPlan plan = new(
                identity,
                bindCommands,
                materializationCommands,
                placementCommands,
                resetCommands,
                command.Source,
                command.Reason);

            if (!plan.IsValid || !plan.HasCommands)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant command plan failed invalid plan.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Invalid participant command plan activityId='{definition.ActivityId}' entrySequence='{identity.EntrySequence}'.");
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantCommandPlanReady,
                identity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant command plan ready totalCommands='{plan.TotalCommandCount}' bind='{bindCommands.Count}' materialization='{materializationCommands.Count}' placement='{placementCommands.Count}' reset='{resetCommands.Count}' participantOwnership='RouteSession' activityOwnership='false' adapterExecution='true' commandOwner='SessionActivityPipeline' status='AdaptersConnected'.");
            EmitSnapshot(
                snapshots,
                "activity_participant_command_plan_ready",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' participant command plan ready totalCommands='{plan.TotalCommandCount}' adapterExecution='true' commandOwner='SessionActivityPipeline'.");

            for (int index = 0; index < bindCommands.Count; index++)
            {
                ActivityParticipantBindCommand bindCommand = bindCommands[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindCommandIssued,
                    bindCommand.Identity,
                    bindCommand.Source,
                    bindCommand.Reason,
                    $"'{definition.ActivityId}' participant bind command issued requirementId='{bindCommand.RequirementId}' requestedParticipantId='{(string.IsNullOrWhiteSpace(bindCommand.RequestedParticipantId) ? "<none>" : bindCommand.RequestedParticipantId)}' resolvedParticipantId='{bindCommand.ResolvedParticipantId}' roleId='{bindCommand.RoleId}' participantOwnership='RouteSession' activityOwnership='false' adapterExecution='true' commandOwner='SessionActivityPipeline'.");
            }

            for (int index = 0; index < materializationCommands.Count; index++)
            {
                ActivityParticipantMaterializationCommand materializationCommand = materializationCommands[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantMaterializationCommandIssued,
                    materializationCommand.Identity,
                    materializationCommand.Source,
                    materializationCommand.Reason,
                    $"'{definition.ActivityId}' participant materialization command issued requirementId='{materializationCommand.RequirementId}' resolvedParticipantId='{materializationCommand.ResolvedParticipantId}' participantKind='{materializationCommand.ParticipantKind}' needKind='{materializationCommand.NeedKind}' participantOwnership='RouteSession' activityOwnership='false' adapterExecution='true' commandOwner='SessionActivityPipeline'.");
            }

            for (int index = 0; index < placementCommands.Count; index++)
            {
                ActivityParticipantPlacementCommand placementCommand = placementCommands[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantPlacementCommandIssued,
                    placementCommand.Identity,
                    placementCommand.Source,
                    placementCommand.Reason,
                    $"'{definition.ActivityId}' participant placement command issued requirementId='{placementCommand.RequirementId}' resolvedParticipantId='{placementCommand.ResolvedParticipantId}' placementRequirementId='{(string.IsNullOrWhiteSpace(placementCommand.PlacementRequirementId) ? "<none>" : placementCommand.PlacementRequirementId)}' placementScope='ActivityLocal' participantOwnership='RouteSession' activityOwnership='false' adapterExecution='true' commandOwner='SessionActivityPipeline'.");
            }

            for (int index = 0; index < resetCommands.Count; index++)
            {
                ActivityParticipantResetCommand resetCommand = resetCommands[index];
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantResetCommandIssued,
                    resetCommand.Identity,
                    resetCommand.Source,
                    resetCommand.Reason,
                    $"'{definition.ActivityId}' participant reset command issued requirementId='{resetCommand.RequirementId}' resolvedParticipantId='{resetCommand.ResolvedParticipantId}' resetGroups='{FormatActivityStateResetGroups(resetCommand.ResetGroups)}' participantOwnership='RouteSession' activityOwnership='false' adapterExecution='true' commandOwner='SessionActivityPipeline'.");
            }

            ExecuteParticipantCommandPlan(definition, command, facts, snapshots, identity, plan, technicalPlanByParticipantId);
        }

        private void ExecuteParticipantCommandPlan(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity identity,
            ActivityParticipantCommandPlan plan,
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> technicalPlanByParticipantId)
        {
            try
            {
                _activityPlayerActorRegistry.BeginActivityScope(identity);
                Dictionary<string, PlayerActorIdentityRecord> ensuredActors = new(StringComparer.Ordinal);
                Dictionary<string, ActivityParticipantPlacementCommand> placementByParticipant = new(StringComparer.Ordinal);

                for (int index = 0; index < plan.PlacementCommands.Count; index++)
                {
                    ActivityParticipantPlacementCommand placementCommand = plan.PlacementCommands[index];
                    placementByParticipant[placementCommand.ResolvedParticipantId] = placementCommand;
                }

                for (int index = 0; index < plan.MaterializationCommands.Count; index++)
                {
                    ActivityParticipantMaterializationCommand materializationCommand = plan.MaterializationCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureParticipantMaterialized(
                        definition,
                        identity,
                        materializationCommand,
                        technicalPlanByParticipantId,
                        command.Source,
                        command.Reason);
                    ensuredActors[materializationCommand.ResolvedParticipantId] = actorIdentity;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantMaterialized,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant materialization applied requirementId='{materializationCommand.RequirementId}' resolvedParticipantId='{materializationCommand.ResolvedParticipantId}' needKind='{materializationCommand.NeedKind}' adapterExecution='true' commandOwner='SessionActivityPipeline' participantOwnership='RouteSession' activityOwnership='false'.");
                }

                for (int index = 0; index < plan.BindCommands.Count; index++)
                {
                    ActivityParticipantBindCommand bindCommand = plan.BindCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityOrFail(bindCommand.ResolvedParticipantId, ensuredActors, definition, "bind");
                    PlayerActorParticipationEnterCommand enterCommand = new(identity, new[] { actorIdentity }, bindCommand.Source, bindCommand.Reason);
                    IReadOnlyList<PlayerActorParticipationEnterRecord> records = _playerActorParticipationAdapter.Execute(
                        enterCommand,
                        identity,
                        _activityPlayerActorRegistry);
                    if (records.Count != 1 || !records[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant bind apply record for participantId='{bindCommand.ResolvedParticipantId}' requirementId='{bindCommand.RequirementId}'.");
                    }

                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantBindApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant bind applied requirementId='{bindCommand.RequirementId}' resolvedParticipantId='{bindCommand.ResolvedParticipantId}' roleId='{bindCommand.RoleId}' adapterExecution='true' commandOwner='SessionActivityPipeline' participantOwnership='RouteSession' activityOwnership='false'.");
                }

                for (int index = 0; index < plan.PlacementCommands.Count; index++)
                {
                    ActivityParticipantPlacementCommand placementCommand = plan.PlacementCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityOrFail(placementCommand.ResolvedParticipantId, ensuredActors, definition, "placement");
                    SessionActivityPlayerTechnicalPlanEntry definitionEntry =
                        ResolveTechnicalPlanEntryOrFail(definition, placementCommand.ResolvedParticipantId, technicalPlanByParticipantId, "placement");

                    bool placementDeclared;
                    bool placementRequired;
                    bool placementOptional;
                    bool hasPlacement;
                    Vector3 placementPosition;
                    Vector3 placementEuler;
                    ResolvePlacementPlanFromDefinition(definitionEntry, out placementDeclared, out placementRequired, out placementOptional, out hasPlacement, out placementPosition, out placementEuler);
                    string placementId = ResolvePlacementIdForCommand(placementCommand, definitionEntry);

                    ActorResetTargetRef placementTarget = new(
                        BuildActorResetActorRef(identity, actorIdentity, definition, "placement"),
                        new[] { ActorResetGroup.Placement },
                        placementId,
                        placementDeclared,
                        placementRequired,
                        placementOptional,
                        hasPlacement,
                        placementPosition,
                        placementEuler);
                    ActorResetCommand placementResetCommand = new(identity, new[] { placementTarget }, placementCommand.Source, placementCommand.Reason);
                    IReadOnlyList<ActorResetResult> placementRecords = _actorResetAdapter.Execute(
                        placementResetCommand,
                        identity);
                    if (placementRecords.Count != 1 || !placementRecords[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant placement apply record for participantId='{placementCommand.ResolvedParticipantId}' requirementId='{placementCommand.RequirementId}'.");
                    }

                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantPlacementApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant placement applied requirementId='{placementCommand.RequirementId}' resolvedParticipantId='{placementCommand.ResolvedParticipantId}' placementRequirementId='{(string.IsNullOrWhiteSpace(placementCommand.PlacementRequirementId) ? "<none>" : placementCommand.PlacementRequirementId)}' appliedGroups='{placementRecords[0].AppliedGroups.Count}' skippedGroups='{placementRecords[0].SkippedGroups.Count}' adapterExecution='true' commandOwner='SessionActivityPipeline' participantOwnership='RouteSession' activityOwnership='false'.");
                }

                for (int index = 0; index < plan.ResetCommands.Count; index++)
                {
                    ActivityParticipantResetCommand resetCommand = plan.ResetCommands[index];
                    PlayerActorIdentityRecord actorIdentity = EnsureResolvedActorIdentityOrFail(resetCommand.ResolvedParticipantId, ensuredActors, definition, "reset");
                    SessionActivityPlayerTechnicalPlanEntry definitionEntry =
                        ResolveTechnicalPlanEntryOrFail(definition, resetCommand.ResolvedParticipantId, technicalPlanByParticipantId, "reset");
                    ActivityParticipantPlacementCommand placementCommand = placementByParticipant.TryGetValue(resetCommand.ResolvedParticipantId, out ActivityParticipantPlacementCommand mappedPlacement)
                        ? mappedPlacement
                        : default;

                    bool placementDeclared;
                    bool placementRequired;
                    bool placementOptional;
                    bool hasPlacement;
                    Vector3 placementPosition;
                    Vector3 placementEuler;
                    ResolvePlacementPlanFromDefinition(definitionEntry, out placementDeclared, out placementRequired, out placementOptional, out hasPlacement, out placementPosition, out placementEuler);
                    string placementId = ResolvePlacementIdForCommand(placementCommand, definitionEntry);

                    ActorResetTargetRef resetTarget = new(
                        BuildActorResetActorRef(identity, actorIdentity, definition, "reset"),
                        MapResetGroupsOrFail(resetCommand.ResetGroups),
                        placementId,
                        placementDeclared,
                        placementRequired,
                        placementOptional,
                        hasPlacement,
                        placementPosition,
                        placementEuler);
                    ActorResetCommand technicalResetCommand = new(identity, new[] { resetTarget }, resetCommand.Source, resetCommand.Reason);
                    IReadOnlyList<ActorResetResult> resetRecords = _actorResetAdapter.Execute(
                        technicalResetCommand,
                        identity);
                    if (resetRecords.Count != 1 || !resetRecords[0].IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid participant reset apply record for participantId='{resetCommand.ResolvedParticipantId}' requirementId='{resetCommand.RequirementId}'.");
                    }
                    ValidateRequiredResetGroupsOrFail(resetCommand, resetRecords[0]);

                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantResetApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' participant reset applied requirementId='{resetCommand.RequirementId}' resolvedParticipantId='{resetCommand.ResolvedParticipantId}' resetGroups='{FormatActivityStateResetGroups(resetCommand.ResetGroups)}' appliedGroups='{resetRecords[0].AppliedGroups.Count}' skippedGroups='{resetRecords[0].SkippedGroups.Count}' adapterExecution='true' commandOwner='SessionActivityPipeline' participantOwnership='RouteSession' activityOwnership='false'.");
                }
            }
            catch (Exception exception)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantSetupFailed,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant setup failed commandOwner='SessionActivityPipeline' participantOwnership='RouteSession' activityOwnership='false' error='{exception.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_setup_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant setup failed error='{exception.Message}'.");
                throw;
            }
        }

        private static IReadOnlyList<ActivityStateResetGroup> BuildDefaultParticipantResetGroups()
        {
            return new[]
            {
                ActivityStateResetGroup.Placement,
                ActivityStateResetGroup.ActivityParticipation,
            };
        }

        private static Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> BuildTechnicalPlanMap(IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> technicalPlanEntries)
        {
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> map = new(StringComparer.Ordinal);
            if (technicalPlanEntries == null || technicalPlanEntries.Count == 0)
            {
                return map;
            }

            for (int index = 0; index < technicalPlanEntries.Count; index++)
            {
                SessionActivityPlayerTechnicalPlanEntry entry = technicalPlanEntries[index];
                if (entry.IsValid && !map.ContainsKey(entry.ParticipantId))
                {
                    map.Add(entry.ParticipantId, entry);
                }
            }

            return map;
        }

        private PlayerActorIdentityRecord EnsureParticipantMaterialized(
            SessionActivityDefinition definition,
            SessionActivityIdentity identity,
            ActivityParticipantMaterializationCommand command,
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> technicalPlanByParticipantId,
            string source,
            string reason)
        {
            string participantId = Normalize(command.ResolvedParticipantId);
            if (_activityPlayerActorRegistry.TryGetRetainedForPlayerSlot(identity, participantId, out GameObject retainedInstance, out PlayerActorIdentityRecord retainedIdentity))
            {
                if (retainedInstance == null)
                {
                    throw new InvalidOperationException($"Retained participant instance is null participantId='{participantId}'.");
                }

                PlayerActorIdentity identityComponent = retainedInstance.GetComponent<PlayerActorIdentity>();
                if (identityComponent == null)
                {
                    throw new InvalidOperationException($"Retained participant is missing PlayerActorIdentity component participantId='{participantId}'.");
                }

                PlayerActorIdentityRecord reboundIdentity = BuildParticipantActorIdentity(identity, participantId);
                identityComponent.Bind(
                    identity.PipelineId,
                    identity.SessionId,
                    identity.ActivityId,
                    identity.ActivityOrdinal,
                    identity.EntrySequence,
                    reboundIdentity.PlayerSlotId,
                    reboundIdentity.PlayerActorId);
                EnsurePlayerRuntimeActorIdentityBoundOrFail(retainedInstance, identity, participantId, "retained_rebind");
                _activityPlayerActorRegistry.RegisterRetainedParticipation(identity, reboundIdentity, retainedInstance);
                return reboundIdentity;
            }

            SessionActivityPlayerTechnicalPlanEntry definitionEntry =
                ResolveTechnicalPlanEntryOrFail(definition, participantId, technicalPlanByParticipantId, "materialization");
            if (definitionEntry.Prefab == null)
            {
                throw new InvalidOperationException(
                    $"missing_route_session_participant_technical_plan: participant prefab missing participantId='{participantId}' activityId='{definition.ActivityId}' operation='materialization' requirementId='{command.RequirementId}'.");
            }

            Vector3 localPosition = definitionEntry.PlacementMode == Actors.Semantic.Preparation.ActorPlacementMode.FixedTransform
                ? definitionEntry.LocalPosition
                : Vector3.zero;
            Vector3 localEuler = definitionEntry.PlacementMode == Actors.Semantic.Preparation.ActorPlacementMode.FixedTransform
                ? definitionEntry.LocalEulerAngles
                : Vector3.zero;
            PlayerActorIdentityRecord actorIdentity = BuildParticipantActorIdentity(identity, participantId);
            PlayerActorEntryPlan plan = new(actorIdentity, definitionEntry.Prefab, localPosition, localEuler);
            PlayerActorMaterializationCommand technicalCommand = new(identity, new[] { plan }, source, reason);
            IReadOnlyList<PlayerActorMaterializationRecord> records = _playerActorMaterializationAdapter.Execute(technicalCommand, identity);
            if (records.Count != 1 || !records[0].IsValid)
            {
                throw new InvalidOperationException(
                    $"Materialization adapter returned invalid record participantId='{participantId}' requirementId='{command.RequirementId}'.");
            }

            EnsurePlayerRuntimeActorIdentityBoundOrFail(records[0].Instance, identity, participantId, "materialization");
            _activityPlayerActorRegistry.RegisterMaterialized(records[0].ActorIdentity, records[0].Instance);
            return records[0].ActorIdentity;
        }

        private static void EnsurePlayerRuntimeActorIdentityBoundOrFail(
            GameObject actorInstance,
            SessionActivityIdentity identity,
            string participantId,
            string operation)
        {
            if (actorInstance == null)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='actor_instance_null'.");
            }

            Actor runtimeActor = actorInstance.GetComponent<Actor>();
            if (runtimeActor == null)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_missing'.");
            }

            ActorInstanceId runtimeActorInstanceId = ActorInstanceId.FromScopedIdentity(
                identity,
                ActorKind.Player,
                runtimeActor.ActorId,
                runtimeActor.ActorScopeMetadata,
                runtimeActor.ActorScopeMetadata.ToString());
            if (!runtimeActorInstanceId.IsValid)
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_instance_id_invalid'.");
            }

            runtimeActor.SetRuntimeActorInstanceId(runtimeActorInstanceId);
            if (!runtimeActor.RuntimeActorInstanceId.IsValid ||
                !string.Equals(runtimeActor.RuntimeActorInstanceId.Value, runtimeActorInstanceId.Value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"player_actor_runtime_identity_missing_after_{operation}: participantId='{participantId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='runtime_actor_instance_id_not_bound'.");
            }
        }

        private static PlayerActorIdentityRecord BuildParticipantActorIdentity(SessionActivityIdentity identity, string participantId)
        {
            string normalized = Normalize(participantId);
            if (!identity.IsValid || string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("Cannot build participant actor identity with invalid inputs.");
            }

            return new PlayerActorIdentityRecord(identity, normalized, $"{identity.SessionId}|{normalized}");
        }

        private static SessionActivityPlayerTechnicalPlanEntry ResolveTechnicalPlanEntryOrFail(
            SessionActivityDefinition definition,
            string participantId,
            Dictionary<string, SessionActivityPlayerTechnicalPlanEntry> technicalPlanByParticipantId,
            string operation)
        {
            string normalized = Normalize(participantId);
            if (technicalPlanByParticipantId == null || !technicalPlanByParticipantId.TryGetValue(normalized, out SessionActivityPlayerTechnicalPlanEntry entry) || !entry.IsValid)
            {
                throw new InvalidOperationException(
                    $"missing_route_session_participant_technical_plan: activityId='{definition.ActivityId}' participantId='{normalized}' operation='{operation}'.");
            }

            return entry;
        }

        private static string ResolvePlacementIdForCommand(
            ActivityParticipantPlacementCommand placementCommand,
            SessionActivityPlayerTechnicalPlanEntry definitionEntry)
        {
            string commandPlacementId = placementCommand.IsValid ? Normalize(placementCommand.PlacementRequirementId) : string.Empty;
            if (!string.IsNullOrWhiteSpace(commandPlacementId))
            {
                return commandPlacementId;
            }

            return Normalize(definitionEntry.PlacementId);
        }

        private static void ResolvePlacementPlanFromDefinition(
            SessionActivityPlayerTechnicalPlanEntry definitionEntry,
            out bool placementDeclared,
            out bool placementRequired,
            out bool placementOptional,
            out bool hasPlacement,
            out Vector3 placementPosition,
            out Vector3 placementEuler)
        {
            placementDeclared = definitionEntry.PlacementMode != ActorPlacementMode.None;
            hasPlacement = definitionEntry.PlacementMode == ActorPlacementMode.FixedTransform;
            placementRequired =
                definitionEntry.PlacementMode == ActorPlacementMode.FixedTransform ||
                definitionEntry.PlacementMode == ActorPlacementMode.SceneMarker;
            placementOptional = placementDeclared && !placementRequired;
            placementPosition = hasPlacement ? definitionEntry.LocalPosition : Vector3.zero;
            placementEuler = hasPlacement ? definitionEntry.LocalEulerAngles : Vector3.zero;
        }

        private static IReadOnlyList<ActorResetGroup> MapResetGroupsOrFail(IReadOnlyList<ActivityStateResetGroup> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                throw new InvalidOperationException("Participant reset command requires at least one reset group.");
            }

            List<ActorResetGroup> mapped = new(groups.Count);
            for (int index = 0; index < groups.Count; index++)
            {
                mapped.Add(MapResetGroupOrFail(groups[index]));
            }

            return mapped;
        }

        private static ActorResetGroup MapResetGroupOrFail(ActivityStateResetGroup group)
        {
            return group switch
            {
                ActivityStateResetGroup.Placement => ActorResetGroup.Placement,
                ActivityStateResetGroup.ActivityParticipation => ActorResetGroup.ActivityParticipation,
                ActivityStateResetGroup.RuntimeTransient => ActorResetGroup.MovementTransient,
                _ => throw new InvalidOperationException($"Unsupported participant reset group mapping '{group}'."),
            };
        }

        private static PlayerActorIdentityRecord EnsureResolvedActorIdentityOrFail(
            string participantId,
            Dictionary<string, PlayerActorIdentityRecord> ensuredActors,
            SessionActivityDefinition definition,
            string operation)
        {
            string normalized = Normalize(participantId);
            if (ensuredActors == null || !ensuredActors.TryGetValue(normalized, out PlayerActorIdentityRecord identity) || !identity.IsValid)
            {
                throw new InvalidOperationException(
                    $"Participant '{normalized}' is not available for operation='{operation}' activityId='{definition.ActivityId}'.");
            }

            return identity;
        }

        private ActorResetActorRef BuildActorResetActorRef(
            SessionActivityIdentity identity,
            PlayerActorIdentityRecord actorIdentity,
            SessionActivityDefinition definition,
            string operation)
        {
            if (!actorIdentity.IsValid)
            {
                throw new InvalidOperationException("Cannot build ActorResetActorRef from invalid PlayerActorIdentityRecord.");
            }

            if (!_activityPlayerActorRegistry.TryResolveInstanceForControl(identity, actorIdentity.PlayerActorId, out GameObject actorInstance, out PlayerActorIdentityRecord observedIdentity) ||
                actorInstance == null ||
                !observedIdentity.IsValid)
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires active player actor instance. activityId='{definition.ActivityId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            Actor runtimeActor = actorInstance.GetComponent<Actor>();
            if (runtimeActor == null || !runtimeActor.RuntimeActorInstanceId.IsValid || string.IsNullOrWhiteSpace(runtimeActor.ActorId))
            {
                throw new InvalidOperationException(
                    $"Actor reset {operation} requires valid runtime actor identity. activityId='{definition.ActivityId}' playerSlotId='{actorIdentity.PlayerSlotId}' playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            return new ActorResetActorRef(
                identity,
                runtimeActor.ActorId,
                runtimeActor.RuntimeActorInstanceId.Value,
                ActorKind.Player,
                observedIdentity.PlayerActorId,
                observedIdentity.PlayerSlotId);
        }

        private static void ValidateRequiredResetGroupsOrFail(
            ActivityParticipantResetCommand resetCommand,
            ActorResetResult record)
        {
            if (record.SkippedGroups == null || record.SkippedGroups.Count == 0)
            {
                return;
            }

            if (record.SkippedGroupReasons == null || record.SkippedGroupReasons.Count == 0)
            {
                throw new InvalidOperationException(
                    $"required_reset_group_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ResolvedParticipantId}' skippedGroups='{record.SkippedGroups.Count}' reason='missing_skip_reason'.");
            }

            for (int index = 0; index < record.SkippedGroupReasons.Count; index++)
            {
                ActorResetSkippedGroupReason reason = record.SkippedGroupReasons[index];
                if (!reason.IsValid)
                {
                    continue;
                }

                if (string.Equals(reason.ReasonCode, "optional_placement_missing", StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"required_reset_group_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ResolvedParticipantId}' group='{reason.Group}' reason='{reason.ReasonCode}'.");
            }
        }

        private static string FormatActivityStateResetGroups(IReadOnlyList<ActivityStateResetGroup> resetGroups)
        {
            if (resetGroups == null || resetGroups.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", resetGroups);
        }

        private void BuildAndValidateActivitySetupInventory(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity setupIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            SessionActivityIdentity buildStartedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryBuildStarted, entrySequence);
            _state.SetCurrentIdentity(buildStartedIdentity, SessionActivityStage.ActivitySetupInventoryBuildStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryBuildStarted,
                buildStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory build started.");
            EmitSnapshot(
                snapshots,
                "activity_setup_inventory_build_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory build started.");

            ActivitySetupInventoryBuildContext buildContext = new(
                definition,
                setupIdentity,
                _state.CurrentActivityContentLoadedSet,
                command.Source,
                command.Reason);

            ActivitySetupInventoryBuildResult buildResult = _activitySetupInventoryBuilder.Build(buildContext);
            if (buildResult.IsFailed || !buildResult.IsValid)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidationFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_build_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory build failed. message='{buildResult.Message}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ActivitySetupInventory] Build failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' message='{buildResult.Message}'.");
            }

            _state.SetCurrentActivitySetupInventory(buildResult.Inventory);

            if (buildResult.IsSkipped)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivitySetupInventorySkippedNoRequirements);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory skipped because no requirements were declared. inventoryId='{buildResult.Inventory.InventoryId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_skipped_no_requirements",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory skipped because no requirements were declared.");
            }
            else
            {
                SessionActivityIdentity builtIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryBuilt, entrySequence);
                _state.SetCurrentIdentity(builtIdentity, SessionActivityStage.ActivitySetupInventoryBuilt);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryBuilt,
                    builtIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory built inventoryId='{buildResult.Inventory.InventoryId}' totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_built",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory built totalRequirements='{buildResult.Inventory.TotalRequirementCount}'.");
            }

            ActivitySetupInventoryValidationResult validationResult = _activitySetupInventoryValidator.Validate(buildResult.Inventory, command.Source, command.Reason);
            if (validationResult.IsFailed || !validationResult.IsValid)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidationFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivitySetupInventoryValidationFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivitySetupInventoryValidationFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}' message='{validationResult.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_setup_inventory_validation_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity setup inventory validation failed errors='{validationResult.Errors.Count}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ActivitySetupInventory] Validation failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' errors='{string.Join(" | ", validationResult.Errors)}'.");
            }

            SessionActivityIdentity validatedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupInventoryValidated, entrySequence);
            _state.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupInventoryValidated);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupInventoryValidated,
                validatedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory validated inventoryId='{validationResult.Inventory.InventoryId}' totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
            EmitSnapshot(
                snapshots,
                "activity_setup_inventory_validated",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity setup inventory validated totalRequirements='{validationResult.Inventory.TotalRequirementCount}' skipped='{validationResult.SkippedRequirementIds.Count}'.");
        }

        private void ObserveActivitySceneContractOrSkip(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
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
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity scene contract skipped because ActivityContentScenes had no ActivitySceneContractAuthoring. routeScene='{routeScene.name}' contentScenes=[{FormatList(contentSceneNames)}] routeContracts='{routeCandidates.Count}' fallback='disabled_for_activity_content'.");
                EmitSnapshot(
                    snapshots,
                    "activity_scene_contract_skipped_no_content",
                    command.Source,
                    command.Reason,
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
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity scene contract skipped as no-content in routeScene='{routeScene.name}'.");
                    EmitSnapshot(
                        snapshots,
                        "activity_scene_contract_skipped_no_content",
                        command.Source,
                        command.Reason,
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
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract observed scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}' {resolutionDetail}");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_observed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract observed scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}' discoveryMode='{contractSnapshot.DiscoveryMode}' revealSafety='{contractSnapshot.RevealSafety}' allowUndeclaredContributors='{contractSnapshot.AllowUndeclaredContributors}' declaredContributors='{contractSnapshot.DeclaredContributors.Count}' {resolutionDetail}");

            SessionActivityIdentity validatedIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(validatedIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivitySceneContractValidated,
                validatedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity scene contract validated scope='{resolutionScope}' sceneName='{selectedCandidate.SceneName}' sceneId='{contractSnapshot.ActivitySceneId}'.");
            EmitSnapshot(
                snapshots,
                "activity_scene_contract_validated",
                command.Source,
                command.Reason,
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

        private sealed class ActivityObjectEntryStage
        {
            private readonly SessionActivityPipeline _owner;

            public ActivityObjectEntryStage(SessionActivityPipeline owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Discover(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.DiscoverActivityObjectContributorsOrSkipCore(definition, command, facts, snapshots, entrySequence);
            }

            public void EmitCapabilityInventoryPreview(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitActivityCapabilityInventoryPreviewStageCore(definition, command, facts, snapshots, entrySequence);
            }

            public void ValidateSnapshotContract(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitObjectSnapshotContractValidationStageCore(definition, command, facts, snapshots, entrySequence);
            }

            public void Reset(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitObjectResetStageCore(definition, command, facts, snapshots, entrySequence);
            }

            public void RestoreSnapshot(
                SessionActivityDefinition definition,
                SessionActivityCommand command,
                List<SessionActivityFact> facts,
                List<SessionActivitySnapshot> snapshots,
                int entrySequence)
            {
                _owner.EmitObjectSnapshotRestoreStageCore(definition, command, facts, snapshots, entrySequence);
            }
        }

        private ActivityObjectEntryStage BuildActivityObjectEntryStage()
        {
            return new ActivityObjectEntryStage(this);
        }

        private void DiscoverActivityObjectContributorsOrSkip(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectEntryStage().Discover(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitNonPlayerActorDiscoveryStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity startedIdentity = BuildIdentity(definition, SessionActivityStage.NonPlayerActorDiscoveryStarted, entrySequence);
            _state.SetCurrentIdentity(startedIdentity, SessionActivityStage.NonPlayerActorDiscoveryStarted);
            EmitFact(facts, SessionActivityFactKind.NonPlayerActorDiscoveryStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery started.");
            EmitSnapshot(snapshots, "non_player_actor_discovery_started", command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery started.");

            try
            {
                ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
                bool canDiscoverFromLoadedSet = HasLoadedSetForCurrentEntry(loadedSet, definition, entrySequence) && loadedSet.HasScenes;
                NonPlayerActorDiscoveryStageResult discovery = NonPlayerActorDiscoveryStage.Execute(
                    definition,
                    startedIdentity,
                    loadedSet,
                    canDiscoverFromLoadedSet,
                    _activityNonPlayerActorRegistry);

                if (!discovery.HasAuthorizedSource)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.NonPlayerActorDiscoverySkipped, entrySequence);
                    _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.NonPlayerActorDiscoverySkipped);
                    EmitFact(facts, SessionActivityFactKind.NonPlayerActorDiscoverySkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery skipped reason='no_authorized_source'.");
                    EmitSnapshot(snapshots, "non_player_actor_discovery_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery skipped reason='no_authorized_source'.");
                }

                SessionActivityIdentity completedIdentity = BuildIdentity(definition, SessionActivityStage.NonPlayerActorDiscoveryCompleted, entrySequence);
                _state.SetCurrentIdentity(completedIdentity, SessionActivityStage.NonPlayerActorDiscoveryCompleted);
                EmitFact(facts, SessionActivityFactKind.NonPlayerActorDiscoveryCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}'.");
                EmitSnapshot(snapshots, "non_player_actor_discovery_completed", command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}'.");
            }
            catch (Exception ex)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.NonPlayerActorDiscoveryFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.NonPlayerActorDiscoveryFailed);
                EmitFact(facts, SessionActivityFactKind.NonPlayerActorDiscoveryFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery failed reason='{ex.Message}'.");
                EmitSnapshot(snapshots, "non_player_actor_discovery_failed", command.Source, command.Reason, $"'{definition.ActivityId}' non-player actor discovery failed reason='{ex.Message}'.");
                throw;
            }
        }

        private void EmitActivityCapabilityInventoryPreviewStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectEntryStage().EmitCapabilityInventoryPreview(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitActivityCapabilityInventoryPreviewStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity previewIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(previewIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview started scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
            EmitPreviewObservationLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewStarted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory preview started scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");

            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            bool hasDiscoveryForCurrentEntry = discoveryResult.IsValid && IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence);
            IReadOnlyList<PlayerActorIdentityRecord> previewActorTargets = ResolvePlayerActorCapabilityTargetsForCurrentEntry(previewIdentity);
            IReadOnlyList<ActorScanTarget> previewGenericActorTargets = BuildActorScanTargetsForCurrentEntry(previewIdentity, previewActorTargets, command.Source, command.Reason);
            bool hasActorTargets = previewGenericActorTargets != null && previewGenericActorTargets.Count > 0;

            if (!hasDiscoveryForCurrentEntry && !hasActorTargets)
            {
                _state.ClearCurrentActivityCapabilityInventoryPreview();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
                EmitPreviewObservationLog(
                    SessionActivityFactKind.ActivityCapabilityInventoryPreviewSkippedNoDiscovery,
                    previewIdentity,
                    $"'{definition.ActivityId}' activity capability inventory preview skipped reason='no_capability_sources' entrySequence='{entrySequence}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
                return;
            }

            ActivityCapabilityInventoryBuildResult buildResult = _activityCapabilityInventoryCoordinator.BuildForEntry(
                previewIdentity,
                hasDiscoveryForCurrentEntry ? discoveryResult : default,
                previewGenericActorTargets,
                command.Source,
                command.Reason);
            ActivityCapabilityInventory inventory = buildResult.Inventory;
            ActivityCapabilityInventoryValidationResult validationResult = buildResult.Validation;
            string capabilityKindsSummary = FormatCapabilityKindsSummary(inventory.Capabilities);
            _state.SetCurrentActivityCapabilityInventoryPreview(inventory, validationResult);
            string validationIssueCodes = FormatValidationIssueCodes(validationResult.Issues);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
            EmitPreviewObservationLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationStarted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory validation started entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");

            SessionActivityFactKind validationOutcomeKind = validationResult.Status switch
            {
                ActivityCapabilityInventoryValidationStatus.Passed => SessionActivityFactKind.ActivityCapabilityInventoryValidationPassed,
                ActivityCapabilityInventoryValidationStatus.PassedWithWarnings => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
                ActivityCapabilityInventoryValidationStatus.FailedPassive => SessionActivityFactKind.ActivityCapabilityInventoryValidationFailedPassive,
                _ => SessionActivityFactKind.ActivityCapabilityInventoryValidationWarning,
            };

            string validationOutcomeMessage =
                $"'{definition.ActivityId}' activity capability inventory validation outcome status='{validationResult.Status}' entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.";

            EmitFact(
                facts,
                validationOutcomeKind,
                previewIdentity,
                command.Source,
                command.Reason,
                validationOutcomeMessage);
            EmitPreviewObservationLog(validationOutcomeKind, previewIdentity, validationOutcomeMessage);

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryValidationCompleted,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
            EmitPreviewObservationLog(
                SessionActivityFactKind.ActivityCapabilityInventoryValidationCompleted,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory validation completed status='{validationResult.Status}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
            EmitPreviewObservationLog(
                SessionActivityFactKind.ActivityCapabilityInventoryPreviewObserved,
                previewIdentity,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
            EmitSnapshot(
                snapshots,
                "activity_capability_inventory_preview_observed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity capability inventory preview observed entrySequence='{entrySequence}' inventorySignature='{inventory.Id.Signature}' ownerCount='{inventory.OwnerCount}' capabilityCount='{inventory.CapabilityCount}' capabilityKinds='{capabilityKindsSummary}' unresolvedReports='{buildResult.UnresolvedReportCount}' issueCount='{validationResult.IssueCount}' warningCount='{validationResult.WarningCount}' errorCount='{validationResult.ErrorCount}' issueCodes='{validationIssueCodes}' scannerId='{_activityCapabilityInventoryCoordinator.ActivityObjectScannerId}'.");
        }

        private static void EmitPreviewObservationLog(
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string message)
        {
            Debug.Log(
                $"[OBS][SessionActivityPipeline][CapabilityInventoryPreview] fact='{kind}' stage='{identity.Stage}' entrySequence='{identity.EntrySequence}' activity='{identity.ActivityId}' message=\"{message}\"");
        }

        private void DiscoverActivityObjectContributorsOrSkipCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity discoveryIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(discoveryIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectContributorDiscoveryStarted,
                discoveryIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor discovery started.");
            EmitSnapshot(
                snapshots,
                "activity_object_contributor_discovery_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object contributor discovery started.");

            ActivityContentLoadedSet loadedSet = _state.CurrentActivityContentLoadedSet;
            if (!HasLoadedSetForCurrentEntry(loadedSet, definition, entrySequence) || !loadedSet.HasScenes)
            {
                _state.ClearCurrentActivityObjectContributorDiscoveryResult();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoverySkippedNoContent,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery skipped as no-content for current entry.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery skipped as no-content for current entry.");
                return;
            }

            try
            {
                List<ActivityObjectContributionReport> reports = new();
                for (int sceneIndex = 0; sceneIndex < loadedSet.Scenes.Count; sceneIndex++)
                {
                    ActivityContentLoadedSceneRecord record = loadedSet.Scenes[sceneIndex];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' has invalid loaded scene record at index '{sceneIndex}' for object contributor discovery.");
                    }

                    Scene contentScene = SceneManager.GetSceneByName(record.SceneName);
                    if (!contentScene.IsValid() || !contentScene.isLoaded)
                    {
                        throw new InvalidOperationException(
                            $"Activity '{definition.ActivityId}' contributor discovery requires loaded content scene '{record.SceneName}' for current entry.");
                    }

                    AppendContributorsFromSceneOrFail(
                        reports,
                        contentScene,
                        loadedSet,
                        record,
                        command.Source,
                        command.Reason);
                }

                ActivityObjectContributorDiscoveryResult result = new(
                    discoveryIdentity,
                    loadedSet.ContentProfileId,
                    reports,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}'.");

                if (!result.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Activity '{definition.ActivityId}' produced invalid object contributor discovery result.");
                }

                _state.SetCurrentActivityObjectContributorDiscoveryResult(result);

                for (int reportIndex = 0; reportIndex < reports.Count; reportIndex++)
                {
                    ActivityObjectContributionReport report = reports[reportIndex];
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectContributorDiscovered,
                        discoveryIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' contributor discovered contentProfileId='{report.ContentProfileId}' sceneName='{report.SceneName}' targetId='{report.TargetId}' roleId='{(string.IsNullOrWhiteSpace(report.RoleId) ? "<none>" : report.RoleId)}' contributorKind='{report.ContributorKind}' requiredness='{report.Requiredness}' resetGroups='{FormatActivityStateResetGroups(report.SupportedResetGroups)}' releaseKinds='{FormatReleaseKinds(report.SupportedReleaseKinds)}'.");
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryCompleted,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery completed discovered='{reports.Count}' contentProfileId='{loadedSet.ContentProfileId}'.");
            }
            catch (Exception exception)
            {
                _state.ClearCurrentActivityObjectContributorDiscoveryResult();
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectContributorDiscoveryFailed,
                    discoveryIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_object_contributor_discovery_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object contributor discovery failed error='{exception.Message}'.");
                throw;
            }
        }

        private void EmitObjectSnapshotContractValidationStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectEntryStage().ValidateSnapshotContract(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitObjectSnapshotContractValidationStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            SessionActivityIdentity validationIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(validationIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationStarted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot contract validation started.");

            if (!discoveryResult.IsValid || !IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence) || discoveryResult.Reports.Count == 0)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                    validationIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='0' skippedCount='0' failedCount='0' targetIds='<none>' providerPaths='<none>' restoreEndpointPaths='<none>' targetTransformPaths='<none>' mismatchReason='<none>'.");
                return;
            }

            int validatedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            string mismatchReason = "<none>";
            HashSet<string> targetIds = new(StringComparer.Ordinal);
            HashSet<string> providerPaths = new(StringComparer.Ordinal);
            HashSet<string> restoreEndpointPaths = new(StringComparer.Ordinal);
            HashSet<string> targetTransformPaths = new(StringComparer.Ordinal);

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, definition, entrySequence))
                {
                    continue;
                }

                targetIds.Add(report.TargetId);
                bool required = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
                GameObject targetObject = ResolveContributorObjectOrFail(definition, report);
                IActivityObjectSnapshotProvider[] providers = ResolveObjectSnapshotProviders(targetObject);
                IActivityObjectSnapshotRestoreEndpoint[] restoreEndpoints = ResolveObjectSnapshotRestoreEndpoints(targetObject, report);

                bool providerFound = TryResolveSupportingSnapshotProvider(report.TargetId, providers, out IActivityObjectSnapshotProvider provider);
                bool restoreFound = TryResolveSupportingSnapshotRestoreEndpoint(report.TargetId, restoreEndpoints, out IActivityObjectSnapshotRestoreEndpoint restoreEndpoint);

                string providerPath = "<none>";
                string providerTargetTransformPath = "<none>";
                string providerFailureReason = providerFound ? "<none>" : "snapshot_provider_missing";
                if (providerFound && provider is IActivityObjectSnapshotProviderContractView providerView)
                {
                    bool providerValid = providerView.TryDescribeContract(report.TargetId, out providerPath, out providerTargetTransformPath, out providerFailureReason);
                    if (!providerValid && string.IsNullOrWhiteSpace(providerFailureReason))
                    {
                        providerFailureReason = "snapshot_provider_contract_invalid";
                    }
                }
                else if (providerFound)
                {
                    providerFailureReason = "snapshot_provider_contract_view_missing";
                }

                string restorePath = "<none>";
                string restoreTargetTransformPath = "<none>";
                string restoreFailureReason = restoreFound ? "<none>" : "snapshot_restore_endpoint_missing";
                if (restoreFound && restoreEndpoint is IActivityObjectSnapshotRestoreEndpointContractView restoreView)
                {
                    bool restoreValid = restoreView.TryDescribeContract(report.TargetId, out restorePath, out restoreTargetTransformPath, out restoreFailureReason);
                    if (!restoreValid && string.IsNullOrWhiteSpace(restoreFailureReason))
                    {
                        restoreFailureReason = "snapshot_restore_contract_invalid";
                    }
                }
                else if (restoreFound)
                {
                    restoreFailureReason = "snapshot_restore_contract_view_missing";
                }

                providerPaths.Add(string.IsNullOrWhiteSpace(providerPath) ? "<none>" : providerPath);
                restoreEndpointPaths.Add(string.IsNullOrWhiteSpace(restorePath) ? "<none>" : restorePath);
                if (!string.IsNullOrWhiteSpace(providerTargetTransformPath) && !string.Equals(providerTargetTransformPath, "<none>", StringComparison.Ordinal))
                {
                    targetTransformPaths.Add(providerTargetTransformPath);
                }

                if (!string.IsNullOrWhiteSpace(restoreTargetTransformPath) && !string.Equals(restoreTargetTransformPath, "<none>", StringComparison.Ordinal))
                {
                    targetTransformPaths.Add(restoreTargetTransformPath);
                }

                bool providerContractValid = providerFound && string.Equals(providerFailureReason, "resolved", StringComparison.Ordinal);
                bool restoreContractValid = restoreFound && string.Equals(restoreFailureReason, "resolved", StringComparison.Ordinal);
                bool transformMismatch = providerContractValid &&
                                         restoreContractValid &&
                                         !string.Equals(providerTargetTransformPath, restoreTargetTransformPath, StringComparison.Ordinal);
                string failureReason = transformMismatch
                    ? "snapshot_restore_target_transform_mismatch"
                    : ResolveSnapshotContractFailureReason(providerFailureReason, restoreFailureReason, providerFound, restoreFound);

                bool hasDeclaredSnapshotCapability = providerFound || restoreFound;
                bool hasNoSnapshotCapability = !providerFound && !restoreFound;
                bool contractValid = providerContractValid && restoreContractValid && !transformMismatch;

                if (hasDeclaredSnapshotCapability && contractValid)
                {
                    validatedCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotContractValidated,
                        validationIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot contract validated targetId='{report.TargetId}' requiredness='{report.Requiredness}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='{providerTargetTransformPath}'.");
                    continue;
                }

                if (hasNoSnapshotCapability && !required)
                {
                    skippedCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotContractSkippedOptional,
                        validationIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot contract skipped optional targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='snapshot_capability_not_declared_optional' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' targetTransformPath='<none>'.");
                    continue;
                }

                failedCount += 1;
                if (transformMismatch)
                {
                    mismatchReason = "snapshot_restore_target_transform_mismatch";
                }
                else if (!string.Equals(failureReason, "<none>", StringComparison.Ordinal))
                {
                    mismatchReason = failureReason;
                }

                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotContractFailed,
                    validationIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot contract failed targetId='{report.TargetId}' requiredness='{report.Requiredness}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
                throw new InvalidOperationException(
                    $"snapshot_contract_validation_failed: activityId='{definition.ActivityId}' targetId='{report.TargetId}' reason='{failureReason}' providerPath='{providerPath}' restoreEndpointPath='{restorePath}' providerTargetTransformPath='{providerTargetTransformPath}' restoreTargetTransformPath='{restoreTargetTransformPath}'.");
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted,
                validationIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot contract validation completed validationStarted='true' validatedCount='{validatedCount}' skippedCount='{skippedCount}' failedCount='{failedCount}' targetIds='{JoinValues(targetIds)}' providerPaths='{JoinValues(providerPaths)}' restoreEndpointPaths='{JoinValues(restoreEndpointPaths)}' targetTransformPaths='{JoinValues(targetTransformPaths)}' mismatchReason='{mismatchReason}'.");
        }

        private void EmitObjectResetStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectEntryStage().Reset(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitObjectResetStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity resetIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(resetIdentity, SessionActivityStage.ActivitySetupStarted);
            IReadOnlyList<PlayerActorIdentityRecord> resetActorTargets = ResolvePlayerActorCapabilityTargetsForCurrentEntry(resetIdentity);
            IReadOnlyList<ActorScanTarget> resetScanTargets = BuildActorScanTargetsForCurrentEntry(
                resetIdentity,
                resetActorTargets,
                command.Source,
                command.Reason);
            ActivityObjectContributorDiscoveryResult resetDiscoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            bool hasResetDiscoveryForCurrentEntry =
                resetDiscoveryResult.IsValid &&
                IsDiscoveryResultForCurrentEntry(resetDiscoveryResult, definition, entrySequence);
            ActivityCapabilityInventoryBuildResult resetInventoryBuildResult = _activityCapabilityInventoryCoordinator.BuildForEntry(
                resetIdentity,
                hasResetDiscoveryForCurrentEntry ? resetDiscoveryResult : default,
                resetScanTargets,
                command.Source,
                command.Reason);
            ActivityResetCommand resetCommand = new(
                resetIdentity,
                definition,
                command.Source,
                command.Reason);
            ActivityResetContext resetContext = new(
                resetDiscoveryResult,
                resetInventoryBuildResult.Inventory,
                resetInventoryBuildResult.Validation);

            ActivityResetStage.Execute(
                resetCommand,
                resetContext,
                IsDiscoveryResultForCurrentEntry,
                IsReportForCurrentEntry,
                HasRequiredResetContributor,
                ResolveObjectResetEndpointsFromInventory,
                ExecuteObjectResetCommand,
                IsObjectResetResultForCurrentEntry,
                (kind, message) => EmitFact(
                    facts,
                    kind,
                    resetIdentity,
                    command.Source,
                    command.Reason,
                    message),
                (snapshotKind, message) => EmitSnapshot(
                    snapshots,
                    snapshotKind,
                    command.Source,
                    command.Reason,
                    message));
        }

        private void EmitObjectSnapshotRestoreStage(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            BuildActivityObjectEntryStage().RestoreSnapshot(definition, command, facts, snapshots, entrySequence);
        }

        private void EmitObjectSnapshotRestoreStageCore(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            ActivityObjectContributorDiscoveryResult discoveryResult = _state.CurrentActivityObjectContributorDiscoveryResult;
            SessionActivityIdentity restoreIdentity = BuildIdentity(definition, SessionActivityStage.ActivitySetupStarted, entrySequence);
            _state.SetCurrentIdentity(restoreIdentity, SessionActivityStage.ActivitySetupStarted);
            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot restore started.");

            if (!TryResolveRouteLoadedSnapshotPayload(out LoadedSessionActivitySnapshotPayload loadedPayload, out string payloadFailureReason))
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoPayload,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='no_loaded_payload' failureReason='{payloadFailureReason}'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='false' payloadObjectCount='0' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            if (!IsLoadedSnapshotPayloadForCurrentActivity(loadedPayload, definition))
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='payload_foreign_or_stale' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
                throw new InvalidOperationException(
                    $"payload_foreign_or_stale: activityId='{definition.ActivityId}' entrySequence='{entrySequence}' payloadSessionStateId='{loadedPayload.SessionStateId}' payloadActivityId='{loadedPayload.ActivityId}' payloadSourceEntrySequence='{loadedPayload.SourceEntrySequence}'.");
            }

            if (!discoveryResult.IsValid || !IsDiscoveryResultForCurrentEntry(discoveryResult, definition, entrySequence) || discoveryResult.Reports.Count == 0)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadObjectCount='{loadedPayload.Objects.Count}'.");
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadObjectCount='{loadedPayload.Objects.Count}' matchedTargetCount='0' restoredCount='0' restoreFailed='false'.");
                return;
            }

            Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> payloadByTargetId = BuildLoadedSnapshotPayloadByTargetId(loadedPayload.Objects);
            int matchedTargetCount = 0;
            int restoredCount = 0;
            bool restoreFailed = false;
            HashSet<string> matchedTargetIds = new(StringComparer.Ordinal);
            ActivityCapabilityInventory restoreInventory = _state.CurrentActivityCapabilityInventoryPreview;
            ActivityCapabilityInventoryValidationResult restoreInventoryValidation = _state.CurrentActivityCapabilityInventoryPreviewValidation;
            bool hasValidRestoreInventory =
                restoreInventory.IsValid &&
                restoreInventoryValidation.IsValid &&
                string.Equals(restoreInventory.Id.PipelineId, restoreIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(restoreInventory.Id.SessionStateId, restoreIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(restoreInventory.Id.ActivityId, restoreIdentity.ActivityId, StringComparison.Ordinal) &&
                restoreInventory.Id.EntrySequence == restoreIdentity.EntrySequence;

            if (!hasValidRestoreInventory)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_inventory_missing_or_invalid' entrySequence='{entrySequence}' inventoryValid='{restoreInventory.IsValid.ToString().ToLowerInvariant()}' validationValid='{restoreInventoryValidation.IsValid.ToString().ToLowerInvariant()}'.");
                throw new InvalidOperationException(
                    $"restore_inventory_missing_or_invalid: activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            for (int reportIndex = 0; reportIndex < discoveryResult.Reports.Count; reportIndex++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[reportIndex];
                if (!report.IsValid || !IsReportForCurrentEntry(report, definition, entrySequence))
                {
                    continue;
                }

                if (!payloadByTargetId.TryGetValue(report.TargetId, out LoadedSessionActivitySnapshotPayloadObject payloadObject) || !payloadObject.IsValid)
                {
                    continue;
                }

                matchedTargetCount += 1;
                matchedTargetIds.Add(report.TargetId);
                IActivityObjectSnapshotRestoreEndpoint[] endpoints = ResolveObjectSnapshotRestoreEndpointsFromInventory(restoreInventory, report);
                ActivityObjectSnapshotRestoreCommand restoreCommand = new(
                    restoreIdentity,
                    PipelineId,
                    _sessionId,
                    definition.ActivityId,
                    definition.ActivityOrdinal,
                    entrySequence,
                    report.TargetId,
                    ActivityObjectSnapshotCoordinateSpace.WorldTransform,
                    payloadObject.PositionX,
                    payloadObject.PositionY,
                    payloadObject.PositionZ,
                    payloadObject.RotationX,
                    payloadObject.RotationY,
                    payloadObject.RotationZ,
                    payloadObject.RotationW,
                    payloadObject.ScaleX,
                    payloadObject.ScaleY,
                    payloadObject.ScaleZ,
                    command.Source,
                    command.Reason);

                ActivityObjectSnapshotRestoreResult result = ExecuteObjectSnapshotRestoreCommand(restoreCommand, endpoints, report);
                if (!IsObjectSnapshotRestoreResultForCurrentEntry(result, definition, entrySequence))
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_result_invalid_or_failed_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                    throw new InvalidOperationException(
                        $"restore_result_invalid_or_failed_required: activityId='{definition.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
                }

                if (result.IsRestored)
                {
                    restoredCount += 1;
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreApplied,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot restore applied targetId='{report.TargetId}' coordinateSpace='{ToCoordinateSpaceToken(restoreCommand.CoordinateSpace)}' payloadPosition='({payloadObject.PositionX:0.###},{payloadObject.PositionY:0.###},{payloadObject.PositionZ:0.###})' beforePosition='({result.BeforePositionX:0.###},{result.BeforePositionY:0.###},{result.BeforePositionZ:0.###})' afterPosition='({result.AfterPositionX:0.###},{result.AfterPositionY:0.###},{result.AfterPositionZ:0.###})' restoreVerified='{result.RestoreVerified.ToString().ToLowerInvariant()}' hasTransformPayload='true' detail='{result.Detail}'.");
                    continue;
                }

                if (result.IsSkippedOptional)
                {
                    EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoEndpointOptional,
                        restoreIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' activity object snapshot restore skipped optional targetId='{report.TargetId}' reason='{result.Detail}'.");
                    continue;
                }

                restoreFailed = true;
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore failed reason='restore_endpoint_missing_required' targetId='{report.TargetId}' detail='{result.Detail}'.");
                throw new InvalidOperationException(
                    $"restore_endpoint_missing_required: activityId='{definition.ActivityId}' targetId='{report.TargetId}' detail='{result.Detail}'.");
            }

            if (matchedTargetCount == 0)
            {
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget,
                    restoreIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity object snapshot restore skipped reason='payload_has_no_matching_target_for_entry' payloadObjectCount='{loadedPayload.Objects.Count}'.");
            }

            EmitFact(
                facts,
                SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted,
                restoreIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity object snapshot restore completed payloadAvailable='true' payloadObjectCount='{loadedPayload.Objects.Count}' matchedTargetCount='{matchedTargetCount}' restoredCount='{restoredCount}' targetIds='{JoinValues(matchedTargetIds)}' appliedTargetIds='{JoinValues(matchedTargetIds)}' failedTargetIds='<none>' coordinateSpace='world_transform' restoreVerified='{(!restoreFailed && restoredCount == matchedTargetCount).ToString().ToLowerInvariant()}' restoreFailed='{restoreFailed.ToString().ToLowerInvariant()}'.");
        }

        private static Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> BuildLoadedSnapshotPayloadByTargetId(IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> objects)
        {
            Dictionary<string, LoadedSessionActivitySnapshotPayloadObject> byTargetId = new(StringComparer.Ordinal);
            if (objects == null)
            {
                return byTargetId;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                LoadedSessionActivitySnapshotPayloadObject current = objects[index];
                if (!current.IsValid || string.IsNullOrWhiteSpace(current.TargetId))
                {
                    continue;
                }

                byTargetId[current.TargetId] = current;
            }

            return byTargetId;
        }

        private static IActivityObjectResetEndpoint[] ResolveObjectResetEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectResetEndpoint>();
            }

            List<IActivityObjectResetEndpoint> endpoints = new();
            HashSet<IActivityObjectResetEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.ResetEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectResetEndpointReference>(capability.CapabilityId, out ActivityObjectResetEndpointReference runtimeReference) ||
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

        private static IActivityObjectSnapshotRestoreEndpoint[] ResolveObjectSnapshotRestoreEndpointsFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectSnapshotRestoreEndpoint>();
            }

            List<IActivityObjectSnapshotRestoreEndpoint> endpoints = new();
            HashSet<IActivityObjectSnapshotRestoreEndpoint> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.SnapshotRestoreEndpoint)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectSnapshotRestoreEndpointReference>(capability.CapabilityId, out ActivityObjectSnapshotRestoreEndpointReference runtimeReference) ||
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

        private static IActivityObjectSnapshotProvider[] ResolveObjectSnapshotProvidersFromInventory(
            ActivityCapabilityInventory inventory,
            ActivityObjectContributionReport report)
        {
            if (!inventory.IsValid || !report.IsValid)
            {
                return Array.Empty<IActivityObjectSnapshotProvider>();
            }

            List<IActivityObjectSnapshotProvider> providers = new();
            HashSet<IActivityObjectSnapshotProvider> unique = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.SnapshotProvider)
                {
                    continue;
                }

                if (!TryGetPolicyValue(capability.PolicyMetadata, "targetId", out string targetId) ||
                    !string.Equals(targetId, report.TargetId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityObjectSnapshotProviderReference>(capability.CapabilityId, out ActivityObjectSnapshotProviderReference runtimeReference) ||
                    runtimeReference.Provider == null)
                {
                    continue;
                }

                if (unique.Add(runtimeReference.Provider))
                {
                    providers.Add(runtimeReference.Provider);
                }
            }

            return providers.ToArray();
        }

        private static bool HasRequiredResetContributor(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            if (!discoveryResult.IsValid || discoveryResult.Reports == null)
            {
                return false;
            }

            for (int index = 0; index < discoveryResult.Reports.Count; index++)
            {
                ActivityObjectContributionReport report = discoveryResult.Reports[index];
                if (!report.IsValid)
                {
                    continue;
                }

                if (!string.Equals(report.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) ||
                    report.Identity.ActivityOrdinal != definition.ActivityOrdinal ||
                    report.Identity.EntrySequence != entrySequence)
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

        private ActivityObjectResetResult ExecuteObjectResetCommand(
            ActivityObjectResetCommand command,
            IActivityObjectResetEndpoint[] endpoints)
        {
            if (endpoints == null || endpoints.Length == 0)
            {
                if (command.IsRequired)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "required_reset_endpoint_missing");
                }

                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.SkippedOptional,
                    command,
                    command.Source,
                    command.Reason,
                    "optional_reset_endpoint_missing");
            }

            bool hasSupportingEndpoint = false;
            for (int index = 0; index < endpoints.Length; index++)
            {
                IActivityObjectResetEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.ResetGroup))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                ActivityObjectResetResult result = endpoint.ApplyReset(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectResetResult(
                        ActivityObjectResetResultKind.Failed,
                        command,
                        command.Source,
                        command.Reason,
                        "invalid_reset_result");
                }

                return result;
            }

            if (command.IsRequired)
            {
                return new ActivityObjectResetResult(
                    ActivityObjectResetResultKind.Failed,
                    command,
                    command.Source,
                    command.Reason,
                    hasSupportingEndpoint ? "required_reset_not_applied" : "required_reset_group_not_supported");
            }

            return new ActivityObjectResetResult(
                ActivityObjectResetResultKind.SkippedOptional,
                command,
                command.Source,
                command.Reason,
                hasSupportingEndpoint ? "optional_reset_not_applied" : "optional_reset_group_not_supported");
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
                result = ActorAttributeApplyResult.Reject(normalizedActorId, runtimeAttributeId, "stale_or_foreign_activity_identity");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                result = ActorAttributeApplyResult.Reject(string.Empty, runtimeAttributeId, "actor_id_missing");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!_activityNonPlayerActorRegistry.TryGetActive(commandIdentity, normalizedActorId, out NonPlayerActorRuntimeEntry activeEntry) || !activeEntry.IsValid)
            {
                result = ActorAttributeApplyResult.Reject(normalizedActorId, runtimeAttributeId, "actor_attribute_target_not_found");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!TryResolveActorInstanceIdForActor(commandIdentity, normalizedActorId, normalizedSource, normalizedReason, out ActorInstanceId actorInstanceId))
            {
                result = ActorAttributeApplyResult.Reject(normalizedActorId, runtimeAttributeId, "actor_attribute_target_not_found");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!_activeActorAttributeCapabilitiesByActorInstanceId.TryGetValue(actorInstanceId, out ActorAttributeCapabilityState capabilityState) || !capabilityState.IsValid)
            {
                result = ActorAttributeApplyResult.Reject(normalizedActorId, runtimeAttributeId, "actor_attribute_capability_not_ready");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            if (!runtimeAttributeId.IsValid || !capabilityState.Endpoint.TryGetState(runtimeAttributeId, out _))
            {
                result = ActorAttributeApplyResult.Reject(normalizedActorId, runtimeAttributeId, "actor_attribute_not_found");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeCommand command = BuildActorAttributeCommand(
                capabilityState.PipelineIdentity,
                capabilityState.ActivityIdentity,
                normalizedActorId,
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
                result = ActorAttributeApplyResult.Fail(normalizedActorId, runtimeAttributeId, "attribute_changed_fact_missing");
                LogActorAttributeCommandRejected(operation, normalizedActorId, runtimeAttributeId, result.Reason, normalizedSource, normalizedReason);
                return false;
            }

            ActorAttributeChangedFact fact = result.Fact;
            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Actor] event='ActorAttributeChanged' activityId='{activityId}' entrySequence='{entrySequence}' actorId='{normalizedActorId}' actorInstanceId='{fact.ActorInstanceId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' operation='{fact.Operation}' clamped='{fact.Clamped}' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
            return true;
        }

        public bool TryQaResetCurrentPlayerActor(
            SessionActivityIdentity commandIdentity,
            string playerSlotId,
            string source,
            string reason,
            out string outcomeReason)
        {
            outcomeReason = "unknown";
            string normalizedPlayerSlotId = Normalize(playerSlotId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRequested' activityId='{_state.CurrentDefinition.ActivityId}' entrySequence='{_state.CurrentEntrySequence}' playerSlotId='{normalizedPlayerSlotId}' source='{normalizedSource}' reason='{normalizedReason}'.",
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

            if (string.IsNullOrWhiteSpace(normalizedPlayerSlotId))
            {
                outcomeReason = "actor_reset_qa_player_slot_missing";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            IReadOnlyList<PlayerActorIdentityRecord> targets = ResolvePlayerActorCapabilityTargetsForCurrentEntry(commandIdentity);
            if (targets == null || targets.Count == 0)
            {
                outcomeReason = "actor_reset_qa_no_active_player_actor";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            PlayerActorIdentityRecord selected = default;
            bool found = false;
            for (int index = 0; index < targets.Count; index++)
            {
                PlayerActorIdentityRecord candidate = targets[index];
                if (!candidate.IsValid || !string.Equals(candidate.PlayerSlotId, normalizedPlayerSlotId, StringComparison.Ordinal))
                {
                    continue;
                }

                selected = candidate;
                found = true;
                break;
            }

            if (!found)
            {
                List<string> availablePlayerSlots = new();
                List<string> availableActorIds = new();
                for (int index = 0; index < targets.Count; index++)
                {
                    PlayerActorIdentityRecord candidate = targets[index];
                    if (!candidate.IsValid)
                    {
                        continue;
                    }

                    availablePlayerSlots.Add(candidate.PlayerSlotId);
                    availableActorIds.Add(candidate.PlayerActorId);
                }

                string targetResolutionDetail = $"requestedPlayerSlotId='{normalizedPlayerSlotId}' requestedPlayerActorId='<unresolved>' availableActorIds='{(availableActorIds.Count == 0 ? "<none>" : string.Join(",", availableActorIds))}' availablePlayerSlots='{(availablePlayerSlots.Count == 0 ? "<none>" : string.Join(",", availablePlayerSlots))}'";
                outcomeReason = "actor_reset_qa_player_actor_not_found";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' playerSlotId='{normalizedPlayerSlotId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' resolutionDetail='{targetResolutionDetail}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

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
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' playerSlotId='{normalizedPlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' resolutionDetail='{resolutionDetail}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            Vector3 placementPosition = instance.transform.localPosition;
            Vector3 placementEulerAngles = instance.transform.localEulerAngles;
            ActorResetTargetRef target = new(
                new ActorResetActorRef(
                    commandIdentity,
                    selectedPlayerInstance.ActorId,
                    selectedPlayerInstance.ActorInstanceId.Value,
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
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaFailed' reason='{outcomeReason}' playerSlotId='{normalizedPlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}' error='{exception.Message}'.",
                    DebugUtility.Colors.Error);
                return false;
            }

            if (results == null || results.Count != 1 || !results[0].IsValid)
            {
                outcomeReason = "actor_reset_qa_invalid_result";
                DebugUtility.Log(
                    typeof(SessionActivityPipeline),
                    $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaRejected' reason='{outcomeReason}' playerSlotId='{normalizedPlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            outcomeReason = "actor_reset_qa_applied";
            ActorResetResult result = results[0];
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][QA] event='ActorResetQaApplied' reason='{outcomeReason}' playerSlotId='{normalizedPlayerSlotId}' playerActorId='{selected.PlayerActorId}' activityId='{commandIdentity.ActivityId}' entrySequence='{commandIdentity.EntrySequence}' appliedGroups='{result.AppliedGroups.Count}' skippedGroups='{result.SkippedGroups.Count}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
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
            IReadOnlyList<PlayerActorIdentityRecord> playerActorTargets = ResolvePlayerActorCapabilityTargetsForCurrentEntry(commandIdentity);
            IReadOnlyList<ActorScanTarget> actorTargets = BuildActorScanTargetsForCurrentEntry(commandIdentity, playerActorTargets, normalizedSource, normalizedReason);
            ActivityCapabilityInventoryBuildResult inventoryBuildResult = _activityCapabilityInventoryCoordinator.BuildForEntry(
                commandIdentity,
                discoveryResult,
                actorTargets,
                normalizedSource,
                normalizedReason);

            ActivityResetResult resetResult;
            try
            {
                resetResult = ActivityResetStage.Execute(
                    new ActivityResetCommand(commandIdentity, definition, normalizedSource, normalizedReason),
                    new ActivityResetContext(discoveryResult, inventoryBuildResult.Inventory, inventoryBuildResult.Validation),
                    IsDiscoveryResultForCurrentEntry,
                    IsReportForCurrentEntry,
                    HasRequiredResetContributor,
                    ResolveObjectResetEndpointsFromInventory,
                    ExecuteObjectResetCommand,
                    IsObjectResetResultForCurrentEntry,
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
                if (!instance.IsValid || instance.Kind != ActorKind.Player || instance.ActorRoot == null)
                {
                    continue;
                }

                availableActorIds.Add(instance.ActorId);

                PlayerActorIdentity identity = instance.ActorRoot.GetComponent<PlayerActorIdentity>();
                if (identity == null || !identity.IsValid)
                {
                    continue;
                }

                availablePlayerSlots.Add(identity.PlayerSlotId);
                if (!string.Equals(identity.PlayerSlotId, selected.PlayerSlotId, StringComparison.Ordinal))
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
                    if (instance.IsValid && instance.Kind == ActorKind.Player)
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
            string requestedPlayerSlotId = requestedIdentity.IsValid ? requestedIdentity.PlayerSlotId : string.Empty;
            string requestedPlayerActorId = requestedIdentity.IsValid ? requestedIdentity.PlayerActorId : string.Empty;
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
                   string.Equals(result.Command.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Command.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Command.EntrySequence == entrySequence &&
                   string.Equals(result.Command.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Command.SessionStateId, _sessionId, StringComparison.Ordinal) &&
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

        private static bool TryResolveCameraTargetReferenceFromInventory(
            ActivityCapabilityInventory inventory,
            IReadOnlyList<CameraBindingRequirement> cameraRequirements,
            out ActivityCameraTargetReference selectedCameraTarget)
        {
            selectedCameraTarget = null;
            if (!inventory.IsValid)
            {
                return false;
            }

            for (int capabilityIndex = 0; capabilityIndex < inventory.Capabilities.Count; capabilityIndex++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[capabilityIndex];
                if (capability.CapabilityKind != ActivityCapabilityKind.CameraTarget)
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference<ActivityCameraTargetReference>(capability.CapabilityId, out ActivityCameraTargetReference reference) ||
                    reference == null ||
                    !reference.IsValid)
                {
                    continue;
                }

                if (!MatchesAnyCameraRequirement(cameraRequirements, reference.PlayerActorId, reference.PlayerSlotId))
                {
                    continue;
                }

                selectedCameraTarget = reference;
                return true;
            }

            return false;
        }

        private static bool MatchesAnyCameraRequirement(
            IReadOnlyList<CameraBindingRequirement> cameraRequirements,
            string playerActorId,
            string playerSlotId)
        {
            bool foundActivityCameraRequirement = false;
            for (int index = 0; index < cameraRequirements.Count; index++)
            {
                CameraBindingRequirement requirement = cameraRequirements[index];
                if (!requirement.IsValid || requirement.CameraBindingKind != ActivityCameraBindingRequirementKind.ActivityCamera)
                {
                    continue;
                }

                foundActivityCameraRequirement = true;
                if (string.IsNullOrWhiteSpace(requirement.TargetId))
                {
                    return true;
                }

                if (string.Equals(requirement.TargetId, playerActorId, StringComparison.Ordinal) ||
                    string.Equals(requirement.TargetId, playerSlotId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return !foundActivityCameraRequirement;
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

        private ActivityObjectSnapshotRestoreResult ExecuteObjectSnapshotRestoreCommand(
            ActivityObjectSnapshotRestoreCommand command,
            IActivityObjectSnapshotRestoreEndpoint[] endpoints,
            ActivityObjectContributionReport report)
        {
            bool isRequired = report.Requiredness == ActivitySetupRequirementRequiredness.Required;
            if (endpoints == null || endpoints.Length == 0)
            {
                return new ActivityObjectSnapshotRestoreResult(
                    isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                    command,
                    restoreVerified: false,
                    beforePositionX: 0f,
                    beforePositionY: 0f,
                    beforePositionZ: 0f,
                    afterPositionX: 0f,
                    afterPositionY: 0f,
                    afterPositionZ: 0f,
                    command.Source,
                    command.Reason,
                    isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional");
            }

            bool hasSupportingEndpoint = false;
            for (int index = 0; index < endpoints.Length; index++)
            {
                IActivityObjectSnapshotRestoreEndpoint endpoint = endpoints[index];
                if (endpoint == null || !endpoint.Supports(command.TargetId))
                {
                    continue;
                }

                hasSupportingEndpoint = true;
                ActivityObjectSnapshotRestoreResult result = endpoint.ApplyRestore(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectSnapshotRestoreResult(
                        ActivityObjectSnapshotRestoreResultKind.Failed,
                        command,
                        restoreVerified: false,
                        beforePositionX: 0f,
                        beforePositionY: 0f,
                        beforePositionZ: 0f,
                        afterPositionX: 0f,
                        afterPositionY: 0f,
                        afterPositionZ: 0f,
                        command.Source,
                        command.Reason,
                        "restore_result_invalid_or_failed_required");
                }

                return result;
            }

            return new ActivityObjectSnapshotRestoreResult(
                isRequired ? ActivityObjectSnapshotRestoreResultKind.Failed : ActivityObjectSnapshotRestoreResultKind.SkippedOptional,
                command,
                restoreVerified: false,
                beforePositionX: 0f,
                beforePositionY: 0f,
                beforePositionZ: 0f,
                afterPositionX: 0f,
                afterPositionY: 0f,
                afterPositionZ: 0f,
                command.Source,
                command.Reason,
                (isRequired ? "restore_endpoint_missing_required" : "target_has_no_restore_endpoint_optional"));
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
                   string.Equals(command.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(command.SessionStateId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(command.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   command.ActivityOrdinal == definition.ActivityOrdinal &&
                   command.EntrySequence == entrySequence &&
                   !string.IsNullOrWhiteSpace(command.TargetId);
        }

        private bool TryResolveRouteLoadedSnapshotPayload(
            out LoadedSessionActivitySnapshotPayload loadedPayload,
            out string failureReason)
        {
            loadedPayload = default;
            if (!DependencyManager.Provider.TryGetGlobal<IRouteActivityLoadedSnapshotPayloadProvider>(out var payloadProvider) ||
                payloadProvider == null)
            {
                failureReason = "no_loaded_payload_provider";
                return false;
            }

            bool resolved = payloadProvider.TryGetPendingLoadedSnapshotPayload(_sessionId, out loadedPayload, out failureReason);
            if (!resolved || !loadedPayload.IsValid)
            {
                loadedPayload = default;
                return false;
            }

            failureReason = "resolved";
            return true;
        }

        private bool IsLoadedSnapshotPayloadForCurrentActivity(
            LoadedSessionActivitySnapshotPayload loadedPayload,
            SessionActivityDefinition definition)
        {
            return loadedPayload.IsValid &&
                   string.Equals(loadedPayload.SchemaId, RouteActivitySnapshotSchemaId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.SessionStateId, _sessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedPayload.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   loadedPayload.SourceEntrySequence > 0;
        }

        private ActivityObjectSnapshotCaptureResult ExecuteObjectSnapshotCaptureCommand(
            ActivityObjectSnapshotCaptureCommand command,
            IActivityObjectSnapshotProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
            {
                return new ActivityObjectSnapshotCaptureResult(
                    ActivityObjectSnapshotCaptureResultKind.SkippedOptional,
                    command,
                    default,
                    false,
                    command.Source,
                    command.Reason,
                    "snapshot_provider_missing");
            }

            for (int index = 0; index < providers.Length; index++)
            {
                IActivityObjectSnapshotProvider provider = providers[index];
                if (provider == null || !provider.Supports(command.TargetId))
                {
                    continue;
                }

                ActivityObjectSnapshotCaptureResult result = provider.CaptureSnapshot(command);
                if (!result.IsValid)
                {
                    return new ActivityObjectSnapshotCaptureResult(
                        ActivityObjectSnapshotCaptureResultKind.Failed,
                        command,
                        default,
                        false,
                        command.Source,
                        command.Reason,
                        "invalid_snapshot_capture_result");
                }

                return result;
            }

            return new ActivityObjectSnapshotCaptureResult(
                ActivityObjectSnapshotCaptureResultKind.SkippedOptional,
                command,
                default,
                false,
                command.Source,
                command.Reason,
                "no_matching_snapshot_provider");
        }

        private bool IsObjectSnapshotCaptureResultForCurrentEntry(
            ActivityObjectSnapshotCaptureResult result,
            SessionActivityDefinition definition,
            int entrySequence)
        {
            SessionActivityIdentity identity = result.Command.Identity;
            if (!result.IsValid ||
                !identity.IsValid ||
                !string.Equals(identity.PipelineId, PipelineId, StringComparison.Ordinal) ||
                !string.Equals(identity.SessionId, _sessionId, StringComparison.Ordinal) ||
                !string.Equals(identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) ||
                identity.ActivityOrdinal != definition.ActivityOrdinal ||
                identity.EntrySequence != entrySequence)
            {
                return false;
            }

            if (result.IsCaptured)
            {
                return result.Snapshot.IsValid &&
                       string.Equals(result.Command.TargetId, result.Snapshot.TargetId, StringComparison.Ordinal);
            }

            return true;
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
                   string.Equals(result.Command.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   result.Command.ActivityOrdinal == definition.ActivityOrdinal &&
                   result.Command.EntrySequence == entrySequence &&
                   string.Equals(result.Command.PipelineId, PipelineId, StringComparison.Ordinal) &&
                   string.Equals(result.Command.SessionStateId, _sessionId, StringComparison.Ordinal) &&
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

        private void AppendContributorsFromSceneOrFail(
            List<ActivityObjectContributionReport> reports,
            Scene contentScene,
            ActivityContentLoadedSet loadedSet,
            ActivityContentLoadedSceneRecord record,
            string source,
            string reason)
        {
            GameObject[] roots = contentScene.GetRootGameObjects();
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

                    contributor.ValidateOrThrow(
                        $"ActivityObjectContributorDiscovery:{contentScene.name}:{rootIndex}:{contributorIndex}");

                    ActivityObjectContributionReport report = new(
                        loadedSet.Identity,
                        loadedSet.ContentProfileId,
                        record.SceneKey,
                        contentScene.name,
                        contributor.TargetId,
                        contributor.RoleId,
                        contributor.ContributorKind,
                        contributor.DefaultRequiredness,
                        contributor.SupportedResetGroups,
                        contributor.SupportedReleaseKinds,
                        source,
                        reason);

                    if (!report.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Invalid ActivityObjectContributionReport targetId='{contributor.TargetId}' scene='{contentScene.name}'.");
                    }

                    reports.Add(report);
                }
            }
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

        private static string FormatReleaseKinds(IReadOnlyList<ActivityReleaseRequirementKind> releaseKinds)
        {
            if (releaseKinds == null || releaseKinds.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", releaseKinds);
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
                EmitSnapshot(snapshots, "gameplay_content_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                EmitMovementControlEnableAtRunning(definition, command, facts, snapshots, entrySequence);
                TryEmitRestartCompletedAtRunning(definition, command, facts, snapshots, entrySequence);
                return;
            }

            SessionActivityIdentity runningIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
            _state.SetCurrentIdentity(runningIdentity, SessionActivityStage.ActivityRunning);
            _state.SetExecutionState(ActivityExecutionState.Running);
            EmitFact(facts, SessionActivityFactKind.ActivityRunningEntered, runningIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' running.");
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

        private SessionActivityPlayerPreparationHandoff ResolveRouteSessionPlayerPreparationOrFail(
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityPlayerPreparationHandoff handoff = _lastRouteSessionPlayerPreparationHandoff;
            if (!handoff.IsValid || !string.Equals(handoff.SessionId, _sessionId, StringComparison.Ordinal))
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingFailed, entrySequence);
                _state.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantBindingFailed);
                EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding failed invalid route/session participant context sessionStateId='{_sessionId}' handoffSessionId='{handoff.SessionId}'.");
                EmitSnapshot(
                    snapshots,
                    "activity_participant_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' participant binding failed invalid route/session participant context.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionActivityPipeline][ParticipantBinding] Missing or stale route/session player preparation context activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            return handoff;
        }

        private static HashSet<string> BuildPlannedRouteSessionParticipantSet(IReadOnlyList<SessionParticipantId> participantIds)
        {
            HashSet<string> participants = new(StringComparer.Ordinal);
            if (participantIds == null || participantIds.Count == 0)
            {
                return participants;
            }

            for (int index = 0; index < participantIds.Count; index++)
            {
                string participantId = Normalize(participantIds[index].Value);
                if (!string.IsNullOrWhiteSpace(participantId))
                {
                    participants.Add(participantId);
                }
            }

            return participants;
        }

        private static bool TryResolveSingleParticipantHint(HashSet<string> participants, out string participantId)
        {
            participantId = string.Empty;
            if (participants == null || participants.Count != 1)
            {
                return false;
            }

            foreach (string candidate in participants)
            {
                participantId = candidate;
                break;
            }

            return !string.IsNullOrWhiteSpace(participantId);
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

            if (!_lastSnapshotPayloadForSaveOnExit.IsValid)
            {
                if (_lastSnapshotCaptureFailedForSaveOnExit)
                {
                    string detail = string.IsNullOrWhiteSpace(_lastSnapshotCaptureFailureDetail)
                        ? "snapshot_capture_failed"
                        : Normalize(_lastSnapshotCaptureFailureDetail);
                    failureReason = $"snapshot_capture_failed:{detail}";
                    return false;
                }

                failureReason = "snapshot_payload_missing";
                return false;
            }

            payload = _lastSnapshotPayloadForSaveOnExit;
            failureReason = "resolved";
            return true;
        }

        public SessionActivityPredefinedVisualReadinessResult ObservePredefinedVisualReadiness(
            string sessionStateId,
            string expectedRouteOperationId,
            string source,
            string reason)
        {
            string normalizedSessionStateId = Normalize(sessionStateId);
            string normalizedExpectedRouteOperationId = Normalize(expectedRouteOperationId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (string.IsNullOrWhiteSpace(normalizedSessionStateId))
            {
                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.Failed,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    _state.CurrentStage,
                    "session_state_id_missing",
                    "SessionActivity visual readiness requires sessionStateId.");
            }

            if (!string.Equals(normalizedSessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.RejectedForeignOrStale,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    _state.CurrentStage,
                    "stale_or_foreign_session_state",
                    $"Observed sessionStateId='{normalizedSessionStateId}' does not match pipeline sessionStateId='{_sessionId}'.");
            }

            if (string.IsNullOrWhiteSpace(normalizedExpectedRouteOperationId))
            {
                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.NotRequired,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    _state.CurrentStage,
                    "route_operation_id_missing",
                    "No expected routeOperationId was provided.");
            }

            if (!_state.HasStarted || !_state.CurrentIdentity.IsValid)
            {
                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.Waiting,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _state.CurrentDefinition.ActivityId,
                    _state.CurrentEntrySequence,
                    _state.CurrentStage,
                    "activity_not_started",
                    "SessionActivity has not started with a valid identity.");
            }

            if (_lastPredefinedVisualReadinessSignal.IsValid)
            {
                if (!string.Equals(_lastPredefinedVisualReadinessSignal.RouteOperationId, normalizedExpectedRouteOperationId, StringComparison.Ordinal))
                {
                    return new SessionActivityPredefinedVisualReadinessResult(
                        SessionActivityPredefinedVisualReadinessKind.RejectedForeignOrStale,
                        normalizedSessionStateId,
                        normalizedExpectedRouteOperationId,
                        _lastPredefinedVisualReadinessSignal.Identity.ActivityId,
                        _lastPredefinedVisualReadinessSignal.Identity.EntrySequence,
                        _state.CurrentStage,
                        "stale_or_foreign_route_operation",
                        $"Readiness routeOperationId='{_lastPredefinedVisualReadinessSignal.RouteOperationId}' does not match expectedRouteOperationId='{normalizedExpectedRouteOperationId}'.");
                }

                if (_state.CurrentIdentity.CycleKey != _lastPredefinedVisualReadinessSignal.Identity.CycleKey)
                {
                    return new SessionActivityPredefinedVisualReadinessResult(
                        SessionActivityPredefinedVisualReadinessKind.RejectedForeignOrStale,
                        normalizedSessionStateId,
                        normalizedExpectedRouteOperationId,
                        _state.CurrentIdentity.ActivityId,
                        _state.CurrentIdentity.EntrySequence,
                        _state.CurrentStage,
                        "stale_or_foreign_activity_cycle",
                        $"Current cycle='{_state.CurrentIdentity.CycleSignature}' diverged from readiness cycle='{_lastPredefinedVisualReadinessSignal.Identity.CycleSignature}'.");
                }

                _state.AppendTrace(
                    $"[OBS][SessionActivityPipeline] PredefinedVisualSetupReadyObserved routeOperationId='{normalizedExpectedRouteOperationId}' activityId='{_state.CurrentIdentity.ActivityId}' entrySequence='{_state.CurrentIdentity.EntrySequence}' source='{normalizedSource}' reason='{normalizedReason}'.");

                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.Ready,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _lastPredefinedVisualReadinessSignal.Identity.ActivityId,
                    _lastPredefinedVisualReadinessSignal.Identity.EntrySequence,
                    _state.CurrentStage,
                    "predefined_visual_setup_ready",
                    "Predefined visual setup is ready for reveal.");
            }

            string observedRouteOperationId = _lastRouteSessionPlayerPreparationHandoff.IsValid
                ? _lastRouteSessionPlayerPreparationHandoff.RouteOperationId
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(observedRouteOperationId) &&
                !string.Equals(observedRouteOperationId, normalizedExpectedRouteOperationId, StringComparison.Ordinal))
            {
                return new SessionActivityPredefinedVisualReadinessResult(
                    SessionActivityPredefinedVisualReadinessKind.RejectedForeignOrStale,
                    normalizedSessionStateId,
                    normalizedExpectedRouteOperationId,
                    _state.CurrentIdentity.ActivityId,
                    _state.CurrentIdentity.EntrySequence,
                    _state.CurrentStage,
                    "stale_or_foreign_route_operation",
                    $"Current handoff routeOperationId='{observedRouteOperationId}' does not match expectedRouteOperationId='{normalizedExpectedRouteOperationId}'.");
            }

            return new SessionActivityPredefinedVisualReadinessResult(
                SessionActivityPredefinedVisualReadinessKind.Waiting,
                normalizedSessionStateId,
                normalizedExpectedRouteOperationId,
                _state.CurrentIdentity.ActivityId,
                _state.CurrentIdentity.EntrySequence,
                _state.CurrentStage,
                "predefined_visual_setup_pending",
                $"Waiting predefined visual setup for activityId='{_state.CurrentIdentity.ActivityId}' entrySequence='{_state.CurrentIdentity.EntrySequence}'.");
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

    }
}
