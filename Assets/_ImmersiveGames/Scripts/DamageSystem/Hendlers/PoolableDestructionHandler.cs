using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Handle de destruição que retorna o objeto ao pool quando possível.
    /// </summary>
    public class PoolableDestructionHandler : IDestructionHandler
    {
        private readonly IPoolable _poolable;
        private readonly ObjectPool _pool;

        public PoolableDestructionHandler(IPoolable poolable, ObjectPool pool)
        {
            _poolable = poolable;
            _pool = pool;
        }

        public void HandleDestruction(GameObject target, bool spawnEffects = true)
        {
            if (_pool != null && _poolable != null)
            {
                // Return to pool
                _pool.ReturnObject(_poolable);
            }
            else
            {
                Object.Destroy(target);
            }
        }

        public void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab == null) return;

            // Try to use configured pool (PoolManager)
            var poolableEffect = effectPrefab.GetComponent<IPoolable>();
            if (poolableEffect != null)
            {
                var poolName = effectPrefab.name.Replace("(Clone)", "").Trim();
                var pool = PoolManager.Instance?.GetPool(poolName);
                if (pool != null)
                {
                    var effect = pool.GetObject(position, null, null, true);
                    if (effect != null)
                    {
                        var effectLifetime = effect.GetData<PoolableObjectData>()?.Lifetime ?? 2f;
                        LifetimeManager.Instance?.Register(effect, effectLifetime);
                        return;
                    }
                }
            }

            // fallback
            Object.Instantiate(effectPrefab, position, rotation);
        }
    }

    /// <summary>
    /// Fallback simples que destrói/instancia normalmente.
    /// </summary>
    public class DefaultDestructionHandler : IDestructionHandler
    {
        public void HandleDestruction(GameObject target, bool spawnEffects = true)
        {
            Object.Destroy(target);
        }

        public void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
                Object.Instantiate(effectPrefab, position, rotation);
        }
    }
}
