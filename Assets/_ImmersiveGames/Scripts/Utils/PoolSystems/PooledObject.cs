using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Error)]
    public abstract class PooledObject : MonoBehaviour, IPoolable
    {
        private PoolableObjectData _config;
        private ObjectPool _pool;
        private float _currentLifetime;
        private bool _isRegisteredInLifetimeManager;

        public IActor Spawner { get; private set; }

        public virtual void Configure(PoolableObjectData config, ObjectPool pool, IActor spawner = null)
        {
            _config = config;
            _pool = pool;
            _currentLifetime = config.Lifetime;
            _isRegisteredInLifetimeManager = false;
            Spawner = spawner;
            gameObject.SetActive(false);

            OnConfigured(config, spawner);
        }

        public virtual void Activate(Vector3 position, Vector3? direction = null, IActor spawner = null)
        {
            if (_config == null || _pool == null)
            {
                DebugUtility.LogError<PooledObject>($"Object '{name}' not configured properly.", this);
                return;
            }

            transform.position = new Vector3(position.x, 0, position.z);
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);

            if (spawner != null) Spawner = spawner;

            if (_currentLifetime > 0 && !_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Register(this, _currentLifetime);
                _isRegisteredInLifetimeManager = true;
            }

            OnActivated(position, direction, spawner);
        }

        public virtual void Deactivate()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
            }

            Spawner = null;

            OnDeactivated();
        }

        public virtual void PoolableReset()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(false);

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
            }

            Spawner = null;

            OnReset();
        }

        public virtual void Reconfigure(PoolableObjectData config)
        {
            _config = config;
            _currentLifetime = config.Lifetime;

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
            }
            if (_currentLifetime > 0 && gameObject.activeSelf)
            {
                LifetimeManager.Instance.Register(this, _currentLifetime);
                _isRegisteredInLifetimeManager = true;
            }

            OnReconfigured(config);
        }

        protected abstract void OnConfigured(PoolableObjectData config, IActor spawner);
        protected abstract void OnActivated(Vector3 pos, Vector3? direction, IActor spawner);
        protected abstract void OnDeactivated();
        protected abstract void OnReset();
        protected abstract void OnReconfigured(PoolableObjectData config);

        public GameObject GetGameObject() => gameObject;
        public T GetData<T>() where T : PoolableObjectData => _config as T;
        public ObjectPool GetPool => _pool;
    }
}