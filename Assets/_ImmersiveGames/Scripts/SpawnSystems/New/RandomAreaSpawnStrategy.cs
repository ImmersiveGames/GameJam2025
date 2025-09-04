using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class RandomAreaSpawnStrategy : BaseSpawnStrategy
    {
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);

        private void OnValidate()
        {
            if (spawnAreaSize.y != 0)
            {
                spawnAreaSize.y = 0;
                DebugUtility.LogWarning<RandomAreaSpawnStrategy>("spawnAreaSize.y reset to 0 for top-down XZ.", this);
            }
        }

        public override void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<RandomAreaSpawnStrategy>("Pool is null.", this);
                return;
            }

            DebugUtility.LogVerbose<RandomAreaSpawnStrategy>(
                $"Executing spawn: spawnCount={spawnCount}, spawnAreaSize={spawnAreaSize}, " +
                $"actor={(actor != null ? actor.Name : "null")}, exhaust={exhaust}",
                "cyan", this);

            int targetCount = exhaust ? pool.GetAvailableCount() + pool.GetActiveObjects().Count + 1 : spawnCount;
            Quaternion rotation = spawnerTransform.rotation;
            var poolables = pool.GetMultipleObjects(targetCount, Vector3.zero, actor, false);
            int successfulSpawns = poolables.Count;

            if (successfulSpawns == 0)
            {
                DebugUtility.LogWarning<RandomAreaSpawnStrategy>($"Failed to spawn any objects from '{pool.name}'. Pool exhausted?", this);
                return;
            }

            for (int i = 0; i < successfulSpawns; i++)
            {
                Vector3 position = spawnerTransform.position;
                if (spawnAreaSize != Vector3.zero)
                {
                    position += new Vector3(
                        Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                        0f,
                        Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                    );
                }
                else
                {
                    position.y = 0f;
                }

                poolables[i].GetGameObject().transform.position = position;
                poolables[i].GetGameObject().transform.rotation = rotation;
                poolables[i].Activate(position, actor);
                pool.ActivateObject(poolables[i], position, actor);
                DebugUtility.Log<RandomAreaSpawnStrategy>(
                    $"Spawned object from '{pool.name}' at {position} with rotation {rotation.eulerAngles}, " +
                    $"actor={(actor != null ? actor.Name : "null")}. Active: {poolables[i].GetGameObject().activeSelf}",
                    "green", this);
            }

            if (exhaust && successfulSpawns < pool.GetAvailableCount() + pool.GetActiveObjects().Count)
            {
                DebugUtility.Log<RandomAreaSpawnStrategy>($"Pool '{pool.name}' exhausted after {successfulSpawns} spawns.", "yellow", this);
            }

            DebugUtility.LogVerbose<RandomAreaSpawnStrategy>($"Completed spawn: {successfulSpawns}/{targetCount} objects spawned.", "cyan", this);
        }

        public void SetCenterTransform(Transform newCenter)
        {
            DebugUtility.LogVerbose<RandomAreaSpawnStrategy>($"SetCenterTransform not implemented for RandomAreaSpawnStrategy. Use spawnerTransform.", "yellow", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnAreaSize != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z));
            }
        }
    }
}