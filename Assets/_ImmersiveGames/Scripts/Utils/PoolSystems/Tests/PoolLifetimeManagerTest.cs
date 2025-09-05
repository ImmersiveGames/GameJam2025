using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolLifetimeManagerTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        private readonly IActor _mockSpawner = new MockActor();

        private void Start()
        {
            StartCoroutine(TestLifetimeManager());
        }

        private IEnumerator TestLifetimeManager()
        {
            DebugUtility.Log<PoolLifetimeManagerTest>("Testing LifetimeManager:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolLifetimeManagerTest>("No PoolData configured for LifetimeManager test.", this);
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
                DebugUtility.LogError<PoolLifetimeManagerTest>($"Failed to spawn object from pool '{data.ObjectName}' for LifetimeManager test.", this);
                pool.ClearPool();
                Destroy(poolObject);
                yield break;
            }

            int instanceID = poolable.GetGameObject().GetInstanceID();
            string objectName = poolable.GetGameObject().name;
            DebugUtility.Log<PoolLifetimeManagerTest>(
                $"Spawned object '{data.ObjectName}' (ID: {instanceID}, Name: {objectName}) for LifetimeManager test. Active: {poolable.GetGameObject().activeSelf}", "green", this);

            // Aguardar o lifetime (assumindo que o lifetime é configurado em ObjectConfigs)
            float lifetime = data.ObjectConfigs[0].Lifetime;
            yield return new WaitForSeconds(lifetime + 0.5f);

            bool isInPool = pool.GetAvailableCount() > 0 && !poolable.GetGameObject().activeSelf;
            DebugUtility.Log<PoolLifetimeManagerTest>(
                $"After lifetime ({lifetime}s), checking object '{data.ObjectName}' (ID: {instanceID}, Name: {objectName}). Active: {poolable.GetGameObject().activeSelf}, In pool: {isInPool}", "cyan", this);

            if (isInPool)
            {
                DebugUtility.Log<PoolLifetimeManagerTest>("LifetimeManager test passed.", "green", this);
            }
            else
            {
                DebugUtility.LogError<PoolLifetimeManagerTest>($"LifetimeManager test failed: Object '{data.ObjectName}' (ID: {instanceID}, Name: {objectName}) not returned to pool after lifetime.", this);
            }

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}