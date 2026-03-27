using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Strategy
{
    /// <summary>
    /// ResolvePlayerActor quais atores devem participar de um ActorGroupRearmRequest.
    /// Mantém a lógica de "grupos/alvos" centralizada e testável.
    /// </summary>
    public interface IActorGroupRearmTargetClassifier
    {
        void CollectTargets(ActorGroupRearmRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}

