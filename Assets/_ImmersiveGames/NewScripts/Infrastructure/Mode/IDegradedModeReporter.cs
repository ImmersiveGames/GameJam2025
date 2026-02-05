namespace _ImmersiveGames.NewScripts.Infrastructure.Mode
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
