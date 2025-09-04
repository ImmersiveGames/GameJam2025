using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolTest : MonoBehaviour
    {
        [SerializeField] private PoolData[] poolDataArray;
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
        [SerializeField, Min(1)] private int multipleSpawnCount = 3;

        private List<string> _validPoolKeys;
        private EventBinding<PoolExhaustedEvent> _poolExhaustedBinding;
        private EventBinding<PoolRestoredEvent> _poolRestoredBinding;
        private bool _testPassed = true;

        private void OnEnable()
        {
            _poolExhaustedBinding = new EventBinding<PoolExhaustedEvent>(OnPoolExhausted);
            EventBus<PoolExhaustedEvent>.Register(_poolExhaustedBinding);
            _poolRestoredBinding = new EventBinding<PoolRestoredEvent>(OnPoolRestored);
            EventBus<PoolRestoredEvent>.Register(_poolRestoredBinding);
        }

        private void OnDisable()
        {
            EventBus<PoolExhaustedEvent>.Unregister(_poolExhaustedBinding);
            EventBus<PoolRestoredEvent>.Unregister(_poolRestoredBinding);
        }

        private void Start()
        {
            _validPoolKeys = new List<string>();
            InitializePools();
            StartCoroutine(RunUnifiedTest());
        }

        private void InitializePools()
        {
            if (LifetimeManager.Instance == null)
            {
                DebugUtility.LogError<PoolTest>("LifetimeManager not found in scene.", this);
                enabled = false;
                return;
            }

            if (PoolManager.Instance == null)
            {
                DebugUtility.LogError<PoolTest>("PoolManager not found in scene.", this);
                enabled = false;
                return;
            }

            if (poolDataArray == null || poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolTest>("poolDataArray is empty or not configured.", this);
                enabled = false;
                return;
            }

            foreach (var data in poolDataArray)
            {
                if (data == null || string.IsNullOrEmpty(data.ObjectName))
                {
                    DebugUtility.LogError<PoolTest>("Null PoolData or empty ObjectName found in poolDataArray.", this);
                    continue;
                }

                PoolManager.Instance.RegisterPool(data);
                if (PoolManager.Instance.GetPool(data.ObjectName) != null)
                {
                    _validPoolKeys.Add(data.ObjectName);
                    DebugUtility.Log<PoolTest>($"Registered pool '{data.ObjectName}' with initial size {data.InitialPoolSize}.", "green", this);
                }
                else
                {
                    DebugUtility.LogError<PoolTest>($"Failed to register pool '{data.ObjectName}'.", this);
                }
            }

            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid pools registered. Disabling PoolTest.", this);
                enabled = false;
            }
        }

        private IEnumerator RunUnifiedTest()
        {
            DebugUtility.Log<PoolTest>("Starting unified pool test.", "cyan", this);
            yield return StartCoroutine(TestValidationAndCache());
            yield return StartCoroutine(TestSpawnRandom());
            yield return StartCoroutine(TestReturnAllObjects());
            yield return StartCoroutine(TestListActiveObjects());
            yield return StartCoroutine(TestSpawnMultiple());
            yield return StartCoroutine(TestStateTransitions());
            yield return StartCoroutine(TestManualDisable());
            yield return StartCoroutine(TestExhaustPool());
            yield return StartCoroutine(TestLifetimeManager());

            DebugUtility.Log<PoolTest>($"Unified pool test completed. All tests passed: {_testPassed}.", _testPassed ? "green" : "red", this);
        }

        private IEnumerator ResetPool(string poolKey)
        {
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool != null)
            {
                pool.ClearPool();
                yield return null; // Separar ClearPool e Initialize
                pool.Initialize();
                DebugUtility.Log<PoolTest>($"Pool '{poolKey}' reset: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);
            }
        }

        private IEnumerator TestValidationAndCache()
        {
            DebugUtility.Log<PoolTest>("Testing validation and cache:", "cyan", this);

            if (poolDataArray.Length == 0)
            {
                DebugUtility.LogError<PoolTest>("No PoolData configured for validation test.", this);
                _testPassed = false;
                yield break;
            }

            bool allValid = true;
            foreach (var data in poolDataArray)
            {
                if (data == null || string.IsNullOrEmpty(data.ObjectName))
                {
                    DebugUtility.LogError<PoolTest>("Null PoolData or empty ObjectName found in poolDataArray.", this);
                    allValid = false;
                    continue;
                }

                var poolObject = new GameObject($"TestPool_{data.ObjectName}");
                var pool = poolObject.AddComponent<ObjectPool>();
                pool.SetData(data);
                pool.Initialize();
                bool isValid = pool.IsInitialized;
                DebugUtility.Log<PoolTest>($"Validation for PoolData '{data.ObjectName}' (Name: {data.name}): {(isValid ? "Valid" : "Invalid")}.", "cyan", this);
                allValid &= isValid;
                Destroy(poolObject);
                yield return null;
            }

            if (!allValid)
            {
                DebugUtility.LogError<PoolTest>("Validation test failed.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Validation test passed.", "green", this);
            }
        }

        private IEnumerator TestSpawnRandom()
        {
            DebugUtility.Log<PoolTest>("Testing random spawn:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for spawn test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for spawn test.", this);
                _testPassed = false;
                yield break;
            }

            bool allSpawned = true;
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position);
                if (poolable != null)
                {
                    DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) at position {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    allSpawned &= poolable.GetGameObject().activeSelf;
                }
                else
                {
                    DebugUtility.LogError<PoolTest>($"Failed to spawn object from pool '{poolKey}'.", this);
                    allSpawned = false;
                }
                yield return null;
            }

            if (!allSpawned || pool.GetActiveObjects().Count != 3)
            {
                DebugUtility.LogError<PoolTest>($"Spawn test failed: Expected 3 active objects, got {pool.GetActiveObjects().Count}.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Spawn test passed.", "green", this);
            }
        }

        private IEnumerator TestReturnAllObjects()
        {
            DebugUtility.Log<PoolTest>("Testing return all objects:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for return test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for return test.", this);
                _testPassed = false;
                yield break;
            }

            var activeObjects = pool.GetActiveObjects().ToList();
            foreach (var obj in activeObjects)
            {
                if (obj != null && obj.GetGameObject() != null)
                {
                    obj.ReturnToPool();
                    DebugUtility.Log<PoolTest>($"Returned object '{poolKey}' (ID: {obj.GetGameObject().GetInstanceID()}) to pool. Active: {obj.GetGameObject().activeSelf}", "blue", this);
                    yield return null;
                }
            }

            if (pool.GetActiveObjects().Count > 0)
            {
                DebugUtility.LogError<PoolTest>($"Return test failed: Expected 0 active objects, got {pool.GetActiveObjects().Count}.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Return test passed.", "green", this);
            }
        }

        private IEnumerator TestListActiveObjects()
        {
            DebugUtility.Log<PoolTest>("Testing list active objects:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for list test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for list test.", this);
                _testPassed = false;
                yield break;
            }

            var activeObjects = pool.GetActiveObjects();
            var availableCount = pool.GetAvailableCount();
            DebugUtility.Log<PoolTest>($"Pool '{poolKey}': {activeObjects.Count} active objects, {availableCount} available.", "cyan", this);

            foreach (var obj in activeObjects)
            {
                if (obj != null && obj.GetGameObject() != null)
                {
                    var objData = obj.Data;
                    DebugUtility.Log<PoolTest>($"- Active object ID: {obj.GetGameObject().GetInstanceID()}, Position: {obj.GetGameObject().transform.position}, Type: {objData.GetType().Name}, Active: {obj.GetGameObject().activeSelf}", "cyan", this);
                }
                else
                {
                    DebugUtility.LogError<PoolTest>($"Null object found in pool '{poolKey}'.", this);
                    _testPassed = false;
                }
            }

            DebugUtility.Log<PoolTest>("List test passed.", "green", this);
            yield return null;
        }

        private IEnumerator TestSpawnMultiple()
        {
            DebugUtility.Log<PoolTest>("Testing multiple spawn:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for multiple spawn test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for multiple spawn test.", this);
                _testPassed = false;
                yield break;
            }

            Vector3 basePosition = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0f,
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            var poolables = new List<IPoolable>();
            for (int i = 0; i < multipleSpawnCount; i++)
            {
                var poolable = pool.GetObject(basePosition);
                if (poolable != null)
                {
                    poolables.Add(poolable);
                }
                yield return null;
            }

            DebugUtility.Log<PoolTest>($"Multiple spawn: Requested {multipleSpawnCount}, retrieved {poolables.Count} objects from pool '{poolKey}'.", "green", this);

            bool allSpawned = poolables.Count == multipleSpawnCount;
            for (int i = 0; i < poolables.Count; i++)
            {
                if (poolables[i] != null && poolables[i].GetGameObject() != null)
                {
                    float angle = i * (360f / poolables.Count);
                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * 1f;
                    Vector3 position = basePosition + offset;
                    poolables[i].GetGameObject().transform.position = position;
                    DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {poolables[i].GetGameObject().GetInstanceID()}) at position {position}. Active: {poolables[i].GetGameObject().activeSelf}", "green", this);
                    allSpawned &= poolables[i].GetGameObject().activeSelf;
                }
                else
                {
                    DebugUtility.LogError<PoolTest>($"Null object in multiple spawn from pool '{poolKey}'.", this);
                    allSpawned = false;
                }
            }

            if (!allSpawned)
            {
                DebugUtility.LogError<PoolTest>("Multiple spawn test failed.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Multiple spawn test passed.", "green", this);
            }
        }

        private IEnumerator TestStateTransitions()
        {
            DebugUtility.Log<PoolTest>("Testing state transitions:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for state transition test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for state transition test.", this);
                _testPassed = false;
                yield break;
            }

            // Primeiro objeto
            var poolable1 = pool.GetObject(new Vector3(-2f, 0f, 0f));
            if (poolable1 == null)
            {
                DebugUtility.LogError<PoolTest>($"Failed to spawn first object from pool '{poolKey}' for state transition test.", this);
                _testPassed = false;
                yield break;
            }
            int id1 = poolable1.GetGameObject().GetInstanceID();
            DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {id1}) at (-2, 0, 0). Active: {poolable1.GetGameObject().activeSelf}", "green", this);
            yield return null;

            poolable1.Activate(new Vector3(-2f, 0f, 0f), null);
            DebugUtility.Log<PoolTest>($"Attempted to activate object '{poolKey}' (ID: {id1}) again.", "cyan", this);
            yield return null;

            poolable1.Deactivate();
            DebugUtility.Log<PoolTest>($"Deactivated object '{poolKey}' (ID: {id1}). Active: {poolable1.GetGameObject().activeSelf}", "cyan", this);
            yield return null;

            poolable1.Activate(new Vector3(-2f, 0f, 0f), null);
            DebugUtility.Log<PoolTest>($"Reactivated object '{poolKey}' (ID: {id1}) at (-2, 0, 0). Active: {poolable1.GetGameObject().activeSelf}", "green", this);
            yield return null;

            // Segundo objeto
            var poolable2 = pool.GetObject(new Vector3(2f, 0f, 0f));
            if (poolable2 == null)
            {
                DebugUtility.LogError<PoolTest>($"Failed to spawn second object from pool '{poolKey}' for state transition test.", this);
                _testPassed = false;
                yield break;
            }
            int id2 = poolable2.GetGameObject().GetInstanceID();
            DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {id2}) at (2, 0, 0). Active: {poolable2.GetGameObject().activeSelf}", "green", this);
            yield return null;

            poolable2.Deactivate();
            DebugUtility.Log<PoolTest>($"Deactivated object '{poolKey}' (ID: {id2}). Active: {poolable2.GetGameObject().activeSelf}", "cyan", this);
            yield return null;

            poolable2.Activate(new Vector3(2f, 0f, 2f), null);
            DebugUtility.Log<PoolTest>($"Reactivated object '{poolKey}' (ID: {id2}) at (2, 0, 2). Active: {poolable2.GetGameObject().activeSelf}", "green", this);
            yield return null;

            // Verificação final
            var activeObjects = pool.GetActiveObjects();
            bool poolable1Active = poolable1.GetGameObject() != null && poolable1.GetGameObject().activeSelf && activeObjects.Contains(poolable1);
            bool poolable2Active = poolable2.GetGameObject() != null && poolable2.GetGameObject().activeSelf && activeObjects.Contains(poolable2);
            if (activeObjects.Count != 2 || !poolable1Active || !poolable2Active)
            {
                DebugUtility.LogError<PoolTest>($"State transition test failed: Expected 2 active objects, got {activeObjects.Count}. Poolable1 active: {poolable1Active}, Poolable2 active: {poolable2Active}", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("State transition test passed.", "green", this);
            }
        }

        private IEnumerator TestManualDisable()
        {
            DebugUtility.Log<PoolTest>("Testing manual disable:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for manual disable test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for manual disable test.", this);
                _testPassed = false;
                yield break;
            }

            var poolable = pool.GetObject(Vector3.zero);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolTest>($"Failed to spawn object from pool '{poolKey}' for manual disable test.", this);
                _testPassed = false;
                yield break;
            }
            DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) for manual disable test. Active: {poolable.GetGameObject().activeSelf}", "green", this);
            yield return null;

            poolable.ReturnToPool();
            DebugUtility.Log<PoolTest>($"Manually returned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) to pool. Active: {poolable.GetGameObject().activeSelf}", "cyan", this);
            yield return null;

            if (pool.GetActiveObjects().Count > 0)
            {
                DebugUtility.LogError<PoolTest>($"Manual disable test failed: Expected 0 active objects, got {pool.GetActiveObjects().Count}.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Manual disable test passed.", "green", this);
            }
        }

        private IEnumerator TestExhaustPool()
        {
            DebugUtility.Log<PoolTest>("Testing pool exhaustion:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for exhaustion test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for exhaustion test.", this);
                _testPassed = false;
                yield break;
            }

            var data = poolDataArray.FirstOrDefault(d => d != null && d.ObjectName == poolKey);
            if (data == null)
            {
                DebugUtility.LogError<PoolTest>($"PoolData for '{poolKey}' not found.", this);
                _testPassed = false;
                yield break;
            }

            DebugUtility.Log<PoolTest>($"Testing pool exhaustion for '{poolKey}' with {data.InitialPoolSize + 5} attempts (InitialPoolSize: {data.InitialPoolSize}, CanExpand: {data.CanExpand}).", "yellow", this);

            bool originalCanExpand = data.CanExpand;
            data.CanExpand = false;
            int successfulSpawns = 0;
            int attempts = data.InitialPoolSize + 5;

            for (int i = 0; i < attempts; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    0f,
                    Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
                );
                var poolable = pool.GetObject(position);
                if (poolable != null)
                {
                    DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {poolable.GetGameObject().GetInstanceID()}) for exhaust test at position {position}. Active: {poolable.GetGameObject().activeSelf}", "green", this);
                    successfulSpawns++;
                }
                else
                {
                    DebugUtility.Log<PoolTest>($"Failed to spawn object from pool '{poolKey}' on attempt {i + 1} (expected after {data.InitialPoolSize} spawns).", "yellow", this);
                }
                DebugUtility.LogVerbose<PoolTest>($"Pool '{poolKey}' status: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "cyan", this);
                yield return null;
            }

            data.CanExpand = originalCanExpand;

            if (successfulSpawns != data.InitialPoolSize)
            {
                DebugUtility.LogError<PoolTest>($"Exhaust test failed: Expected {data.InitialPoolSize} successful spawns, got {successfulSpawns}.", this);
                _testPassed = false;
            }
            else if (pool.GetActiveObjects().Count != data.InitialPoolSize)
            {
                DebugUtility.LogError<PoolTest>($"Exhaust test failed: Expected {data.InitialPoolSize} active objects, got {pool.GetActiveObjects().Count}.", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>("Exhaust test passed.", "green", this);
            }

            foreach (var obj in pool.GetActiveObjects().ToList())
            {
                if (obj != null && obj.GetGameObject() != null)
                {
                    obj.ReturnToPool();
                    DebugUtility.Log<PoolTest>($"Returned object '{poolKey}' (ID: {obj.GetGameObject().GetInstanceID()}) to pool after exhaustion test. Active: {obj.GetGameObject().activeSelf}", "blue", this);
                    yield return null;
                }
            }

            DebugUtility.Log<PoolTest>($"Pool '{poolKey}' restored: {pool.GetActiveObjects().Count} active, {pool.GetAvailableCount()} available.", "green", this);
        }

        private IEnumerator TestLifetimeManager()
        {
            DebugUtility.Log<PoolTest>("Testing LifetimeManager:", "cyan", this);
            if (_validPoolKeys.Count == 0)
            {
                DebugUtility.LogError<PoolTest>("No valid PoolData configured for LifetimeManager test.", this);
                _testPassed = false;
                yield break;
            }

            var poolKey = _validPoolKeys[Random.Range(0, _validPoolKeys.Count)];
            yield return StartCoroutine(ResetPool(poolKey));
            var pool = PoolManager.Instance.GetPool(poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PoolTest>($"Pool '{poolKey}' not found for LifetimeManager test.", this);
                _testPassed = false;
                yield break;
            }

            var poolable = pool.GetObject(Vector3.zero);
            if (poolable == null)
            {
                DebugUtility.LogError<PoolTest>($"Failed to spawn object from pool '{poolKey}' for LifetimeManager test.", this);
                _testPassed = false;
                yield break;
            }

            var objId = poolable.GetGameObject().GetInstanceID();
            var objName = poolable.GetGameObject().name;
            DebugUtility.Log<PoolTest>($"Spawned object '{poolKey}' (ID: {objId}, Name: {objName}) for LifetimeManager test. Active: {poolable.GetGameObject().activeSelf}", "green", this);

            float lifetime = poolable.Data.Lifetime + 0.5f;
            yield return new WaitForSeconds(lifetime);

            if (poolable.GetGameObject() == null)
            {
                DebugUtility.LogError<PoolTest>($"Object '{poolKey}' (ID: {objId}, Name: {objName}) was destroyed during LifetimeManager test!", this);
                _testPassed = false;
                yield break;
            }

            bool isInPool = pool.GetAvailableCount() > 0 && !pool.GetActiveObjects().Contains(poolable);
            DebugUtility.Log<PoolTest>($"After lifetime ({lifetime}s), checking object '{poolKey}' (ID: {objId}, Name: {objName}). Active: {poolable.GetGameObject().activeSelf}, In pool: {isInPool}", "cyan", this);

            if (poolable.GetGameObject().activeSelf)
            {
                DebugUtility.LogError<PoolTest>($"Object '{poolKey}' (ID: {objId}, Name: {objName}) still active after lifetime expiration!", this);
                _testPassed = false;
            }
            else if (pool.GetActiveObjects().Contains(poolable))
            {
                DebugUtility.LogError<PoolTest>($"Object '{poolKey}' (ID: {objId}, Name: {objName}) inactive but still in active objects list!", this);
                _testPassed = false;
            }
            else
            {
                DebugUtility.Log<PoolTest>($"Object '{poolKey}' (ID: {objId}, Name: {objName}) correctly returned to pool after lifetime.", "green", this);
            }
        }

        private void OnPoolExhausted(PoolExhaustedEvent evt)
        {
            DebugUtility.Log<PoolTest>($"Pool '{evt.PoolKey}' exhausted. Expected during exhaust test.", "yellow", this);
        }

        private void OnPoolRestored(PoolRestoredEvent evt)
        {
            DebugUtility.Log<PoolTest>($"Pool '{evt.PoolKey}' restored. Objects available again.", "green", this);
        }
    }
}