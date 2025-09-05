using System;
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
        private IObjectPoolFactory _factory = new ObjectPoolFactory();
        private float _lastGetObjectTime;
        private bool _allowMultipleGetsInFrame; // Nova flag para testes

        public UnityEvent<IPoolable> OnObjectActivated { get; } = new UnityEvent<IPoolable>();
        public UnityEvent<IPoolable> OnObjectReturned { get; } = new UnityEvent<IPoolable>();

        // Propriedade para ativar/desativar verificação de múltiplas chamadas no mesmo frame (para testes)
        public void SetAllowMultipleGetsInFrame(bool allow)
        {
            _allowMultipleGetsInFrame = allow;
        }

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

        public IPoolable GetObject(Vector3 position, IActor spawner = null, bool activateImmediately = true)
        {
            if (!IsInitialized || !Data)
            {
                DebugUtility.LogWarning<ObjectPool>($"Pool '{name}' not initialized or data missing.", this);
                return null;
            }

            if (!_allowMultipleGetsInFrame && Mathf.Approximately(Time.time, _lastGetObjectTime))
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
                // Usar config fixa se ReconfigureOnReturn for false
                var config = Data.ReconfigureOnReturn ? 
                    Data.ObjectConfigs[(_activeObjects.Count + _pool.Count) % Data.ObjectConfigs.Length] : 
                    Data.ObjectConfigs[0];
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
            if (activateImmediately)
            {
                poolable.Activate(position, spawner);
                _activeObjects.Add(poolable);
                OnObjectActivated.Invoke(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' activated. Active objects: {_activeObjects.Count}, Available: {_pool.Count}", "green", this);
            }
            return poolable;
        }

        public List<IPoolable> GetMultipleObjects(int count, Vector3 position, IActor spawner = null, bool activateImmediately = true)
        {
            if (!IsInitialized || !Data)
            {
                DebugUtility.LogWarning<ObjectPool>($"Pool '{name}' not initialized or data missing.", this);
                return new List<IPoolable>();
            }

            var result = new List<IPoolable>();
            count = Mathf.Max(1, Mathf.Min(count, _pool.Count + (Data.CanExpand ? int.MaxValue : 0)));
            for (int i = 0; i < count; i++)
            {
                IPoolable poolable = null;
                if (_pool.Count > 0)
                {
                    poolable = _pool.Dequeue();
                }
                else if (Data.CanExpand)
                {
                    // Usar config fixa se ReconfigureOnReturn for false
                    var config = Data.ReconfigureOnReturn ? 
                        Data.ObjectConfigs[(_activeObjects.Count + _pool.Count) % Data.ObjectConfigs.Length] : 
                        Data.ObjectConfigs[0];
                    poolable = _factory.CreateObject(config, transform, position, $"{Data.ObjectName}_{_activeObjects.Count + _pool.Count}", this);
                }

                if (poolable == null)
                {
                    EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(Data.ObjectName));
                    DebugUtility.LogWarning<ObjectPool>($"Pool '{Data.ObjectName}' exhausted during GetMultipleObjects at attempt {i + 1}.", this);
                    break;
                }

                poolable.PoolableReset();
                if (activateImmediately)
                {
                    poolable.Activate(position, spawner);
                    _activeObjects.Add(poolable);
                    OnObjectActivated.Invoke(poolable);
                }
                result.Add(poolable);
                DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' retrieved in GetMultipleObjects. Active objects: {_activeObjects.Count}, Available: {_pool.Count}", "green", this);
            }

            DebugUtility.Log<ObjectPool>($"Retrieved {result.Count} objects from pool '{Data.ObjectName}' in GetMultipleObjects.", "green", this);
            return result;
        }

        public void ActivateObject(IPoolable poolable, Vector3 position, IActor spawner = null)
        {
            if (poolable == null || _activeObjects.Contains(poolable)) return;
            poolable.Activate(position, spawner);
            _activeObjects.Add(poolable);
            OnObjectActivated.Invoke(poolable);
            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' activated at {position}. Active objects: {_activeObjects.Count}", "green", this);
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
            EventBus<PoolObjectReturnedEvent>.Raise(new PoolObjectReturnedEvent(Data.ObjectName, poolable));

            if (Data.ReconfigureOnReturn)
            {
                var configIndex = (_activeObjects.Count + _pool.Count - 1) % Data.ObjectConfigs.Length;
                var config = Data.ObjectConfigs[configIndex];
                poolable.Reconfigure(config);
            }
            else
            {
                // Garantir que o objeto mantém a configuração original
                poolable.Reconfigure(Data.ObjectConfigs[0]);
            }

            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' returned to pool. Available now: {_pool.Count}", "blue", this);
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
        public PoolData GetData() => Data;
    }
}