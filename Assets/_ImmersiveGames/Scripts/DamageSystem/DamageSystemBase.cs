using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public abstract class DamageSystemBase : MonoBehaviour
    {
        [Header("Base Damage Configuration")]
        [SerializeField] protected LayerMask damageableLayers = -1;
        [SerializeField] protected bool damageSelf;
        
        protected IActor _actor;
        protected IDestructionHandler _destructionHandler;

        protected virtual void Awake()
        {
            _actor = GetComponent<IActor>();
            InitializeDestructionHandler();
        }
        protected virtual void InitializeDestructionHandler()
        {
            // Tenta obter um poolable primeiro
            var poolable = GetComponent<IPoolable>();
            if (poolable != null)
            {
                var pooledObject = GetComponent<PooledObject>();
                if (pooledObject?.GetPool != null)
                {
                    _destructionHandler = new PoolableDestructionHandler(poolable, pooledObject.GetPool);
                    return;
                }
            }

            // Fallback para handler padrão
            _destructionHandler = new DefaultDestructionHandler();
        }

        protected virtual bool IsValidTarget(GameObject target)
        {
            if (target == null) return false;
            
            if (!damageSelf && target == gameObject) return false;
            
            if (!IsInDamageableLayer(target)) return false;
            
            return true;
        }


        protected virtual bool IsInDamageableLayer(GameObject target)
        {
            return (damageableLayers.value & (1 << target.layer)) != 0;
        }

        protected virtual IDamageable GetDamageableFromTarget(GameObject target)
        {
            if (target == null) return null;

            // Tentar obter do próprio objeto primeiro
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null) return damageable;

            // Tentar obter do parent
            damageable = target.GetComponentInParent<IDamageable>();
            return damageable;
        }

        // Getters para interface
        public LayerMask DamageableLayers => damageableLayers;
        public IActor Actor => _actor;
        public IDestructionHandler DestructionHandler => _destructionHandler;
    }
}