using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolListActiveObjectsTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);

        private void Start()
        {
            StartCoroutine(TestListActiveObjects());
        }

        private IEnumerator TestListActiveObjects()
        {
            DebugUtility.Log<PoolListActiveObjectsTest>("Testing list active objects:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolListActiveObjectsTest>("No PoolData configured for list test.", this);
                yield break;
            }

            var data = poolDataArray[0];
            var poolObject = new GameObject($"TestPool_{data.ObjectName}");
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.SetAllowMultipleGetsInFrame(true);
            pool.Initialize();

            // Ativar alguns objetos
            var spawnedObjects = new List<IPoolable>();
            for (int i = 0; i < 2; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position, null, true);
                if (poolable != null)
                {
                    spawnedObjects.Add(poolable);
                }
                yield return null;
            }

            var activeObjects = pool.GetActiveObjects();
            DebugUtility.Log<PoolListActiveObjectsTest>(
                $"Pool '{data.ObjectName}': {activeObjects.Count} active objects, {pool.GetAvailableCount()} available.", "cyan", this);

            if (activeObjects.Count == spawnedObjects.Count)
            {
                DebugUtility.Log<PoolListActiveObjectsTest>("List test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolListActiveObjectsTest>($"List test failed: Expected {spawnedObjects.Count} active objects, got {activeObjects.Count}.", this);
            }

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}