using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
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

        public void RegisterPool(PoolableObjectData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<PoolManager>("PoolableObjectData ou ObjectName nulo. Verifique a configuração do SpawnData.PoolableData.", this);
                return;
            }

            if (!data.Prefab || !data.ModelPrefab)
            {
                DebugUtility.LogError<PoolManager>($"Prefab ou ModelPrefab nulo para pool '{data.ObjectName}'. Configure corretamente o PoolableObjectData.", this);
                return;
            }

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
                DebugUtility.LogError<PoolManager>($"Falha ao inicializar pool '{data.ObjectName}'. Verifique o PoolableObjectData.", this);
                Destroy(poolObject);
                return;
            }
            _pools[data.ObjectName] = pool;

            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registrado com sucesso.", "green", this);
        }

        public ObjectPool GetPool(string key)
        {
            if (_pools.TryGetValue(key, out var pool) && pool.IsInitialized)
                return pool;
            DebugUtility.LogError<PoolManager>($"Pool '{key}' não encontrado ou não inicializado. Verifique se o pool foi registrado pelo SpawnManager.", this);
            return null;
        }
    }
}