using UnityEngine;
using UnityEngine.Events;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class PooledObject : MonoBehaviour, IPoolable
    {
        private ObjectPool _pool;
        private bool _isActive;
        private bool _returningToPool;
        private PoolableObjectData _data;
        private IActor _spawner;

        public UnityEvent OnActivated { get; } = new UnityEvent();
        public UnityEvent OnDeactivated { get; } = new UnityEvent();

        public void Initialize(PoolableObjectData data, ObjectPool pool, IActor actor = null)
        {
            if (!PoolValidationUtility.ValidatePoolableObjectData(data, this))
                throw new System.ArgumentNullException(nameof(data));
            
            _data = data;
            _pool = pool ?? throw new System.ArgumentNullException(nameof(pool));
            _isActive = false;
            _returningToPool = false;
            gameObject.SetActive(false);
            DebugUtility.LogVerbose<PooledObject>($"Objeto '{name}' inicializado com lifetime {_data.Lifetime}.", "green", this);
        }

        public void Activate(Vector3 position, IActor actor)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _isActive = true;
            _spawner = actor ?? _spawner;
            gameObject.SetActive(true);
            _returningToPool = false;
            LifetimeManager.Instance.RegisterObject(this, _data.Lifetime);
            OnActivated.Invoke();
            DebugUtility.LogVerbose<PooledObject>($"Objeto '{name}' ativado na posição {transform.position}.", "green", this);
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            gameObject.SetActive(false);
            LifetimeManager.Instance.UnregisterObject(this);
            OnDeactivated.Invoke();
            if (!_returningToPool)
                ReturnToPool();
        }

        public IActor Spawner => _spawner;
        public PoolableObjectData Data => _data;

        public GameObject GetGameObject() => gameObject;

        public T GetData<T>() where T : PoolableObjectData
        {
            if (_data is T data)
                return data;
            throw new System.InvalidCastException($"Não é possível converter {_data.GetType()} para {typeof(T)}.");
        }

        public void PoolableReset()
        {
            transform.rotation = Quaternion.identity;
            _returningToPool = false;
            DebugUtility.LogVerbose<PooledObject>($"Objeto '{name}' resetado.", "green", this);
        }

        public void ReturnToPool()
        {
            if (_returningToPool) return;
            _returningToPool = true;
            if (_pool)
            {
                _pool.ReturnObject(this);
                DebugUtility.LogVerbose<PooledObject>($"Objeto '{name}' retornado ao pool.", "blue", this);
            }
            else
            {
                DebugUtility.LogError<PooledObject>("Pool é nulo, não retornado.", this);
            }
            _isActive = false;
        }

        private void OnDisable()
        {
            if (_isActive && !_returningToPool && _pool)
                Deactivate();
        }
    }
}