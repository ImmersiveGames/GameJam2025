using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityWindowSceneUnloadContinuationStage
    {
        public const string Owner = "ActivityWindowSceneUnloadContinuationStage";
        public const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static void LogActivationUnloadContinuationStarted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityCommand command)
        {
            LogContinuation(
                "ActivationWindowSceneUnloadContinuationStarted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                command,
                completed: false);
        }

        public static void LogActivationUnloadContinuationCompleted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityCommand command)
        {
            LogContinuation(
                "ActivationWindowSceneUnloadContinuationCompleted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                command,
                completed: true);
        }

        public static void LogDeactivationUnloadContinuationStarted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityCommand command)
        {
            LogContinuation(
                "DeactivationWindowSceneUnloadContinuationStarted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                command,
                completed: false);
        }

        public static void LogDeactivationUnloadContinuationCompleted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityCommand command)
        {
            LogContinuation(
                "DeactivationWindowSceneUnloadContinuationCompleted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                command,
                completed: true);
        }

        private static void LogContinuation(
            string eventName,
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            SessionActivityCommand command,
            bool completed)
        {
            if (!definition.IsValid || entrySequence <= 0)
            {
                return;
            }

            string message =
                $"event='{Normalize(eventName)}' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' pipelineId='{Normalize(pipelineId)}' sessionStateId='{Normalize(sessionStateId)}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{Normalize(command.Source)}' reason='{Normalize(command.Reason)}'.";

            if (completed)
            {
                DebugUtility.Log(
                    typeof(ActivityWindowSceneUnloadContinuationStage),
                    message,
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActivityWindowSceneUnloadContinuationStage),
                message,
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
