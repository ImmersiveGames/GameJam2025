using System.Collections.Generic;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Discovery
{
    /// <summary>
    /// Estrat�gia de descoberta de atores para GameplayReset.
    /// </summary>
    public interface IActorGroupGameplayResetDiscoveryStrategy
    {
        string Name { get; }

        int CollectTargets(ActorGroupGameplayResetRequest request, List<IActor> results, out bool fallbackUsed);
    }
}



