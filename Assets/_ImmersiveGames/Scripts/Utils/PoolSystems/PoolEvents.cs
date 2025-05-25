using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PoolEvents : IEvent
    {
        public string PoolKey { get; }
        public PoolEvents(string poolKey) => PoolKey = poolKey;
    }

    public class ObjectSpawnedEvent : IEvent
    {
        public string PoolKey { get; }
        public Vector3 Position { get; }
        public GameObject Object { get; }

        public ObjectSpawnedEvent(string poolKey, Vector3 position, GameObject obj)
        {
            PoolKey = poolKey;
            Position = position;
            Object = obj;
        }
    }

    public class ObjectReturnedEvent : IEvent
    {
        public string PoolKey { get; }
        public GameObject Object { get; }

        public ObjectReturnedEvent(string poolKey, GameObject obj)
        {
            PoolKey = poolKey;
            Object = obj;
        }
    }

    public class ObjectDeactivatedEvent : IEvent
    {
        public string PoolKey { get; }
        public GameObject Object { get; }

        public ObjectDeactivatedEvent(string poolKey, GameObject obj)
        {
            PoolKey = poolKey;
            Object = obj;
        }
    }
}