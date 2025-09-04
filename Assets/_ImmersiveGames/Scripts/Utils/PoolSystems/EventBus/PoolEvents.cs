using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PoolExhaustedEvent : IEvent
    {
        public string PoolKey { get; }
        public PoolExhaustedEvent(string poolKey) => PoolKey = poolKey;
    }
    
    public class PoolRestoredEvent : IEvent
    {
        public string PoolKey { get; }
        public PoolRestoredEvent(string poolKey) => PoolKey = poolKey;
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
    public class ObjectCreatedEvent: IEvent
    {
        public IPoolable Poolable{ get; }
        public PoolableObjectData Config{ get; }
        public ObjectPool Pool{ get; }

        public ObjectCreatedEvent(IPoolable poolable, PoolableObjectData config, ObjectPool pool)
        {
            Poolable = poolable;
            Config = config;
            Pool = pool;
        }
    }
}