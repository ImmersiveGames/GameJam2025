using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public abstract class DefaultPoolableObject : MonoBehaviour, IPoolable
    {
        private PoolableObjectData _data;
        private ObjectPool _pool;
        private float _elapsedTime;
        private bool _isActive;

        public bool IsActive => _isActive;
        public PoolableObjectData Data => _data;

        public virtual void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            if (data == null || pool == null)
            {
                DebugUtility.LogError<DefaultPoolableObject>($"Initialize: Data or pool is null for {gameObject.name}", this, this);
                return;
            }

            _data = data;
            _pool = pool;
        }

        public virtual void Activate(Vector3 position)
        {
            _elapsedTime = 0f;
            _isActive = true;
            transform.position = position;
            gameObject.SetActive(true);

            DebugUtility.LogVerbose<DefaultPoolableObject>($"Objeto {gameObject.name} ativado em {position} com lifetime {_data.Lifetime}s. Ativo: {gameObject.activeSelf}.", "blue", this);
        }

        public virtual void Deactivate()
        {
            _isActive = false;
            gameObject.SetActive(false);
            if (_pool != null)
            {
                _pool.ReturnObject(this);
                DebugUtility.LogVerbose<DefaultPoolableObject>($"Objeto {gameObject.name} desativado e retornado ao pool.", "blue", this);
            }
            else
            {
                DebugUtility.LogWarning<DefaultPoolableObject>($"Pool é nulo ao desativar {gameObject.name}.", this, this);
            }
        }
        public void OnObjectReturned()
        {
            throw new System.NotImplementedException();
        }
        public void OnObjectSpawned()
        {
            throw new System.NotImplementedException();
        }

        protected virtual void Update()
        {
            if (!_isActive)
                return;

            if (_data == null || _data.Lifetime <= 0)
                return;

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _data.Lifetime)
            {
                Deactivate();
            }
        }
    }
}