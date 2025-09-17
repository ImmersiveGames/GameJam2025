using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PoolManager : PersistentSingleton<PoolManager>
    {
        private readonly Dictionary<string, ObjectPool> _pools = new();

        protected override void Awake()
        {
            base.Awake();
            DebugUtility.Log<PoolManager>("PoolManager initialized.", "cyan", this);
        }

        public void RegisterPool(PoolData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<PoolManager>("PoolData is null or ObjectName is empty.", this);
                return;
            }

            if (_pools.ContainsKey(data.ObjectName))
            {
                DebugUtility.LogWarning<PoolManager>($"Pool '{data.ObjectName}' already registered.", this);
                return;
            }

            var poolObject = new GameObject($"Pool_{data.ObjectName}");
            poolObject.transform.SetParent(transform);
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.Initialize();
            if (!pool.IsInitialized)
            {
                DebugUtility.LogError<PoolManager>($"Failed to initialize pool '{data.ObjectName}'. Check PoolData and prefab.", this);
                Destroy(poolObject);
                return;
            }
            _pools.Add(data.ObjectName, pool);
            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registered successfully.", "green", this);
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