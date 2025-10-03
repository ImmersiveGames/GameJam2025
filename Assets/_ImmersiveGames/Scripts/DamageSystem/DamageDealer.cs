using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DamageDealer : DamageSystemBase, IDamageSource
    {
        [Header("Damage Settings")]
        [SerializeField] private float damageAmount = 20f;
        [SerializeField] private ResourceType damageResourceType = ResourceType.Health;
        [SerializeField] private DamageType damageType = DamageType.Physical;
        [SerializeField] private bool destroyOnDamage;
        
        [Header("Cooldown")]
        [SerializeField] private bool useCooldown;
        [SerializeField] private float cooldownTime = 0.5f;
        
        [Header("Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject destructionEffect;
        
        private float _lastDamageTime = -999f;

        // Eventos
        public event System.Action<float, IDamageable> OnDamageDealt;
        public event System.Action<IDamageable> OnDamageBlocked;

        private void OnCollisionEnter(Collision other) => TryDealDamage(other.gameObject, other.contacts[0].point);
        private void OnTriggerEnter(Collider other) => TryDealDamage(other.gameObject, other.ClosestPoint(transform.position));

        // No método TryDealDamage do DamageDealer, adicione:
        private void TryDealDamage(GameObject target, Vector3 contactPoint)
        {
            if (!IsValidTarget(target)) return;
            if (useCooldown && Time.time - _lastDamageTime < cooldownTime) return;

            if (useCooldown)
            {
                _lastDamageTime = Time.time;
                Invoke(nameof(ResetCooldown), cooldownTime);
            }

            var damageable = GetDamageableFromTarget(target);
            if (damageable is { CanReceiveDamage: true })
            {
                damageable.ReceiveDamage(damageAmount, _actor, damageResourceType);
        
                // Disparar evento global
                if (damageable.Actor != null)
                {
                    var damageEvent = new DamageDealtEvent(_actor, damageable.Actor, damageAmount, damageType, contactPoint);
                    EventBus<DamageDealtEvent>.Raise(damageEvent);
                }
                // Spawn hit effect
                if (hitEffect != null)
                {
                    _destructionHandler.HandleEffectSpawn(hitEffect, contactPoint, Quaternion.identity);
                }
        
                OnDamageDealt?.Invoke(damageAmount, damageable);

                if (destroyOnDamage)
                {
                    HandleDestruction();
                }
            }
            else if (damageable != null)
            {
                OnDamageBlocked?.Invoke(damageable);
            }
        }
        private void HandleDestruction()
        {
            // Spawn destruction effect if any
            if (destructionEffect != null)
            {
                _destructionHandler.HandleEffectSpawn(destructionEffect, transform.position, transform.rotation);
            }
            
            _destructionHandler.HandleDestruction(gameObject, false);
        }

        private void ResetCooldown() { }

        // IDamageSource Implementation
        public float DamageAmount => damageAmount;
        public ResourceType DamageResourceType => damageResourceType;
        public DamageType DamageType => damageType;
        public IActor DamageSourceActor => _actor;
    }
}