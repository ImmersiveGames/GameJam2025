using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Attributes.UI;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;
using UnityEngine.InputSystem;
using PlayerSessionParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class ActivityEntryPipeline : IActivityEntryPipeline
    {
        private sealed class PendingContentLoadContext
        {
            public PendingContentLoadContext(
                ActivityContentLoadPlan plan)
            {
                Plan = plan;
                LoadedRecords = new List<ActivityContentLoadedSceneRecord>(plan.Scenes.Count);
                NextSceneOrdinal = 1;
            }

            public ActivityContentLoadPlan Plan { get; }
            public SessionActivityIdentity Identity => Plan.Identity;
            public string ContentProfileId => Plan.ActivityContentProfileId;
            public IReadOnlyList<ActivityContentLoadPlanScene> Scenes => Plan.Scenes;
            public List<ActivityContentLoadedSceneRecord> LoadedRecords { get; }
            public int NextSceneOrdinal { get; set; }
            public bool IsValid => Plan.IsValid && LoadedRecords != null;
        }

        // Ponte transitória SA-7B0: mantida apenas para stages ainda não migrados
        // para bridges menores. O ActivityEntryPipeline usa os campos de domínio abaixo
        // nos pontos já normalizados deste corte.
        private readonly IActivityEntryRuntimeBridge _runtimeBridge;
        private readonly IActivityEntryIdentityRuntimeBridge _identityBridge;
        private readonly IActivityEntryFactRuntimeBridge _factBridge;
        private readonly IActivityEntryContentPendingOperationRuntimeBridge _contentPendingOperationBridge;
        private readonly ActivityEntryLogSink _logSink;
        private readonly IActivityEntryPreparationRuntimeBridge _preparationBridge;
        private readonly IActivityEntryActorPresentationRuntimeBridge _actorPresentationBridge;
        private readonly IActivityEntryActorParticipationRuntimeBridge _actorParticipationBridge;
        private readonly IPlayerActorMaterializationAdapter _playerActorMaterializationAdapter;
        private readonly IPlayerActorParticipationAdapter _playerActorParticipationAdapter;
        private readonly IActorResetAdapter _actorResetAdapter;
        private readonly IActivityEntryPermissionTargetRuntimeBridge _permissionTargetBridge;
        private readonly IActivityEntryMovementBindingRuntimeBridge _movementBindingBridge;
        private readonly IActivityEntryCameraBindingRuntimeBridge _cameraBindingBridge;
        private readonly ActivitySceneActorRegistry _activitySceneActorRegistry;
        private readonly ActivityPlayerActorRegistry _activityPlayerActorRegistry;
        private readonly SessionActorRuntimeStore _sessionActorRuntimeStore;
        private readonly ActivityContentRuntimeState _activityContentRuntimeState;
        private readonly IActivityEntryPlacementMarkerLookup _placementMarkerLookup;
        private readonly IMovementBindingAdapter _movementBindingAdapter;
        private readonly IActivityCameraPreparationExecutor _activityCameraPreparationExecutor;
        private readonly ActivityActorExitRuntimeState _activityActorExitRuntimeState;
        private readonly IActivityRetainedParticipantLookup _activityRetainedParticipantLookup;
        private readonly ISessionActivityPendingOperationRunner _pendingOperationRunner;
        private readonly ISessionActivityPendingOperationCallback _pendingOperationCallback;
        private readonly ActorPresentationPlanResolver _actorPresentationPlanResolver;
        private readonly IActorPresentationMaterializationAdapter _actorPresentationMaterializationAdapter;
        private readonly IPlayerInputBindingAdapter _playerInputBindingAdapter;
        private readonly IActorCommandBindingAdapter _actorCommandBindingAdapter;
        private readonly IActorAttributeEventStream _actorAttributeEventStream;
        private readonly IActorAttributeUiBindingRequestProvider _actorAttributeUiBindingRequestProvider;
        private readonly IPoolService _poolService;
        private readonly InputActionAsset _canonicalPlayerInputActionsAsset;
        private readonly ActivitySetupInventoryBuilder _activitySetupInventoryBuilder;
        private readonly ActivityEntryCapabilityInventoryBuildStage _activityEntryCapabilityInventoryBuildStage;
        private readonly ActivityEntryInventoryRuntimeState _activityInventoryRuntimeState = new();
        private readonly ActivityParticipationRuntimeState _activityParticipationRuntimeState = new();
        private readonly ActivityActorAttributeUiBindingRuntimeState _activityActorAttributeUiBindingRuntimeState = new();
        private IReadOnlyList<SessionActivityActorMaterializationPlanEntry> _currentActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
        private PendingContentLoadContext _pendingContentLoadContext;

        internal ActivityEntryPipeline(
            IActivityEntryRuntimeBridge endpoint,
            IPlayerActorMaterializationAdapter playerActorMaterializationAdapter,
            IPlayerActorParticipationAdapter playerActorParticipationAdapter,
            IActorResetAdapter actorResetAdapter,
            IActivityEntryActorPresentationRuntimeBridge actorPresentationBridge,
            IActivityEntryActorParticipationRuntimeBridge actorParticipationBridge,
            IActivityEntryPermissionTargetRuntimeBridge permissionTargetBridge,
            IActivityEntryMovementBindingRuntimeBridge movementBindingBridge,
            IActivityEntryCameraBindingRuntimeBridge cameraBindingBridge,
            ActivitySceneActorRegistry activitySceneActorRegistry,
            ActivityPlayerActorRegistry activityPlayerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            ActivityContentRuntimeState activityContentRuntimeState,
            IMovementBindingAdapter movementBindingAdapter,
            IActivityCameraPreparationExecutor activityCameraPreparationExecutor,
            ISessionActivityPendingOperationRunner pendingOperationRunner,
            ISessionActivityPendingOperationCallback pendingOperationCallback,
            InputActionAsset canonicalPlayerInputActionsAsset,
            ActivityActorExitRuntimeState activityActorExitRuntimeState,
            IActorAttributeEventStream actorAttributeEventStream,
            IActorAttributeUiBindingRequestProvider actorAttributeUiBindingRequestProvider,
            IPoolService poolService,
            IGlobalAudioService globalAudioService)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _runtimeBridge = endpoint;
            _identityBridge = endpoint;
            _factBridge = endpoint;
            _contentPendingOperationBridge = endpoint;
            _logSink = new ActivityEntryLogSink();
            _preparationBridge = endpoint;
            _playerActorMaterializationAdapter = playerActorMaterializationAdapter ?? throw new ArgumentNullException(nameof(playerActorMaterializationAdapter));
            _playerActorParticipationAdapter = playerActorParticipationAdapter ?? throw new ArgumentNullException(nameof(playerActorParticipationAdapter));
            _actorResetAdapter = actorResetAdapter ?? throw new ArgumentNullException(nameof(actorResetAdapter));
            _actorPresentationBridge = actorPresentationBridge ?? throw new ArgumentNullException(nameof(actorPresentationBridge));
            _actorParticipationBridge = actorParticipationBridge ?? throw new ArgumentNullException(nameof(actorParticipationBridge));
            _permissionTargetBridge = permissionTargetBridge ?? throw new ArgumentNullException(nameof(permissionTargetBridge));
            _movementBindingBridge = movementBindingBridge ?? throw new ArgumentNullException(nameof(movementBindingBridge));
            _cameraBindingBridge = cameraBindingBridge ?? throw new ArgumentNullException(nameof(cameraBindingBridge));
            _activitySceneActorRegistry = activitySceneActorRegistry ?? throw new ArgumentNullException(nameof(activitySceneActorRegistry));
            _activityPlayerActorRegistry = activityPlayerActorRegistry ?? throw new ArgumentNullException(nameof(activityPlayerActorRegistry));
            _sessionActorRuntimeStore = sessionActorRuntimeStore ?? throw new ArgumentNullException(nameof(sessionActorRuntimeStore));
            _activityContentRuntimeState = activityContentRuntimeState ?? throw new ArgumentNullException(nameof(activityContentRuntimeState));
            _placementMarkerLookup = new ActivityEntryPlacementMarkerLookup(_activityContentRuntimeState);
            _movementBindingAdapter = movementBindingAdapter ?? throw new ArgumentNullException(nameof(movementBindingAdapter));
            _activityCameraPreparationExecutor = activityCameraPreparationExecutor ?? throw new ArgumentNullException(nameof(activityCameraPreparationExecutor));
            _pendingOperationRunner = pendingOperationRunner ?? throw new ArgumentNullException(nameof(pendingOperationRunner));
            _pendingOperationCallback = pendingOperationCallback ?? throw new ArgumentNullException(nameof(pendingOperationCallback));
            _canonicalPlayerInputActionsAsset = canonicalPlayerInputActionsAsset ?? throw new ArgumentNullException(nameof(canonicalPlayerInputActionsAsset));
            _activityActorExitRuntimeState = activityActorExitRuntimeState ?? throw new ArgumentNullException(nameof(activityActorExitRuntimeState));
            _actorAttributeEventStream = actorAttributeEventStream ?? throw new ArgumentNullException(nameof(actorAttributeEventStream));
            _actorAttributeUiBindingRequestProvider = actorAttributeUiBindingRequestProvider ?? throw new ArgumentNullException(nameof(actorAttributeUiBindingRequestProvider));
            _activityRetainedParticipantLookup = new ActivityRetainedParticipantLookup(_activityPlayerActorRegistry, _sessionActorRuntimeStore);
            _actorPresentationPlanResolver = new ActorPresentationPlanResolver();
            _actorPresentationMaterializationAdapter = new UnityActorPresentationMaterializationAdapter();
            _playerInputBindingAdapter = new PlayerInputBindingAdapter(_canonicalPlayerInputActionsAsset);
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _actorCommandBindingAdapter = new ActorCommandBindingAdapter(
                _poolService,
                globalAudioService ?? throw new ArgumentNullException(nameof(globalAudioService)),
                _actorAttributeEventStream);
            _activitySetupInventoryBuilder = new ActivitySetupInventoryBuilder();
            _activityEntryCapabilityInventoryBuildStage = new ActivityEntryCapabilityInventoryBuildStage();
        }

        public ActivityEntryPreparationResult PrepareEntry(
            ActivityEntryPreparationCommand command,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext = default)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPreparationCommand is invalid.");
            }

            string loadedSnapshotPayloadState = loadedSnapshotPayloadContext.HasPayload ? "true" : "false";
            int loadedSnapshotPayloadRecordCount = loadedSnapshotPayloadContext.HasPayload
                ? loadedSnapshotPayloadContext.Payload.RecordCount
                : 0;
            string loadedSnapshotPayloadSourceActivityId = loadedSnapshotPayloadContext.HasPayload
                ? Normalize(loadedSnapshotPayloadContext.Payload.ActivityId)
                : "<none>";
            int loadedSnapshotPayloadSourceEntrySequence = loadedSnapshotPayloadContext.HasPayload
                ? loadedSnapshotPayloadContext.Payload.SourceEntrySequence
                : 0;

            _logSink.LogEntryOwnerEvent(
                "ActivityEntryPipelineStarted",
                command.Identity,
                command.Source,
                command.Reason,
                $"owner='ActivityEntryPipeline' decompositionStatus='runtime_domain_split' stageOwnershipSplit='content_object_actor_inventory' runtimeStateStoreSplit='content_preparation_store_sources' objectActorStoreSourceSplit='stage_owned_store_sources' contentPendingOperationSplit='loaded_set_store_pending_operation_runner' loadedSnapshotPayload='{loadedSnapshotPayloadState}' loadedSnapshotPayloadRecordCount='{loadedSnapshotPayloadRecordCount}' loadedSnapshotPayloadSourceActivityId='{loadedSnapshotPayloadSourceActivityId}' loadedSnapshotPayloadSourceEntrySequence='{loadedSnapshotPayloadSourceEntrySequence}'");
            _activityContentRuntimeState.ClearCurrentLoadedSet(command.Identity.ActivityId, command.Identity.EntrySequence, "ActivityEntryPipeline", "prepare_entry_clear_loaded_set");
            _activityInventoryRuntimeState.ClearCurrentActivityObjectContributorDiscoveryResult();
            _activityInventoryRuntimeState.ClearCurrentActivitySetupInventory();
            _activityInventoryRuntimeState.ClearCurrentActorInventoryFeedResult();
            _activityInventoryRuntimeState.ClearCurrentActivityCapabilityInventoryPreview();

            _logSink.LogEntryOwnerEvent(
                "ActivityEntryPreparationCompleted",
                command.Identity,
                command.Source,
                command.Reason,
                "block='entry_preparation' outcome='applied' resultKind='Prepared'");
            _logSink.LogEntryOwnerEvent(
                "ActivityEntryPrepared",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' outcome='prepared' resultKind='Prepared' next='content_load' note='setup_readiness_owned_by_activity_entry_pipeline'");

            ActivityEntryPreparationResult result = new(
                ActivityEntryPreparationResultKind.Prepared,
                command.Identity,
                "entry_preparation_completed");
            return result;
        }

        public ActivityEntrySetupReadinessResult ExecuteSetupAndReadiness(
            ActivityEntryCommand command,
            SessionActivityDefinition definition,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid for setup/readiness execution.");
            }

            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid for setup/readiness execution.");
            }

            SessionActivityIdentity setupStartedIdentity = command.Identity;
            try
            {
                BeginActivitySetupReadiness(
                    definition,
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    facts,
                    snapshots);

                _preparationBridge.ObserveActivitySceneContractOrSkip(
                    definition,
                    command.Source,
                    command.Reason,
                    facts,
                    snapshots,
                    setupStartedIdentity.EntrySequence);

                ActivityObjectSetupInventoryPlan objectSetupInventoryPlan = new(
                    setupStartedIdentity,
                    command.ActivityId,
                    command.ActivityOrdinal,
                    definition.ActivityContentProfile != null ? definition.ActivityContentProfile.SetupRequirements : null,
                    command.Source,
                    command.Reason);

                ActivityObjectResetRestorePlan objectResetRestorePlan = new(
                    setupStartedIdentity,
                    command.ActivityId,
                    command.ActivityOrdinal,
                    command.Source,
                    command.Reason);

                ActivityEntryObjectSetupCommand objectSetupCommand = new(
                    setupStartedIdentity,
                    objectSetupInventoryPlan,
                    objectResetRestorePlan);

                ActivityResetScopePlan resetScopePlan = ActivityResetBoundaryPolicy.ResolveForEntry(command);
                if (!resetScopePlan.IsValid)
                {
                    throw new InvalidOperationException($"Activity reset scope plan is invalid. activityId='{command.ActivityId}' entrySequence='{setupStartedIdentity.EntrySequence}'.");
                }

                _logSink.LogEntryOwnerEvent(
                    "ActivityResetScopePlanResolved",
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityResetBoundaryPolicy' entryPipelineOwner='ActivityEntryPipeline' block='reset_boundary_policy' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' boundaryKind='{resetScopePlan.BoundaryKind}' targetScope='{resetScopePlan.TargetScope}' boundaryEligibilityRequired='{ActivityResetBoundaryPolicy.ResolveEligibility(resetScopePlan.BoundaryKind)}' policyId='{resetScopePlan.PolicyId}' behaviorMode='ResetIntentStateProfilePolicy'");

                ActivityEntryObjectSetupResult setupInfrastructureResult = ExecuteSetupInfrastructure(
                    objectSetupCommand,
                    facts,
                    snapshots);
                if (!setupInfrastructureResult.Completed || !setupInfrastructureResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline setup infrastructure failed. reason='{setupInfrastructureResult.Reason}' identity='{setupInfrastructureResult.Identity}'.");
                }

                ActivityEntryParticipantBindingResult participantBindingResult = ExecuteParticipantBinding(
                    new ActivityEntryParticipantBindingCommand(
                        new ActivityParticipantBindingPlan(
                            setupStartedIdentity,
                            command.ActivityId,
                            command.ActivityOrdinal,
                            command.Source,
                            command.Reason)),
                    facts,
                    snapshots);
                if (!participantBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline participant binding failed. identity='{participantBindingResult.Identity}'.");
                }

                EmitActivityParticipantReadiness(
                    definition,
                    command.Source,
                    command.Reason,
                    facts,
                    snapshots,
                    setupStartedIdentity.EntrySequence,
                    participantBindingResult);

                ActivityEntryObjectSetupResult capabilityObjectSetupResult = ExecuteCapabilityObjectSetup(
                    objectSetupCommand,
                    resetScopePlan,
                    loadedSnapshotPayloadContext,
                    facts,
                    snapshots);
                if (!capabilityObjectSetupResult.Completed || !capabilityObjectSetupResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline capability object setup failed. reason='{capabilityObjectSetupResult.Reason}' identity='{capabilityObjectSetupResult.Identity}'.");
                }

                ActivityEntryParticipantResetStage.Execute(
                    command,
                    resetScopePlan,
                    participantBindingResult,
                    _activityInventoryRuntimeState.CurrentActorInventoryFeedResult,
                    _activityInventoryRuntimeState.CurrentActivityCapabilityInventoryPreview,
                    _actorResetAdapter,
                    _activityPlayerActorRegistry,
                    _currentActorMaterializationPlanEntries,
                    _placementMarkerLookup,
                    _factBridge,
                    _logSink,
                    facts,
                    snapshots);

                ActivityObjectExitCorrelationBundle exitCorrelation = BuildActivityObjectExitCorrelationBundle();

                ActivityEntryActorPresentationSetupResult actorPresentationSetupResult = ExecuteActorPresentationSetup(
                    new ActivityEntryActorPresentationSetupCommand(
                        setupStartedIdentity,
                        _activityInventoryRuntimeState.CurrentActivityPresentationSetupContributions,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!actorPresentationSetupResult.Completed || !actorPresentationSetupResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline actor presentation setup failed. reason='{actorPresentationSetupResult.Reason}' identity='{actorPresentationSetupResult.Identity}'.");
                }

                ActivityEntryActorAttributeSetupResult actorAttributeSetupResult = ExecuteActorAttributeSetup(
                    new ActivityEntryActorAttributeSetupCommand(
                        setupStartedIdentity,
                        _activityInventoryRuntimeState.CurrentActivityAttributeSetupContributions,
                        resetScopePlan.ResetIntent,
                        resetScopePlan.StateProfileKind,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!actorAttributeSetupResult.Completed || !actorAttributeSetupResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline actor attribute setup failed. reason='{actorAttributeSetupResult.Reason}' identity='{actorAttributeSetupResult.Identity}'.");
                }

                ExecuteActorAttributeUiBinding(command);

                ActivityEntryActorParticipationEnterResult actorParticipationEnterResult = ExecuteActorParticipationEnter(
                    new ActivityEntryActorParticipationEnterCommand(
                        setupStartedIdentity,
                        command.ActivityId,
                        command.ActivityOrdinal,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!actorParticipationEnterResult.Completed || !actorParticipationEnterResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline actor participation enter failed. reason='{actorParticipationEnterResult.Reason}' identity='{actorParticipationEnterResult.Identity}'.");
                }

                ActivityEntryPlayerInputBindingResult playerInputBindingResult = ExecutePlayerInputBinding(
                    new ActivityEntryPlayerInputBindingCommand(
                        setupStartedIdentity,
                        definition.ActivityId,
                        definition.ActivityOrdinal,
                        BuildPlayerInputBindingReferences(participantBindingResult),
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!playerInputBindingResult.Completed || !playerInputBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline player input binding failed. reason='{playerInputBindingResult.Reason}' identity='{playerInputBindingResult.Identity}'.");
                }

                ActorCommandBindingResult actorCommandBindingResult = ExecuteActorCommandBinding(
                    new ActorCommandBindingCommand(
                        setupStartedIdentity,
                        BuildActorCommandBindingReferences(participantBindingResult),
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!actorCommandBindingResult.Completed || !actorCommandBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline actor command binding failed. reason='{actorCommandBindingResult.Reason}' identity='{actorCommandBindingResult.Identity}'.");
                }

                ActivityEntryPermissionTargetPreparationResult permissionTargetPreparationResult = ExecutePermissionTargetPreparation(
                    new ActivityEntryPermissionTargetPreparationCommand(
                        setupStartedIdentity,
                        definition.ActivityId,
                        definition.ActivityOrdinal,
                        requireReceivers: true,
                        registerReceivers: true,
                        _activityInventoryRuntimeState.CurrentActivityPermissionReceiverContributions,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!permissionTargetPreparationResult.Completed || !permissionTargetPreparationResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline permission target preparation failed. reason='{permissionTargetPreparationResult.Reason}' identity='{permissionTargetPreparationResult.Identity}'.");
                }

                ActivityEntryMovementBindingResult movementBindingResult = ExecuteMovementBinding(
                    new ActivityEntryMovementBindingCommand(
                        setupStartedIdentity,
                        definition.ActivityId,
                        definition.ActivityOrdinal,
                        BuildMovementBindingReferences(participantBindingResult),
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!movementBindingResult.Completed || !movementBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline movement binding failed. reason='{movementBindingResult.Reason}' identity='{movementBindingResult.Identity}'.");
                }

                ActivityEntryCameraBindingResult cameraBindingResult = ExecuteCameraBinding(
                    new ActivityEntryCameraBindingCommand(
                        setupStartedIdentity,
                        _activityInventoryRuntimeState.CurrentActivityCameraBindingContributions,
                        definition.ActivityId,
                        definition.ActivityOrdinal,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!cameraBindingResult.Completed || !cameraBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline camera binding failed. reason='{cameraBindingResult.Reason}' identity='{cameraBindingResult.Identity}'.");
                }

                CompleteActivitySetupReadiness(
                    definition,
                    command.Source,
                    command.Reason,
                    facts,
                    snapshots,
                    setupStartedIdentity.EntrySequence);

                SessionActivityIdentity completedIdentity = _identityBridge.BuildIdentity(
                    definition,
                    SessionActivityStage.ActivitySetupCompleted,
                    setupStartedIdentity.EntrySequence);
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntrySetupReadinessCompleted",
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='setup_readiness_orchestration' resultKind='Completed' next='activation_flow' macroLifecycleOwner='SessionActivityPipeline' phaseOwner='ActivityEntryPipeline' phaseOwnership='entry_phase_internalized'");
                return new ActivityEntrySetupReadinessResult(
                    ActivityEntrySetupReadinessResultKind.Completed,
                    completedIdentity,
                    "setup_readiness_completed",
                    exitCorrelation);
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntrySetupReadinessFailed",
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='setup_readiness_orchestration' resultKind='Failed' error='{exception.Message}'");
                throw;
            }
        }

        private ActivityObjectExitCorrelationBundle BuildActivityObjectExitCorrelationBundle()
        {
            return new ActivityObjectExitCorrelationBundle(
                _activityInventoryRuntimeState.CurrentActivityObjectContributorDiscoveryResult,
                _activityInventoryRuntimeState.CurrentActivityCapabilityInventoryPreview);
        }

        private void BeginActivitySetupReadiness(
            SessionActivityDefinition definition,
            SessionActivityIdentity setupStartedIdentity,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            if (!setupStartedIdentity.IsValid || setupStartedIdentity.Stage != SessionActivityStage.ActivitySetupStarted)
            {
                throw new InvalidOperationException($"Activity setup/readiness requires ActivitySetupStarted identity. identity='{setupStartedIdentity}'.");
            }

            _preparationBridge.ResetMovementControlStateForEntry();
            _identityBridge.SetCurrentIdentity(setupStartedIdentity, SessionActivityStage.ActivitySetupStarted);
            _preparationBridge.BeginActivityActorScope(setupStartedIdentity);
            _preparationBridge.ClearActiveActorParticipations(definition.ActivityId, setupStartedIdentity.EntrySequence, source, reason);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupStarted,
                setupStartedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity setup started.");
            _factBridge.EmitSnapshot(
                snapshots,
                "activity_setup_started",
                source,
                reason,
                $"'{definition.ActivityId}' activity setup started.");
        }

        private void EmitActivityParticipantReadiness(
            SessionActivityDefinition definition,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            ActivityEntryParticipantBindingResult participantBindingResult)
        {
            SessionActivityIdentity startedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessStarted, entrySequence);
            _identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActivityParticipantReadinessStarted);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantReadinessStarted,
                startedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity participant readiness started.");
            _factBridge.EmitSnapshot(
                snapshots,
                "activity_participant_readiness_started",
                source,
                reason,
                $"'{definition.ActivityId}' activity participant readiness started.");

            SessionActivityIdentity expectedBindingIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantBindingCompleted, entrySequence);
            _preparationBridge.TryGetActivePlayerActorIdentities(startedIdentity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors);
            SessionActivityPipeline.ActivityParticipantReadinessStageResult readinessResult = ActivityParticipantReadinessStage.Execute(
                startedIdentity,
                expectedBindingIdentity,
                participantBindingResult,
                activeActors);

            if (readinessResult.IsSkipped)
            {
                SessionActivityIdentity skippedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessSkippedNoRequiredParticipant, entrySequence);
                _identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityParticipantReadinessSkippedNoRequiredParticipant);
                _factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantReadinessSkippedNoRequiredParticipant,
                    skippedIdentity,
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness skipped because no required participant was declared.");
                _factBridge.EmitSnapshot(
                    snapshots,
                    "activity_participant_readiness_skipped_no_required_participant",
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness skipped because no required participant was declared.");

                SessionActivityIdentity skippedCompletedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessCompleted, entrySequence);
                _identityBridge.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.ActivityParticipantReadinessCompleted);
                _factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantReadinessCompleted,
                    skippedCompletedIdentity,
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness completed with skip.");
                _factBridge.EmitSnapshot(
                    snapshots,
                    "activity_participant_readiness_completed",
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness completed with skip.");
                return;
            }

            if (readinessResult.IsFailed)
            {
                SessionActivityIdentity failedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessFailed, entrySequence);
                _identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityParticipantReadinessFailed);
                _factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantReadinessFailed,
                    failedIdentity,
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness failed reason='{readinessResult.ReasonCode}' requiredResolved='{readinessResult.RequiredResolvedRequirements}' required='{readinessResult.RequiredRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
                _factBridge.EmitSnapshot(
                    snapshots,
                    "activity_participant_readiness_failed",
                    source,
                    reason,
                    $"'{definition.ActivityId}' activity participant readiness failed reason='{readinessResult.ReasonCode}' requiredResolved='{readinessResult.RequiredResolvedRequirements}' required='{readinessResult.RequiredRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActivityParticipantReadiness] Failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='{readinessResult.ReasonCode}'.");
            }

            SessionActivityIdentity readyIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessValidatedMaterializedActors, entrySequence);
            _identityBridge.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivityParticipantReadinessValidatedMaterializedActors);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantReadyMaterializedActors,
                readyIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity participant materialized actors ready requiredReady='{readinessResult.RequiredResolvedRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");
            _factBridge.EmitSnapshot(
                snapshots,
                "activity_participant_ready_materialized_actors",
                source,
                reason,
                $"'{definition.ActivityId}' activity participant materialized actors ready requiredReady='{readinessResult.RequiredResolvedRequirements}' activeActors='{readinessResult.ActiveActorsCount}'.");

            SessionActivityIdentity completedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityParticipantReadinessCompleted, entrySequence);
            _identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityParticipantReadinessCompleted);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityParticipantReadinessCompleted,
                completedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity participant readiness completed.");
            _factBridge.EmitSnapshot(
                snapshots,
                "activity_participant_readiness_completed",
                source,
                reason,
                $"'{definition.ActivityId}' activity participant readiness completed.");
        }

        private void CompleteActivitySetupReadiness(
            SessionActivityDefinition definition,
            string source,
            string reason,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            SessionActivityIdentity setupCompletedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivitySetupCompleted, entrySequence);
            _identityBridge.SetCurrentIdentity(setupCompletedIdentity, SessionActivityStage.ActivitySetupCompleted);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivitySetupCompleted,
                setupCompletedIdentity,
                source,
                reason,
                $"'{definition.ActivityId}' activity setup completed.");
            _logSink.LogPhaseBoundary("SessionActivitySetupCompleted", setupCompletedIdentity, source, reason, completed: true, detail: "phase='setup'");
            _logSink.LogPhaseBoundary("SessionActivityBindingCompleted", setupCompletedIdentity, source, reason, completed: true, detail: "phase='binding'");
            if (definition.ActivationWindowMode == ActivityWindowMode.None)
            {
                _preparationBridge.EmitPredefinedVisualSetupReadyFactIfApplicable(
                    definition,
                    facts,
                    setupCompletedIdentity,
                    source,
                    reason,
                    "activity_setup_completed");
            }

            _factBridge.EmitSnapshot(
                snapshots,
                "activity_setup_completed",
                source,
                reason,
                $"'{definition.ActivityId}' activity setup completed.");
        }

        public ActivityEntryContentLoadResult BeginContentLoad(
            ActivityEntryContentLoadCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryContentLoadCommand is invalid.");
            }

            ActivityContentLoadPlan plan = command.Plan;
            ValidateContentLoadPlanOrThrow(plan);

            int entrySequence = plan.Identity.EntrySequence;
            SessionActivityIdentity profileResolvedIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentProfileResolved, command.Source);
            _identityBridge.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActivityContentProfileResolved);

            if (plan.ActivityContentMode == ActivityContentMode.None)
            {
                _factBridge.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentProfileResolved,
                    profileResolvedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{plan.ActivityId}' activity content profile resolved mode='None'.");
                _factBridge.EmitSnapshot(snapshots,
                    "activity_content_profile_resolved_none",
                    command.Source,
                    command.Reason,
                    $"'{plan.ActivityId}' activity content profile resolved mode='None'.");

                SessionActivityIdentity skippedIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentLoadSkippedNoContent, command.Source);
                _identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityContentLoadSkippedNoContent);
                _factBridge.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentLoadSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{plan.ActivityId}' activity content load skipped as no-content.");
                _factBridge.EmitSnapshot(snapshots,
                    "activity_content_load_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{plan.ActivityId}' activity content load skipped as no-content.");
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryContentLoadSkipped",
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    "mode='None'");
                return new ActivityEntryContentLoadResult(
                    shouldContinueEntry: true,
                    pendingOperationIssued: false,
                    reason: "content_load_skipped_no_content");
            }

            if (plan.ActivityContentMode != ActivityContentMode.Profile)
            {
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' has unsupported ActivityContentMode='{plan.ActivityContentMode}'.");
            }

            string profileId = Normalize(plan.ActivityContentProfileId);
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentProfileResolved,
                profileResolvedIdentity,
                command.Source,
                command.Reason,
                $"'{plan.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_profile_resolved_profile",
                command.Source,
                command.Reason,
                $"'{plan.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");

            SessionActivityIdentity loadStartedIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentLoadStarted, command.Source);
            _identityBridge.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.ActivityContentLoadStarted);
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentLoadStarted,
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"'{plan.ActivityId}' activity content load started profileId='{profileId}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_load_started",
                command.Source,
                command.Reason,
                $"'{plan.ActivityId}' activity content load started profileId='{profileId}'.");
            _logSink.LogEntryOwnerEvent(
                "ActivityEntryContentLoadStarted",
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"profileId='{profileId}'");

            _pendingContentLoadContext = new PendingContentLoadContext(plan);

            ExecuteNextContentSceneLoad(plan, command.Source, command.Reason, entrySequence, facts, snapshots);
            return new ActivityEntryContentLoadResult(
                shouldContinueEntry: false,
                pendingOperationIssued: true,
                reason: "content_load_pending_operation_started");
        }

        public ActivityEntryContentLoadResult CompleteContentLoad(
            ActivityEntryContentLoadCompletionCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryContentLoadCompletionCommand is invalid.");
            }

            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{command.ActivityId}' missing pending activity content load context on completion.");
            }

            if (!MatchesContext(command.Operation, command.ActiveIdentity, command.ActivityId))
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryContentLoadFailed",
                    command.ActiveIdentity,
                    command.Source,
                    command.Reason,
                    "reason='stale_or_foreign_content_load_completion'");
                throw new InvalidOperationException("stale_or_foreign_content_load_completion");
            }

            int entrySequence = command.ActiveIdentity.EntrySequence;
            SessionActivityIdentity loadedIdentity = BuildIdentity(command.ActiveIdentity, SessionActivityStage.ActivityContentSceneLoaded, command.Source);
            _identityBridge.SetCurrentIdentity(loadedIdentity, SessionActivityStage.ActivityContentSceneLoaded);
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentSceneLoaded,
                loadedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity content scene loaded operationId='{command.Operation.OperationId}' sceneName='{command.Operation.SceneName}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_scene_loaded",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity content scene loaded operationId='{command.Operation.OperationId}' sceneName='{command.Operation.SceneName}'.");

            ActivityContentLoadedSceneRecord record = new(
                loadedIdentity,
                _pendingContentLoadContext.ContentProfileId,
                _pendingContentLoadContext.NextSceneOrdinal,
                new ActivityContentSceneRuntimeReference(command.Operation.SceneKey, command.Operation.SceneName),
                command.Operation.OperationId,
                ResolveRequirednessForCurrentLoadedSceneOrFail(),
                command.Source,
                command.Reason);
                _pendingContentLoadContext.LoadedRecords.Add(record);
            _pendingContentLoadContext.NextSceneOrdinal += 1;

            if (_pendingContentLoadContext.NextSceneOrdinal > _pendingContentLoadContext.Scenes.Count)
            {
                FinalizeContentLoadedSet(command.Source, command.Reason, entrySequence, facts, snapshots);
                return new ActivityEntryContentLoadResult(
                    shouldContinueEntry: true,
                    pendingOperationIssued: false,
                    reason: "content_load_completed_loaded_set_ready");
            }

            ExecuteNextContentSceneLoad(_pendingContentLoadContext.Plan, command.Source, command.Reason, entrySequence, facts, snapshots);
            return new ActivityEntryContentLoadResult(
                shouldContinueEntry: false,
                pendingOperationIssued: true,
                reason: "content_load_pending_next_scene");
        }

        public void FailContentLoad(
            ActivityEntryContentLoadFailureCommand command,
            List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryContentLoadFailureCommand is invalid.");
            }

            SessionActivityIdentity failedIdentity = BuildIdentity(
                command.ActiveIdentity,
                SessionActivityStage.ActivityContentLoadFailed,
                command.Source);
            _identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentLoadFailed,
                failedIdentity,
                command.Source,
                command.Reason,
                $"Activity content scene load failed operationId='{command.Operation.OperationId}' scene='{command.Operation.SceneName}' error='{command.Error}'.");
            _logSink.LogEntryOwnerEvent(
                "ActivityEntryContentLoadFailed",
                failedIdentity,
                command.Source,
                command.Reason,
                $"operationId='{command.Operation.OperationId}'");
            _pendingContentLoadContext = null;
        }

        public ActivityEntryObjectSetupResult ExecuteSetupInfrastructure(
            ActivityEntryObjectSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            try
            {
                ActivityContentLoadedSet loadedSet = _activityContentRuntimeState.CurrentLoadedSet;
                ActivityEntryActorInventoryStage.ExecuteSceneDiscovery(
                    command,
                    loadedSet,
                    _identityBridge,
                    _factBridge,
                    _logSink,
                    _activitySceneActorRegistry,
                    facts,
                    snapshots);

                ActivityEntryObjectContributorDiscoveryStage.Execute(
                    command,
                    loadedSet,
                    _factBridge,
                    _logSink,
                    _activityInventoryRuntimeState,
                    facts,
                    snapshots);

                ActivityEntrySetupInventoryStage.Execute(
                    command,
                    _activitySetupInventoryBuilder,
                    _runtimeBridge,
                    _activityInventoryRuntimeState,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntrySetupInfrastructureCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='setup_inventory'");
                return new ActivityEntryObjectSetupResult(
                    completed: true,
                    command.Identity,
                    "setup_infrastructure_applied");
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntrySetupInfrastructureFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='setup_inventory' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryParticipantBindingResult ExecuteParticipantBinding(
            ActivityEntryParticipantBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantBindingCommand is invalid.");
            }

            try
            {
                ActivityEntryParticipantBindingResult result = ActivityEntryParticipantBindingStage.Execute(
                    command,
                    _runtimeBridge,
                    _activityInventoryRuntimeState.CurrentActivitySetupInventory,
                    _playerActorMaterializationAdapter,
                    _playerActorParticipationAdapter,
                    _currentActorMaterializationPlanEntries,
                    _activityPlayerActorRegistry,
                    _sessionActorRuntimeStore,
                    _activityParticipationRuntimeState,
                    _activityActorExitRuntimeState,
                    _activityRetainedParticipantLookup,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryParticipantBindingCompleted",
                    result.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='participant_binding' totalRequirements='{result.TotalRequirements}' resolved='{result.ResolvedRequirements}' skipped='{result.SkippedRequirements}' required='{result.RequiredRequirements}' requiredResolved='{result.RequiredResolvedRequirements}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryParticipantBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='participant_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActorCommandBindingResult ExecuteActorCommandBinding(
            ActorCommandBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorCommandBindingCommand is invalid.");
            }

            try
            {
                ActorCommandBindingResult result = ActivityEntryActorCommandBindingStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _actorCommandBindingAdapter,
                    _activityPlayerActorRegistry,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorCommandBindingCompleted",
                    result.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_command_binding' total='{result.TotalRequirements}' required='{result.RequiredRequirements}' requiredBound='{result.RequiredBoundCount}' totalBound='{result.TotalBoundCount}' skipped='{result.SkippedCount}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorCommandBindingFailed",
                    command.PipelineIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_command_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryObjectSetupResult ExecuteCapabilityObjectSetup(
            ActivityEntryObjectSetupCommand command,
            ActivityResetScopePlan resetScopePlan,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            if (!resetScopePlan.IsValid)
            {
                throw new InvalidOperationException("ActivityResetScopePlan is invalid for capability object setup.");
            }

            if (!resetScopePlan.Identity.CycleKey.Equals(command.Identity.CycleKey))
            {
                throw new InvalidOperationException(
                    $"ActivityEntryPipeline capability object setup requires reset scope plan from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            try
            {
                ActivityObjectContributorDiscoveryResult discoveryResult = _activityInventoryRuntimeState.CurrentActivityObjectContributorDiscoveryResult;
                ActorInventoryFeedResult actorInventoryFeed = ActivityEntryActorInventoryStage.ExecuteActorInventoryFeed(
                    command,
                    _logSink,
                    _activityParticipationRuntimeState.CurrentParticipationContext,
                    _activitySceneActorRegistry,
                    _activityPlayerActorRegistry,
                    _sessionActorRuntimeStore,
                    _activityInventoryRuntimeState);
                _activityActorExitRuntimeState.StoreActorInventoryFeedResult(
                    actorInventoryFeed,
                    command.Identity.ActivityId,
                    command.Identity.EntrySequence,
                    "ActivityEntryActorInventoryStage",
                    "actor_inventory_feed_result_stored");
                IReadOnlyList<ActorScanTarget> actorTargets = actorInventoryFeed.BuildScanTargets(command.Source);

                ActivityCapabilityInventoryBuildResult buildResult = ActivityEntryCapabilityInventoryPreviewStage.Execute(
                    command,
                    discoveryResult,
                    actorTargets,
                    _activityEntryCapabilityInventoryBuildStage,
                    _runtimeBridge,
                    _activityInventoryRuntimeState,
                    facts,
                    snapshots);

                ActivityEntryObjectResetStage.Execute(
                    command,
                    resetScopePlan,
                    discoveryResult,
                    buildResult.Inventory,
                    _runtimeBridge,
                    facts,
                    snapshots);

                string restorePayloadState = loadedSnapshotPayloadContext.HasPayload ? "true" : "false";
                int restorePayloadRecordCount = loadedSnapshotPayloadContext.HasPayload
                    ? loadedSnapshotPayloadContext.Payload.RecordCount
                    : 0;
                string restorePayloadSourceActivityId = loadedSnapshotPayloadContext.HasPayload
                    ? Normalize(loadedSnapshotPayloadContext.Payload.ActivityId)
                    : "<none>";
                int restorePayloadSourceEntrySequence = loadedSnapshotPayloadContext.HasPayload
                    ? loadedSnapshotPayloadContext.Payload.SourceEntrySequence
                    : 0;
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntrySnapshotRestoreReady",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='capability_inventory_object_state' loadedSnapshotPayload='{restorePayloadState}' loadedSnapshotPayloadRecordCount='{restorePayloadRecordCount}' loadedSnapshotPayloadSourceActivityId='{restorePayloadSourceActivityId}' loadedSnapshotPayloadSourceEntrySequence='{restorePayloadSourceEntrySequence}'");
                ActivityEntryObjectSnapshotRestoreStage.Execute(
                    command,
                    discoveryResult,
                    buildResult.Inventory,
                    _runtimeBridge,
                    loadedSnapshotPayloadContext,
                    facts);

                ActivityEntryPoolPreparationStage.Execute(
                    command.Identity,
                    actorInventoryFeed,
                    _poolService,
                    command.Source,
                    command.Reason);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryCapabilityObjectSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='capability_inventory_object_state'");
                return new ActivityEntryObjectSetupResult(
                    completed: true,
                    command.Identity,
                    "capability_object_setup_applied");
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryCapabilityObjectSetupFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='capability_inventory_object_state' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryActorPresentationSetupResult ExecuteActorPresentationSetup(
            ActivityEntryActorPresentationSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorPresentationSetupCommand is invalid.");
            }

            try
            {
                ActivityEntryActorPresentationSetupResult result = ActivityEntryActorPresentationStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _activityInventoryRuntimeState.CurrentActivityPresentationSetupContributions,
                    _activityActorExitRuntimeState,
                    _actorPresentationBridge,
                    _actorPresentationPlanResolver,
                    _actorPresentationMaterializationAdapter,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorPresentationSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_presentation_setup' total='{result.Total}' resolved='{result.Resolved}' materialized='{result.Materialized}' retained='{result.Retained}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorPresentationSetupFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_presentation_setup' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryActorAttributeSetupResult ExecuteActorAttributeSetup(
            ActivityEntryActorAttributeSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorAttributeSetupCommand is invalid.");
            }

            try
            {
                ActivityEntryActorAttributeSetupResult result = ActivityEntryActorAttributeStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _activityActorExitRuntimeState,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_setup' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' stateProfileSource='attribute_setup_state_profile' total='{result.Total}' resolved='{result.Resolved}' ready='{result.Ready}' skipped='{result.Skipped}' failed='{result.Failed}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeSetupFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_setup' error='{exception.Message}'");
                throw;
            }
        }

        private ActivityEntryActorAttributeUiBindingResult ExecuteActorAttributeUiBinding(ActivityEntryCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid for actor attribute UI binding.");
            }

            try
            {
                return ActivityEntryActorAttributeUiBindingStage.Execute(
                    command.Identity,
                    command.Source,
                    command.Reason,
                    _actorAttributeEventStream,
                    _activityPlayerActorRegistry,
                    _activityParticipationRuntimeState.CurrentParticipationContext,
                    _activityActorExitRuntimeState,
                    _actorAttributeUiBindingRequestProvider,
                    _activityActorAttributeUiBindingRuntimeState);
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeUiBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_ui_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryActorParticipationEnterResult ExecuteActorParticipationEnter(
            ActivityEntryActorParticipationEnterCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorParticipationEnterCommand is invalid.");
            }

            try
            {
                ActivityEntryActorParticipationEnterResult result = ActivityEntryActorParticipationStage.ExecuteEnter(
                    command,
                    _identityBridge,
                    _factBridge,
                    _activityInventoryRuntimeState.CurrentActorInventoryFeedResult,
                    _activityActorExitRuntimeState,
                    _actorParticipationBridge,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorParticipationEnterCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_participation_enter' total='{result.Total}' entered='{result.Entered}' skipped='{result.Skipped}' failed='{result.Failed}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorParticipationEnterFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_participation_enter' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryPlayerInputBindingResult ExecutePlayerInputBinding(
            ActivityEntryPlayerInputBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPlayerInputBindingCommand is invalid.");
            }

            try
            {
                ActivityEntryPlayerInputBindingResult result = ActivityEntryPlayerInputBindingStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _playerInputBindingAdapter,
                    _activityPlayerActorRegistry,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryPlayerInputBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='player_input_binding' required='{result.RequiredCount}' requiredBound='{result.RequiredBoundCount}' totalBound='{result.TotalBoundCount}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryPlayerInputBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='player_input_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryPermissionTargetPreparationResult ExecutePermissionTargetPreparation(
            ActivityEntryPermissionTargetPreparationCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPermissionTargetPreparationCommand is invalid.");
            }

            try
            {
                ActivityEntryPermissionTargetPreparationResult result = ActivityGateBindingStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _permissionTargetBridge,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityGateBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='gate_binding' receivers='{result.ReceiverCount}' skipped='{result.Skipped}' registerReceivers='{command.RegisterReceivers}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityGateBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='gate_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryMovementBindingResult ExecuteMovementBinding(
            ActivityEntryMovementBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryMovementBindingCommand is invalid.");
            }

            try
            {
                ActivityEntryMovementBindingResult result = ActivityEntryMovementBindingStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _activityPlayerActorRegistry,
                    _movementBindingAdapter,
                    _movementBindingBridge,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryMovementBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='movement_binding' required='{result.RequiredCount}' requiredBound='{result.RequiredBoundCount}' totalBound='{result.TotalBoundCount}' retained='{result.RetainedCount}' skipped='{result.Skipped}' retainedExisting='{result.RetainedExistingBinding}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryMovementBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='movement_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryCameraBindingResult ExecuteCameraBinding(
            ActivityEntryCameraBindingCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCameraBindingCommand is invalid.");
            }

            if (_activityCameraPreparationExecutor == null)
            {
                throw new InvalidOperationException("ActivityEntryPipeline camera preparation executor is not configured.");
            }

            try
            {
                ActivityEntryCameraBindingResult result = ActivityEntryCameraBindingStage.Execute(
                    command,
                    _identityBridge,
                    _factBridge,
                    _activityCameraPreparationExecutor,
                    _activityInventoryRuntimeState.CurrentActivitySetupInventory,
                    _activityInventoryRuntimeState.CurrentActivityCapabilityInventoryPreview,
                    command.CameraBindingContributions,
                    _activityParticipationRuntimeState.CurrentParticipationContext?.Participants ?? Array.Empty<ActivityParticipantBinding>(),
                    _cameraBindingBridge,
                    facts,
                    snapshots);

                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryCameraBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='camera_binding' required='{result.RequiredCount}' targetBound='{result.TargetBound}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryCameraBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='camera_binding' error='{exception.Message}'");
                throw;
            }
        }

        private static IReadOnlyList<ActivityEntryPlayerInputBindingReference> BuildPlayerInputBindingReferences(
            ActivityEntryParticipantBindingResult participantBindingResult)
        {
            if (!participantBindingResult.IsValid)
            {
                return Array.Empty<ActivityEntryPlayerInputBindingReference>();
            }

            IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants = participantBindingResult.ResolvedParticipants;
            if (resolvedParticipants == null || resolvedParticipants.Count == 0)
            {
                return Array.Empty<ActivityEntryPlayerInputBindingReference>();
            }

            List<ActivityEntryPlayerInputBindingReference> references = new(resolvedParticipants.Count);
            for (int index = 0; index < resolvedParticipants.Count; index++)
            {
                ActivityEntryParticipantBindingResolvedRecord resolved = resolvedParticipants[index];
                if (!resolved.IsValid)
                {
                    continue;
                }

                references.Add(new ActivityEntryPlayerInputBindingReference(
                    resolved.RequirementId,
                    resolved.ParticipantKind,
                    resolved.ParticipantBinding,
                    resolved.Required));
            }

            return references;
        }

        private static IReadOnlyList<ActorCommandBindingReference> BuildActorCommandBindingReferences(
            ActivityEntryParticipantBindingResult participantBindingResult)
        {
            if (!participantBindingResult.IsValid)
            {
                return Array.Empty<ActorCommandBindingReference>();
            }

            IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants = participantBindingResult.ResolvedParticipants;
            if (resolvedParticipants == null || resolvedParticipants.Count == 0)
            {
                return Array.Empty<ActorCommandBindingReference>();
            }

            List<ActorCommandBindingReference> references = new(resolvedParticipants.Count);
            for (int index = 0; index < resolvedParticipants.Count; index++)
            {
                ActivityEntryParticipantBindingResolvedRecord resolved = resolvedParticipants[index];
                if (!resolved.IsValid || !resolved.ParticipantBinding.RequiresPlayerInput)
                {
                    continue;
                }

                references.Add(new ActorCommandBindingReference(
                    resolved.RequirementId,
                    resolved.ParticipantKind,
                    resolved.ParticipantBinding,
                    required: false));
            }

            return references;
        }

        private static IReadOnlyList<ActivityEntryMovementBindingReference> BuildMovementBindingReferences(
            ActivityEntryParticipantBindingResult participantBindingResult)
        {
            if (!participantBindingResult.IsValid)
            {
                return Array.Empty<ActivityEntryMovementBindingReference>();
            }

            IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants = participantBindingResult.ResolvedParticipants;
            if (resolvedParticipants == null || resolvedParticipants.Count == 0)
            {
                return Array.Empty<ActivityEntryMovementBindingReference>();
            }

            List<ActivityEntryMovementBindingReference> references = new(resolvedParticipants.Count);
            for (int index = 0; index < resolvedParticipants.Count; index++)
            {
                ActivityEntryParticipantBindingResolvedRecord resolved = resolvedParticipants[index];
                if (!resolved.IsValid)
                {
                    continue;
                }

                references.Add(new ActivityEntryMovementBindingReference(
                    resolved.RequirementId,
                    resolved.ParticipantKind,
                    resolved.ParticipantBinding,
                    resolved.Required));
            }

            return references;
        }

        public void ResetState()
        {
            _pendingContentLoadContext = null;
            _activityActorAttributeUiBindingRuntimeState.ClearAll();
            _activityInventoryRuntimeState.ClearCurrentActivityObjectContributorDiscoveryResult();
            _activityInventoryRuntimeState.ClearCurrentActorInventoryFeedResult();
            _activityInventoryRuntimeState.ClearCurrentActivitySetupInventory();
            _activityInventoryRuntimeState.ClearCurrentActivityCapabilityInventoryPreview();
            _activityParticipationRuntimeState.ClearCurrentParticipationContext();
            ClearCurrentActorMaterializationPlanEntries();
        }

        public ActivityObjectContributorDiscoveryResult GetCurrentActivityObjectContributorDiscoveryResult()
        {
            return _activityInventoryRuntimeState.CurrentActivityObjectContributorDiscoveryResult;
        }

        public void SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result)
        {
            _activityInventoryRuntimeState.SetCurrentActivityObjectContributorDiscoveryResult(result);
        }

        public void ClearCurrentActivityObjectContributorDiscoveryResult()
        {
            _activityInventoryRuntimeState.ClearCurrentActivityObjectContributorDiscoveryResult();
        }

        public ActorInventoryFeedResult GetCurrentActorInventoryFeedResult()
        {
            return _activityInventoryRuntimeState.CurrentActorInventoryFeedResult;
        }

        public void SetCurrentActorInventoryFeedResult(ActorInventoryFeedResult result)
        {
            _activityInventoryRuntimeState.SetCurrentActorInventoryFeedResult(result);
        }

        public void ClearCurrentActorInventoryFeedResult()
        {
            _activityInventoryRuntimeState.ClearCurrentActorInventoryFeedResult();
        }

        public ActivitySetupInventory GetCurrentActivitySetupInventory()
        {
            return _activityInventoryRuntimeState.CurrentActivitySetupInventory;
        }

        public void SetCurrentActivitySetupInventory(ActivitySetupInventory inventory)
        {
            _activityInventoryRuntimeState.SetCurrentActivitySetupInventory(inventory);
        }

        public void ClearCurrentActivitySetupInventory()
        {
            _activityInventoryRuntimeState.ClearCurrentActivitySetupInventory();
        }

        public ActivityCapabilityInventory GetCurrentActivityCapabilityInventoryPreview()
        {
            return _activityInventoryRuntimeState.CurrentActivityCapabilityInventoryPreview;
        }

        public void SetCurrentActivityCapabilityInventoryPreview(ActivityCapabilityInventory inventory)
        {
            _activityInventoryRuntimeState.SetCurrentActivityCapabilityInventoryPreview(inventory);
        }

        public void ClearCurrentActivityCapabilityInventoryPreview()
        {
            _activityInventoryRuntimeState.ClearCurrentActivityCapabilityInventoryPreview();
        }

        public ActivityParticipationContext GetCurrentActivityParticipationContext()
        {
            return _activityParticipationRuntimeState.CurrentParticipationContext;
        }

        internal void StoreCurrentSessionParticipationContext(PlayerSessionParticipationContext context)
        {
            _activityParticipationRuntimeState.StoreCurrentSessionParticipationContext(context);
        }

        internal void ClearCurrentSessionParticipationContext()
        {
            _activityParticipationRuntimeState.ClearCurrentSessionParticipationContext();
        }

        internal void StoreCurrentActorMaterializationPlanEntries(IReadOnlyList<SessionActivityActorMaterializationPlanEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                _currentActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
                return;
            }

            SessionActivityActorMaterializationPlanEntry[] frozenEntries = new SessionActivityActorMaterializationPlanEntry[entries.Count];
            for (int index = 0; index < entries.Count; index++)
            {
                frozenEntries[index] = entries[index];
            }

            _currentActorMaterializationPlanEntries = frozenEntries;
        }

        internal void ClearCurrentActorMaterializationPlanEntries()
        {
            _currentActorMaterializationPlanEntries = Array.Empty<SessionActivityActorMaterializationPlanEntry>();
        }

        internal int ClearActorAttributeUiBindings(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            int releasedCount = _activityActorAttributeUiBindingRuntimeState.ClearAll();
            if (releasedCount > 0 && identity.IsValid)
            {
                _logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeUiBindingsReleased",
                    identity,
                    source,
                    reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_ui_binding' releasedCount='{releasedCount}'");
            }

            return releasedCount;
        }

        private void ExecuteNextContentSceneLoad(
            ActivityContentLoadPlan plan,
            string source,
            string reason,
            int entrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' has no valid pending activity content load context.");
            }

            int sceneOrdinal = _pendingContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingContentLoadContext.Scenes.Count)
            {
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' next content scene ordinal is out of range. next='{sceneOrdinal}' total='{_pendingContentLoadContext.Scenes.Count}'.");
            }

            ActivityContentLoadPlanScene scene = _pendingContentLoadContext.Scenes[sceneOrdinal - 1];
            if (scene is { IsValid: false, Requiredness: ActivityContentRequiredness.Required })
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentLoadFailed, source);
                _identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadFailed, failedIdentity, source, reason, $"'{plan.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _factBridge.EmitSnapshot(snapshots, "activity_content_load_failed_required_scene_invalid", source, reason, $"'{plan.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _logSink.LogEntryOwnerEvent("ActivityEntryContentLoadFailed", failedIdentity, source, reason, "reason='required_scene_invalid'");
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' required content scene at ordinal='{sceneOrdinal}' is invalid.");
            }

            if (!scene.HasSceneReference)
            {
                SessionActivityIdentity rejectedIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentLoadFailed, source);
                _identityBridge.SetCurrentIdentity(rejectedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadRejected, rejectedIdentity, source, reason, $"'{plan.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{scene.Requiredness}'.");
                _factBridge.EmitSnapshot(snapshots, "activity_content_scene_load_rejected", source, reason, $"'{plan.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{scene.Requiredness}'.");
                _pendingContentLoadContext.NextSceneOrdinal += 1;
                if (_pendingContentLoadContext.NextSceneOrdinal > _pendingContentLoadContext.Scenes.Count)
                {
                    FinalizeContentLoadedSet(source, reason, entrySequence, facts, snapshots);
                }
                else
                {
                    ExecuteNextContentSceneLoad(plan, source, reason, entrySequence, facts, snapshots);
                }

                return;
            }

            SessionActivityIdentity loadingIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentSceneLoading, source);
            _identityBridge.SetCurrentIdentity(loadingIdentity, SessionActivityStage.ActivityContentSceneLoading);

            ActivityContentSceneLoadCommand loadCommand = new(
                Guid.NewGuid().ToString("N"),
                loadingIdentity,
                _pendingContentLoadContext.ContentProfileId,
                scene.SceneOrdinal,
                scene.SceneReference,
                scene.Requiredness,
                source,
                reason);

            SessionActivityPendingOperation pendingOperation = BuildActivityContentPendingOperation(plan, entrySequence, loadCommand);
            _contentPendingOperationBridge.SetPendingOperation(pendingOperation);
            _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadCommandIssued, loadingIdentity, source, reason, $"'{plan.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' contentProfileId='{loadCommand.ContentProfileId}' sceneOrdinal='{loadCommand.SceneOrdinal}' sceneKey='{loadCommand.SceneKey}' sceneName='{loadCommand.SceneName}' requiredness='{loadCommand.Requiredness}'.");
            _factBridge.EmitSnapshot(snapshots, "activity_content_scene_load_command_issued", source, reason, $"'{plan.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' sceneName='{loadCommand.SceneName}'.");
            _pendingOperationRunner.RunActivityContentOperation(pendingOperation, loadCommand, _pendingOperationCallback);
        }

        private SessionActivityPendingOperation BuildActivityContentPendingOperation(
            ActivityContentLoadPlan plan,
            int entrySequence,
            ActivityContentSceneLoadCommand command)
        {
            return new SessionActivityPendingOperation(
                command.OperationId,
                _identityBridge.PipelineId,
                _identityBridge.SessionId,
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

        private void FinalizeContentLoadedSet(
            string source,
            string reason,
            int entrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException("Pending activity content load context is invalid to finalize loaded set.");
            }

            ActivityContentLoadPlan plan = _pendingContentLoadContext.Plan;
            string profileId = _pendingContentLoadContext.ContentProfileId;
            int loadedSceneCount = _pendingContentLoadContext.LoadedRecords.Count;
            SessionActivityIdentity readyIdentity = BuildIdentity(plan.Identity, SessionActivityStage.ActivityContentLoadedSetReady, source);
            _identityBridge.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivityContentLoadedSetReady);
            ActivityContentLoadedSet loadedSet = new(
                readyIdentity,
                profileId,
                _pendingContentLoadContext.LoadedRecords,
                source,
                reason);
            if (!loadedSet.IsValid)
            {
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' produced invalid ActivityContentLoadedSet.");
            }

            _activityContentRuntimeState.StoreCurrentLoadedSet(
                loadedSet,
                plan.ActivityId,
                entrySequence,
                "ActivityEntryPipeline",
                "activity_content_loaded_set_ready");
            _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadedSetReady, readyIdentity, source, reason, $"'{plan.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _factBridge.EmitSnapshot(snapshots, "activity_content_loaded_set_ready", source, reason, $"'{plan.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _logSink.LogEntryOwnerEvent(
                "ActivityEntryContentLoadCompleted",
                readyIdentity,
                source,
                reason,
                $"loadedScenes='{loadedSceneCount}'");
            _pendingContentLoadContext = null;
        }

        private bool MatchesContext(
            SessionActivityPendingOperation operation,
            SessionActivityIdentity activeIdentity,
            string activityId)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                return false;
            }

            return string.Equals(operation.PipelineId, _identityBridge.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(operation.SessionStateId, _identityBridge.SessionId, StringComparison.Ordinal) &&
                   string.Equals(operation.ActivityId, activityId, StringComparison.Ordinal) &&
                   operation.EntrySequence == activeIdentity.EntrySequence &&
                   string.Equals(_pendingContentLoadContext.Identity.PipelineId, activeIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(_pendingContentLoadContext.Identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(_pendingContentLoadContext.Identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) &&
                   _pendingContentLoadContext.Identity.EntrySequence == activeIdentity.EntrySequence;
        }

        private ActivityContentRequiredness ResolveRequirednessForCurrentLoadedSceneOrFail()
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException("Pending activity content load context is invalid while resolving requiredness.");
            }

            int sceneOrdinal = _pendingContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingContentLoadContext.Scenes.Count)
            {
                throw new InvalidOperationException($"Pending activity content scene ordinal '{sceneOrdinal}' is out of range while resolving requiredness.");
            }

            ActivityContentLoadPlanScene scene = _pendingContentLoadContext.Scenes[sceneOrdinal - 1];
            if (scene.Requiredness == ActivityContentRequiredness.Unknown)
            {
                throw new InvalidOperationException($"Pending activity content scene requiredness is invalid at ordinal='{sceneOrdinal}'.");
            }

            return scene.Requiredness;
        }

        private static SessionActivityIdentity BuildIdentity(
            SessionActivityIdentity identity,
            SessionActivityStage stage,
            string source)
        {
            return new SessionActivityIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                stage,
                source);
        }

        private static void ValidateContentLoadPlanOrThrow(ActivityContentLoadPlan plan)
        {
            if (!plan.IsValid)
            {
                throw new InvalidOperationException($"Activity '{plan.ActivityId}' has invalid ActivityContentLoadPlan.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
