// File: _ImmersiveGames/Scripts/DamageSystem/DamageDealer.cs
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
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
        
        [SerializeField] private GameObject destructionEffect;
        private float _nextAvailableTime;

        // Eventos locais
        public event System.Action<float, IDamageable> OnDamageDealt;
        public event System.Action<IDamageable> OnDamageBlocked;

        // Injetado via DependencyManager
        [Inject] private DamageService _damageService;

        private void OnCollisionEnter(Collision other) => TryDealDamage(other.gameObject, other.contacts[0].point);
        private void OnTriggerEnter(Collider other) => TryDealDamage(other.gameObject, other.ClosestPoint(transform.position));

        private void TryDealDamage(GameObject target, Vector3 contactPoint)
        {
            if (!IsValidTarget(target)) return;
            if (useCooldown && Time.time < _nextAvailableTime) return;

            if (useCooldown)
                _nextAvailableTime = Time.time + cooldownTime;

            var damageable = GetDamageableFromTarget(target);
            if (damageable is { CanReceiveDamage: true })
            {
                var ctx = new DamageContext
                {
                    Source = actor,
                    Target = damageable.Actor,
                    Amount = damageAmount,
                    DamageType = damageType,
                    ResourceType = damageResourceType,
                    HitPosition = contactPoint
                };

                _damageService?.ApplyDamage(ctx);

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
                destructionHandler.HandleEffectSpawn(destructionEffect, transform.position, transform.rotation);
            }
            
            destructionHandler.HandleDestruction(gameObject, false);
        }
        // IDamageSource Implementation
        public float DamageAmount => damageAmount;
        public ResourceType DamageResourceType => damageResourceType;
        public DamageType DamageType => damageType;
        public IActor DamageSourceActor => actor;
    }
}
