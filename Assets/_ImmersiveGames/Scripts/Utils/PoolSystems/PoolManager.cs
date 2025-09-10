using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{

    [DebugLevel(DebugLevel.Verbose)]
    public class PoolManager : PersistentSingleton<PoolManager>
    {
        private readonly Dictionary<FactoryType, ObjectPool> _pools = new();

        protected override void Awake()
        {
            base.Awake();
            DebugUtility.Log<PoolManager>("PoolManager initialized.", "cyan", this);
        }

        public void RegisterPool(PoolData data, FactoryType factoryType)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<PoolManager>("PoolData is null or ObjectName is empty.", this);
                return;
            }

            if (_pools.ContainsKey(factoryType))
            {
                DebugUtility.LogWarning<PoolManager>($"Pool '{factoryType}' already registered.", this);
                return;
            }

            var poolObject = new GameObject($"Pool_{data.ObjectName}");
            poolObject.transform.SetParent(transform);
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetFactory(new ObjectPoolFactory());
            pool.SetData(data);
            pool.Initialize();
            if (!pool.IsInitialized)
            {
                DebugUtility.LogError<PoolManager>($"Failed to initialize pool '{factoryType}'. Check PoolData and prefab.", this);
                Destroy(poolObject);
                return;
            }
            _pools.Add(factoryType, pool);
            DebugUtility.Log<PoolManager>($"Pool '{factoryType}' registered successfully.", "green", this);
        }

        public ObjectPool GetPool(FactoryType factoryType)
        {
            if (_pools.TryGetValue(factoryType, out var pool))
            {
                DebugUtility.LogVerbose<PoolManager>($"Pool '{factoryType}' found.", "green", this);
                return pool;
            }

            DebugUtility.LogWarning<PoolManager>($"Pool '{factoryType}' not found.", this);
            return null;
        }

        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool != null)
                {
                    pool.ClearPool();
                    Destroy(pool.gameObject);
                }
            }
            _pools.Clear();
            DebugUtility.Log<PoolManager>("All pools cleared.", "cyan", this);
        }
    }
}