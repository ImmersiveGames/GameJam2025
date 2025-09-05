using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolReconfigurationTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;

        private void Start()
        {
            StartCoroutine(TestReconfiguration());
        }

        private IEnumerator TestReconfiguration()
        {
            DebugUtility.Log<PoolReconfigurationTest>("Testing reconfiguration:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolReconfigurationTest>("No PoolData configured for reconfiguration test.", this);
                yield break;
            }

            var data = poolDataArray[0];
            var poolObject = new GameObject($"TestPool_{data.ObjectName}");
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.SetAllowMultipleGetsInFrame(true);
            pool.Initialize();

            DebugUtility.Log<PoolReconfigurationTest>($"ReconfigureOnReturn: {data.ReconfigureOnReturn}", "cyan", this);

            var originalConfig = data.ObjectConfigs[0];
            var poolable = pool.GetObject(Vector3.zero, null, true);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolReconfigurationTest>($"Failed to get object from pool '{data.ObjectName}'.", this);
                pool.ClearPool();
                Destroy(poolObject);
                yield break;
            }

            pool.ReturnObject(poolable);
            yield return null;

            var newPoolable = pool.GetObject(Vector3.zero, null, true);
            if (newPoolable == null)
            {
                DebugUtility.LogError<PoolReconfigurationTest>($"Failed to get object from pool '{data.ObjectName}' after return.", this);
                pool.ClearPool();
                Destroy(poolObject);
                yield break;
            }

            bool configsMatch = true;
            if (!data.ReconfigureOnReturn)
            {
                var newConfig = pool.GetData().ObjectConfigs[0];
                configsMatch = originalConfig.Equals(newConfig);
                DebugUtility.Log<PoolReconfigurationTest>(
                    $"Reconfiguration test for '{data.ObjectName}': Original config: {originalConfig.name} (Lifetime: {originalConfig.Lifetime}), " +
                    $"New config: {newConfig.name} (Lifetime: {newConfig.Lifetime}), Different: {!configsMatch}", "cyan", this);
            }

            if (!configsMatch)
            {
                DebugUtility.LogError<PoolReconfigurationTest>("Reconfiguration test failed: Expected same config without reconfiguration.", this);
            }
            else
            {
                DebugUtility.Log<PoolReconfigurationTest>("Reconfiguration test passed.", "green", this);
            }

            // Limpar o pool e desregistrar objetos antes de destruir
            pool.ClearPool();
            yield return null; // Aguardar um frame para garantir que LifetimeManager processe
            Destroy(poolObject);
        }
    }
}