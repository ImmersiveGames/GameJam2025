using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages;
using UnityEngine;

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

        private readonly IActivityEntryRuntimeEndpoint _endpoint;
        private readonly IActivityEntryObjectSetupRuntimeBridge _objectSetupBridge;
        private readonly IActivityEntryActorInventoryRuntimeBridge _actorInventoryBridge;
        private readonly IActivityEntryActorPresentationRuntimeBridge _actorPresentationBridge;
        private readonly ActorPresentationPlanResolver _actorPresentationPlanResolver;
        private readonly IActorPresentationMaterializationAdapter _actorPresentationMaterializationAdapter;
        private readonly ActivitySetupInventoryBuilder _activitySetupInventoryBuilder;
        private readonly ActivitySetupInventoryValidator _activitySetupInventoryValidator;
        private readonly ActivityCapabilityInventoryCoordinator _activityCapabilityInventoryCoordinator;
        private PendingContentLoadContext _pendingContentLoadContext;

        public ActivityEntryPipeline(
            IActivityEntryRuntimeEndpoint endpoint,
            IActivityEntryObjectSetupRuntimeBridge objectSetupBridge,
            IActivityEntryActorInventoryRuntimeBridge actorInventoryBridge,
            IActivityEntryActorPresentationRuntimeBridge actorPresentationBridge)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _objectSetupBridge = objectSetupBridge ?? throw new ArgumentNullException(nameof(objectSetupBridge));
            _actorInventoryBridge = actorInventoryBridge ?? throw new ArgumentNullException(nameof(actorInventoryBridge));
            _actorPresentationBridge = actorPresentationBridge ?? throw new ArgumentNullException(nameof(actorPresentationBridge));
            _actorPresentationPlanResolver = new ActorPresentationPlanResolver();
            _actorPresentationMaterializationAdapter = new UnityActorPresentationMaterializationAdapter();
            _activitySetupInventoryBuilder = new ActivitySetupInventoryBuilder();
            _activitySetupInventoryValidator = new ActivitySetupInventoryValidator();
            _activityCapabilityInventoryCoordinator = new ActivityCapabilityInventoryCoordinator();
        }

        public Task<ActivityEntryResult> ExecuteAsync(ActivityEntryCommand command, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<ActivityEntryResult>(cancellationToken);
            }

            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid.");
            }

            _endpoint.LogEntryOwnerEvent(
                "ActivityEntryPipelineStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline'");
            _endpoint.LogEntryOwnerEvent(
                "ActivityEntryPreparationStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "block='entry_preparation'");

            _endpoint.LogPhaseBoundary("SessionActivitySetupStarted", command.Identity, command.Source, command.Reason, detail: "phase='setup'");
            _endpoint.LogPhaseBoundary("SessionActivityMaterializationStarted", command.Identity, command.Source, command.Reason, detail: "phase='materialization'");
            _endpoint.LogPhaseBoundary("SessionActivityBindingStarted", command.Identity, command.Source, command.Reason, detail: "phase='binding'");

            _endpoint.ClearCurrentActivityContentLoadedSet();
            _endpoint.ClearCurrentActivityObjectContributorDiscoveryResult();
            _endpoint.ClearCurrentActivitySetupInventory();
            _endpoint.ClearCurrentActorInventoryFeedResult();

            _endpoint.LogEntryOwnerEvent(
                "ActivityEntryPreparationCompleted",
                command.Identity,
                command.Source,
                command.Reason,
                "block='entry_preparation' outcome='applied'");
            _endpoint.LogEntryOwnerEvent(
                "ActivityEntryPreparationAccepted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' outcome='accepted' note='pipeline_not_completed_yet'");

            ActivityEntryResult result = new(
                accepted: true,
                command.Identity,
                "entry_preparation_applied");
            return Task.FromResult(result);
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
            SessionActivityIdentity profileResolvedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentProfileResolved, entrySequence);
            _endpoint.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActivityContentProfileResolved);

            if (definition.ActivityContentMode == ActivityContentMode.None)
            {
                _endpoint.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentProfileResolved,
                    profileResolvedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");
                _endpoint.EmitSnapshot(snapshots,
                    "activity_content_profile_resolved_none",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content profile resolved mode='None'.");

                SessionActivityIdentity skippedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadSkippedNoContent, entrySequence);
                _endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivityContentLoadSkippedNoContent);
                _endpoint.EmitFact(facts,
                    SessionActivityFactKind.ActivityContentLoadSkippedNoContent,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                _endpoint.EmitSnapshot(snapshots,
                    "activity_content_load_skipped_no_content",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' activity content load skipped as no-content.");
                _endpoint.LogEntryOwnerEvent(
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
            _endpoint.EmitFact(facts,
                SessionActivityFactKind.ActivityContentProfileResolved,
                profileResolvedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");
            _endpoint.EmitSnapshot(snapshots,
                "activity_content_profile_resolved_profile",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content profile resolved mode='Profile' profileId='{profileId}'.");

            SessionActivityIdentity loadStartedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadStarted, entrySequence);
            _endpoint.SetCurrentIdentity(loadStartedIdentity, SessionActivityStage.ActivityContentLoadStarted);
            _endpoint.EmitFact(facts,
                SessionActivityFactKind.ActivityContentLoadStarted,
                loadStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");
            _endpoint.EmitSnapshot(snapshots,
                "activity_content_load_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content load started profileId='{profileId}'.");
            _endpoint.LogEntryOwnerEvent(
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
                _endpoint.LogEntryOwnerEvent(
                    "ActivityEntryContentLoadFailed",
                    command.ActiveIdentity,
                    command.Source,
                    command.Reason,
                    "reason='stale_or_foreign_content_load_completion'");
                throw new InvalidOperationException("stale_or_foreign_content_load_completion");
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.ActiveIdentity.EntrySequence;
            SessionActivityIdentity loadedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoaded, entrySequence);
            _endpoint.SetCurrentIdentity(loadedIdentity, SessionActivityStage.ActivityContentSceneLoaded);
            _endpoint.EmitFact(facts,
                SessionActivityFactKind.ActivityContentSceneLoaded,
                loadedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene loaded operationId='{command.Operation.OperationId}' sceneName='{command.Operation.SceneName}'.");
            _endpoint.EmitSnapshot(snapshots,
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

            SessionActivityIdentity failedIdentity = _endpoint.BuildIdentity(
                command.Definition,
                SessionActivityStage.ActivityContentLoadFailed,
                command.ActiveIdentity.EntrySequence);
            _endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
            _endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentLoadFailed,
                failedIdentity,
                command.Source,
                command.Reason,
                $"Activity content scene load failed operationId='{command.Operation.OperationId}' scene='{command.Operation.SceneName}' error='{command.Error}'.");
            _endpoint.LogEntryOwnerEvent(
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

            _endpoint.LogEntryOwnerEvent(
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
                    _endpoint,
                    _actorInventoryBridge.GetActivitySceneActorRegistry(),
                    facts,
                    snapshots);

                ActivityEntryObjectContributorDiscoveryStage.Execute(
                    command,
                    loadedSet,
                    _endpoint,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntrySetupInventoryStage.Execute(
                    command,
                    loadedSet,
                    _activitySetupInventoryBuilder,
                    _activitySetupInventoryValidator,
                    _endpoint,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntryObjectSnapshotContractValidationStage.Execute(
                    command,
                    loadedSet,
                    _objectSetupBridge.GetCurrentActivityObjectContributorDiscoveryResult(),
                    _endpoint,
                    facts);

                _endpoint.LogEntryOwnerEvent(
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
                _endpoint.LogEntryOwnerEvent(
                    "ActivityEntrySetupInfrastructureFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='setup_inventory_snapshot_contract' error='{exception.Message}'");
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

            _endpoint.LogEntryOwnerEvent(
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
                    _endpoint,
                    _actorInventoryBridge);
                IReadOnlyList<ActorScanTarget> actorTargets = actorInventoryFeed.BuildScanTargets(command.Source);

                ActivityCapabilityInventoryBuildResult buildResult = ActivityEntryCapabilityInventoryPreviewStage.Execute(
                    command,
                    discoveryResult,
                    actorTargets,
                    _activityCapabilityInventoryCoordinator,
                    _endpoint,
                    _objectSetupBridge,
                    facts,
                    snapshots);

                ActivityEntryObjectResetStage.Execute(
                    command,
                    discoveryResult,
                    buildResult.Inventory,
                    buildResult.Validation,
                    _endpoint,
                    facts,
                    snapshots);

                ActivityEntryObjectSnapshotRestoreStage.Execute(
                    command,
                    discoveryResult,
                    buildResult.Inventory,
                    buildResult.Validation,
                    _endpoint,
                    facts);

                _endpoint.LogEntryOwnerEvent(
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
                _endpoint.LogEntryOwnerEvent(
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

            _endpoint.LogEntryOwnerEvent(
                "ActivityEntryActorPresentationSetupStarted",
                command.Identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_presentation_setup'");

            try
            {
                ActivityEntryActorPresentationSetupResult result = ActivityEntryActorPresentationStage.Execute(
                    command,
                    _endpoint,
                    _actorPresentationBridge,
                    _actorPresentationPlanResolver,
                    _actorPresentationMaterializationAdapter,
                    facts,
                    snapshots);

                _endpoint.LogEntryOwnerEvent(
                    "ActivityEntryActorPresentationSetupCompleted",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_presentation_setup' total='{result.Total}' resolved='{result.Resolved}' materialized='{result.Materialized}' retained='{result.Retained}' skipped='{result.Skipped}'");
                return result;
            }
            catch (Exception exception)
            {
                _endpoint.LogEntryOwnerEvent(
                    "ActivityEntryActorPresentationSetupFailed",
                    command.Identity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_presentation_setup' error='{exception.Message}'");
                throw;
            }
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
                SessionActivityIdentity failedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _endpoint.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadFailed, failedIdentity, source, reason, $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _endpoint.EmitSnapshot(snapshots, "activity_content_load_failed_required_scene_invalid", source, reason, $"'{definition.ActivityId}' required activity content scene is invalid at ordinal='{sceneOrdinal}'.");
                _endpoint.LogEntryOwnerEvent("ActivityEntryContentLoadFailed", failedIdentity, source, reason, "reason='required_scene_invalid'");
                throw new InvalidOperationException($"Activity '{definition.ActivityId}' required content scene at ordinal='{sceneOrdinal}' is invalid.");
            }

            if (entry.SceneKey == null || string.IsNullOrWhiteSpace(entry.SceneKey.SceneName))
            {
                SessionActivityIdentity rejectedIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadFailed, entrySequence);
                _endpoint.SetCurrentIdentity(rejectedIdentity, SessionActivityStage.ActivityContentLoadFailed);
                _endpoint.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadRejected, rejectedIdentity, source, reason, $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
                _endpoint.EmitSnapshot(snapshots, "activity_content_scene_load_rejected", source, reason, $"'{definition.ActivityId}' activity content scene rejected at ordinal='{sceneOrdinal}' requiredness='{entry.Requiredness}'.");
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

            SessionActivityIdentity loadingIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentSceneLoading, entrySequence);
            _endpoint.SetCurrentIdentity(loadingIdentity, SessionActivityStage.ActivityContentSceneLoading);

            ActivityContentSceneLoadCommand loadCommand = new(
                Guid.NewGuid().ToString("N"),
                loadingIdentity,
                _pendingContentLoadContext.ContentProfileId,
                sceneOrdinal,
                entry.SceneKey,
                entry.Requiredness,
                source,
                reason);

            SessionActivityPendingOperation pendingOperation = _endpoint.BuildActivityContentPendingOperation(definition, entrySequence, loadCommand);
            _endpoint.SetPendingOperation(pendingOperation);
            _endpoint.EmitFact(facts, SessionActivityFactKind.ActivityContentSceneLoadCommandIssued, loadingIdentity, source, reason, $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' contentProfileId='{loadCommand.ContentProfileId}' sceneOrdinal='{loadCommand.SceneOrdinal}' sceneKey='{loadCommand.SceneKey.name}' sceneName='{loadCommand.SceneName}' requiredness='{loadCommand.Requiredness}'.");
            _endpoint.EmitSnapshot(snapshots, "activity_content_scene_load_command_issued", source, reason, $"'{definition.ActivityId}' activity content scene load command issued operationId='{loadCommand.OperationId}' sceneName='{loadCommand.SceneName}'.");
            _endpoint.RunActivityContentOperation(pendingOperation, loadCommand);
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
            SessionActivityIdentity readyIdentity = _endpoint.BuildIdentity(definition, SessionActivityStage.ActivityContentLoadedSetReady, entrySequence);
            _endpoint.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActivityContentLoadedSetReady);
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

            _endpoint.SetCurrentActivityContentLoadedSet(loadedSet);
            _endpoint.EmitFact(facts, SessionActivityFactKind.ActivityContentLoadedSetReady, readyIdentity, source, reason, $"'{definition.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _endpoint.EmitSnapshot(snapshots, "activity_content_loaded_set_ready", source, reason, $"'{definition.ActivityId}' activity content loaded set ready profileId='{profileId}' loadedScenes='{loadedSet.Scenes.Count}'.");
            _endpoint.LogEntryOwnerEvent(
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

            return string.Equals(operation.PipelineId, _endpoint.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(operation.SessionStateId, _endpoint.SessionId, StringComparison.Ordinal) &&
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
