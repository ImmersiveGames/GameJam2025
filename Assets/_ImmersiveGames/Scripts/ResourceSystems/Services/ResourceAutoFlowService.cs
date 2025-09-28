using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro para autoflow (regen/drain).
    /// Chamado via Process(deltaTime) pelo bridge.
    /// Aplica mudanças por ResourceSystemService. Modify(...) para garantir fluxo único de alterações.
    /// </summary>
    public class ResourceAutoFlowService : IDisposable
    {
        private readonly ResourceSystemService _resourceSystem;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private readonly Dictionary<ResourceType, ResourceAutoFlowConfig> _configs = new();

        public bool IsPaused { get; private set; }

        public ResourceAutoFlowService(ResourceSystemService resourceSystem, bool startPaused = true)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            IsPaused = startPaused;
            RefreshConfigsFromResourceSystem();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            // Make sure timer entries exist (keeps in sync if resources were added later)
            if (!_timers.ContainsKey(evt.ResourceType))
            {
                var inst = _resourceSystem.GetInstanceConfig(evt.ResourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                    _timers[evt.ResourceType] = 0f;
            }
        }

        private void RefreshConfigsFromResourceSystem()
        {
            _timers.Clear();
            _configs.Clear();

            foreach (var kv in _resourceSystem.GetAll())
            {
                var type = kv.Key;
                var inst = _resourceSystem.GetInstanceConfig(type);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    _configs[type] = inst.autoFlowConfig;
                    _timers[type] = 0f;
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

                if (cfg.autoFill && cfg.regenDelayAfterDamage > 0f)
                {
                    if (Time.time - _resourceSystem.LastDamageTime < cfg.regenDelayAfterDamage)
                        continue;
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
                        _resourceSystem.Modify(resourceType, totalDelta);
                }
            }
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void Toggle() => IsPaused = !IsPaused;
        public void ResetTimers()
        {
            var keys = _timers.Keys.ToList();
            foreach (var k in keys) _timers[k] = 0f;
        }

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _timers.Clear();
            _configs.Clear();
        }
    }
}
