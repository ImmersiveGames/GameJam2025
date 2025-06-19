using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public struct SpawnRequestEvent : IEvent
    {
        public string PoolKey { get; }
        public GameObject SourceGameObject { get; }
        public Vector3? SpawnPosition { get; }

        public SpawnRequestEvent(string poolKey, GameObject sourceGameObject, Vector3? spawnPosition = null)
        {
            PoolKey = poolKey;
            SourceGameObject = sourceGameObject;
            SpawnPosition = spawnPosition;
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

    public class GlobalSpawnEvent : ISpawnEvent
    {
        public string EventName { get; }
        public Vector3 Position { get; }
        public GameObject GameObject { get; }

        public GlobalSpawnEvent(string eventName, Vector3 position, GameObject sourceObject = null)
        {
            EventName = eventName;
            Position = position;
            GameObject = sourceObject;
        }
    }
    public class GlobalGenericSpawnEvent : IEvent
    {
        public string EventName { get; }

        public GlobalGenericSpawnEvent(string eventName)
        {
            EventName = eventName;
        }
    }
}