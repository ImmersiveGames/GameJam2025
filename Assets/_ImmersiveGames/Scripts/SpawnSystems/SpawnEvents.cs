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

    public struct SpawnTriggeredEvent : IEvent
    {
        public string PoolName { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnTriggeredEvent(string poolName, Vector3 position, SpawnData data)
        {
            PoolName = poolName;
            Position = position;
            Data = data;
        }
    }

    public struct SpawnFailedEvent : IEvent
    {
        public string PoolName { get; }
        public Vector3 Position { get; }
        public SpawnData Data { get; }

        public SpawnFailedEvent(string poolName, Vector3 position, SpawnData data)
        {
            PoolName = poolName;
            Position = position;
            Data = data;
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