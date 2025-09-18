using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Error)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();

        private PoolData Data { get; set; }
        public bool IsInitialized { get; private set; }

        private int _lastGetFrame;
        private bool _allowMultipleGetsInFrame = true;
        private bool _hasWarnedExhausted;

        public UnityEvent<IPoolable> OnObjectActivated { get; } = new();
        public UnityEvent<IPoolable> OnObjectReturned { get; } = new();

        public void SetData(PoolData data)
        {
            if (!PoolData.Validate(data, this))
            {
                DebugUtility.LogError<ObjectPool>("Invalid PoolData.", this);
                return;
            }
            Data = data;
        }

        public void SetAllowMultipleGetsInFrame(bool allow) => _allowMultipleGetsInFrame = allow;

        public void Initialize()
        {
            if (!Data)
            {
                DebugUtility.LogError<ObjectPool>("Cannot initialize pool: missing Data.", this);
                return;
            }

            for (int i = 0; i < Data.InitialPoolSize; i++)
            {
                var poolable = CreatePoolable(i, Vector3.zero, null);
                if (poolable != null) _pool.Enqueue(poolable);
            }

            IsInitialized = _pool.Count > 0;
        }

        public IPoolable GetObject(Vector3 position, IActor spawner = null, Vector3? direction = null, bool activateImmediately = true)
        {
            if (!ValidatePool()) return null;
            WarnIfMultipleGetsSameFrame();

            var poolable = RetrieveOrCreate(position, spawner);
            if (poolable == null) return null;

            if (activateImmediately)
            {
                ActivatePoolable(poolable, position, direction, spawner);
            }

            return poolable;
        }

        public List<IPoolable> GetMultipleObjects(int count, Vector3 position, IActor spawner = null, Vector3? direction = null, bool activateImmediately = true)
        {
            if (!ValidatePool()) return new();

            var result = new List<IPoolable>();
            int maxCount = Data.CanExpand ? count : Mathf.Min(count, _pool.Count);

            for (int i = 0; i < maxCount; i++)
            {
                var poolable = RetrieveOrCreate(position, spawner);
                if (poolable == null) break;

                if (activateImmediately)
                {
                    ActivatePoolable(poolable, position, direction, spawner);
                }

                result.Add(poolable);
            }

            if (result.Count > 0) _hasWarnedExhausted = false;
            return result;
        }

        public void ActivateObject(IPoolable poolable, Vector3 position, Vector3? direction = null, IActor spawner = null)
        {
            if (poolable == null || _activeObjects.Contains(poolable)) return;

            ActivatePoolable(poolable, position, direction, spawner);
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Contains(poolable)) return;

            _activeObjects.Remove(poolable);
            if (poolable.GetGameObject().activeSelf)
                poolable.Deactivate();

            poolable.GetGameObject().transform.SetParent(transform);
            _pool.Enqueue(poolable);
            OnObjectReturned.Invoke(poolable);

            if (Data.ReconfigureOnReturn) ReconfigureObject(poolable);

            _hasWarnedExhausted = false;
        }

        public void ExpandPool(int additionalCount)
        {
            if (!Data.CanExpand)
            {
                DebugUtility.LogWarning<ObjectPool>($"Cannot expand pool '{Data.ObjectName}', expansion disabled.", this);
                return;
            }

            for (int i = 0; i < additionalCount; i++)
            {
                var poolable = CreatePoolable(_activeObjects.Count + _pool.Count, Vector3.zero, null);
                if (poolable != null) _pool.Enqueue(poolable);
            }
        }

        public void ClearPool()
        {
            foreach (var obj in _activeObjects) DestroyObject(obj);
            _activeObjects.Clear();

            foreach (var obj in _pool) DestroyObject(obj);
            _pool.Clear();

            IsInitialized = false;
        }

        private IPoolable RetrieveOrCreate(Vector3 position, IActor spawner)
        {
            if (_pool.Count > 0) return PreparePoolable(_pool.Dequeue());

            if (Data.CanExpand)
            {
                return CreatePoolable(_activeObjects.Count + _pool.Count, position, spawner);
            }

            if (!_hasWarnedExhausted)
            {
                DebugUtility.LogWarning<ObjectPool>($"Pool '{Data.ObjectName}' exhausted.", this);
                _hasWarnedExhausted = true;
            }

            return null;
        }

        private IPoolable CreatePoolable(int index, Vector3 position, IActor spawner)
        {
            var config = GetConfigForIndex(index);

            if (config == null || config.Prefab == null)
            {
                DebugUtility.LogError<ObjectPool>($"Invalid PoolableObjectData or null Prefab in '{config?.ObjectName}'.", this);
                return null;
            }

            if (config.Prefab.GetComponent<IPoolable>() == null)
            {
                DebugUtility.LogError<ObjectPool>($"Prefab '{config.Prefab.name}' does not implement IPoolable.", this);
                return null;
            }

            var instance = Instantiate(config.Prefab, position, Quaternion.identity, transform);
            instance.name = $"{Data.ObjectName}_{index}";
            instance.SetActive(false);

            var poolable = instance.GetComponent<IPoolable>();
            poolable.Configure(config, this, spawner);

            DebugUtility.LogVerbose<ObjectPool>($"Created pooled object '{instance.name}' with config '{config.ObjectName}'.", "cyan", this);

            return poolable;
        }

        private IPoolable PreparePoolable(IPoolable poolable)
        {
            if (poolable == null) return null;
            poolable.PoolableReset();
            return poolable;
        }

        private void ActivatePoolable(IPoolable poolable, Vector3 position, Vector3? direction, IActor spawner)
        {
            poolable.Activate(new Vector3(position.x, 0, position.z), direction, spawner);
            _activeObjects.Add(poolable);
            OnObjectActivated.Invoke(poolable);
        }

        private void ReconfigureObject(IPoolable poolable)
        {
            var config = GetConfigForIndex(_activeObjects.Count + _pool.Count - 1);
            poolable.Reconfigure(config);
        }

        private void DestroyObject(IPoolable poolable)
        {
            if (poolable == null) return;
            LifetimeManager.Instance?.Unregister(poolable);
            if (poolable.GetGameObject() != null)
                Destroy(poolable.GetGameObject());
        }

        private PoolableObjectData GetConfigForIndex(int index)
        {
            return Data.ReconfigureOnReturn
                ? Data.ObjectConfigs[index % Data.ObjectConfigs.Length]
                : Data.ObjectConfigs[0];
        }

        private bool ValidatePool()
        {
            if (IsInitialized && Data) return true;
            DebugUtility.LogWarning<ObjectPool>($"Pool '{name}' not initialized or missing data.", this);
            return false;
        }

        private void WarnIfMultipleGetsSameFrame()
        {
            if (!_allowMultipleGetsInFrame && Time.frameCount == _lastGetFrame)
            {
                DebugUtility.LogWarning<ObjectPool>("Multiple GetObject calls in the same frame.", this);
            }
            _lastGetFrame = Time.frameCount;
        }
    }
}