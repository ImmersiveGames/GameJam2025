using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public abstract class BaseSpawnStrategy : MonoBehaviour, ISpawnStrategy
    {
        public abstract void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null);

        
        public virtual void SetCenterTransform(Transform newCenter)
        {
            DebugUtility.LogVerbose(GetType(), $"SetCenterTransform not implemented for {GetType().Name}.", "yellow", this);
        }

        protected List<IPoolable> GetObjects(ObjectPool pool, int count, IActor actor, bool exhaust)
        {
            int targetCount = exhaust ? pool.GetAvailableCount() + pool.GetActiveObjects().Count + 1 : count;
            var poolables = pool.GetMultipleObjects(targetCount, Vector3.zero, actor, false);
            int successfulSpawns = poolables.Count;

            if (successfulSpawns == 0)
            {
                DebugUtility.LogWarning(GetType(), $"Failed to spawn any objects from '{pool.name}'. Pool exhausted?", this);
            }
            else
            {
                DebugUtility.LogVerbose(GetType(), $"Retrieved {successfulSpawns}/{targetCount} objects from pool '{pool.name}'.", "cyan", this);
            }

            return poolables;
        }

        protected void ActivatePoolable(IPoolable poolable, Vector3 position, Quaternion rotation, IActor actor, ObjectPool pool)
        {
            poolable.GetGameObject().transform.position = position;
            poolable.GetGameObject().transform.rotation = rotation;
            poolable.Activate(position, actor);
            pool.ActivateObject(poolable, position, actor);
            DebugUtility.Log(GetType(), 
                $"Spawned object from '{pool.name}' at {position} with rotation {rotation.eulerAngles}, " +
                $"actor={(actor != null ? actor.Name : "null")}. Active: {poolable.GetGameObject().activeSelf}",
                "green", this);
        }

        protected void LogExhausted(ObjectPool pool, int successfulSpawns, int targetCount)
        {
            if (successfulSpawns < pool.GetAvailableCount() + pool.GetActiveObjects().Count)
            {
                DebugUtility.Log(GetType(), $"Pool '{pool.name}' exhausted after {successfulSpawns} spawns.", "yellow", this);
            }
        }
    }
}