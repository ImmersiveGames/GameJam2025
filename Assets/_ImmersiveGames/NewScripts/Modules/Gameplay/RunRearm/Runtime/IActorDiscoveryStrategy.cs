using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Runtime
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
