using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.DamageSystem.Services
{
    public sealed class DamageService
    {
        private readonly EffectService _effectService;
        private readonly IActorResourceOrchestrator _orchestrator;
        private readonly List<IDamageModifier> _modifiers = new();
        private readonly Dictionary<DamageType, GameObject> _effectPrefabs = new()
        {
            { DamageType.Physical, Resources.Load<GameObject>($"Effects/PhysicalHitEffect") },
            { DamageType.Magical, Resources.Load<GameObject>($"Effects/MagicalHitEffect") },
            { DamageType.Fire, Resources.Load<GameObject>($"Effects/FireHitEffect") },
            { DamageType.Ice, Resources.Load<GameObject>($"Effects/IceHitEffect") },
            { DamageType.Lightning, Resources.Load<GameObject>($"Effects/LightningHitEffect") },
            { DamageType.Poison, Resources.Load<GameObject>($"Effects/PoisonHitEffect") }
        };

        public DamageService(EffectService effectService = null, IActorResourceOrchestrator orchestrator = null)
        {
            _effectService = effectService;
            _orchestrator = orchestrator;
            if (_orchestrator == null)
            {
                DependencyManager.Instance.TryGetGlobal(out _orchestrator);
            }
            
            // CORREÇÃO: Obter modificadores de forma segura
            try
            {
                DependencyManager.Instance.GetAll(_modifiers);
                DebugUtility.LogVerbose<DamageService>($"Carregados {_modifiers.Count} modificadores de dano");
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogWarning<DamageService>($"Erro ao carregar modificadores de dano: {ex.Message}");
            }

            // CORREÇÃO: Registrar-se como serviço global
            if (!DependencyManager.Instance.TryGetGlobal(out DamageService _))
            {
                DependencyManager.Instance.RegisterGlobal(this);
                DebugUtility.LogVerbose<DamageService>("DamageService registrado globalmente");
            }
        }

        public float CalculateFinalDamage(DamageContext ctx)
        {
            if (ctx == null) return 0f;
            float modified = ctx.Amount;
            
            // CORREÇÃO: Aplicar modificadores de forma segura
            foreach (var mod in _modifiers)
            {
                try
                {
                    modified = mod.Modify(ctx);
                }
                catch (System.Exception ex)
                {
                    DebugUtility.LogError<DamageService>($"Erro no modificador {mod.GetType().Name}: {ex.Message}");
                }
            }
            
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

        // CORREÇÃO: Este método não é mais usado pelo DamageReceiver, mas mantido para compatibilidade
        public bool TryApplyToResourceSystem(DamageContext ctx)
        {
            if (ctx?.Target == null) return false;

            string targetId = ctx.Target.ActorId;

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

            if (_orchestrator == null) return false;
            
            try
            {
                var resourceSystem = _orchestrator.GetActorResourceSystem(targetId);
                if (resourceSystem != null)
                {
                    resourceSystem.Modify(ctx.ResourceType, -ctx.Amount);
                    DebugUtility.LogVerbose<DamageService>($"Applied damage via Orchestrator for {targetId}: {ctx.Amount}");
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<DamageService>($"TryApplyToResourceSystem error (Orchestrator) for {targetId}: {ex}");
            }

            return false;
        }
    }

    // CORREÇÃO: Manter as classes de contexto e enum no mesmo arquivo para organização
    public class DamageContext
    {
        public IActor Source { get; set; }
        public IActor Target { get; set; }
        public float Amount { get; set; }
        public DamageType DamageType { get; set; }
        public ResourceType ResourceType { get; set; } = ResourceType.Health;
        public Vector3 HitPosition { get; set; } = Vector3.zero;
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Fire,
        Ice,
        Lightning,
        Poison
    }
}