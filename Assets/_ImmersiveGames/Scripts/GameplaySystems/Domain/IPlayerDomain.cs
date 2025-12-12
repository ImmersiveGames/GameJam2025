using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IPlayerDomain
    {
        event Action<IActor> PlayerRegistered;
        event Action<IActor> PlayerUnregistered;

        IReadOnlyList<IActor> Players { get; }

        bool RegisterPlayer(IActor actor);
        bool UnregisterPlayer(IActor actor);
        bool TryGetPlayerByIndex(int index, out IActor player);
    }
}