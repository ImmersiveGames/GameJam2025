using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm
{
    /// <summary>
    /// Estratégia de descoberta de atores para GameplayReset.
    /// </summary>
    public interface IActorDiscoveryStrategy
    {
        string Name { get; }

        int CollectTargets(GameplayResetRequest request, List<IActor> results, out bool fallbackUsed);
    }
}
