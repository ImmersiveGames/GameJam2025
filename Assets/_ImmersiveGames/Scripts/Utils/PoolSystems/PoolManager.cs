using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [DefaultExecutionOrder(-100), DebugLevel(DebugLevel.Warning)]
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }
        private readonly Dictionary<string, ObjectPool> _pools = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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
                    // Acessa _pool diretamente (assumindo Queue<IPoolable> _pool no ObjectPool)
                    var poolField = typeof(ObjectPool).GetField("_pool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (poolField != null)
                    {
                        var poolQueue = poolField.GetValue(pool) as Queue<IPoolable>;
                        if (poolQueue != null)
                        {
                            foreach (var obj in poolQueue)
                            {
                                if (obj != null && obj.GetGameObject() != null)
                                {
                                    obj.Deactivate();
                                }
                            }
                        }
                    }
                    if (pool.gameObject != null)
                    {
                        Destroy(pool.gameObject);
                    }
                }
            }
            _pools.Clear();
        }

        public void RegisterPool(PoolableObjectData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<PoolManager>("PoolableObjectData ou ObjectName nulo.", this);
                return;
            }

            if (_pools.ContainsKey(data.ObjectName))
            {
                return;
            }

            var poolObject = new GameObject($"Pool_{data.ObjectName}");
            poolObject.transform.SetParent(transform);
            var pool = poolObject.AddComponent<ObjectPool>();
            pool.SetData(data);
            pool.Initialize();
            _pools[data.ObjectName] = pool;

            DebugUtility.Log<PoolManager>($"Pool '{data.ObjectName}' registrado com sucesso.", "green", this);
        }

        public ObjectPool GetPool(string key)
        {
            return _pools.GetValueOrDefault(key);
        }

        public IPoolable GetObject(string key, Vector3 position)
        {
            var pool = GetPool(key);
            return pool?.GetObject(position);
        }
    }
}