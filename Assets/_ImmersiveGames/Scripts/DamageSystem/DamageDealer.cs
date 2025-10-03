using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using System.Collections.Generic;

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

        [Header("Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject destructionEffect;

        private float _nextAvailableTime;
        private HashSet<GameObject> _processedTargetsThisFrame = new HashSet<GameObject>();

        public event System.Action<float, IDamageable> OnDamageDealt;
        public event System.Action<IDamageable> OnDamageBlocked;

        [Inject] private DamageService _damageService;

        private void OnCollisionEnter(Collision other) => TryDealDamage(other.gameObject, other.contacts[0].point);
        private void OnTriggerEnter(Collider other) => TryDealDamage(other.gameObject, other.ClosestPoint(transform.position));

        private void LateUpdate()
        {
            _processedTargetsThisFrame.Clear();
        }

        private void TryDealDamage(GameObject target, Vector3 contactPoint)
        {
            if (_processedTargetsThisFrame.Contains(target) || HasProcessedPair(gameObject, target))
            {
                DebugUtility.LogVerbose<DamageDealer>($"[Dealer {gameObject.name}] Target {target.name} já processado neste frame, pulando");
                return;
            }
            _processedTargetsThisFrame.Add(target);
            RegisterProcessedPair(gameObject, target);

            DebugUtility.LogVerbose<DamageDealer>($"[Dealer {gameObject.name}] TryDealDamage iniciado para {target.name} em {contactPoint}");
            if (!IsValidTarget(target)) return;

            if (useCooldown && Time.time < _nextAvailableTime)
            {
                DebugUtility.LogVerbose<DamageDealer>($"[Dealer {gameObject.name}] Cooldown ativo, pulando dano");
                return;
            }
            if (useCooldown) _nextAvailableTime = Time.time + cooldownTime;

            var damageable = GetDamageableFromTarget(target);
            if (damageable == null)
            {
                DebugUtility.LogVerbose<DamageDealer>($"[Dealer {gameObject.name}] Nenhum damageable encontrado em {target.name}");
                return;
            }

            if (!damageable.CanReceiveDamage)
            {
                OnDamageBlocked?.Invoke(damageable);
                return;
            }

            var ctx = new DamageContext
            {
                Source = actor,
                Target = damageable.Actor,
                Amount = damageAmount,
                DamageType = damageType,
                ResourceType = damageResourceType,
                HitPosition = contactPoint
            };

            float finalDamage = _damageService != null ? _damageService.CalculateFinalDamage(ctx) : damageAmount;
            ctx.Amount = finalDamage;

            damageable.ReceiveDamage(finalDamage, actor, damageResourceType);

            if (hitEffect != null)
                destructionHandler.HandleEffectSpawn(hitEffect, contactPoint, Quaternion.identity);

            _damageService?.ApplyPostDamageEffects(ctx);

            OnDamageDealt?.Invoke(finalDamage, damageable);

            if (destroyOnDamage)
                HandleDestruction();
        }

        private void HandleDestruction()
        {
            if (destructionEffect != null)
                destructionHandler.HandleEffectSpawn(destructionEffect, transform.position, transform.rotation);
            destructionHandler.HandleDestruction(gameObject, false);
        }

        public float DamageAmount => damageAmount;
        public ResourceType DamageResourceType => damageResourceType;
        public DamageType DamageType => damageType;
        public IActor DamageSourceActor => actor;

        public void SetDamage(float amount) => damageAmount = Mathf.Max(0f, amount);
        public void SetDestroyOnDamage(bool destroy) => destroyOnDamage = destroy;

        public void ForceDealDamage(GameObject target, Vector3 hitPoint) => TryDealDamage(target, hitPoint);

        public void DebugDealDamageTo(GameObject target)
        {
            if (target == null) return;
            var hitPoint = target.transform.position;
            TryDealDamage(target, hitPoint);
            Debug.Log($"[DamageDealer Debug] Tried to deal {damageAmount} damage to {target.name}");
        }
    }
}