using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolMultipleSpawnTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
        private readonly IActor _mockSpawner = new MockActor();

        private void Start()
        {
            StartCoroutine(TestMultipleSpawn());
        }

        private IEnumerator TestMultipleSpawn()
        {
            DebugUtility.Log<PoolMultipleSpawnTest>("Testing multiple spawn:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolMultipleSpawnTest>("No PoolData configured for multiple spawn test.", this);
                yield break;
            }

            var data = poolDataArray[0];
            var poolObject = new GameObject($"TestPool_{data.ObjectName}");
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.SetAllowMultipleGetsInFrame(true);
            pool.Initialize();

            pool.ClearPool();
            yield return null;
            pool.Initialize();
            DebugUtility.Log<PoolMultipleSpawnTest>($"Pool '{data.ObjectName}' reset: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);

            Vector3 position = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0f,
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );
            var spawnedObjects = pool.GetMultipleObjects(3, position, _mockSpawner, true);
            DebugUtility.Log<PoolMultipleSpawnTest>($"Multiple spawn: Requested 3, retrieved {spawnedObjects.Count} objects from pool '{data.ObjectName}'.", "green", this);

            bool allSpawnedCorrectly = spawnedObjects.Count == 3;
            foreach (var poolable in spawnedObjects)
            {
                if (poolable != null)
                {
                    DebugUtility.Log<PoolMultipleSpawnTest>(
                        $"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    allSpawnedCorrectly &= poolable.GetGameObject().activeSelf;
                }
            }

            if (allSpawnedCorrectly)
            {
                DebugUtility.Log<PoolMultipleSpawnTest>("Multiple spawn test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolMultipleSpawnTest>($"Multiple spawn test failed: Expected 3 objects, got {spawnedObjects.Count}.", this);
            }

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}