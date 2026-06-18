using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;

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
            TriggerSource = triggerSource.TrimToEmpty();
            TriggerReason = triggerReason.TrimToEmpty();
        }

        public SessionActivityIdentity InitialIdentity { get; }
        public int EntrySequence { get; }
        public string TriggerSource { get; }
        public string TriggerReason { get; }

        public bool IsValid => InitialIdentity.IsValid && EntrySequence > 0;
    }

    internal static class ActivityHandoffRuntimeResetStage
    {
        private const string Owner = "ActivityHandoffRuntimeResetStage";
        private const string ResetReason = "pipeline_reset_for_new_handoff";

        public static void Execute(
            ActivityHandoffRuntimeResetStageCommand command,
            IActivityEntryPipeline activityEntryPipeline,
            ActivityContentRuntimeState activityContentRuntimeState,
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
            activityContentRuntimeState.ClearCurrentLoadedSet("<none>", 0, Owner, ResetReason);
            activityContentReleaseRuntimeState.ClearPendingReleaseContext("<none>", 0, Owner, ResetReason);
            activityContentReleaseRuntimeState.SetAwaitingContinuation(false, "<none>", 0, Owner, ResetReason);
            activityObjectExitRuntimeState.ClearAll("<none>", 0, Owner, ResetReason);
            SessionActivityActorRuntimeReleaseStage.ReleaseIndexedRouteScopedPlayerActors(
                activityPlayerActorRegistry,
                command.TriggerSource,
                ResetReason);
            SessionActivityActorRuntimeReleaseStage.ReleaseAllSessionScopedActors(
                command.InitialIdentity,
                sessionActorRuntimeStore,
                command.TriggerSource,
                ResetReason);
            activityPlayerActorRegistry.ClearAllRouteScopedIndexes();
            activitySceneActorRegistry.ClearAllRouteRetained();
            activityActorExitRuntimeState.ClearAll(command.InitialIdentity.ActivityId, command.EntrySequence, Owner, ResetReason);

            Log("ActivityHandoffRuntimeResetCompleted", command, DebugUtility.Colors.Success);
        }

        private static void Log(string eventName, ActivityHandoffRuntimeResetStageCommand command, string color)
        {
            string message = $"event='{eventName.TrimToEmpty()}' owner='{Owner}' activityId='{command.InitialIdentity.ActivityId.TrimToEmpty()}' entrySequence='{command.EntrySequence}' triggerSource='{command.TriggerSource.TrimToEmpty()}' triggerReason='{command.TriggerReason.TrimToEmpty()}' resetReason='{ResetReason}'.";

            if (eventName.EndsWith("Completed", System.StringComparison.Ordinal))
            {
                DebugUtility.Log(
                    typeof(ActivityHandoffRuntimeResetStage),
                    message,
                    color);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActivityHandoffRuntimeResetStage),
                message,
                color);
        }
    }
}
