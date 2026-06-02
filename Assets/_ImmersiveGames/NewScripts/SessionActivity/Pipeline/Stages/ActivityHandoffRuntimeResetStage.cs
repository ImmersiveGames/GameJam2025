using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityHandoffRuntimeResetStageCommand
    {
        public ActivityHandoffRuntimeResetStageCommand(
            SessionActivityDefinition initialDefinition,
            int entrySequence,
            string triggerSource,
            string triggerReason)
        {
            InitialDefinition = initialDefinition;
            EntrySequence = entrySequence;
            TriggerSource = Normalize(triggerSource);
            TriggerReason = Normalize(triggerReason);
        }

        public SessionActivityDefinition InitialDefinition { get; }
        public int EntrySequence { get; }
        public string TriggerSource { get; }
        public string TriggerReason { get; }

        public bool IsValid => InitialDefinition.IsValid && EntrySequence > 0;

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
            ActivitySceneActorRegistry activitySceneActorRegistry)
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
            activityPlayerActorRegistry.ClearAllRouteRetained();
            activitySceneActorRegistry.ClearAllRouteRetained();
            activityActorExitRuntimeState.ClearAll(command.InitialDefinition.ActivityId, command.EntrySequence, Owner, ResetReason);

            Log("ActivityHandoffRuntimeResetCompleted", command, DebugUtility.Colors.Success);
        }

        private static void Log(string eventName, ActivityHandoffRuntimeResetStageCommand command, string color)
        {
            DebugUtility.Log(
                typeof(ActivityHandoffRuntimeResetStage),
                $"[OBS][ActivityHandoffRuntimeResetStage] event='{Normalize(eventName)}' owner='{Owner}' activityId='{Normalize(command.InitialDefinition.ActivityId)}' entrySequence='{command.EntrySequence}' triggerSource='{Normalize(command.TriggerSource)}' triggerReason='{Normalize(command.TriggerReason)}' resetReason='{ResetReason}'.",
                color);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
