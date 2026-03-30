using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Strategy
{
    /// <summary>
    /// Estratégia de descoberta de atores para GameplayReset.
    /// </summary>
    public interface IActorGroupRearmDiscoveryStrategy
    {
        string Name { get; }

        int CollectTargets(ActorGroupRearmRequest request, List<IActor> results, out bool fallbackUsed);
    }
}

