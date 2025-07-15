using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Warning)]
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }
        private readonly Dictionary<string, ObjectPool> _pools = new();
        private Transform _poolsParent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _poolsParent = new GameObject("Pools").transform;
                _poolsParent.SetParent(transform);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                    if (pool.gameObject != null)
                        Destroy(pool.gameObject);
                }
            }
            _pools.Clear();
            if (_poolsParent != null)
                Destroy(_poolsParent.gameObject);
        }

        public void RegisterPool(PoolData data)
        {
            if (!PoolValidationUtility.ValidatePoolData(data, this))
                return;

            if (_pools.ContainsKey(data.ObjectName))
            {
                DebugUtility.LogVerbose<PoolManager>($"Pool '{data.ObjectName}' já registrado.", "yellow", this);
                return;
            }

            var poolObject = new GameObject($"Pool_{data.ObjectName}");
            poolObject.transform.SetParent(_poolsParent);
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.Initialize();
            if (!pool.IsInitialized)
            {
                DebugUtility.LogError<PoolManager>($"Falha ao inicializar pool '{data.ObjectName}'.", this);
                Destroy(poolObject);
                return;
            }
            _pools[data.ObjectName] = pool;

            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registrado com sucesso.", "green", this);
        }

        public void RemovePool(string key)
        {
            if (!PoolValidationUtility.ValidatePoolKey(key, this))
                return;

            if (_pools.TryGetValue(key, out var pool))
            {
                pool.ClearPool();
                if (pool.gameObject != null)
                    Destroy(pool.gameObject);
                _pools.Remove(key);
                DebugUtility.Log<PoolManager>($"Pool '{key}' removido com sucesso.", "green", this);
            }
            else
            {
                DebugUtility.LogVerbose<PoolManager>($"Pool '{key}' não encontrado.", "yellow", this);
            }
        }

        public ObjectPool GetPool(string key)
        {
            if (!PoolValidationUtility.ValidatePoolKey(key, this))
                return null;

            if (_pools.TryGetValue(key, out var pool) && pool.IsInitialized)
                return pool;

            DebugUtility.LogError<PoolManager>($"Pool '{key}' não encontrado ou não inicializado.", this);
            return null;
        }
    }
}