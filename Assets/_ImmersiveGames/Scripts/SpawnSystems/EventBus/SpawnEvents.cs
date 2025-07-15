using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.EventBus
{
    public struct SpawnRequestEvent : ISpawnEvent
    {
        public string PoolKey { get; }
        public Vector3? Position { get; }
        public GameObject SourceGameObject { get; }

        public SpawnRequestEvent(string poolKey, GameObject sourceGameObject, Vector3? spawnPosition)
        {
            PoolKey = poolKey;
            SourceGameObject = sourceGameObject;
            Position = spawnPosition;
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
        public Vector3? Position { get; }
        public GameObject SourceGameObject { get; }

        public GlobalSpawnEvent(string eventName, Vector3 position, GameObject sourceObject = null)
        {
            EventName = eventName;
            Position = position;
            SourceGameObject = sourceObject;
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