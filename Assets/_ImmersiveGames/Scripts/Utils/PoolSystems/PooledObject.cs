using System;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class PooledObject : MonoBehaviour, IPoolable
    {
        private ObjectPool _pool;
        private GameObject _model;
        private float _lifetime = 5f;
        private bool _isActive;
        private float _timer;
        private bool _returningToPool;

        public bool IsActive => _isActive;

        public void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _lifetime = Mathf.Max(_lifetime, 0.1f);
            _isActive = false;
            _timer = 0f;
            _returningToPool = false;
            gameObject.SetActive(false);
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
                _model.SetActive(false);
            }
        }

        public void Activate(Vector3 position)
        {
            transform.position = position;
            _isActive = true;
            gameObject.SetActive(true);
            if (_model != null)
            {
                _model.SetActive(true);
            }
            _timer = _lifetime;
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

        /// <summary>
        /// Chamado quando o objeto é spawnado do pool. Implemente para lógica personalizada de ativação.
        /// </summary>
        public void OnObjectSpawned()
        {
        }

        /// <summary>
        /// Chamado quando o objeto é retornado ao pool. Implemente para lógica personalizada de desativação.
        /// </summary>
        public void OnObjectReturned()
        {
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        private void Update()
        {
            if (!_isActive || _returningToPool) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
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