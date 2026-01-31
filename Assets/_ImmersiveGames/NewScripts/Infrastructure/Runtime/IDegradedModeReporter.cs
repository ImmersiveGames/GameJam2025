namespace _ImmersiveGames.NewScripts.Infrastructure.Runtime
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
