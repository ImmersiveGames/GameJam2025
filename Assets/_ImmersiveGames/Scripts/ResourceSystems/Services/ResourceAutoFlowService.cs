using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro para autoflow (regen/drain).
    /// Chamado via Process(deltaTime) pelo bridge.
    /// Aplica mudanças por ResourceSystem. Modify(...) para garantir fluxo único de alterações.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceAutoFlowService : IDisposable
    {
        private readonly ResourceSystem _resourceSystem;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private readonly Dictionary<ResourceType, ResourceAutoFlowConfig> _configs = new();

        private readonly IResourceLinkService _linkService;
        public bool IsPaused { get; private set; }

        public ResourceAutoFlowService(ResourceSystem resourceSystem, bool startPaused = true)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            IsPaused = startPaused;
            
            // Obter serviço de links
            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
            }
            RefreshConfigsFromResourceSystem();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;

            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Inicializado com {_configs.Count} recursos com autoflow");
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            // Make sure timer entries exist (keeps in sync if resources were added later)
            if (!_timers.ContainsKey(evt.ResourceType))
            {
                var inst = _resourceSystem.GetInstanceConfig(evt.ResourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    _timers[evt.ResourceType] = 0f;
                    _configs[evt.ResourceType] = inst.autoFlowConfig;
                    DebugUtility.LogVerbose<ResourceAutoFlowService>($"Novo recurso com autoflow detectado: {evt.ResourceType}");
                }
            }
        }

        private void RefreshConfigsFromResourceSystem()
        {
            _timers.Clear();
            _configs.Clear();

            foreach (KeyValuePair<ResourceType, IResourceValue> kv in _resourceSystem.GetAll())
            {
                var type = kv.Key;
                var inst = _resourceSystem.GetInstanceConfig(type);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    _configs[type] = inst.autoFlowConfig;
                    _timers[type] = 0f;
                    DebugUtility.LogVerbose<ResourceAutoFlowService>($"Configurado autoflow para {type}: " +
                             $"Fill={inst.autoFlowConfig.autoFill}, " +
                             $"Drain={inst.autoFlowConfig.autoDrain}");
                }
            }
        }

        public void Process(float deltaTime)
        {
            if (IsPaused || _configs.Count == 0) return;

            foreach (var resourceType in _configs.Keys.ToList())
            {
                var cfg = _configs[resourceType];
                if (cfg == null) continue;

                if (IsInRegenDelayAfterDamage(cfg))
                {
                    _timers[resourceType] = 0f;
                    continue;
                }

                ProcessResourceTick(resourceType, cfg, deltaTime);
            }
        }

        private bool IsInRegenDelayAfterDamage(ResourceAutoFlowConfig cfg)
        {
            return cfg.autoFill && 
                   cfg.regenDelayAfterDamage > 0f && 
                   Time.time - _resourceSystem.LastDamageTime < cfg.regenDelayAfterDamage;
        }

        private void ProcessResourceTick(ResourceType resourceType, ResourceAutoFlowConfig cfg, float deltaTime)
        {
            _timers[resourceType] += deltaTime;

            if (_timers[resourceType] < cfg.tickInterval) return;

            int ticks = CalculateTicksAndUpdateTimer(resourceType, cfg);
            float totalDelta = CalculateTotalDelta(resourceType, cfg, ticks);

            ApplyResourceChange(resourceType, totalDelta, ticks, cfg);
        }

        private int CalculateTicksAndUpdateTimer(ResourceType resourceType, ResourceAutoFlowConfig cfg)
        {
            int ticks = Mathf.FloorToInt(_timers[resourceType] / cfg.tickInterval);
            _timers[resourceType] -= ticks * cfg.tickInterval;
            return ticks;
        }

        private float CalculateTotalDelta(ResourceType resourceType, ResourceAutoFlowConfig cfg, int ticks)
        {
            float perTickAmount = CalculatePerTickAmount(resourceType, cfg);
            float totalDelta = 0f;

            if (cfg.autoDrain)
            {
                totalDelta -= ProcessDrainWithLinks(resourceType, Mathf.Abs(perTickAmount) * ticks);
            }

            if (cfg.autoFill)
            {
                totalDelta += Mathf.Abs(perTickAmount) * ticks;
            }

            return totalDelta;
        }

        private float CalculatePerTickAmount(ResourceType resourceType, ResourceAutoFlowConfig cfg)
        {
            if (cfg.usePercentage)
            {
                float maxValue = _resourceSystem.Get(resourceType)?.GetMaxValue() ?? 0f;
                return maxValue * cfg.amountPerTick / 100f;
            }

            return cfg.amountPerTick;
        }

        private void ApplyResourceChange(ResourceType resourceType, float totalDelta, int ticks, ResourceAutoFlowConfig cfg)
        {
            if (Mathf.Abs(totalDelta) <= 0.0001f) return;

            _resourceSystem.Modify(resourceType, totalDelta);

            float perTickAmount = CalculatePerTickAmount(resourceType, cfg);
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Aplicado {totalDelta:F2} em {resourceType} " +
                     $"({ticks} ticks de {perTickAmount:F2})");
        }
        private float ProcessDrainWithLinks(ResourceType resourceType, float desiredDrain)
        {
            // Verificar se há link para este recurso
            var linkConfig = _linkService.GetLink(_resourceSystem.EntityId, resourceType);
            if (linkConfig == null || !linkConfig.affectTargetWithAutoFlow)
            {
                // Comportamento normal se não há link ou não afeta auto-flow
                return desiredDrain;
            }

            var sourceResource = _resourceSystem.Get(resourceType);
            var targetResource = _resourceSystem.Get(linkConfig.targetResource);

            if (sourceResource == null || targetResource == null) 
                return desiredDrain;

            // Verificar condições de transferência
            if (!linkConfig.ShouldTransfer(sourceResource.GetCurrentValue(), sourceResource.GetMaxValue()))
            {
                return desiredDrain;
            }

            // Calcular drenagem considerando o link
            float sourceAvailable = sourceResource.GetCurrentValue();
            float sourceDrain = Mathf.Min(desiredDrain, sourceAvailable);
            float remainingDrain = desiredDrain - sourceDrain;

            // Aplicar drenagem restante no recurso alvo
            if (remainingDrain > 0)
            {
                _resourceSystem.Modify(linkConfig.targetResource, -remainingDrain);
            }

            DebugUtility.LogVerbose<ResourceAutoFlowService>($"AutoFlow link: {resourceType} drained {sourceDrain}, {linkConfig.targetResource} drained {remainingDrain}");

            return sourceDrain;
        }
        public void Pause() 
        { 
            IsPaused = true;
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Pausado");
        }
        
        public void Resume() 
        { 
            IsPaused = false;
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Retomado");
        }
        
        public void Toggle() 
        { 
            IsPaused = !IsPaused;
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Alternado para {(IsPaused ? "Pausado" : "Executando")}");
        }
        
        public void ResetTimers()
        {
            var keys = _timers.Keys.ToList();
            foreach (var k in keys) _timers[k] = 0f;
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Timers resetados para {keys.Count} recursos");
        }

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _timers.Clear();
            _configs.Clear();
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Dispose realizado");
        }
    }
}