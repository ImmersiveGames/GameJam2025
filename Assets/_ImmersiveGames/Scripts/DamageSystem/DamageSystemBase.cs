using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public abstract class DamageSystemBase : MonoBehaviour
    {
        [Header("Base Damage Configuration")]
        [SerializeField] protected LayerMask damageableLayers = -1;
        [SerializeField] protected bool damageSelf;

        protected IActor actor;
        protected IDestructionHandler destructionHandler;
        protected readonly Dictionary<GameObject, IDamageable> _damageableCache = new();
        private static readonly HashSet<(GameObject, GameObject)> _processedPairsThisFrame = new(); // Novo: Cache de pares

        protected virtual void Awake()
        {
            actor = GetComponent<IActor>();
            InitializeDestructionHandler();
        }

        protected virtual void InitializeDestructionHandler()
        {
            var poolable = GetComponent<IPoolable>();
            if (poolable != null)
            {
                var pooledObject = GetComponent<PooledObject>();
                if (pooledObject?.GetPool != null)
                {
                    destructionHandler = new PoolableDestructionHandler(poolable, pooledObject.GetPool);
                    return;
                }
            }
            destructionHandler = new DefaultDestructionHandler();
        }

        protected virtual void LateUpdate()
        {
            _processedPairsThisFrame.Clear(); // Limpar fim frame
        }

        protected virtual bool IsValidTarget(GameObject target)
        {
            if (target == null) return false;
            if (!damageSelf && target == gameObject) return false;
            return IsInDamageableLayer(target);
        }

        protected virtual bool IsInDamageableLayer(GameObject target)
        {
            return (damageableLayers.value & (1 << target.layer)) != 0;
        }

        protected virtual bool HasProcessedPair(GameObject source, GameObject target)
        {
            var pair = (source, target);
            var reversePair = (target, source);
            return _processedPairsThisFrame.Contains(pair) || _processedPairsThisFrame.Contains(reversePair);
        }

        protected virtual void RegisterProcessedPair(GameObject source, GameObject target)
        {
            _processedPairsThisFrame.Add((source, target));
        }

        protected virtual IDamageable GetDamageableFromTarget(GameObject target)
        {
            if (target == null) return null;
            if (_damageableCache.TryGetValue(target, out var cached)) return cached;

            var damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
            if (damageable != null) _damageableCache[target] = damageable;
            return damageable;
        }

        public LayerMask DamageableLayers => damageableLayers;
        public IActor Actor => actor;
        public IDestructionHandler DestructionHandler => destructionHandler;
    }
}