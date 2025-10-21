using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public class AutoFlowEffectEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public float Delta { get; }
        public Vector3 Position { get; }

        public AutoFlowEffectEvent(string actorId, ResourceType resourceType, float delta, Vector3 position)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            Delta = delta;
            Position = position;
        }
    }

    
    public class ResourceAutoFlowService : IDisposable
    {
        private readonly ResourceSystem _resourceSystem;
        private readonly IResourceLinkService _linkService;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private readonly Dictionary<ResourceType, ResourceAutoFlowConfig> _configs = new();

        public bool IsPaused { get; private set; }

        public ResourceAutoFlowService(ResourceSystem resourceSystem, bool startPaused = true)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            IsPaused = startPaused;

            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Instance.RegisterGlobal(_linkService);
            }

            RefreshConfigs();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;

            DebugUtility.LogVerbose<ResourceAutoFlowService>(
                $"🧩 AutoFlow configurado: {_configs.Count} recursos ativos para {_resourceSystem.EntityId}");
        }

        private void RefreshConfigs()
        {
            _configs.Clear();
            _timers.Clear();

            foreach (var (type, _) in _resourceSystem.GetAll())
            {
                var cfg = _resourceSystem.GetInstanceConfig(type);
                if (cfg is { hasAutoFlow: true } && cfg.autoFlowConfig != null)
                {
                    _configs[type] = cfg.autoFlowConfig;
                    _timers[type] = 0f;
                }
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            var inst = _resourceSystem.GetInstanceConfig(evt.ResourceType);
            if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null && !_configs.ContainsKey(evt.ResourceType))
            {
                _configs[evt.ResourceType] = inst.autoFlowConfig;
                _timers[evt.ResourceType] = 0f;
                DebugUtility.LogVerbose<ResourceAutoFlowService>($"Novo AutoFlow detectado: {evt.ResourceType}");
            }
        }

        public void Process(float deltaTime)
        {
            if (IsPaused || _configs.Count == 0) return;

            foreach (var (type, cfg) in _configs.ToList())
            {
                _timers[type] += deltaTime;
                if (_timers[type] < cfg.tickInterval) continue;

                _timers[type] -= cfg.tickInterval;

                float perTick = CalculateDelta(cfg, type);
                if (Mathf.Abs(perTick) <= 0.0001f) continue;

                _resourceSystem.Modify(type, perTick);

                EventBus<AutoFlowEffectEvent>.Raise(
                    new AutoFlowEffectEvent(_resourceSystem.EntityId, type, perTick, Vector3.zero)
                );
            }
        }

        private float CalculateDelta(ResourceAutoFlowConfig cfg, ResourceType type)
        {
            float amount = cfg.usePercentage
                ? (_resourceSystem.Get(type)?.GetMaxValue() ?? 0) * cfg.amountPerTick / 100f
                : cfg.amountPerTick;

            float delta = 0f;
            if (cfg.autoFill) delta += amount;
            if (cfg.autoDrain) delta -= _linkService.ProcessLinkedDrain(_resourceSystem.EntityId, type, amount, _resourceSystem);
            return delta;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void Toggle() => IsPaused = !IsPaused;

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _timers.Clear();
            _configs.Clear();
        }
    }
}
