using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityContentReleaseContinuationStage
    {
        public const string Owner = "ActivityContentReleaseContinuationStage";
        public const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static void LogEvent(string message, bool completed)
        {
            DebugUtility.Log(
                typeof(ActivityContentReleaseContinuationStage),
                message,
                completed ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        public static void LogUnloadCompletionContinuationStarted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            int nextSceneIndex,
            SessionActivityCommand command,
            int totalScenes)
        {
            LogUnloadCompletionContinuation(
                "ActivityContentUnloadCompletionContinuationStarted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                nextSceneIndex,
                command,
                totalScenes,
                completed: false);
        }

        public static void LogUnloadCompletionContinuationCompleted(
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            int nextSceneIndex,
            SessionActivityCommand command,
            int totalScenes)
        {
            LogUnloadCompletionContinuation(
                "ActivityContentUnloadCompletionContinuationCompleted",
                pipelineId,
                sessionStateId,
                definition,
                entrySequence,
                nextSceneIndex,
                command,
                totalScenes,
                completed: true);
        }

        private static void LogUnloadCompletionContinuation(
            string eventName,
            string pipelineId,
            string sessionStateId,
            SessionActivityDefinition definition,
            int entrySequence,
            int nextSceneIndex,
            SessionActivityCommand command,
            int totalScenes,
            bool completed)
        {
            if (!definition.IsValid || entrySequence <= 0)
            {
                return;
            }

            string message =
                $"[OBS][ActivityContentReleaseContinuationStage] event='{Normalize(eventName)}' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' pipelineId='{Normalize(pipelineId)}' sessionStateId='{Normalize(sessionStateId)}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{Normalize(command.Source)}' reason='{Normalize(command.Reason)}' nextSceneIndex='{nextSceneIndex}' totalScenes='{totalScenes}'.";

            LogEvent(message, completed);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
