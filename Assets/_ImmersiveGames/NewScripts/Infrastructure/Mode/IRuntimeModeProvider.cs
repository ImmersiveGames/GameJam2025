namespace _ImmersiveGames.NewScripts.Infrastructure.Mode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}
