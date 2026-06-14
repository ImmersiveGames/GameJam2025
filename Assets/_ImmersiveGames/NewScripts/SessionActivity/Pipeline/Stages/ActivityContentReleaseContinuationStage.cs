using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityContentReleaseContinuationStage
    {
        public const string Owner = "ActivityContentReleaseContinuationStage";
        public const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static void LogEvent(string message, bool completed)
        {
            if (completed)
            {
                DebugUtility.Log(
                    typeof(ActivityContentReleaseContinuationStage),
                    message,
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActivityContentReleaseContinuationStage),
                message,
                DebugUtility.Colors.Info);
        }

    }
}
