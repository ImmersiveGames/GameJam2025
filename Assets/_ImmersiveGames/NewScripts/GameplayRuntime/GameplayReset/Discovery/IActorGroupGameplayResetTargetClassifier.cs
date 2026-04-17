using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Core;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Discovery
{
    /// <summary>
    /// ResolvePlayerActor quais atores devem participar de um ActorGroupGameplayResetRequest.
    /// Mantém a lógica de "grupos/alvos" centralizada e testável.
    /// </summary>
    public interface IActorGroupGameplayResetTargetClassifier
    {
        void CollectTargets(ActorGroupGameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}



