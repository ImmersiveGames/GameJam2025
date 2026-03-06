namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface IRestartContextService
    {
        GameplayStartSnapshot Current { get; }
        GameplayStartSnapshot UpdateGameplayStartSnapshot(GameplayStartSnapshot snapshot);
        bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot);
        GameplayStartSnapshot RegisterGameplayStart(GameplayStartSnapshot snapshot);
        bool TryGetCurrent(out GameplayStartSnapshot snapshot);
        void Clear(string reason = null);
    }
}
