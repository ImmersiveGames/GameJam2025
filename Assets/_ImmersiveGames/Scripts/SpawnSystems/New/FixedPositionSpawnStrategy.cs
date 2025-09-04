using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class FixedPositionSpawnStrategy : BaseSpawnStrategy
    {
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;

        private void OnValidate()
        {
            if (spawnPosition.y != 0)
            {
                spawnPosition.y = 0f;
                DebugUtility.LogWarning<FixedPositionSpawnStrategy>("spawnPosition.y reset to 0 for top-down XZ.", this);
            }
        }

        public override void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<FixedPositionSpawnStrategy>("Pool is null.", this);
                return;
            }

            DebugUtility.LogVerbose<FixedPositionSpawnStrategy>(
                $"Executing spawn: spawnCount={spawnCount}, spawnPosition={spawnPosition}, " +
                $"actor={(actor != null ? actor.Name : "null")}, exhaust={exhaust}",
                "cyan", this);

            Vector3 position = spawnerTransform.position + spawnPosition;
            position.y = 0f;
            Quaternion rotation = spawnerTransform.rotation;
            int targetCount = exhaust ? pool.GetAvailableCount() + pool.GetActiveObjects().Count + 1 : spawnCount;
            var poolables = pool.GetMultipleObjects(targetCount, Vector3.zero, actor, false);
            int successfulSpawns = poolables.Count;

            if (successfulSpawns == 0)
            {
                DebugUtility.LogWarning<FixedPositionSpawnStrategy>($"Failed to spawn any objects from '{pool.name}'. Pool exhausted?", this);
                return;
            }

            foreach (var poolable in poolables)
            {
                poolable.GetGameObject().transform.position = position;
                poolable.GetGameObject().transform.rotation = rotation;
                poolable.Activate(position, actor);
                pool.ActivateObject(poolable, position, actor);
                DebugUtility.Log<FixedPositionSpawnStrategy>(
                    $"Spawned object from '{pool.name}' at {position} with rotation {rotation.eulerAngles}, " +
                    $"actor={(actor != null ? actor.Name : "null")}. Active: {poolable.GetGameObject().activeSelf}",
                    "green", this);
            }

            if (exhaust && successfulSpawns < pool.GetAvailableCount() + pool.GetActiveObjects().Count)
            {
                DebugUtility.Log<FixedPositionSpawnStrategy>($"Pool '{pool.name}' exhausted after {successfulSpawns} spawns.", "yellow", this);
            }

            DebugUtility.LogVerbose<FixedPositionSpawnStrategy>($"Completed spawn: {successfulSpawns}/{targetCount} objects spawned.", "cyan", this);
        }

        public void SetCenterTransform(Transform newCenter)
        {
            spawnPosition = newCenter.position;
            spawnPosition.y = 0f;
            DebugUtility.LogVerbose<FixedPositionSpawnStrategy>($"Spawn position updated to {spawnPosition}.", "cyan", this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + spawnPosition, 0.2f);
        }
    }
}