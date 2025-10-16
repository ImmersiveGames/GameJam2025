using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [RequireComponent(typeof(ActorMaster))]
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private float damageCooldown = 0.25f;

        private ActorMaster _actor;
        private InjectableEntityResourceBridge _bridge;
        private IDamageStrategy _strategy;
        private DamageCooldownTracker _cooldowns;

        private void Awake()
        {
            _actor = GetComponent<ActorMaster>();
            _bridge = GetComponent<InjectableEntityResourceBridge>();
            _strategy = new BasicDamageStrategy();
            _cooldowns = new DamageCooldownTracker(damageCooldown);
        }

        public void ReceiveDamage(DamageContext ctx)
        {
            if (_bridge == null) return;
            var system = _bridge.GetResourceSystem();
            if (system == null) return;

            if (!_cooldowns.CanDealDamage(ctx.AttackerId, ctx.TargetId))
                return;

            float finalDamage = _strategy.CalculateDamage(ctx);
            system.Modify(targetResource, -finalDamage);
            
            // Usar FilteredEventBus para enviar apenas para os interessados
            var damageEvent = new DamageEvent(
                ctx.AttackerId,
                ctx.TargetId,
                finalDamage,
                targetResource,
                ctx.DamageType,
                ctx.HitPosition
            );

            // Enviar para o TARGET (quem recebeu o dano)
            FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, ctx.TargetId);
            
            // Opcional: enviar para o ATTACKER também (quem causou o dano)
            if (!string.IsNullOrEmpty(ctx.AttackerId))
            {
                FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, ctx.AttackerId);
            }

            DamageLifecycleManager.CheckDeath(system, targetResource);
        }

        public string GetReceiverId() => _actor.ActorId;
    }
}