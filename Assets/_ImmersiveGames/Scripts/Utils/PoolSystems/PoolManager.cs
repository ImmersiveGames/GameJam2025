using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    
    public class PoolManager : PersistentSingleton<PoolManager>
    {
        private readonly Dictionary<string, ObjectPool> _pools = new();

        protected override void Awake()
        {
            base.Awake();
            DebugUtility.Log<PoolManager>("PoolManager initialized.", "cyan", this);
        }

        public ObjectPool RegisterPool(PoolData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<PoolManager>("PoolData is null or ObjectName is empty.", this);
                return null;
            }

            if (_pools.TryGetValue(data.ObjectName, out var existingPool))
            {
                DebugUtility.LogVerbose<PoolManager>($"Pool '{data.ObjectName}' já estava registrado. Reutilizando instância existente.", "yellow", this);
                return existingPool;
            }

            var poolObject = new GameObject($"Pool_{data.ObjectName}");
            poolObject.transform.SetParent(transform, false);
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.Initialize();
            if (!pool.IsInitialized)
            {
                DebugUtility.LogError<PoolManager>($"Failed to initialize pool '{data.ObjectName}'. Check PoolData and prefab.", this);
                Destroy(poolObject);
                return null;
            }
            _pools.Add(data.ObjectName, pool);
            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registered successfully.", "green", this);
            return pool;
        }

        public ObjectPool GetPool(string poolName)
        {
            if (_pools.TryGetValue(poolName, out var pool))
            {
                DebugUtility.LogVerbose<PoolManager>($"Pool '{poolName}' found.", "green", this);
                return pool;
            }

            DebugUtility.LogWarning<PoolManager>($"Pool '{poolName}' not found.", this);
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