using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    /// <summary>
    /// Resolve quais atores devem participar de um ActorGroupRearmRequest.
    /// Mantém a lógica de "grupos/alvos" centralizada e testável.
    /// </summary>
    public interface IActorGroupRearmTargetClassifier
    {
        void CollectTargets(ActorGroupRearmRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}

