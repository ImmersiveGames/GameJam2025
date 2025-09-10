using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public interface IPoolable
    {
        void Configure(PoolableObjectData config, ObjectPool pool, IActor spawner = null);
        void Activate(Vector3 position, IActor spawner = null);
        void Deactivate();
        void PoolableReset();
        void Reconfigure(PoolableObjectData config);
        GameObject GetGameObject();
        T GetData<T>() where T : PoolableObjectData;
    }

    public class PooledObject : MonoBehaviour, IPoolable
    {
        private PoolableObjectData _config;
        private ObjectPool _pool;
        private float _currentLifetime;
        private bool _isRegisteredInLifetimeManager;
        public IActor Spawner { get; private set; }

        private bool IsConfigured() => _config != null && _pool != null;

        public void Configure(PoolableObjectData config, ObjectPool pool, IActor spawner = null)
        {
            if (config == null)
            {
                DebugUtility.LogError<PooledObject>($"Invalid config for '{gameObject.name}'.", this);
                return;
            }
            if (pool == null)
            {
                DebugUtility.LogWarning<PooledObject>($"Pool is null for '{gameObject.name}'. This is acceptable in tests but may cause issues in runtime.", this);
            }
            _config = config;
            _pool = pool;
            _currentLifetime = config.Lifetime;
            _isRegisteredInLifetimeManager = false;
            gameObject.SetActive(false);
            Spawner = spawner;
            DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' configured with lifetime {_currentLifetime}.", "green", this);
        }

        public void Activate(Vector3 position, IActor spawner = null)
        {
            if (!IsConfigured())
            {
                DebugUtility.LogError<PooledObject>($"Cannot activate '{gameObject.name}': not configured.", this);
                return;
            }

            transform.position = new Vector3(position.x, 0, position.z);
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);

            if (spawner != null)
            {
                SetSpawner(spawner);
                DebugUtility.LogVerbose<PooledObject>($"Spawner set for '{gameObject.name}' to {spawner.Name}.", "cyan", this);

                //TODO: Activate object based on spawner
            }

            if (_currentLifetime > 0 && !_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Register(this, _currentLifetime);
                _isRegisteredInLifetimeManager = true;
                DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' registered in LifetimeManager with lifetime {_currentLifetime}.", "cyan", this);
            }

            DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' activated at {position}.", "green", this);
        }

        public void Deactivate()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
                DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' unregistered from LifetimeManager.", "blue", this);
            }

            if (Spawner != null)
            {
                ResetSpawner();
                DebugUtility.LogVerbose<PooledObject>($"Spawner reset for '{gameObject.name}'.", "blue", this);
            }

            DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' deactivated.", "blue", this);
        }

        public void PoolableReset()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(false);

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
                DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' unregistered from LifetimeManager during reset.", "blue", this);
            }

            if (Spawner != null)
            {
                ResetSpawner();
                DebugUtility.LogVerbose<PooledObject>($"Spawner reset for '{gameObject.name}' during reset.", "blue", this);
            }

            if (_config is BulletObjectData)
            {
                var rb = gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }

            DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' reset.", "green", this);
        }

        public void Reconfigure(PoolableObjectData config)
        {
            if (config == null)
            {
                DebugUtility.LogError<PooledObject>($"Invalid config for reconfiguration of '{gameObject.name}'.", this);
                return;
            }

            _config = config;
            _currentLifetime = config.Lifetime;

            //TODO: Reconfigure object based on config

            if (_isRegisteredInLifetimeManager)
            {
                LifetimeManager.Instance.Unregister(this);
                _isRegisteredInLifetimeManager = false;
            }
            if (_currentLifetime > 0 && gameObject.activeSelf)
            {
                LifetimeManager.Instance.Register(this, _currentLifetime);
                _isRegisteredInLifetimeManager = true;
                DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' re-registered in LifetimeManager with new lifetime {_currentLifetime}.", "blue", this);
            }

            DebugUtility.LogVerbose<PooledObject>($"Object '{gameObject.name}' reconfigured with lifetime {_currentLifetime}.", "blue", this);
        }

        public GameObject GetGameObject() => gameObject;
        public T GetData<T>() where T : PoolableObjectData => _config as T;

        private void SetSpawner(IActor spawner) => Spawner = spawner;
        private void ResetSpawner() => Spawner = null;
        public ObjectPool GetPool => _pool;
    }
}