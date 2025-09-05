using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolExhaustTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
        private readonly IActor _mockSpawner = new MockActor();

        private void Start()
        {
            StartCoroutine(TestExhaustPool());
        }

        private IEnumerator TestExhaustPool()
        {
            DebugUtility.Log<PoolExhaustTest>($"Testing pool exhaustion for '{poolDataArray[0].ObjectName}' with 10 attempts (InitialPoolSize: {poolDataArray[0].InitialPoolSize}, CanExpand: {poolDataArray[0].CanExpand}).", "yellow", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolExhaustTest>("No PoolData configured for exhaust test.", this);
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
            DebugUtility.Log<PoolExhaustTest>($"Pool '{data.ObjectName}' reset: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);

            var spawnedObjects = new List<IPoolable>();
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position, _mockSpawner, true);
                if (poolable != null)
                {
                    DebugUtility.Log<PoolExhaustTest>(
                        $"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) for exhaust test at position {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    spawnedObjects.Add(poolable);
                }
                else
                {
                    DebugUtility.Log<PoolExhaustTest>($"Failed to spawn object from pool '{data.ObjectName}' on attempt {i + 1} (expected after {data.InitialPoolSize} spawns).", "yellow", this);
                }
                DebugUtility.LogVerbose<PoolExhaustTest>($"Pool '{data.ObjectName}' status: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);
                yield return null;
            }

            bool poolExhausted = pool.GetAvailableCount() == 0 && pool.GetActiveObjects().Count == data.InitialPoolSize;
            if (poolExhausted)
            {
                DebugUtility.Log<PoolExhaustTest>("Exhaust test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolExhaustTest>($"Exhaust test failed: Expected {data.InitialPoolSize} active, 0 available. Got {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", this);
            }

            foreach (var poolable in spawnedObjects)
            {
                pool.ReturnObject(poolable);
                DebugUtility.Log<PoolExhaustTest>(
                    $"Returned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) to pool after exhaustion test. Active: {poolable.GetGameObject().activeSelf}", "blue", this);
                yield return null;
            }

            DebugUtility.Log<PoolExhaustTest>($"Pool '{data.ObjectName}' restored: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "green", this);

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}