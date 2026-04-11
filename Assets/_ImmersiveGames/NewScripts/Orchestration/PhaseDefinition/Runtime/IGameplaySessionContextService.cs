namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public interface IGameplaySessionContextService
    {
        GameplaySessionContextSnapshot Current { get; }
        bool TryGetCurrent(out GameplaySessionContextSnapshot snapshot);
        bool TryGetLast(out GameplaySessionContextSnapshot snapshot);
        GameplaySessionContextSnapshot Update(GameplaySessionContextSnapshot snapshot);
        GameplaySessionContextSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
    }
}
