using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SpawnRequestEvent : IEvent
    {
        public string PoolKey { get; }
        public GameObject SourceGameObject { get; }

        public SpawnRequestEvent(string poolKey, GameObject sourceGameObject)
        {
            PoolKey = poolKey;
            SourceGameObject = sourceGameObject;
        }
    }

    public class SpawnTriggeredEvent : IEvent
    {
        public string PoolKey { get; }
        public Vector3 Position { get; }

        public SpawnTriggeredEvent(string poolKey, Vector3 position)
        {
            PoolKey = poolKey;
            Position = position;
        }
    }

    public class SpawnFailedEvent : IEvent
    {
        public string PoolKey { get; }
        public Vector3 Position { get; }

        public SpawnFailedEvent(string poolKey, Vector3 position)
        {
            PoolKey = poolKey;
            Position = position;
        }
    }
    public class SpawnPointLockedEvent : IEvent
    {
        public SpawnPoint Point { get; }
        public SpawnPointLockedEvent(SpawnPoint point) => Point = point;
    }

    public class SpawnPointUnlockedEvent : IEvent
    {
        public SpawnPoint Point { get; }
        public SpawnPointUnlockedEvent(SpawnPoint point) => Point = point;
    }

    public class SpawnPointResetEvent : IEvent
    {
        public SpawnPoint Point { get; }
        public SpawnPointResetEvent(SpawnPoint point) => Point = point;
    }
    
    public class GlobalSpawnEvent : IEvent
    {
        public string EventName { get; }
        public GlobalSpawnEvent(string eventName) => EventName = eventName;
    }
    
    public interface IGlobalEvent : IEvent
    {
        string EventName { get; }
    }
}