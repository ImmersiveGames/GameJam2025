using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Core;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Discovery
{
    /// <summary>
    /// Estratégia de descoberta de atores para GameplayReset.
    /// </summary>
    public interface IActorGroupGameplayResetDiscoveryStrategy
    {
        string Name { get; }

        int CollectTargets(ActorGroupGameplayResetRequest request, List<IActor> results, out bool fallbackUsed);
    }
}


