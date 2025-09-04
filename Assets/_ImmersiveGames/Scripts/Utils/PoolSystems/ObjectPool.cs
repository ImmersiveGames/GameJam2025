using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ObjectPool : MonoBehaviour
    {
        private PoolData _data;
        private readonly List<IPoolable> _availableObjects = new();
        private readonly List<IPoolable> _activeObjects = new();
        private bool _isInitialized;
        private ObjectPoolFactory _factory;

        public bool IsInitialized => _isInitialized;
        public string PoolKey => _data.ObjectName;

        public void SetData(PoolData data)
        {
            _data = data;
            _factory = new ObjectPoolFactory(); // Usa factory padrão
        }

        public PoolData GetData() => _data;

        public void Initialize()
        {
            if (_data == null || !ValidationService.ValidatePoolData(_data, this))
            {
                DebugUtility.LogError<ObjectPool>("Cannot initialize pool with invalid PoolData.", this);
                return;
            }

            if (_isInitialized)
            {
                DebugUtility.LogWarning<ObjectPool>("Pool already initialized.", this);
                return;
            }

            foreach (var config in _data.ObjectConfigs)
            {
                for (int i = 0; i < _data.InitialPoolSize / _data.ObjectConfigs.Length; i++)
                {
                    var poolableObject = CreatePoolableObject(config);
                    if (poolableObject != null)
                    {
                        _availableObjects.Add(poolableObject);
                    }
                }
            }

            _isInitialized = true;
            DebugUtility.Log<ObjectPool>($"Pool initialized with {_availableObjects.Count} objects for '{_data.ObjectName}'.", "green", this);
        }

        private IPoolable CreatePoolableObject(PoolableObjectData config)
        {
            var go = _factory.CreateObject(config, transform, Vector3.zero, $"{config.ObjectName}_{_availableObjects.Count}", this);
            if (go == null)
            {
                DebugUtility.LogError<ObjectPool>($"Failed to create object for config '{config.ObjectName}'.", this);
                return null;
            }

            var poolable = go.GetComponent<IPoolable>();
            if (poolable == null)
            {
                DebugUtility.LogError<ObjectPool>($"Object '{go.name}' does not implement IPoolable.", this);
                Destroy(go);
                return null;
            }

            poolable.Configure(config, this);
            go.SetActive(false);
            EventBus<ObjectCreatedEvent>.Raise(new ObjectCreatedEvent(poolable, config, this));
            return poolable;
        }

        public List<IPoolable> GetActiveObjects() => new List<IPoolable>(_activeObjects);

        public int GetAvailableCount() => _availableObjects.Count;

        public IPoolable GetObject(Vector3 position, IActor actor = null, bool activateImmediately = true)
        {
            if (!_isInitialized)
            {
                DebugUtility.LogError<ObjectPool>("Pool not initialized.", this);
                return null;
            }

            if (_availableObjects.Count == 0)
            {
                if (_data.CanExpand)
                {
                    var config = _data.ObjectConfigs[Random.Range(0, _data.ObjectConfigs.Length)];
                    var poolable = CreatePoolableObject(config);
                    if (poolable != null)
                    {
                        _availableObjects.Add(poolable);
                    }
                }
                else
                {
                    DebugUtility.LogWarning<ObjectPool>($"Pool '{name}' exhausted.", this);
                    EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(name));
                    return null;
                }
            }

            var poolable = _availableObjects[0];
            _availableObjects.RemoveAt(0);
            _activeObjects.Add(poolable);

            if (activateImmediately)
            {
                ActivateObject(poolable, position, actor);
            }

            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' retrieved in GetObject. Active objects: {_activeObjects.Count}, Available: {_availableObjects.Count}", "green", this);
            return poolable;
        }

        public List<IPoolable> GetMultipleObjects(int count, Vector3 position, IActor actor = null, bool activateImmediately = true)
        {
            var poolables = new List<IPoolable>();
            for (int i = 0; i < count && (_availableObjects.Count > 0 || _data.CanExpand); i++)
            {
                var poolable = GetObject(position, actor, activateImmediately);
                if (poolable != null)
                {
                    poolables.Add(poolable);
                }
            }

            if (poolables.Count > 0)
            {
                DebugUtility.Log<ObjectPool>($"Retrieved {poolables.Count} objects from pool '{name}' in GetMultipleObjects.", "green", this);
                if (_availableObjects.Count == 0 && !_data.CanExpand)
                {
                    EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(name));
                }
            }

            return poolables;
        }

        public void ReturnObject(IPoolable poolable)
        {
            if (!_activeObjects.Contains(poolable))
            {
                DebugUtility.LogWarning<ObjectPool>($"Object '{poolable.GetGameObject().name}' is not active in pool '{name}'.", this);
                return;
            }

            _activeObjects.Remove(poolable);
            _availableObjects.Add(poolable);
            poolable.GetGameObject().SetActive(false);
            if (_data.ReconfigureOnReturn)
            {
                var config = _data.ObjectConfigs[Random.Range(0, _data.ObjectConfigs.Length)];
                _factory.ReinitializeObject(poolable, config);
            }
            EventBus<PoolObjectReturnedEvent>.Raise(new PoolObjectReturnedEvent(name, poolable));
            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' returned to pool '{name}'. Active objects: {_activeObjects.Count}, Available: {_availableObjects.Count}", "blue", this);

            if (_availableObjects.Count == 1)
            {
                EventBus<PoolRestoredEvent>.Raise(new PoolRestoredEvent(name));
            }
        }

        public void ActivateObject(IPoolable poolable, Vector3 position, IActor actor = null)
        {
            if (!_activeObjects.Contains(poolable))
            {
                DebugUtility.LogWarning<ObjectPool>($"Object '{poolable.GetGameObject().name}' is not active in pool '{name}'. Cannot activate.", this);
                return;
            }

            poolable.GetGameObject().transform.position = position;
            poolable.Activate(position, actor);
            DebugUtility.LogVerbose<ObjectPool>($"Object '{poolable.GetGameObject().name}' activated at {position}. Active objects: {_activeObjects.Count}", "green", this);
        }

        public void ClearPool()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj.GetGameObject() != null)
                {
                    Destroy(obj.GetGameObject());
                }
            }

            foreach (var obj in _availableObjects)
            {
                if (obj.GetGameObject() != null)
                {
                    Destroy(obj.GetGameObject());
                }
            }

            _activeObjects.Clear();
            _availableObjects.Clear();
            _isInitialized = false;
            DebugUtility.Log<ObjectPool>($"Pool '{name}' cleared.", "cyan", this);
        }
    }
}