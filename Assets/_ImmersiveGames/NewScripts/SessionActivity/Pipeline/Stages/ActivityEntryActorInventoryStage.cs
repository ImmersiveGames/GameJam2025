using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorInventoryStage
    {
        public static ActorSceneDiscoveryStageResult ExecuteSceneDiscovery(
            ActivityEntryObjectSetupCommand command,
            ActivityContentLoadedSet loadedSet,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            IActivityEntryLogRuntimeBridge logBridge,
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

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = identityBridge.BuildIdentity(definition, SessionActivityStage.ActorSceneDiscoveryStarted, entrySequence);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorSceneDiscoveryStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActorSceneDiscoveryStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' actor scene discovery started owner='ActivityEntryPipeline'.");
            factBridge.EmitSnapshot(
                snapshots,
                "actor_scene_discovery_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' actor scene discovery started owner='ActivityEntryPipeline'.");
            logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorSceneDiscoveryStarted",
                startedIdentity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_scene_discovery'");

            try
            {
                bool canDiscoverFromLoadedSet = HasLoadedSetForCurrentEntry(loadedSet, definition, entrySequence, command.Identity) && loadedSet.HasScenes;
                ActorSceneDiscoveryStageResult discovery = ActorSceneDiscoveryStage.Execute(
                    definition,
                    startedIdentity,
                    loadedSet,
                    canDiscoverFromLoadedSet,
                    sceneActorRegistry);

                if (!discovery.HasAuthorizedSource)
                {
                    SessionActivityIdentity skippedIdentity = identityBridge.BuildIdentity(definition, SessionActivityStage.ActorSceneDiscoverySkipped, entrySequence);
                    identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorSceneDiscoverySkipped);
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.ActorSceneDiscoverySkipped,
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' actor scene discovery skipped reason='no_authorized_source' owner='ActivityEntryPipeline'.");
                    factBridge.EmitSnapshot(
                        snapshots,
                        "actor_scene_discovery_skipped",
                        command.Source,
                        command.Reason,
                        $"'{definition.ActivityId}' actor scene discovery skipped reason='no_authorized_source' owner='ActivityEntryPipeline'.");
                    logBridge.LogEntryOwnerEvent(
                        "ActivityEntryActorSceneDiscoverySkipped",
                        skippedIdentity,
                        command.Source,
                        command.Reason,
                        "owner='ActivityEntryPipeline' block='actor_scene_discovery' reason='no_authorized_source'");
                }

                SessionActivityIdentity completedIdentity = identityBridge.BuildIdentity(definition, SessionActivityStage.ActorSceneDiscoveryCompleted, entrySequence);
                identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorSceneDiscoveryCompleted);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorSceneDiscoveryCompleted,
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' actor scene discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}' owner='ActivityEntryPipeline'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "actor_scene_discovery_completed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' actor scene discovery completed discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}' owner='ActivityEntryPipeline'.");
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorSceneDiscoveryCompleted",
                    completedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_scene_discovery' discovered='{discovery.DiscoveredCount}' authorizedSource='{discovery.HasAuthorizedSource}'");

                return discovery;
            }
            catch (Exception exception)
            {
                SessionActivityIdentity failedIdentity = identityBridge.BuildIdentity(definition, SessionActivityStage.ActorSceneDiscoveryFailed, entrySequence);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorSceneDiscoveryFailed);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorSceneDiscoveryFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' actor scene discovery failed reason='{exception.Message}' owner='ActivityEntryPipeline'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "actor_scene_discovery_failed",
                    command.Source,
                    command.Reason,
                    $"'{definition.ActivityId}' actor scene discovery failed reason='{exception.Message}' owner='ActivityEntryPipeline'.");
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorSceneDiscoveryFailed",
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"owner='ActivityEntryPipeline' block='actor_scene_discovery' error='{exception.Message}'");
                throw;
            }
        }

        public static ActorInventoryFeedResult ExecuteActorInventoryFeed(
            ActivityEntryObjectSetupCommand command,
            IActivityEntryLogRuntimeBridge logBridge,
            IActivityEntryActorInventoryRuntimeBridge bridge)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryObjectSetupCommand is invalid.");
            }

            ActivitySceneActorRegistry sceneActorRegistry = bridge.GetActivitySceneActorRegistry();
            ActivityPlayerActorRegistry playerActorRegistry = bridge.GetActivityPlayerActorRegistry();
            if (sceneActorRegistry == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires scene actor registry.");
            }

            if (playerActorRegistry == null)
            {
                throw new InvalidOperationException("ActivityEntryActorInventoryStage requires player actor registry.");
            }

            SessionActivityIdentity identity = command.Identity;
            logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorInventoryFeedStarted",
                identity,
                command.Source,
                command.Reason,
                "owner='ActivityEntryPipeline' block='actor_inventory_feed'");

            IReadOnlyList<PlayerActorIdentityRecord> playerActors = bridge.ResolvePlayerActorCapabilityTargetsForCurrentEntry(identity);
            IReadOnlyList<SceneAuthoredActorRuntimeEntry> sceneActors = ResolveActiveSceneActors(sceneActorRegistry, identity);
            IActivityActorInstanceSource[] actorSources =
            {
                new PlayerActorInstanceSource(playerActors ?? Array.Empty<PlayerActorIdentityRecord>(), playerActorRegistry),
                new SceneAuthoredActorInstanceSource(sceneActors),
            };

            ActorInventoryFeed feed = new();
            ActorInventoryFeedResult result = feed.BuildFromSources(identity, actorSources, command.Source, command.Reason);
            if (!result.IsValid)
            {
                logBridge.LogEntryOwnerEvent(
                    "ActivityEntryActorInventoryFeedFailed",
                    identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryPipeline' block='actor_inventory_feed' reason='invalid_feed_result'");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][ActorInventoryFeed] Invalid feed result activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}'.");
            }

            bridge.SetCurrentActorInventoryFeedResult(result);
            logBridge.LogEntryOwnerEvent(
                "ActivityEntryActorInventoryFeedCompleted",
                identity,
                command.Source,
                command.Reason,
                $"owner='ActivityEntryPipeline' block='actor_inventory_feed' actorInstances='{result.ActorInstances.Count}' actorEntries='{result.ActorEntries.Count}' actorParticipations='{result.ActorParticipations.Count}'");
            return result;
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
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityIdentity setupIdentity)
        {
            return loadedSet.IsValid &&
                   loadedSet.Identity.IsValid &&
                   string.Equals(loadedSet.Identity.PipelineId, setupIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.SessionId, setupIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(loadedSet.Identity.ActivityId, definition.ActivityId, StringComparison.Ordinal) &&
                   loadedSet.Identity.EntrySequence == entrySequence;
        }
    }
}
