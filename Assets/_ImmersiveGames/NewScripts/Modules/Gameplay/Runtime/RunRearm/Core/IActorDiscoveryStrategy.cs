using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core
{
    /// <summary>
    /// Estratégia de descoberta de atores para GameplayReset.
    /// </summary>
    public interface IActorDiscoveryStrategy
    {
        string Name { get; }

        int CollectTargets(RunRearmRequest request, List<IActor> results, out bool fallbackUsed);
    }
}
