using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityContentReleaseContinuationStage
    {
        public const string Owner = "ActivityContentReleaseContinuationStage";

        public static void LogEvent(string message, bool completed)
        {
            DebugUtility.Log(
                typeof(ActivityContentReleaseContinuationStage),
                message,
                completed ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }
    }
}
