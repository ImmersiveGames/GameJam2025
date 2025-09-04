using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SpawnSystems.New;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Events
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
    
    public class SpawnEvent : IEvent
    {
        public IPoolable Poolable { get; }
        public string PoolKey { get; }

        public SpawnEvent(IPoolable poolable, string poolKey)
        {
            Poolable = poolable;
            PoolKey = poolKey;
        }
    }
    
    public class OrbitsSpawnedEvent : IEvent
    {
        public List<IPoolable> SpawnedObjects { get; }
        public Vector3 Center { get; }
        public List<float> Radii { get; }
        public SpawnSystem SpawnSystem { get; }

        public OrbitsSpawnedEvent(List<IPoolable> spawnedObjects, Vector3 center, List<float> radii, SpawnSystem spawnSystem = null)
        {
            SpawnedObjects = spawnedObjects;
            Center = center;
            Radii = radii;
            SpawnSystem = spawnSystem;
        }
    }

}