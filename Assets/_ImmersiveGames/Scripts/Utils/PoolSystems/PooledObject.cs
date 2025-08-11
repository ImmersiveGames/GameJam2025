using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PooledObject : MonoBehaviour, IPoolable
    {
        public PoolableObjectData Data { get; private set; }
        public UnityEvent OnActivated { get; } = new UnityEvent();
        public UnityEvent OnDeactivated { get; } = new UnityEvent();
        public IActor Spawner { get; private set; }
        private ObjectPool _pool;
        private bool _isInitialized;

        public void Initialize(PoolableObjectData data, ObjectPool pool, IActor actor = null)
        {
            if (_isInitialized)
            {
                DebugUtility.LogWarning<PooledObject>($"Object '{name}' already initialized.", this);
                return;
            }

            if (!ValidationService.ValidatePoolableObjectData(data, this))
            {
                DebugUtility.LogError<PooledObject>($"Invalid PoolableObjectData for '{name}'.", this);
                return;
            }

            Data = data;
            _pool = pool;
            Spawner = actor;
            _isInitialized = true;

            gameObject.SetActive(false);
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' initialized with lifetime {data.Lifetime}.", "green", this);
        }

        public void Activate(Vector3 position, IActor actor)
        {
            if (!_isInitialized)
            {
                DebugUtility.LogError<PooledObject>($"Cannot activate '{name}': not initialized.", this);
                return;
            }

            Spawner = actor;
            transform.position = position;
            gameObject.SetActive(true);
            LifetimeManager.Instance.RegisterObject(this, Data.Lifetime);
            OnActivated.Invoke();
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' activated at {position} by actor {Spawner?.GetType().Name ?? "null"}.", "green", this);
        }

        public void Deactivate()
        {
            if (!_isInitialized)
            {
                DebugUtility.LogError<PooledObject>($"Cannot deactivate '{name}': not initialized.", this);
                return;
            }

            if (!gameObject.activeSelf)
            {
                DebugUtility.LogVerbose<PooledObject>($"Object '{name}' already deactivated.", "blue", this);
                return;
            }

            gameObject.SetActive(false);
            LifetimeManager.Instance.UnregisterObject(this);
            OnDeactivated.Invoke();
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' deactivated (set inactive). Active: {gameObject.activeSelf}", "blue", this);
        }

        public void PoolableReset()
        {
            if (!_isInitialized)
            {
                DebugUtility.LogError<PooledObject>($"Cannot reset '{name}': not initialized.", this);
                return;
            }

            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            Spawner = null;
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' reset.", "green", this);
        }

        public void ReturnToPool()
        {
            if (!_isInitialized || _pool == null)
            {
                DebugUtility.LogError<PooledObject>($"Cannot return '{name}' to pool: not initialized or pool not set.", this);
                return;
            }

            _pool.ReturnObject(this);
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' returned to pool. Active: {gameObject.activeSelf}", "blue", this);
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public T GetData<T>() where T : PoolableObjectData
        {
            return Data as T;
        }
    }
}