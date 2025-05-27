using System;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class PooledObject : MonoBehaviour, IPoolable
    {
        private ObjectPool _pool;
        private GameObject _model;
        private float _lifetime;
        private bool _isActive;
        private float _timer;
        private bool _returningToPool;

        public bool IsActive => _isActive;
        public float Lifetime => _lifetime; // Adicionado para acesso externo

        public void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _lifetime = data.Lifetime; // Usa o lifetime diretamente
            _isActive = false;
            _timer = 0f;
            _returningToPool = false;
            gameObject.SetActive(false);
            DebugUtility.Log<PooledObject>($"Objeto '{name}' inicializado com lifetime {_lifetime}.", "green", this);
        }

        public void SetModel(GameObject model)
        {
            if (_model != null)
            {
                Destroy(_model);
            }
            _model = model;
            if (_model != null)
            {
                _model.transform.SetParent(transform);
                _model.transform.localPosition = Vector3.zero;
                _model.transform.localRotation = Quaternion.identity;
                _model.SetActive(false);
            }
        }

        public void Activate(Vector3 position)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            _isActive = true;
            gameObject.SetActive(true);
            if (_model != null)
            {
                _model.SetActive(true);
            }
            _timer = _lifetime > 0 ? _lifetime : float.MaxValue; // Lifetime 0 = permanente
            _returningToPool = false;
            OnObjectSpawned();
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            gameObject.SetActive(false);
            if (_model != null)
            {
                _model.SetActive(false);
            }
            _timer = 0f;
            EventBus<ObjectDeactivatedEvent>.Raise(new ObjectDeactivatedEvent(_pool.Data.ObjectName, gameObject));
            if (!_returningToPool)
            {
                ReturnToPool();
            }
        }

        public void OnObjectSpawned()
        {
            // Pode ser sobrescrito
        }

        public void OnObjectReturned()
        {
            // Pode ser sobrescrito
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public void Reset()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            _timer = 0f;
            _returningToPool = false;
            DebugUtility.Log<PooledObject>($"Objeto '{name}' resetado.", "green", this);
        }

        private void Update()
        {
            if (!_isActive || _returningToPool) return;
            if (_lifetime > 0) // Só decrementa timer se lifetime > 0
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    ReturnToPool();
                }
            }
        }

        public void ReturnToPool()
        {
            if (_returningToPool) return;
            _returningToPool = true;
            if (_pool != null)
            {
                _pool.ReturnObject(this);
                OnObjectReturned();
                DebugUtility.Log<PooledObject>($"Objeto {gameObject.name} retornado ao pool.", "blue", this);
            }
            else
            {
                DebugUtility.LogError<PooledObject>("Pool é nulo, não retornado.", this);
            }
            _isActive = false;
        }

        private void OnDisable()
        {
            if (_isActive && !_returningToPool)
            {
                ReturnToPool();
            }
        }
    }
}