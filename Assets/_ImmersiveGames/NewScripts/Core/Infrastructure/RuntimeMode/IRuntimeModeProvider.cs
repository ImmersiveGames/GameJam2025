namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}
