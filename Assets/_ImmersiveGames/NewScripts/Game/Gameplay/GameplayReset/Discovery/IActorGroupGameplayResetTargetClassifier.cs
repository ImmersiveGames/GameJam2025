using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Core;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Discovery
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


