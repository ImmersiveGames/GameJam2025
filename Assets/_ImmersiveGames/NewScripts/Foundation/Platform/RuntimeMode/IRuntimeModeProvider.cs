namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public interface IRuntimeModeProvider
    {
        RuntimeMode Current { get; }
        bool IsStrict { get; }
    }
}

