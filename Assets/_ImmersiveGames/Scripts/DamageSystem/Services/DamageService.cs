using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.DamageSystem.Services
{
    public sealed class DamageService
    {
        private readonly EffectService _effectService;
        private readonly IActorResourceOrchestrator _orchestrator;
        private readonly List<IDamageModifier> _modifiers = new();
        private readonly Dictionary<DamageType, GameObject> _effectPrefabs = new()
        {
            { DamageType.Physical, Resources.Load<GameObject>("Effects/PhysicalHitEffect") },
            { DamageType.Magical, Resources.Load<GameObject>("Effects/MagicalHitEffect") },
            { DamageType.Fire, Resources.Load<GameObject>("Effects/FireHitEffect") },
            { DamageType.Ice, Resources.Load<GameObject>("Effects/IceHitEffect") },
            { DamageType.Lightning, Resources.Load<GameObject>("Effects/LightningHitEffect") },
            { DamageType.Poison, Resources.Load<GameObject>("Effects/PoisonHitEffect") }
        };

        public DamageService(EffectService effectService = null, IActorResourceOrchestrator orchestrator = null)
        {
            _effectService = effectService;
            _orchestrator = orchestrator;
            DependencyManager.Instance.GetAll<IDamageModifier>(_modifiers);
        }

        public float CalculateFinalDamage(DamageContext ctx)
        {
            if (ctx == null) return 0f;
            float modified = ctx.Amount;
            foreach (var mod in _modifiers)
                modified = mod.Modify(ctx);
            return Mathf.Max(0f, modified);
        }

        public void ApplyPostDamageEffects(DamageContext ctx)
        {
            if (ctx == null) return;

            try
            {
                EventBus<DamageDealtEvent>.Raise(new DamageDealtEvent(ctx.Source, ctx.Target, ctx.Amount, ctx.DamageType, ctx.HitPosition));
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<DamageService>($"ApplyPostDamageEffects: failed to raise DamageDealtEvent: {ex}");
            }

            if (_effectService != null && _effectPrefabs.TryGetValue(ctx.DamageType, out var prefab) && prefab != null)
            {
                _effectService.SpawnEffect(prefab, ctx.HitPosition, Quaternion.identity);
            }
        }

        public bool TryApplyToResourceSystem(DamageContext ctx)
        {
            if (ctx?.Target == null) return false;

            var targetId = ctx.Target.ActorId;

            if (DependencyManager.Instance != null)
            {
                try
                {
                    if (DependencyManager.Instance.TryGetForObject(targetId, out ResourceSystem rs))
                    {
                        rs.Modify(ctx.ResourceType, -ctx.Amount);
                        DebugUtility.LogVerbose<DamageService>($"Applied damage via DependencyManager for {targetId}: {ctx.Amount}");
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    DebugUtility.LogError<DamageService>($"TryApplyToResourceSystem error (DependencyManager) for {targetId}: {ex}");
                }
            }

            if (_orchestrator != null)
            {
                try
                {
                    if (_orchestrator.TryGetActorResource(targetId, out var orchRs))
                    {
                        orchRs.Modify(ctx.ResourceType, -ctx.Amount);
                        DebugUtility.LogVerbose<DamageService>($"Applied damage via Orchestrator for {targetId}: {ctx.Amount}");
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    DebugUtility.LogError<DamageService>($"TryApplyToResourceSystem error (Orchestrator) for {targetId}: {ex}");
                }
            }

            return false;
        }
    }
}