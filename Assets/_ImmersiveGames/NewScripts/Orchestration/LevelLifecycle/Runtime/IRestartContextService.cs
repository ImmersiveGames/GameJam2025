namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface IRestartContextService
    {
        GameplayStartSnapshot Current { get; }
        GameplayStartSnapshot UpdateGameplayStartSnapshot(GameplayStartSnapshot snapshot);
        GameplayStartSnapshot RegisterGameplayStart(_ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime.PhaseDefinitionSelectedEvent evt);
        bool TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot);
        GameplayStartSnapshot RegisterGameplayStart(GameplayStartSnapshot snapshot);
        bool TryGetCurrent(out GameplayStartSnapshot snapshot);
        void Clear(string reason = null);
    }
}
