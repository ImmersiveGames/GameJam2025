using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Services
{
    public class EffectService
    {
        private readonly PoolManager _poolManager;
        private readonly LifetimeManager _lifetimeManager;

        public EffectService(PoolManager poolManager, LifetimeManager lifetimeManager)
        {
            _poolManager = poolManager;
            _lifetimeManager = lifetimeManager;
        }

        public void SpawnHitEffect(DamageType damageType, Vector3 position)
        {
            // define mapping from DamageType -> poolName / prefab name
            string poolName = damageType.ToString(); // customize mapping if necessary

            var pool = _poolManager?.GetPool(poolName);
            if (pool == null) return;
            var poolable = pool.GetObject(position);
            if (poolable == null) return;
            var cfg = poolable.GetData<PoolableObjectData>();
            float lifetime = cfg?.Lifetime ?? 2f;
            _lifetimeManager?.Register(poolable, lifetime);
            //return;

            // fallback: try to find prefab in Resources or configuration (example)
            // You can add a dictionary Resource->Prefab if you prefer.
        }

        public GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float? lifetime = null)
        {
            if (prefab == null) return null;

            var poolable = prefab.GetComponent<IPoolable>();
            if (poolable == null) return Object.Instantiate(prefab, position, rotation);
            
            string poolName = prefab.name.Replace("(Clone)", "").Trim();
            var pool = _poolManager.GetPool(poolName);
            if (pool == null) return Object.Instantiate(prefab, position, rotation);
            
            var obj = pool.GetObject(position);
            if (obj == null) return Object.Instantiate(prefab, position, rotation);
            
            var cfg = obj.GetData<PoolableObjectData>();
            float life = lifetime ?? cfg?.Lifetime ?? 2f;
            _lifetimeManager?.Register(obj, life);
            return obj.GetGameObject();

            // fallback instantiate
        }
    }
}