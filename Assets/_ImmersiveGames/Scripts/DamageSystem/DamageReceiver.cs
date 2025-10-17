using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [RequireComponent(typeof(ActorMaster))]
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private float damageCooldown = 0.25f;

        [Header("Estratégia de Dano")]
        [SerializeReference] private DamageStrategy strategy = new BasicDamageStrategy();

        private ActorMaster _actor;
        private InjectableEntityResourceBridge _bridge;
        private DamageCooldownModule _cooldowns;
        private DamageLifecycleModule _lifecycle;

        private void Awake()
        {
            _actor = GetComponent<ActorMaster>();
            _bridge = GetComponent<InjectableEntityResourceBridge>();
            _cooldowns = new DamageCooldownModule(damageCooldown);
            _lifecycle = new DamageLifecycleModule(_actor.ActorId);
        }

        public void ReceiveDamage(DamageContext ctx)
        {
            if (_bridge == null) return;
            var system = _bridge.GetResourceSystem();
            if (system == null) return;

            if (!_cooldowns.CanDealDamage(ctx.AttackerId, ctx.TargetId))
                return;

            float finalDamage = strategy?.CalculateDamage(ctx) ?? ctx.DamageValue;
            system.Modify(targetResource, -finalDamage);

            var damageEvent = new DamageEvent(
                ctx.AttackerId,
                ctx.TargetId,
                finalDamage,
                targetResource,
                ctx.DamageType,
                ctx.HitPosition
            );

            FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, ctx.TargetId);
            if (!string.IsNullOrEmpty(ctx.AttackerId))
                FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, ctx.AttackerId);

            _lifecycle.CheckDeath(system, targetResource);
        }

        public string GetReceiverId() => _actor.ActorId;
    }
}
