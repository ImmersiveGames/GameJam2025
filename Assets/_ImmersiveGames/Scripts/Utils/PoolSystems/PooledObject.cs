﻿using System;
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
        private PoolableObjectData _data;

        public bool IsActive => _isActive;
        public float Lifetime => _lifetime;

        public void Initialize(PoolableObjectData data, ObjectPool pool)
        {
            if (!data) throw new ArgumentNullException(nameof(data));
            _data = data;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _lifetime = data.Lifetime;
            _isActive = false;
            _timer = 0f;
            _returningToPool = false;
            gameObject.SetActive(false);
            DebugUtility.Log<PooledObject>($"Objeto '{name}' inicializado com lifetime {_lifetime}.", "green", this);
        }

        public void SetModel(GameObject model)
        {
            if (_model)
                Destroy(_model);
            _model = model;
            if (_model)
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
            if (_model)
                _model.SetActive(true);
            _timer = _lifetime > 0 ? _lifetime : float.MaxValue;
            _returningToPool = false;
            OnObjectSpawned();
        }

        public void Deactivate()
        {
            if (!_isActive) return;
            _isActive = false;
            gameObject.SetActive(false);
            if (_model)
                _model.SetActive(false);
            _timer = 0f;
            if (!_returningToPool)
                ReturnToPool();
        }

        public void OnObjectSpawned() { }
        public void OnObjectReturned() { }

        public GameObject GetGameObject() => gameObject;
        public T GetData<T>() where T : PoolableObjectData
        {
            if (_data is T data)
                return data;
            throw new InvalidCastException($"Não é possível converter {_data.GetType()} para {typeof(T)}.");
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
            if (!_isActive || _returningToPool || _lifetime <= 0) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                ReturnToPool();
        }

        public void ReturnToPool()
        {
            if (_returningToPool) return;
            _returningToPool = true;
            if (_pool)
            {
                _pool.ReturnObject(this);
                OnObjectReturned();
                DebugUtility.Log<PooledObject>($"Objeto '{name}' retornado ao pool.", "blue", this);
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
                ReturnToPool();
        }
    }
}