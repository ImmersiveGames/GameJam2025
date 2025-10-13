using _ImmersiveGames.Scripts.Utils.DebugSystems;
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
                DebugUtility.LogVerbose<PoolableDestructionHandler>($"PoolableDestructionHandler: Objeto {target.name} retornado ao pool");
            }
            else
            {
                Object.Destroy(target);
                DebugUtility.LogVerbose<PoolableDestructionHandler>($"PoolableDestructionHandler: Objeto {target.name} destruído");
            }
        }

        public void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab == null) return;

            // Try to use a configured pool (PoolManager)
            var poolableEffect = effectPrefab.GetComponent<IPoolable>();
            if (poolableEffect != null && PoolManager.HasInstance)
            {
                string poolName = effectPrefab.name.Replace("(Clone)", "").Trim();
                var pool = PoolManager.Instance.GetPool(poolName);
                if (pool != null)
                {
                    var effect = pool.GetObject(position);
                    if (effect != null)
                    {
                        float effectLifetime = effect.GetData<PoolableObjectData>()?.Lifetime ?? 2f;
                        if (LifetimeManager.HasInstance)
                        {
                            LifetimeManager.Instance.Register(effect, effectLifetime);
                        }
                        DebugUtility.LogVerbose<PoolableDestructionHandler>($"PoolableDestructionHandler: Efeito {poolName} spawnado do pool");
                        return;
                    }
                }
            }

            // CORREÇÃO: Fallback com log
            var instantiatedEffect = Object.Instantiate(effectPrefab, position, rotation);
            DebugUtility.LogVerbose<PoolableDestructionHandler>($"PoolableDestructionHandler: Efeito {effectPrefab.name} instanciado (fallback)");
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
            DebugUtility.LogVerbose<PoolableDestructionHandler>($"DefaultDestructionHandler: Objeto {target.name} destruído");
        }

        public void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab != null)
            {
                Object.Instantiate(effectPrefab, position, rotation);
                DebugUtility.LogVerbose<PoolableDestructionHandler>($"DefaultDestructionHandler: Efeito {effectPrefab.name} instanciado");
            }
        }
    }
}