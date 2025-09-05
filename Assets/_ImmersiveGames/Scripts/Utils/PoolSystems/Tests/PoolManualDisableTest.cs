using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolManualDisableTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        private readonly IActor _mockSpawner = new MockActor();

        private void Start()
        {
            StartCoroutine(TestManualDisable());
        }

        private IEnumerator TestManualDisable()
        {
            DebugUtility.Log<PoolManualDisableTest>("Testing manual disable:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolManualDisableTest>("No PoolData configured for manual disable test.", this);
                yield break;
            }

            var data = poolDataArray[0];
            var poolObject = new GameObject($"TestPool_{data.ObjectName}");
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.SetAllowMultipleGetsInFrame(true);
            pool.Initialize();

            var poolable = pool.GetObject(Vector3.zero, _mockSpawner, true);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolManualDisableTest>($"Failed to spawn object from pool '{data.ObjectName}' for manual disable test.", this);
                pool.ClearPool();
                Destroy(poolObject);
                yield break;
            }
            DebugUtility.Log<PoolManualDisableTest>(
                $"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) for manual disable test. Active: {poolable.GetGameObject().activeSelf}", "green", this);

            pool.ReturnObject(poolable);
            yield return null;
            DebugUtility.Log<PoolManualDisableTest>(
                $"Manually returned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) to pool. Active: {poolable.GetGameObject().activeSelf}, In pool: {pool.GetAvailableCount() > 0}", "cyan", this);

            if (!poolable.GetGameObject().activeSelf && pool.GetAvailableCount() > 0)
            {
                DebugUtility.Log<PoolManualDisableTest>("Manual disable test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolManualDisableTest>("Manual disable test failed.", this);
            }

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}