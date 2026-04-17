namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public interface IDegradedModeReporter
    {
        void Report(
            string feature,
            string reason,
            string detail = null,
            string signature = null,
            string profile = null);
    }
}

