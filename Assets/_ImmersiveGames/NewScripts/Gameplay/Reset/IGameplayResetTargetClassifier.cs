using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Resolve quais atores devem participar de um GameplayResetRequest.
    /// Mantém a lógica de "grupos/alvos" centralizada e testável.
    /// </summary>
    public interface IGameplayResetTargetClassifier
    {
        void CollectTargets(GameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}
