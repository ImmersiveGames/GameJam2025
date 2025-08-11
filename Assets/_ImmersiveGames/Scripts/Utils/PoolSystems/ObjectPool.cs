using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ObjectPool : MonoBehaviour
    {
        private readonly Queue<IPoolable> _pool = new();
        private readonly List<IPoolable> _activeObjects = new();
        private PoolData Data { get; set; }
        public bool IsInitialized { get; private set; }
        private readonly ObjectPoolFactory _factory = new();
        private float _lastGetObjectTime;

        public UnityEvent<IPoolable> OnObjectActivated { get; } = new UnityEvent<IPoolable>();
        public UnityEvent<IPoolable> OnObjectReturned { get; } = new UnityEvent<IPoolable>();

        public void SetData(PoolData data)
        {
            if (!ValidationService.ValidatePoolData(data, this))
                return;
            Data = data;
        }

        public void Initialize()
        {
            if (!Data)
            {
                DebugUtility.LogError<ObjectPool>("Data not set for pool.", this);
                return;
            }

            for (int i = 0; i < Data.InitialPoolSize; i++)
            {
                var config = Data.ObjectConfigs[i % Data.ObjectConfigs.Length];
                var poolable = _factory.CreateObject(config, transform, Vector3.zero, $"{Data.ObjectName}_{i}", this);
                if (poolable == null) continue;
                _pool.Enqueue(poolable);
            }
            IsInitialized = true;
            DebugUtility.Log<ObjectPool>($"Pool initialized with {_pool.Count} objects for '{Data.ObjectName}'.", "green", this);
        }

        public IPoolable GetObject(Vector3 position, IActor actor = null)
        {
            if (!IsInitialized || !Data) return null;

            if (Time.time == _lastGetObjectTime)
            {
                DebugUtility.LogWarning<ObjectPool>("Multiple GetObject calls in the same frame, skipping.", this);
                return null;
            }
            _lastGetObjectTime = Time.time;

            IPoolable poolable = null;
            if (_pool.Count > 0)
            {
                poolable = _pool.Dequeue();
            }
            else if (Data.CanExpand)
            {
                var configIndex = (_activeObjects.Count + _pool.Count) % Data.ObjectConfigs.Length;
                var config = Data.ObjectConfigs[configIndex];
                poolable = _factory.CreateObject(config, transform, position, $"{Data.ObjectName}_{_activeObjects.Count + _pool.Count}", this);
            }
            else
            {
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                DebugUtility.LogWarning<ObjectPool>($"Pool '{Data.ObjectName}' exhausted. No objects available.", this);
                return null;
            }

            if (poolable == null)
            {
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                return null;
            }

            poolable.PoolableReset();
            poolable.Activate(position, actor);
            _activeObjects.Add(poolable);
            OnObjectActivated.Invoke(poolable);
            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' activated. Active objects: {_activeObjects.Count}, Available: {_pool.Count}", "green", this);
            return poolable;
        }

        public List<IPoolable> GetMultipleObjects(int count, Vector3 position, IActor actor = null)
        {
            if (!IsInitialized || !Data) return new List<IPoolable>();

            var result = new List<IPoolable>();
            for (int i = 0; i < count; i++)
            {
                var poolable = GetObject(position, actor);
                if (poolable == null) break;
                result.Add(poolable);
            }
            DebugUtility.Log<ObjectPool>($"Retrieved {result.Count} objects from pool '{Data.ObjectName}'.", "green", this);
            return result;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (poolable == null || !_activeObjects.Contains(poolable)) return;

            _activeObjects.Remove(poolable);
            if (poolable.GetGameObject().activeSelf)
            {
                poolable.Deactivate();
            }
            poolable.GetGameObject().transform.SetParent(transform);
            _pool.Enqueue(poolable);
            OnObjectReturned.Invoke(poolable);
            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' returned to pool. Active: {poolable.GetGameObject().activeSelf}, Available now: {_pool.Count}", "blue", this);
        }

        public void ClearPool()
        {
            foreach (var obj in _activeObjects.ToList())
            {
                if (obj != null && obj.GetGameObject() != null)
                {
                    if (obj.GetGameObject().activeSelf)
                    {
                        obj.Deactivate();
                    }
                    obj.GetGameObject().transform.SetParent(transform);
                }
            }
            _activeObjects.Clear();
            _pool.Clear();
            IsInitialized = false;
        }

        public IReadOnlyList<IPoolable> GetActiveObjects() => _activeObjects.AsReadOnly();
        public int GetAvailableCount() => _pool.Count;
    }
}