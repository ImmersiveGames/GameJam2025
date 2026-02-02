namespace _ImmersiveGames.NewScripts.Runtime.Mode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}
