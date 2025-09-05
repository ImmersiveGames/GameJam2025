using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
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
        private ObjectPool _pool;
        private bool _isConfigured;

        public void Configure(PoolableObjectData data, ObjectPool pool)
        {
            if (!ValidationService.ValidatePoolableObjectData(data, this))
            {
                DebugUtility.LogError<PooledObject>($"Invalid PoolableObjectData for '{name}'.", this);
                return;
            }

            Data = data;
            _pool = pool;
            _isConfigured = true;

            gameObject.SetActive(false);
            EventBus<ObjectCreatedEvent>.Raise(new ObjectCreatedEvent(this, data, pool));
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' configured with lifetime {data.Lifetime}.", "green", this);
        }

        public void Reconfigure(PoolableObjectData data)
        {
            if (!ValidationService.ValidatePoolableObjectData(data, this))
            {
                DebugUtility.LogError<PooledObject>($"Invalid PoolableObjectData for reconfiguration of '{name}'.", this);
                return;
            }

            Data = data;
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' reconfigured with lifetime {data.Lifetime}.", "blue", this);
        }

        public void Activate(Vector3 position, IActor spawner = null)
        {
            if (!_isConfigured)
            {
                DebugUtility.LogError<PooledObject>($"Cannot activate '{name}': not configured.", this);
                return;
            }

            var tracker = gameObject.GetComponent<SpawnerTracker>();
            if (tracker != null && spawner != null)
            {
                tracker.SetSpawner(spawner);
            }
            transform.position = position;
            gameObject.SetActive(true);
            LifetimeManager.Instance.RegisterObject(this, Data.Lifetime);
            OnActivated.Invoke();
            EventBus<ObjectActivatedEvent>.Raise(new ObjectActivatedEvent(this, spawner, position));
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' activated at {position}.", "green", this);
        }

        public void Deactivate()
        {
            if (!_isConfigured || !gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            LifetimeManager.Instance.UnregisterObject(this);
            OnDeactivated.Invoke();
            EventBus<PoolObjectReturnedEvent>.Raise(new PoolObjectReturnedEvent(_pool.GetData().ObjectName, this));
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' deactivated.", "blue", this);
        }

        public void PoolableReset()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            var tracker = gameObject.GetComponent<SpawnerTracker>();
            if (tracker != null)
            {
                tracker.SetSpawner(null);
            }
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' reset.", "green", this);
        }

        public void ReturnToPool()
        {
            if (!_isConfigured || _pool == null)
            {
                DebugUtility.LogError<PooledObject>($"Cannot return '{name}' to pool: not configured or pool not set.", this);
                return;
            }

            Deactivate();
            PoolableReset();  // Adicionado para garantir o reset do tracker ao return
            _pool.ReturnObject(this);
            DebugUtility.LogVerbose<PooledObject>($"Object '{name}' returned to pool.", "blue", this);
        }

        public GameObject GetGameObject() => gameObject;
        public T GetData<T>() where T : PoolableObjectData => Data as T;
        public ObjectPool GetPool() => _pool;
    }
}