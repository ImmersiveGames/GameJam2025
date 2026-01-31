namespace _ImmersiveGames.NewScripts.Infrastructure.Runtime
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}
