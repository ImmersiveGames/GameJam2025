using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro para autoflow (regen/drain).
    /// Chamado via Process(deltaTime) pelo bridge.
    /// Aplica mudanças por ResourceSystem. Modify(...) para garantir fluxo único de alterações.
    /// </summary>
    public class ResourceAutoFlowService : IDisposable
    {
        private readonly ResourceSystem _resourceSystem;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private readonly Dictionary<ResourceType, ResourceAutoFlowConfig> _configs = new();

        public bool IsPaused { get; private set; }

        public ResourceAutoFlowService(ResourceSystem resourceSystem, bool startPaused = true)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            IsPaused = startPaused;
            RefreshConfigsFromResourceSystem();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;

            DebugUtility.LogVerbose<EntityResourceBridge>($"Inicializado com {_configs.Count} recursos com autoflow");
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
                    DebugUtility.LogVerbose<EntityResourceBridge>($"Novo recurso com autoflow detectado: {evt.ResourceType}");
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
                    DebugUtility.LogVerbose<EntityResourceBridge>($"Configurado autoflow para {type}: " +
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

                // Verificar delay de regeneração após dano
                if (cfg.autoFill && cfg.regenDelayAfterDamage > 0f)
                {
                    if (Time.time - _resourceSystem.LastDamageTime < cfg.regenDelayAfterDamage)
                    {
                        _timers[resourceType] = 0f; // Reset timer enquanto em delay
                        continue;
                    }
                }

                _timers[resourceType] += deltaTime;

                if (_timers[resourceType] >= cfg.tickInterval)
                {
                    int ticks = Mathf.FloorToInt(_timers[resourceType] / cfg.tickInterval);
                    _timers[resourceType] -= ticks * cfg.tickInterval;

                    float perTickAmount = cfg.usePercentage
                        ? (_resourceSystem.Get(resourceType)?.GetMaxValue() ?? 0f) * cfg.amountPerTick / 100f
                        : cfg.amountPerTick;

                    float totalDelta = 0f;
                    if (cfg.autoDrain) totalDelta -= Mathf.Abs(perTickAmount) * ticks;
                    if (cfg.autoFill) totalDelta += Mathf.Abs(perTickAmount) * ticks;

                    if (Mathf.Abs(totalDelta) > 0.0001f)
                    {
                        _resourceSystem.Modify(resourceType, totalDelta);
                        DebugUtility.LogVerbose<EntityResourceBridge>($"Aplicado {totalDelta:F2} em {resourceType} " +
                                 $"({ticks} ticks de {perTickAmount:F2})");
                    }
                }
            }
        }

        public void Pause() 
        { 
            IsPaused = true;
            DebugUtility.LogVerbose<EntityResourceBridge>($"Pausado");
        }
        
        public void Resume() 
        { 
            IsPaused = false;
            DebugUtility.LogVerbose<EntityResourceBridge>($"Retomado");
        }
        
        public void Toggle() 
        { 
            IsPaused = !IsPaused;
            DebugUtility.LogVerbose<EntityResourceBridge>($"Alternado para {(IsPaused ? "Pausado" : "Executando")}");
        }
        
        public void ResetTimers()
        {
            var keys = _timers.Keys.ToList();
            foreach (var k in keys) _timers[k] = 0f;
            DebugUtility.LogVerbose<EntityResourceBridge>($"Timers resetados para {keys.Count} recursos");
        }

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _timers.Clear();
            _configs.Clear();
            DebugUtility.LogVerbose<EntityResourceBridge>($"Dispose realizado");
        }
    }
}