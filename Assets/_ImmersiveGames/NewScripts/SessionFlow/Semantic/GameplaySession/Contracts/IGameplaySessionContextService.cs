using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts
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

