using System;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IEaterDomain
    {
        event Action<IActor> EaterRegistered;
        event Action<IActor> EaterUnregistered;

        IActor Eater { get; }

        bool RegisterEater(IActor actor);
        bool UnregisterEater(IActor actor);
        void Clear();
    }
}