using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset;
using _ImmersiveGames.NewScripts.Runtime.Actors;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset.Domain
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
