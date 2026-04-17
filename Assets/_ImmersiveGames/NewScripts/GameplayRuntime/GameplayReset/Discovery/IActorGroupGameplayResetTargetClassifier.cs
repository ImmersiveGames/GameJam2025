using System.Collections.Generic;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Discovery
{
    /// <summary>
    /// ResolvePlayerActor quais atores devem participar de um ActorGroupGameplayResetRequest.
    /// Mant�m a l�gica de "grupos/alvos" centralizada e test�vel.
    /// </summary>
    public interface IActorGroupGameplayResetTargetClassifier
    {
        void CollectTargets(ActorGroupGameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results);
    }
}



