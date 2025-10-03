using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// A ideia aqui é que o pool de objetos possa ser usado para destruir objetos.
    /// Então criar uma estratégia para como se deve destruir os objetos.
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
                _pool.ReturnObject(_poolable);
            }
            else
            {
                // Fallback para destruição normal
                Object.Destroy(target);
            }
        }

        public void HandleEffectSpawn(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (effectPrefab == null) return;

            // Tenta usar pool para efeitos também
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

            // Fallback para instanciação normal
            Object.Instantiate(effectPrefab, position, rotation);
        }
    }
}

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