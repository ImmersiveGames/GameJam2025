using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
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

