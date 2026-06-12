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
            DebugUtility.Log(
                typeof(ActivityHandoffRuntimeResetStage),
                $"[OBS][ActivityHandoffRuntimeResetStage] event='{Normalize(eventName)}' owner='{Owner}' activityId='{Normalize(command.InitialIdentity.ActivityId)}' entrySequence='{command.EntrySequence}' triggerSource='{Normalize(command.TriggerSource)}' triggerReason='{Normalize(command.TriggerReason)}' resetReason='{ResetReason}'.",
                color);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
