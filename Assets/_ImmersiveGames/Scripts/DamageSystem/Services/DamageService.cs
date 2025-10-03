// _ImmersiveGames/Scripts/DamageSystem/Services/DamageService.cs
using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Services
{
    public class DamageContext
    {
        public IActor Source { get; set; }
        public IActor Target { get; set; }
        public float Amount { get; set; }
        public DamageType DamageType { get; set; }
        public ResourceType ResourceType { get; set; } = ResourceType.Health;
        public Vector3 HitPosition { get; set; } = Vector3.zero;
    }

    public class DamageService
    {
        private readonly IEventBus<DamageDealtEvent> _damageBus;
        private readonly EffectService _effectService;
        private readonly IActorResourceOrchestrator _orchestrator;

        public DamageService(IEventBus<DamageDealtEvent> damageBus,
                             EffectService effectService,
                             IActorResourceOrchestrator orchestrator)
        {
            _damageBus = damageBus;
            _effectService = effectService;
            _orchestrator = orchestrator;
        }

        public void ApplyDamage(DamageContext ctx)
        {
            if (ctx == null || ctx.Target == null) return;

            // 1) compute final damage (hooks for resistances, crits, buffs)
            float finalDamage = CalculateFinalDamage(ctx);

            // 2) Apply to a resource system
            if (_orchestrator != null)
            {
                if (_orchestrator.TryGetActorResource(ctx.Target.ActorId, out var resourceSystem))
                {
                    resourceSystem.Modify(ctx.ResourceType, -finalDamage);
                }
                else
                {
                    // Fallback: try via DependencyManager per-object
                    if (DependencyManager.Instance.TryGetForObject(ctx.Target.ActorId, out ResourceSystem rs))
                        rs.Modify(ctx.ResourceType, -finalDamage);
                }
            }

            // 3) Fire global damage event
            _damageBus?.Raise(new DamageDealtEvent(ctx.Source, ctx.Target, finalDamage, ctx.DamageType, ctx.HitPosition));

            // 4) Spawn hit effect
            _effectService?.SpawnHitEffect(ctx.DamageType, ctx.HitPosition);

            // 5) Optionally return value or trigger other flows
        }

        protected virtual float CalculateFinalDamage(DamageContext ctx)
        {
            // TODO: integrate resistances/buffs/shield
            // By default, just pass-through
            return Mathf.Max(0f, ctx.Amount);
        }
    }
}
