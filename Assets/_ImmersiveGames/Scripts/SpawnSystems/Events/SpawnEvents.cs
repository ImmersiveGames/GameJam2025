using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Events
{
    public class SpawnSuccessEvent : ISpawnEvent
    {
        public SpawnSystem SpawnSystem { get; }
        public IActor Spawner { get; }
        public SpawnSystem.PoolConfig PoolConfig { get; }
        public List<IPoolable> SpawnedObjects { get; }
        public Vector3? Position { get; }
        public GameObject SourceGameObject { get; }

        public SpawnSuccessEvent(SpawnSystem spawnSystem, IActor spawner, SpawnSystem.PoolConfig poolConfig, 
            List<IPoolable> spawnedObjects, Vector3 position, GameObject sourceGameObject)
        {
            SpawnSystem = spawnSystem;
            Spawner = spawner;
            PoolConfig = poolConfig;
            SpawnedObjects = spawnedObjects;
            Position = position;
            SourceGameObject = sourceGameObject;
        }
    }

    public class SpawnFailureEvent : ISpawnEvent
    {
        public SpawnSystem SpawnSystem { get; }
        public IActor Spawner { get; }
        public SpawnSystem.PoolConfig PoolConfig { get; }
        public string ErrorMessage { get; }
        public Vector3? Position { get; }
        public GameObject SourceGameObject { get; }

        public SpawnFailureEvent(SpawnSystem spawnSystem, IActor spawner, SpawnSystem.PoolConfig poolConfig, 
            string errorMessage, Vector3? position, GameObject sourceGameObject)
        {
            SpawnSystem = spawnSystem;
            Spawner = spawner;
            PoolConfig = poolConfig;
            ErrorMessage = errorMessage;
            Position = position;
            SourceGameObject = sourceGameObject;
        }
    }
}