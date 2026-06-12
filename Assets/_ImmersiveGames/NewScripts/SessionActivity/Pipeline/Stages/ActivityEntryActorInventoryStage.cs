using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using static _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages.ActivityEntryObjectSetupStageUtility;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorInventoryStage
    {
        public static ActorSceneDiscoveryStageResult ExecuteSceneDiscovery(
            ActivityEntryObjectSetupCommand command,
            ActivityContentLoadedSet loadedSet,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            ActivityEntryLogSink logSink,
            ActivitySceneActorRegistry sceneActorRegistry,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            if (sceneActorRegistry == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires a scene actor registry.");
            }

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActorSceneDiscoveryStarted);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorSceneDiscoveryStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActorSceneDiscoveryStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' actor scene discovery started owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
            factBridge.EmitSnapshot(
                snapshots,
                "actor_scene_discovery_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' actor scene discovery started owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
            logSink.LogEntryOwnerEvent(
                "ActivityEntryActorSceneDiscoveryStarted",
                startedIdentity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_scene_discovery'");

            try
            {
                bool canDiscoverFromLoadedSet = HasLoadedSetForCurrentEntry(loadedSet, command.Identity, entrySequence) && loadedSet.HasScenes;
                var discovery = ActorSceneDiscoveryStage.Execute(
                    command.ActivityId,
                    startedIdentity,
                    loadedSet,
                    canDiscoverFromLoadedSet,
                    sceneActorRegistry);

                if (!discovery.HasAuthorizedSource)
                {
                    var skippedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActorSceneDiscoverySkipped);
                    identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorSceneDiscoverySkipped);
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.ActorSceneDiscoverySkipped,
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' actor scene discovery skipped reason='no_authorized_source' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                    factBridge.EmitSnapshot(
                        snapshots,
                        "actor_scene_discovery_skipped",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' actor scene discovery skipped reason='no_authorized_source' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                    logSink.LogEntryOwnerEvent(
                        "ActivityEntryActorSceneDiscoverySkipped",
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        "owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_scene_discovery' reason='no_authorized_source'");
                }

                var completedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActorSceneDiscoveryCompleted);
                identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorSceneDiscoveryCompleted);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorSceneDiscoveryCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' actor scene discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "actor_scene_discovery_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' actor scene discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorSceneDiscoveryCompleted",
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_scene_discovery' discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}'");

                return discovery;
            }
            catch (Exception exception)
            {
                var failedIdentity = BuildIdentityFromCommandIdentity(command.Identity, SessionActivityStage.ActorSceneDiscoveryFailed);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorSceneDiscoveryFailed);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorSceneDiscoveryFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' actor scene discovery failed reason='{exception.Message}' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "actor_scene_discovery_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' actor scene discovery failed reason='{exception.Message}' owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline'.");
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorSceneDiscoveryFailed",
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_scene_discovery' error='{exception.Message}'");
                throw;
            }
        }

        public static ActorInventoryFeedResult ExecuteActorInventoryFeed(
            ActivityEntryObjectSetupCommand command,
            ActivityEntryLogSink logSink,
            ActivityParticipationContext participationContext,
            ActivitySceneActorRegistry sceneActorRegistry,
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            ActivityEntryInventoryRuntimeState inventoryState)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            if (sceneActorRegistry == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires scene actor registry.");
            }

            if (playerActorRegistry == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires player actor registry.");
            }

            if (sessionActorRuntimeStore == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires session actor runtime store.");
            }

            var identity = command.Identity;
            logSink.LogEntryOwnerEvent(
                "ActivityEntryActorInventoryFeedStarted",
                identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_inventory_feed'");

            IReadOnlyList<PlayerActorIdentityRecord> playerActors = ResolvePlayerActorCapabilityTargetsForCurrentEntry(
                participationContext,
                playerActorRegistry,
                sessionActorRuntimeStore,
                identity);
            IReadOnlyList<SceneAuthoredActorRuntimeEntry> sceneActors = ResolveActiveSceneActors(sceneActorRegistry, identity);
            IActivityActorInstanceSource[] actorSources =
            {
                new PlayerActorInstanceSource(playerActors ?? Array.Empty<PlayerActorIdentityRecord>(), playerActorRegistry, sessionActorRuntimeStore),
                new SceneAuthoredActorInstanceSource(sceneActors),
            };

            ActorInventoryFeed feed = new();
            var result = feed.BuildFromSources(identity, actorSources, command.Source, command.Reason);
            if (!result.IsValid)
            {
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryActorInventoryFeedFailed",
                    identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_inventory_feed' reason='invalid_feed_result'");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActorInventoryFeed] Invalid feed result activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}'.");
            }

            inventoryState.SetCurrentActorInventoryFeedResult(result);
            logSink.LogEntryOwnerEvent(
                "ActivityEntryActorInventoryFeedCompleted",
                identity,
                command.Source,
                command.Reason,
                $"owner='ActivityEntryActorInventoryStage' entryPipelineOwner='ActivityEntryPipeline' block='actor_inventory_feed' actorInstances='{result.ActorInstances.Count}' actorEntries='{result.ActorEntries.Count}' actorParticipations='{result.ActorParticipations.Count}'");
            return result;
        }

        internal static IReadOnlyList<PlayerActorIdentityRecord> ResolvePlayerActorCapabilityTargetsForCurrentEntry(
            ActivityParticipationContext participationContext,
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            SessionActivityIdentity identity)
        {
            List<PlayerActorIdentityRecord> resolved = new();
            HashSet<SessionParticipantId> resolvedParticipantIds = new();

            if (participationContext is { IsValid: true, Participants: { Count: > 0 } })
            {
                AddPlayerActorCapabilityTargetsFromParticipationContext(
                    playerActorRegistry,
                    sessionActorRuntimeStore,
                    identity,
                    participationContext.Participants,
                    resolved,
                    resolvedParticipantIds);
            }

            if (resolved.Count > 0)
            {
                return resolved;
            }

            if (playerActorRegistry != null &&
                playerActorRegistry.TryGetIndexedActiveActorIdentities(out IReadOnlyList<PlayerActorIdentityRecord> activeActors) &&
                activeActors is { Count: > 0 } &&
                ActivityActorScopeCompatibilityPolicy.IsScopeCompatible(
                    playerActorRegistry.ActiveScopeIdentity,
                    identity,
                    ActorScope.ActivityScoped))
            {
                return activeActors;
            }

            return ResolveRouteRetainedPlayerActorIdentitiesOrEmpty(identity, playerActorRegistry);
        }

        private static void AddPlayerActorCapabilityTargetsFromParticipationContext(
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            SessionActivityIdentity identity,
            IReadOnlyList<ActivityParticipantBinding> participants,
            List<PlayerActorIdentityRecord> resolved,
            HashSet<SessionParticipantId> resolvedParticipantIds)
        {
            if (!identity.IsValid || playerActorRegistry == null || sessionActorRuntimeStore == null || participants == null || resolved == null || resolvedParticipantIds == null)
            {
                return;
            }

            for (int index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (!participant.IsValid || !participant.RequiresPlayerActor || !participant.ParticipantId.IsValid)
                {
                    continue;
                }

                if (!TryResolvePlayerActorHandleForCapabilityInventory(playerActorRegistry, sessionActorRuntimeStore, identity, participant, out var handle) || !handle.IsValid)
                {
                    continue;
                }

                if (!resolvedParticipantIds.Add(participant.ParticipantId))
                {
                    continue;
                }

                resolved.Add(handle.ActorIdentity);
            }
        }

        private static bool TryResolvePlayerActorHandleForCapabilityInventory(
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            SessionActivityIdentity identity,
            ActivityParticipantBinding participant,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!identity.IsValid || !participant.IsValid || !participant.RequiresPlayerActor)
            {
                return false;
            }

            if (playerActorRegistry != null &&
                ActivityActorScopeCompatibilityPolicy.IsScopeCompatible(
                    playerActorRegistry.ActiveScopeIdentity,
                    identity,
                    ActorScope.ActivityScoped) &&
                (playerActorRegistry.TryGetActiveHandleByParticipant(participant.ParticipantId, out handle) ||
                    playerActorRegistry.TryGetRouteScopedHandleByParticipant(participant.ParticipantId, out handle)) &&
                handle.IsValid)
            {
                return true;
            }

            if (sessionActorRuntimeStore != null &&
                sessionActorRuntimeStore.TryGetByParticipantId(identity, participant.ParticipantId, out var entry) &&
                entry.IsValid)
            {
                PlayerActorIdentityRecord actorIdentity = new(identity, participant, PlayerActorIdentityRecord.BuildPlayerActorId(identity, participant.ActorId));
                handle = new PlayerActorRuntimeHandle(actorIdentity, entry.Instance, entry.Actor);
                return handle.IsValid;
            }

            return false;
        }

        private static IReadOnlyList<PlayerActorIdentityRecord> ResolveRouteRetainedPlayerActorIdentitiesOrEmpty(
            SessionActivityIdentity identity,
            ActivityPlayerActorRegistry playerActorRegistry)
        {
            if (playerActorRegistry == null)
            {
                return Array.Empty<PlayerActorIdentityRecord>();
            }

            IReadOnlyList<PlayerActorIdentityRecord> retained = playerActorRegistry.GetIndexedRouteScopedActorIdentitiesForSession(identity);
            return retained == null || retained.Count == 0
                ? Array.Empty<PlayerActorIdentityRecord>()
                : FilterRetainedPlayerActorIdentities(identity, playerActorRegistry, retained);
        }

        private static IReadOnlyList<PlayerActorIdentityRecord> FilterRetainedPlayerActorIdentities(
            SessionActivityIdentity identity,
            ActivityPlayerActorRegistry playerActorRegistry,
            IReadOnlyList<PlayerActorIdentityRecord> retained)
        {
            List<PlayerActorIdentityRecord> resolved = new();
            for (int index = 0; index < retained.Count; index++)
            {
                var candidate = retained[index];
                if (!candidate.IsValid)
                {
                    continue;
                }

                if ((!playerActorRegistry.TryGetActiveHandleByParticipant(candidate.ParticipantId, out var handle) ||
                    !handle.IsValid) &&
                    (!playerActorRegistry.TryGetRouteScopedHandleByParticipant(candidate.ParticipantId, out handle) ||
                     !handle.IsValid))
                {
                    continue;
                }

                if (handle.PlayerSlotId != candidate.PlayerSlotId || handle.ParticipantId != candidate.ParticipantId)
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

        private static IReadOnlyList<SceneAuthoredActorRuntimeEntry> ResolveActiveSceneActors(
            ActivitySceneActorRegistry sceneActorRegistry,
            SessionActivityIdentity identity)
        {
            try
            {
                return sceneActorRegistry.GetActiveEntries(identity) ?? Array.Empty<SceneAuthoredActorRuntimeEntry>();
            }
            catch (InvalidOperationException)
            {
                return Array.Empty<SceneAuthoredActorRuntimeEntry>();
            }
        }

        private static bool HasLoadedSetForCurrentEntry(
            ActivityContentLoadedSet loadedSet,
            SessionActivityIdentity identity,
            int entrySequence)
        {
            return loadedSet is { IsValid: true, Identity: { IsValid: true } } &&
                string.Equals(loadedSet.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.SessionId, identity.SessionId, StringComparison.Ordinal) &&
                string.Equals(loadedSet.Identity.ActivityId, identity.ActivityId, StringComparison.Ordinal) &&
                loadedSet.Identity.EntrySequence == entrySequence;
        }
    }
}
