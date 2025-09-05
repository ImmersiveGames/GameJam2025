using System;
using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolSpawnerTrackerTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        private IActor _mockSpawner;

        private void Awake()
        {
            _mockSpawner = gameObject.GetOrAdd<MockActor>();
        }
        private void Start()
        {
            StartCoroutine(TestSpawnerTracker());
        }

        private IEnumerator TestSpawnerTracker()
        {
            DebugUtility.Log<PoolSpawnerTrackerTest>("Testing SpawnerTracker:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolSpawnerTrackerTest>("No PoolData configured for SpawnerTracker test.", this);
                yield break;
            }

            var data = poolDataArray[0];
            var poolObject = new GameObject($"TestPool_{data.ObjectName}");
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.SetAllowMultipleGetsInFrame(true); // Permitir múltiplas chamadas no mesmo frame
            pool.Initialize();

            // Teste de ativação com spawner
            var poolable = pool.GetObject(Vector3.zero, _mockSpawner, true);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolSpawnerTrackerTest>($"Failed to spawn object from pool '{data.ObjectName}' for SpawnerTracker test.", this);
                Destroy(poolObject);
                yield break;
            }

            // Verificar se o objeto está ativo
            bool isActive = poolable.GetGameObject().activeSelf;
            DebugUtility.Log<PoolSpawnerTrackerTest>(
                $"Object activated from pool '{data.ObjectName}' at {Vector3.zero} with spawner {(_mockSpawner != null ? _mockSpawner.ToString() : "null")}. " +
                $"Active: {isActive}", "green", this);

            // Retornar o objeto ao pool
            pool.ReturnObject(poolable);
            yield return null;

            // Verificar se o objeto foi resetado e está inativo
            bool isInPool = !poolable.GetGameObject().activeSelf && pool.GetAvailableCount() > 0;
            DebugUtility.Log<PoolSpawnerTrackerTest>(
                $"After return, object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}, Name: {poolable.GetGameObject().name}). " +
                $"Active: {poolable.GetGameObject().activeSelf}, In pool: {isInPool}", "cyan", this);

            if (!isInPool)
            {
                DebugUtility.LogError<PoolSpawnerTrackerTest>(
                    $"Object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}, Name: {poolable.GetGameObject().name}) not properly returned to pool!", this);
            }
            else
            {
                DebugUtility.Log<PoolSpawnerTrackerTest>("SpawnerTracker test passed.", "green", this);
            }

            Destroy(poolObject);
        }
    }
}