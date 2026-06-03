using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityHandoffRuntimeResetStageCommand
    {
        public ActivityHandoffRuntimeResetStageCommand(
            SessionActivityIdentity initialIdentity,
            int entrySequence,
            string triggerSource,
            string triggerReason)
        {
            InitialIdentity = initialIdentity;
            EntrySequence = entrySequence;
            TriggerSource = Normalize(triggerSource);
            TriggerReason = Normalize(triggerReason);
        }

        public SessionActivityIdentity InitialIdentity { get; }
        public int EntrySequence { get; }
        public string TriggerSource { get; }
        public string TriggerReason { get; }

        public bool IsValid => InitialIdentity.IsValid && EntrySequence > 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal static class ActivityHandoffRuntimeResetStage
    {
        private const string Owner = "ActivityHandoffRuntimeResetStage";
        private const string ResetReason = "pipeline_reset_for_new_handoff";

        public static void Execute(
            ActivityHandoffRuntimeResetStageCommand command,
            IActivityEntryPipeline activityEntryPipeline,
            ActivityContentReleaseRuntimeState activityContentReleaseRuntimeState,
            ActivityObjectExitRuntimeState activityObjectExitRuntimeState,
            ActivityActorExitRuntimeState activityActorExitRuntimeState,
            ActivityPlayerActorRegistry activityPlayerActorRegistry,
            ActivitySceneActorRegistry activitySceneActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore)
        {
            if (!command.IsValid)
            {
                throw new System.InvalidOperationException("Activity handoff runtime reset command is invalid.");
            }

            Log("ActivityHandoffRuntimeResetStarted", command, DebugUtility.Colors.Info);

            activityEntryPipeline.ResetState();
            activityContentReleaseRuntimeState.ClearCurrentLoadedSet("<none>", 0, Owner, ResetReason);
            activityContentReleaseRuntimeState.ClearPendingReleaseContext("<none>", 0, Owner, ResetReason);
            activityContentReleaseRuntimeState.SetAwaitingContinuation(false, "<none>", 0, Owner, ResetReason);
            activityObjectExitRuntimeState.ClearAll("<none>", 0, Owner, ResetReason);
            ReleaseIndexedRouteScopedPlayerActors(activityPlayerActorRegistry);
            ReleaseSessionScopedActors(command, sessionActorRuntimeStore);
            activityPlayerActorRegistry.ClearAllRouteScopedIndexes();
            activitySceneActorRegistry.ClearAllRouteRetained();
            activityActorExitRuntimeState.ClearAll(command.InitialIdentity.ActivityId, command.EntrySequence, Owner, ResetReason);

            Log("ActivityHandoffRuntimeResetCompleted", command, DebugUtility.Colors.Success);
        }

        private static void Log(string eventName, ActivityHandoffRuntimeResetStageCommand command, string color)
        {
            DebugUtility.Log(
                typeof(ActivityHandoffRuntimeResetStage),
                $"[OBS][ActivityHandoffRuntimeResetStage] event='{Normalize(eventName)}' owner='{Owner}' activityId='{Normalize(command.InitialIdentity.ActivityId)}' entrySequence='{command.EntrySequence}' triggerSource='{Normalize(command.TriggerSource)}' triggerReason='{Normalize(command.TriggerReason)}' resetReason='{ResetReason}'.",
                color);
        }

        private static void ReleaseIndexedRouteScopedPlayerActors(ActivityPlayerActorRegistry activityPlayerActorRegistry)
        {
            if (activityPlayerActorRegistry == null)
            {
                throw new System.InvalidOperationException("Activity handoff runtime reset requires player actor registry.");
            }

            IReadOnlyList<PlayerActorRuntimeHandle> handles = activityPlayerActorRegistry.GetIndexedRouteScopedHandles();
            for (int index = 0; index < handles.Count; index++)
            {
                PlayerActorRuntimeHandle handle = handles[index];
                if (handle.Instance != null)
                {
                    Object.Destroy(handle.Instance);
                }
            }
        }

        private static void ReleaseSessionScopedActors(
            ActivityHandoffRuntimeResetStageCommand command,
            SessionActorRuntimeStore sessionActorRuntimeStore)
        {
            if (sessionActorRuntimeStore == null)
            {
                throw new System.InvalidOperationException("Activity handoff runtime reset requires session actor runtime store.");
            }

            IReadOnlyList<SessionActorRuntimeEntry> entries = sessionActorRuntimeStore.GetAllEntries();
            for (int index = 0; index < entries.Count; index++)
            {
                SessionActorRuntimeEntry entry = entries[index];
                DebugUtility.LogVerbose(typeof(ActivityHandoffRuntimeResetStage),
                    $"[OBS][ActorLifetime] event='ActorLifetimeDecisionResolved' owner='{Owner}' activityId='{Normalize(command.InitialIdentity.ActivityId)}' entrySequence='{command.EntrySequence}' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' decision='Release' source='{Normalize(command.TriggerSource)}' reason='{Normalize(command.TriggerReason)}'.",
                    DebugUtility.Colors.Info);
                if (entry.Instance != null)
                {
                    Object.Destroy(entry.Instance);
                }

                sessionActorRuntimeStore.Remove(entry.ActorInstanceRuntimeId);
                DebugUtility.LogVerbose(typeof(ActivityHandoffRuntimeResetStage),
                    $"[OBS][ActorLifetime] event='ActorLifetimeReleased' owner='{Owner}' activityId='{Normalize(command.InitialIdentity.ActivityId)}' entrySequence='{command.EntrySequence}' trigger='SessionReset' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' source='{Normalize(command.TriggerSource)}' reason='{Normalize(command.TriggerReason)}'.",
                    DebugUtility.Colors.Success);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
