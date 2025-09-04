using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class TransformBasedSpawnStrategy : BaseSpawnStrategy
    {
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField] private Transform targetTransform;

        public override void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<TransformBasedSpawnStrategy>("Pool is null.", this);
                return;
            }

            DebugUtility.LogVerbose<TransformBasedSpawnStrategy>(
                $"Executing spawn: spawnCount={spawnCount}, targetTransform={(targetTransform != null ? targetTransform.name : "null")}, " +
                $"actor={(actor != null ? actor.Name : "null")}, exhaust={exhaust}",
                "cyan", this);

            Vector3 position = (targetTransform != null) ? targetTransform.position : spawnerTransform.position;
            position.y = 0f;
            Quaternion rotation = (targetTransform != null) ? targetTransform.rotation : spawnerTransform.rotation;
            int targetCount = exhaust ? pool.GetAvailableCount() + pool.GetActiveObjects().Count + 1 : spawnCount;
            var poolables = pool.GetMultipleObjects(targetCount, Vector3.zero, actor, false);
            int successfulSpawns = poolables.Count;

            if (successfulSpawns == 0)
            {
                DebugUtility.LogWarning<TransformBasedSpawnStrategy>($"Failed to spawn any objects from '{pool.name}'. Pool exhausted?", this);
                return;
            }

            foreach (var poolable in poolables)
            {
                poolable.GetGameObject().transform.position = position;
                poolable.GetGameObject().transform.rotation = rotation;
                poolable.Activate(position, actor);
                pool.ActivateObject(poolable, position, actor);
                DebugUtility.Log<TransformBasedSpawnStrategy>(
                    $"Spawned object from '{pool.name}' at {position} with rotation {rotation.eulerAngles}, " +
                    $"actor={(actor != null ? actor.Name : "null")}. Active: {poolable.GetGameObject().activeSelf}",
                    "green", this);
            }

            if (exhaust && successfulSpawns < pool.GetAvailableCount() + pool.GetActiveObjects().Count)
            {
                DebugUtility.Log<TransformBasedSpawnStrategy>($"Pool '{pool.name}' exhausted after {successfulSpawns} spawns.", "yellow", this);
            }

            DebugUtility.LogVerbose<TransformBasedSpawnStrategy>($"Completed spawn: {successfulSpawns}/{targetCount} objects spawned.", "cyan", this);
        }

        public void SetCenterTransform(Transform newCenter)
        {
            targetTransform = newCenter;
            DebugUtility.LogVerbose<TransformBasedSpawnStrategy>($"Target transform set to {(newCenter != null ? newCenter.name : "null")}.", "cyan", this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 position = (targetTransform != null) ? targetTransform.position : transform.position;
            position.y = 0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, 0.2f);
        }
    }
}