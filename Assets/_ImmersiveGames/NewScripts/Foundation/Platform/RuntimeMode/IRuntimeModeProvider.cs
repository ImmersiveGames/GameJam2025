namespace ImmersiveGames.GameJam2025.Infrastructure.RuntimeMode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}

