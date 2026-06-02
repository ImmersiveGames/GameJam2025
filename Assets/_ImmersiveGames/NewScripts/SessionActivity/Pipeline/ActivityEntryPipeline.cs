using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class ActivityEntryPipeline : IActivityEntryPipeline
    {
        private sealed class PendingContentLoadContext
        {
            public PendingContentLoadContext(
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

        // Ponte transitória SA-7B0: mantida apenas para stages ainda não migrados
        // para bridges menores. O ActivityEntryPipeline usa os campos de domínio abaixo
        // nos pontos já normalizados deste corte.
        private readonly IActivityEntryRuntimeBridge _runtimeBridge;
        private readonly IActivityEntryIdentityRuntimeBridge _identityBridge;
        private readonly IActivityEntryFactRuntimeBridge _factBridge;
        private readonly IActivityEntryContentLoadedSetRuntimeBridge _contentLoadedSetBridge;
        private readonly IActivityEntryContentPendingOperationRuntimeBridge _contentPendingOperationBridge;
        private readonly IActivityEntryLogRuntimeBridge _logBridge;
        private readonly IActivityEntryPreparationRuntimeBridge _preparationBridge;
        private readonly IActivityEntryObjectSetupRuntimeBridge _objectSetupBridge;
        private readonly IActivityEntryActorInventoryRuntimeBridge _actorInventoryBridge;
        private readonly IActivityEntryActorPresentationRuntimeBridge _actorPresentationBridge;
        private readonly IActivityEntryActorAttributeRuntimeBridge _actorAttributeBridge;
        private readonly IActivityEntryActorParticipationRuntimeBridge _actorParticipationBridge;
        private readonly IActivityEntryParticipantBindingRuntimeBridge _participantBindingBridge;
        private readonly IActivityEntryPermissionTargetRuntimeBridge _permissionTargetBridge;
        private readonly IActivityEntryMovementBindingRuntimeBridge _movementBindingBridge;
        private readonly IActivityEntryCameraBindingRuntimeBridge _cameraBindingBridge;
        private readonly ActorPresentationPlanResolver _actorPresentationPlanResolver;
        private readonly IActorPresentationMaterializationAdapter _actorPresentationMaterializationAdapter;
        private readonly IPlayerInputBindingAdapter _playerInputBindingAdapter;
        private readonly ActivitySetupInventoryBuilder _activitySetupInventoryBuilder;
        private readonly ActivitySetupInventoryValidator _activitySetupInventoryValidator;
        private readonly ActivityCapabilityInventoryCoordinator _activityCapabilityInventoryCoordinator;
        private PendingContentLoadContext _pendingContentLoadContext;

        public ActivityEntryPipeline(
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryObjectSetupRuntimeBridge objectSetupBridge,
            IActivityEntryActorInventoryRuntimeBridge actorInventoryBridge,
            IActivityEntryActorPresentationRuntimeBridge actorPresentationBridge,
            IActivityEntryActorAttributeRuntimeBridge actorAttributeBridge,
            IActivityEntryActorParticipationRuntimeBridge actorParticipationBridge,
            IActivityEntryPermissionTargetRuntimeBridge permissionTargetBridge,
            IActivityEntryMovementBindingRuntimeBridge movementBindingBridge,
            IActivityEntryCameraBindingRuntimeBridge cameraBindingBridge)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _runtimeBridge = endpoint;
            _identityBridge = endpoint;
            _factBridge = endpoint;
            _contentLoadedSetBridge = endpoint;
            _contentPendingOperationBridge = endpoint;
            _logBridge = endpoint;
            _preparationBridge = endpoint;
            if (endpoint is not IActivityEntryParticipantBindingRuntimeBridge participantBindingBridge)
            {
                throw new ArgumentException("ActivityEntry runtime bridge must expose participant binding runtime operations.", nameof(endpoint));
            }

            _participantBindingBridge = participantBindingBridge;
            _objectSetupBridge = objectSetupBridge ?? throw new ArgumentNullException(nameof(objectSetupBridge));
            _actorInventoryBridge = actorInventoryBridge ?? throw new ArgumentNullException(nameof(actorInventoryBridge));
            _actorPresentationBridge = actorPresentationBridge ?? throw new ArgumentNullException(nameof(actorPresentationBridge));
            _actorAttributeBridge = actorAttributeBridge ?? throw new ArgumentNullException(nameof(actorAttributeBridge));
            _actorParticipationBridge = actorParticipationBridge ?? throw new ArgumentNullException(nameof(actorParticipationBridge));
            _permissionTargetBridge = permissionTargetBridge ?? throw new ArgumentNullException(nameof(permissionTargetBridge));
            _movementBindingBridge = movementBindingBridge ?? throw new ArgumentNullException(nameof(movementBindingBridge));
            _cameraBindingBridge = cameraBindingBridge ?? throw new ArgumentNullException(nameof(cameraBindingBridge));
            _actorPresentationPlanResolver = new ActorPresentationPlanResolver();
            _actorPresentationMaterializationAdapter = new UnityActorPresentationMaterializationAdapter();
            _playerInputBindingAdapter = new PlayerInputBindingAdapter();
            _activitySetupInventoryBuilder = new ActivitySetupInventoryBuilder();
            _activitySetupInventoryValidator = new ActivitySetupInventoryValidator();
            _activityCapabilityInventoryCoordinator = new ActivityCapabilityInventoryCoordinator();
        }

        public ActivityEntryPreparationResult PrepareEntry(ActivityEntryCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid.");
            }

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryPipelineStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' bridgeReduction='runtime_bridge_domain_split' stageBridgeSplit='content_object_actor_inventory' runtimeStateStoreSplit='content_preparation_store_sources' objectActorStoreSourceSplit='stage_owned_store_sources' contentPendingOperationSplit='loaded_set_store_pending_operation_dispatch'");
            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryPreparationStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "block='entry_preparation'");

            _logBridge.LogPhaseBoundary("SessionActivitySetupStarted", command.Identity, command.Source, command.Reason, detail: "phase='setup'");
            _logBridge.LogPhaseBoundary("SessionActivityMaterializationStarted", command.Identity, command.Source, command.Reason, detail: "phase='materialization'");
            _logBridge.LogPhaseBoundary("SessionActivityBindingStarted", command.Identity, command.Source, command.Reason, detail: "phase='binding'");

            _contentLoadedSetBridge.ClearCurrentActivityContentLoadedSet();
            _preparationBridge.ClearCurrentActivityObjectContributorDiscoveryResult();
            _preparationBridge.ClearCurrentActivitySetupInventory();
            _preparationBridge.ClearCurrentActorInventoryFeedResult();

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryPreparationCompleted",
                command.Identity,
                command.Source,
                command.Reason,
                "block='entry_preparation' outcome='applied' resultKind='Prepared'");
            _logBridge.LogEntryOwnerEvent(
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
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid for setup/readiness execution.");
            }

            SessionActivityDefinition definition = command.Definition;
            SessionActivityIdentity setupStartedIdentity = command.Identity;
            _logBridge.LogEntryOwnerEvent(
                "ActivityEntrySetupReadinessStarted",
                setupStartedIdentity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='setup_readiness_orchestration' phaseOwner='ActivityEntryPipeline' bridgeReduction='macro_phase_internalized'");

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

                ActivityEntryObjectSetupCommand objectSetupCommand = new(
                    setupStartedIdentity,
                    definition,
                    command.Source,
                    command.Reason);

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
                        setupStartedIdentity,
                        definition,
                        command.Source,
                        command.Reason),
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
                    facts,
                    snapshots);
                if (!capabilityObjectSetupResult.Completed || !capabilityObjectSetupResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline capability object setup failed. reason='{capabilityObjectSetupResult.Reason}' identity='{capabilityObjectSetupResult.Identity}'.");
                }

                ActivityEntryActorPresentationSetupResult actorPresentationSetupResult = ExecuteActorPresentationSetup(
                    new ActivityEntryActorPresentationSetupCommand(
                        setupStartedIdentity,
                        definition,
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
                        definition,
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!actorAttributeSetupResult.Completed || !actorAttributeSetupResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline actor attribute setup failed. reason='{actorAttributeSetupResult.Reason}' identity='{actorAttributeSetupResult.Identity}'.");
                }

                ActivityEntryActorParticipationEnterResult actorParticipationEnterResult = ExecuteActorParticipationEnter(
                    new ActivityEntryActorParticipationEnterCommand(
                        setupStartedIdentity,
                        definition,
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
                        definition,
                        BuildPlayerInputBindingReferences(participantBindingResult),
                        command.Source,
                        command.Reason),
                    facts,
                    snapshots);
                if (!playerInputBindingResult.Completed || !playerInputBindingResult.IsValid)
                {
                    throw new InvalidOperationException($"ActivityEntryPipeline player input binding failed. reason='{playerInputBindingResult.Reason}' identity='{playerInputBindingResult.Identity}'.");
                }

                ActivityEntryPermissionTargetPreparationResult permissionTargetPreparationResult = ExecutePermissionTargetPreparation(
                    new ActivityEntryPermissionTargetPreparationCommand(
                        setupStartedIdentity,
                        definition,
                        registerReceivers: true,
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
                        definition,
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
                        definition,
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
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntrySetupReadinessCompleted",
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='setup_readiness_orchestration' resultKind='Completed' next='activation_flow' macroLifecycleOwner='SessionActivityPipeline' phaseOwner='ActivityEntryPipeline' bridgeReduction='macro_phase_internalized'");
                return new ActivityEntrySetupReadinessResult(
                    ActivityEntrySetupReadinessResultKind.Completed,
                    completedIdentity,
                    "setup_readiness_completed");
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntrySetupReadinessFailed",
                    setupStartedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='setup_readiness_orchestration' resultKind='Failed' error='{exception.Message}'");
                throw;
            }
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
            _logBridge.LogPhaseBoundary("SessionActivitySetupCompleted", setupCompletedIdentity, source, reason, completed: true, detail: "phase='setup'");
            _logBridge.LogPhaseBoundary("SessionActivityBindingCompleted", setupCompletedIdentity, source, reason, completed: true, detail: "phase='binding'");
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

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity profileResolvedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentProfileResolved, entrySequence);
            _identityBridge.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActivityContentProfileResolved);

            if (definition.ActivityContentMode == ActivityContentMode.None)
            {
                _factBridge.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentProfileResolved,
                    profileResolvedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");
                _factBridge.EmitSnapshot(snapshots,
                    "activity_content_profile_resolved_none",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");

                SessionActivityIdentity skippedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadSkippedNoContent, entrySequence);
                _identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityContentLoadSkippedNoContent);
                _factBridge.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentLoadSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                _factBridge.EmitSnapshot(snapshots,
                    "activity_content_load_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                _logBridge.LogEntryOwnerEvent(
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
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentProfileResolved,
                profileResolvedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_profile_resolved_profile",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");

            SessionActivityIdentity loadStartedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadStarted, entrySequence);
            _identityBridge.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.ActivityContentLoadStarted);
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentLoadStarted,
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_load_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");
            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryContentLoadStarted",
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"profileId='{profileId}'");

            IReadOnlyList<ActivityContentSceneEntry> entries = profile.ContentScenes ?? Array.Empty<ActivityContentSceneEntry>();
            _pendingContentLoadContext = new PendingContentLoadContext(loadStartedIdentity, profileId, entries);

            ExecuteNextContentSceneLoad(definition, command.Source, command.Reason, entrySequence, facts, snapshots);
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
                throw new InvalidOperationException($"Activity '{command.Definition.ActivityId}' missing pending activity content load context on completion.");
            }

            if (!MatchesContext(command.Operation, command.ActiveIdentity, command.Definition))
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryContentLoadFailed",
                    command.ActiveIdentity,
                    command.Source,
                    command.Reason,
                    "reason='stale_or_foreign_content_load_completion'");
                throw new InvalidOperationException("stale_or_foreign_content_load_completion");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.ActiveIdentity.EntrySequence;
            SessionActivityIdentity loadedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoaded, entrySequence);
            _identityBridge.SetCurrentIdentity(loadedIdentity, SessionActivityStage.ActivityContentSceneLoaded);
            _factBridge.EmitFact(facts,
                SessionActivityFactKind.ActivityContentSceneLoaded,
                loadedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene loaded operationId='{command.Operation.OperationId}' sceneName='{command.Operation.SceneName}'.");
            _factBridge.EmitSnapshot(snapshots,
                "activity_content_scene_loaded",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene loaded operationId='{command.Operation.OperationId}' sceneName='{command.Operation.SceneName}'.");

            ActivityContentLoadedSceneRecord record = new(
                loadedIdentity,
                _pendingContentLoadContext.ContentProfileId,
                _pendingContentLoadContext.NextSceneOrdinal,
                ResolveSceneKeyForCurrentLoadedRecordOrFail(command.Operation),
                command.Operation.OperationId,
                ResolveRequirednessForCurrentLoadedSceneOrFail(),
                command.Source,
                command.Reason);
            _pendingContentLoadContext.LoadedRecords.Add(record);
            _pendingContentLoadContext.NextSceneOrdinal += 1;

            if (_pendingContentLoadContext.NextSceneOrdinal > _pendingContentLoadContext.Entries.Count)
            {
                FinalizeContentLoadedSet(definition, command.Source, command.Reason, entrySequence, facts, snapshots);
                return new ActivityEntryContentLoadResult(
                    shouldContinueEntry: true,
                    pendingOperationIssued: false,
                    reason: "content_load_completed_loaded_set_ready");
            }

            ExecuteNextContentSceneLoad(definition, command.Source, command.Reason, entrySequence, facts, snapshots);
            return new ActivityEntryContentLoadResult(
                shouldContinueEntry: false,
                pendingOperationIssued: true,
                reason: "content_load_pending_next_scene");
        }

        public void FailContentLoad(ActivityEntryContentLoadFailureCommand command, List<SessionActivityFact> facts)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryContentLoadFailureCommand is invalid.");
            }

            SessionActivityIdentity failedIdentity = _identityBridge.BuildIdentity(
                command.Definition,
                SessionActivityStage.ActivityContentLoadFailed,
                command.ActiveIdentity.EntrySequence);
            _identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
            _factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentLoadFailed,
                failedIdentity,
                command.Source,
                command.Reason,
                $"Activity content scene load failed operationId='{command.Operation.OperationId}' scene='{command.Operation.SceneName}' error='{command.Error}'.");
            _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntrySetupInfrastructureStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='setup_inventory_snapshot_contract'");

            try
            {
                ActivityContentLoadedSet loadedSet = _objectSetupBridge.GetCurrentActivityContentLoadedSet();
                ActivityEntryActorInventoryStage.ExecuteSceneDiscovery(
                    command,
                    loadedSet,
                    _identityBridge,
                    _factBridge,
                    _logBridge,
                    _actorInventoryBridge.GetActivitySceneActorRegistry(),
                    facts,
                    snapshots);

                ActivityEntryObjectContributorDiscoveryStage.Execute(
                    command,
                    loadedSet,
                    _identityBridge,
                    _factBridge,
                    _logBridge,
                    _preparationBridge,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntrySetupInventoryStage.Execute(
                    command,
                    loadedSet,
                    _activitySetupInventoryBuilder,
                    _activitySetupInventoryValidator,
                    _runtimeBridge,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntryObjectSnapshotContractValidationStage.Execute(
                    command,
                    loadedSet,
                    _objectSetupBridge.GetCurrentActivityObjectContributorDiscoveryResult(),
                    _runtimeBridge,
                    facts);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntrySetupInfrastructureCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='setup_inventory_snapshot_contract'");
                return new ActivityEntryObjectSetupResult(
                    completed: true,
                    command.Identity,
                    "setup_infrastructure_applied");
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntrySetupInfrastructureFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='setup_inventory_snapshot_contract' error='{exception.Message}'");
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryParticipantBindingStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='participant_binding'");

            try
            {
                ActivityEntryParticipantBindingResult result = ActivityEntryParticipantBindingStage.Execute(
                    command,
                    _runtimeBridge,
                    _participantBindingBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryParticipantBindingCompleted",
                    result.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='participant_binding' totalRequirements='{result.TotalRequirements}' resolved='{result.ResolvedRequirements}' skipped='{result.SkippedRequirements}' required='{result.RequiredRequirements}' requiredResolved='{result.RequiredResolvedRequirements}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryParticipantBindingFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='participant_binding' error='{exception.Message}'");
                throw;
            }
        }

        public ActivityEntryObjectSetupResult ExecuteCapabilityObjectSetup(
            ActivityEntryObjectSetupCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryCapabilityObjectSetupStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='capability_inventory_object_state'");

            try
            {
                ActivityObjectContributorDiscoveryResult discoveryResult = _objectSetupBridge.GetCurrentActivityObjectContributorDiscoveryResult();
                ActorInventoryFeedResult actorInventoryFeed = ActivityEntryActorInventoryStage.ExecuteActorInventoryFeed(
                    command,
                    _logBridge,
                    _actorInventoryBridge);
                IReadOnlyList<ActorScanTarget> actorTargets = actorInventoryFeed.BuildScanTargets(command.Source);

                ActivityCapabilityInventoryBuildResult buildResult = ActivityEntryCapabilityInventoryPreviewStage.Execute(
                    command,
                    discoveryResult,
                    actorTargets,
                    _activityCapabilityInventoryCoordinator,
                    _runtimeBridge,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntryObjectResetStage.Execute(
                    command,
                    discoveryResult,
                    buildResult.Inventory,
                    buildResult.Validation,
                    _runtimeBridge,
                    facts,
                    snapshots);

                ActivityEntryObjectSnapshotRestoreStage.Execute(
                    command,
                    discoveryResult,
                    buildResult.Inventory,
                    buildResult.Validation,
                    _runtimeBridge,
                    facts);

                _logBridge.LogEntryOwnerEvent(
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
                _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorPresentationSetupStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_presentation_setup'");

            try
            {
                ActivityEntryActorPresentationSetupResult result = ActivityEntryActorPresentationStage.Execute(
                    command,
                    _runtimeBridge,
                    _actorPresentationBridge,
                    _actorPresentationPlanResolver,
                    _actorPresentationMaterializationAdapter,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorPresentationSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_presentation_setup' total='{result.Total}' resolved='{result.Resolved}' materialized='{result.Materialized}' retained='{result.Retained}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorAttributeSetupStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_attribute_setup'");

            try
            {
                ActivityEntryActorAttributeSetupResult result = ActivityEntryActorAttributeStage.Execute(
                    command,
                    _runtimeBridge,
                    _actorAttributeBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_setup' total='{result.Total}' resolved='{result.Resolved}' ready='{result.Ready}' skipped='{result.Skipped}' failed='{result.Failed}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorAttributeSetupFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_attribute_setup' error='{exception.Message}'");
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorParticipationEnterStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_participation_enter'");

            try
            {
                ActivityEntryActorParticipationEnterResult result = ActivityEntryActorParticipationStage.ExecuteEnter(
                    command,
                    _runtimeBridge,
                    _actorInventoryBridge,
                    _actorParticipationBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorParticipationEnterCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_participation_enter' total='{result.Total}' entered='{result.Entered}' skipped='{result.Skipped}' failed='{result.Failed}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryPlayerInputBindingStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='player_input_binding'");

            try
            {
                ActivityEntryPlayerInputBindingResult result = ActivityEntryPlayerInputBindingStage.Execute(
                    command,
                    _runtimeBridge,
                    _playerInputBindingAdapter,
                    _actorInventoryBridge.GetActivityPlayerActorRegistry(),
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryPlayerInputBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='player_input_binding' required='{result.RequiredCount}' requiredBound='{result.RequiredBoundCount}' totalBound='{result.TotalBoundCount}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryPermissionTargetPreparationStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='permission_target_preparation'");

            try
            {
                ActivityEntryPermissionTargetPreparationResult result = ActivityEntryPermissionTargetPreparationStage.Execute(
                    command,
                    _runtimeBridge,
                    _permissionTargetBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryPermissionTargetPreparationCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='permission_target_preparation' receivers='{result.ReceiverCount}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryPermissionTargetPreparationFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='permission_target_preparation' error='{exception.Message}'");
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryMovementBindingStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='movement_binding'");

            try
            {
                ActivityEntryMovementBindingResult result = ActivityEntryMovementBindingStage.Execute(
                    command,
                    _runtimeBridge,
                    _movementBindingBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryMovementBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='movement_binding' required='{result.RequiredCount}' requiredBound='{result.RequiredBoundCount}' totalBound='{result.TotalBoundCount}' retained='{result.RetainedCount}' skipped='{result.Skipped}' retainedExisting='{result.RetainedExistingBinding}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
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

            _logBridge.LogEntryOwnerEvent(
                "ActivityEntryCameraBindingStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='camera_binding'");

            try
            {
                ActivityEntryCameraBindingResult result = ActivityEntryCameraBindingStage.Execute(
                    command,
                    _runtimeBridge,
                    _cameraBindingBridge,
                    facts,
                    snapshots);

                _logBridge.LogEntryOwnerEvent(
                    "ActivityEntryCameraBindingCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='camera_binding' required='{result.RequiredCount}' targetBound='{result.TargetBound}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _logBridge.LogEntryOwnerEvent(
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
        }

        private void ExecuteNextContentSceneLoad(
            SessionActivityDefinition definition,
            string source,
            string reason,
            int entrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' has no valid pending activity content load context.");
            }

            int sceneOrdinal = _pendingContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' next content scene ordinal is out of range. next='{sceneOrdinal}' total='{_pendingContentLoadContext.Entries.Count}'.");
            }

            ActivityContentSceneEntry entry = _pendingContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' content scene entry is null at ordinal='{sceneOrdinal}'.");
            }

            if (entry.Requiredness == ActivityContentRequiredness.Required &&
                (entry.SceneKey == null || string.IsNullOrWhiteSpace(entry.SceneKey.SceneName)))
            {
                SessionActivityIdentity failedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadFailed, failedIdentity, source, reason, $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _factBridge.EmitSnapshot(snapshots, "activity_content_load_failed_required_scene_invalid", source, reason, $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _logBridge.LogEntryOwnerEvent("ActivityEntryContentLoadFailed", failedIdentity, source, reason, "reason='required_scene_invalid'");
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' required content scene at ordinal='{sceneOrdinal}' is invalid.");
            }

            if (entry.SceneKey == null || string.IsNullOrWhiteSpace(entry.SceneKey.SceneName))
            {
                SessionActivityIdentity rejectedIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _identityBridge.SetCurrentIdentity(rejectedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadRejected, rejectedIdentity, source, reason, $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
                _factBridge.EmitSnapshot(snapshots, "activity_content_scene_load_rejected", source, reason, $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
                _pendingContentLoadContext.NextSceneOrdinal += 1;
                if (_pendingContentLoadContext.NextSceneOrdinal > _pendingContentLoadContext.Entries.Count)
                {
                    FinalizeContentLoadedSet(definition, source, reason, entrySequence, facts, snapshots);
                }
                else
                {
                    ExecuteNextContentSceneLoad(definition, source, reason, entrySequence, facts, snapshots);
                }

                return;
            }

            SessionActivityIdentity loadingIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoading, entrySequence);
            _identityBridge.SetCurrentIdentity(loadingIdentity, SessionActivityStage.ActivityContentSceneLoading);

            ActivityContentSceneLoadCommand loadCommand = new(
                Guid.NewGuid().ToString("N"),
                loadingIdentity,
                _pendingContentLoadContext.ContentProfileId,
                sceneOrdinal,
                entry.SceneKey,
                entry.Requiredness,
                source,
                reason);

            SessionActivityPendingOperation pendingOperation = _contentPendingOperationBridge.BuildActivityContentPendingOperation(definition, entrySequence, loadCommand);
            _contentPendingOperationBridge.SetPendingOperation(pendingOperation);
            _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadCommandIssued, loadingIdentity, source, reason, $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' contentProfileId='{loadCommand.ContentProfileId}' sceneOrdinal='{loadCommand.SceneOrdinal}' sceneKey='{loadCommand.SceneKey.name}' sceneName='{loadCommand.SceneName}' requiredness='{loadCommand.Requiredness}'.");
            _factBridge.EmitSnapshot(snapshots, "activity_content_scene_load_command_issued", source, reason, $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' sceneName='{loadCommand.SceneName}'.");
            _contentPendingOperationBridge.RunActivityContentOperation(pendingOperation, loadCommand);
        }

        private void FinalizeContentLoadedSet(
            SessionActivityDefinition definition,
            string source,
            string reason,
            int entrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' missing pending activity content load context to finalize loaded set.");
            }

            string profileId = _pendingContentLoadContext.ContentProfileId;
            int loadedSceneCount = _pendingContentLoadContext.LoadedRecords.Count;
            SessionActivityIdentity readyIdentity = _identityBridge.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadedSetReady, entrySequence);
            _identityBridge.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivityContentLoadedSetReady);
            ActivityContentLoadedSet loadedSet = new(
                readyIdentity,
                profileId,
                _pendingContentLoadContext.LoadedRecords,
                source,
                reason);
            if (!loadedSet.IsValid)
            {
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' produced invalid ActivityContentLoadedSet.");
            }

            _contentLoadedSetBridge.SetCurrentActivityContentLoadedSet(loadedSet);
            _factBridge.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadedSetReady, readyIdentity, source, reason, $"'{definition.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _factBridge.EmitSnapshot(snapshots, "activity_content_loaded_set_ready", source, reason, $"'{definition.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _logBridge.LogEntryOwnerEvent(
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
            SessionActivityDefinition definition)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                return false;
            }

            return string.Equals(operation.PipelineId, _identityBridge.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(operation.SessionStateId, _identityBridge.SessionId, StringComparison.Ordinal) &&
                   string.Equals(operation.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
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
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Pending activity content scene ordinal '{sceneOrdinal}' is out of range while resolving requiredness.");
            }

            ActivityContentSceneEntry entry = _pendingContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null || entry.Requiredness == ActivityContentRequiredness.Unknown)
            {
                throw new InvalidOperationException($"Pending activity content scene requiredness is invalid at ordinal='{sceneOrdinal}'.");
            }

            return entry.Requiredness;
        }

        private Foundation.Platform.SceneReferences.SceneKeyAsset ResolveSceneKeyForCurrentLoadedRecordOrFail(SessionActivityPendingOperation operation)
        {
            if (_pendingContentLoadContext == null || !_pendingContentLoadContext.IsValid)
            {
                throw new InvalidOperationException("Pending activity content load context is invalid while resolving scene key.");
            }

            int sceneOrdinal = _pendingContentLoadContext.NextSceneOrdinal;
            if (sceneOrdinal <= 0 || sceneOrdinal > _pendingContentLoadContext.Entries.Count)
            {
                throw new InvalidOperationException($"Pending activity content scene ordinal '{sceneOrdinal}' is out of range while resolving scene key.");
            }

            ActivityContentSceneEntry entry = _pendingContentLoadContext.Entries[sceneOrdinal - 1];
            if (entry == null || entry.SceneKey == null)
            {
                throw new InvalidOperationException($"Pending activity content scene key is missing at ordinal='{sceneOrdinal}' operationId='{operation.OperationId}'.");
            }

            return entry.SceneKey;
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
