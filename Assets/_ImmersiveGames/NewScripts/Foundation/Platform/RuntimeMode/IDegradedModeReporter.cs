namespace ImmersiveGames.GameJam2025.Infrastructure.RuntimeMode
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

