using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Core;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Discovery
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



