using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolStateTransitionsTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        private readonly IActor _mockSpawner = new MockActor();

        private void Start()
        {
            StartCoroutine(TestStateTransitions());
        }

        private IEnumerator TestStateTransitions()
        {
            DebugUtility.Log<PoolStateTransitionsTest>("Testing state transitions:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolStateTransitionsTest>("No PoolData configured for state transitions test.", this);
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
            DebugUtility.Log<PoolStateTransitionsTest>($"Pool '{data.ObjectName}' reset: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);

            // Ativar objeto
            var poolable = pool.GetObject(new Vector3(-2f, 0f, 0f), _mockSpawner, true);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolStateTransitionsTest>($"Failed to spawn object from pool '{data.ObjectName}'.", this);
                pool.ClearPool();
                Destroy(poolObject);
                yield break;
            }
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Spawned object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) at (-2, 0, 0). Active: {poolable.GetGameObject().activeSelf}", "green", this);

            // Tentar ativar novamente (deve falhar)
            pool.ActivateObject(poolable, new Vector3(-2f, 0f, 0f));
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Attempted to activate object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) again.", "cyan", this);

            // Desativar
            pool.ReturnObject(poolable);
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Deactivated object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}). Active: {poolable.GetGameObject().activeSelf}", "cyan", this);

            // Reativar
            poolable = pool.GetObject(new Vector3(-2f, 0f, 0f), _mockSpawner, true);
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Reactivated object '{data.ObjectName}' (ID: {poolable.GetGameObject().GetInstanceID()}) at (-2, 0, 0). Active: {poolable.GetGameObject().activeSelf}", "green", this);

            // Ativar outro objeto
            var poolable2 = pool.GetObject(new Vector3(2f, 0f, 0f), _mockSpawner, true);
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Spawned object '{data.ObjectName}' (ID: {poolable2.GetGameObject().GetInstanceID()}) at (2, 0, 0). Active: {poolable2.GetGameObject().activeSelf}", "green", this);

            // Desativar e reativar em nova posição
            pool.ReturnObject(poolable2);
            poolable2 = pool.GetObject(new Vector3(2f, 0f, 2f), _mockSpawner, true);
            DebugUtility.Log<PoolStateTransitionsTest>(
                $"Reactivated object '{data.ObjectName}' (ID: {poolable2.GetGameObject().GetInstanceID()}) at (2, 0, 2). Active: {poolable2.GetGameObject().activeSelf}", "green", this);

            DebugUtility.Log<PoolStateTransitionsTest>("State transition test passed.", "green", this);

            pool.ClearPool();
            yield return null;
            Destroy(poolObject);
        }
    }
}