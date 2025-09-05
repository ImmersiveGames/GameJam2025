using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolReturnAllObjectsTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);

        private void Start()
        {
            StartCoroutine(TestReturnAllObjects());
        }

        private IEnumerator TestReturnAllObjects()
        {
            DebugUtility.Log<PoolReturnAllObjectsTest>("Testing return all objects:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolReturnAllObjectsTest>("No PoolData configured for return test.", this);
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
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position, null, true);
                if (poolable != null)
                {
                    DebugUtility.Log<PoolReturnAllObjectsTest>(
                        $"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    spawnedObjects.Add(poolable);
                }
                yield return null;
            }

            // Retornar todos os objetos
            foreach (var poolable in spawnedObjects)
            {
                pool.ReturnObject(poolable);
                DebugUtility.Log<PoolReturnAllObjectsTest>(
                    $"Returned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) to pool. Active: {poolable.GetGameObject().activeSelf}", "blue", this);
                yield return null;
            }

            if (pool.GetActiveObjects().Count == 0 && pool.GetAvailableCount() >= spawnedObjects.Count)
            {
                DebugUtility.Log<PoolReturnAllObjectsTest>("Return test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolReturnAllObjectsTest>($"Return test failed: Expected 0 active, {spawnedObjects.Count} available. Got {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", this);
            }

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}