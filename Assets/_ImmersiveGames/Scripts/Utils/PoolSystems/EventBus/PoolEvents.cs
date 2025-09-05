using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PoolExhaustedEvent : IEvent
    {
        public string PoolKey { get; }
        public PoolExhaustedEvent(string poolKey) => PoolKey = poolKey;
    }

    public class PoolObjectReturnedEvent : IEvent
    {
        public string PoolKey { get; }
        public IPoolable Poolable { get; }

        public PoolObjectReturnedEvent(string poolKey, IPoolable poolable)
        {
            PoolKey = poolKey;
            Poolable = poolable;
        }
    }

    public class ObjectCreatedEvent : IEvent
    {
        public IPoolable Poolable { get; }
        public PoolableObjectData Config { get; }
        public ObjectPool Pool { get; }

        public ObjectCreatedEvent(IPoolable poolable, PoolableObjectData config, ObjectPool pool)
        {
            Poolable = poolable;
            Config = config;
            Pool = pool;
        }
    }

    public class ObjectActivatedEvent : IEvent
    {
        public IPoolable Poolable { get; }
        public IActor Spawner { get; }
        public Vector3 Position { get; }

        public ObjectActivatedEvent(IPoolable poolable, IActor spawner, Vector3 position)
        {
            Poolable = poolable;
            Spawner = spawner;
            Position = position;
        }
    }
}