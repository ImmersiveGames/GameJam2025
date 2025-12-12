using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IActorRegistry
    {
        event Action<IActor> ActorRegistered;
        event Action<IActor> ActorUnregistered;

        IReadOnlyCollection<IActor> Actors { get; }

        bool TryGetActor(string actorId, out IActor actor);

        bool Register(IActor actor);
        bool Unregister(IActor actor);
        bool UnregisterById(string actorId);
        void Clear();
    }
}