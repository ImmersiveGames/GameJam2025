namespace _ImmersiveGames.NewScripts.Core.Infrastructure.RuntimeMode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}
