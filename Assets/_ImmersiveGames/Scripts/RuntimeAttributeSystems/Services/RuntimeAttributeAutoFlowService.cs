using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services
{
    public class RuntimeAttributeAutoFlowEffectEvent : IEvent
    {
        public string ActorId { get; }
        public RuntimeAttributeType RuntimeAttributeType { get; }
        public float Delta { get; }
        public Vector3 Position { get; }

        public RuntimeAttributeAutoFlowEffectEvent(string actorId, RuntimeAttributeType runtimeAttributeType, float delta, Vector3 position)
        {
            ActorId = actorId;
            RuntimeAttributeType = runtimeAttributeType;
            Delta = delta;
            Position = position;
        }
    }

    
    public class RuntimeAttributeAutoFlowService : IDisposable
    {
        private readonly RuntimeAttributeContext _runtimeAttributeContext;
        private readonly IRuntimeAttributeLinkService _linkService;
        private readonly Dictionary<RuntimeAttributeType, float> _timers = new();
        private readonly Dictionary<RuntimeAttributeType, RuntimeAttributeAutoFlowConfig> _configs = new();

        public bool IsPaused { get; private set; }

        public RuntimeAttributeAutoFlowService(RuntimeAttributeContext runtimeAttributeContext, bool startPaused = true)
        {
            _runtimeAttributeContext = runtimeAttributeContext ?? throw new ArgumentNullException(nameof(runtimeAttributeContext));
            IsPaused = startPaused;

            if (!DependencyManager.Provider.TryGetGlobal(out _linkService))
            {
                _linkService = new RuntimeAttributeLinkService();
                DependencyManager.Provider.RegisterGlobal(_linkService);
            }

            RefreshConfigs();
            _runtimeAttributeContext.ResourceUpdated += OnResourceUpdated;

            DebugUtility.LogVerbose<RuntimeAttributeAutoFlowService>(
                $"🧩 AutoFlow configurado: {_configs.Count} recursos ativos para {_runtimeAttributeContext.EntityId}");
        }

        private void RefreshConfigs()
        {
            _configs.Clear();
            _timers.Clear();

            foreach (var (type, _) in _runtimeAttributeContext.GetAll())
            {
                var cfg = _runtimeAttributeContext.GetInstanceConfig(type);
                if (cfg is { hasAutoFlow: true } && cfg.autoFlowConfig != null)
                {
                    _configs[type] = cfg.autoFlowConfig;
                    _timers[type] = 0f;
                }
            }
        }

        private void OnResourceUpdated(RuntimeAttributeUpdateEvent evt)
        {
            var inst = _runtimeAttributeContext.GetInstanceConfig(evt.RuntimeAttributeType);
            if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null && !_configs.ContainsKey(evt.RuntimeAttributeType))
            {
                _configs[evt.RuntimeAttributeType] = inst.autoFlowConfig;
                _timers[evt.RuntimeAttributeType] = 0f;
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowService>($"Novo AutoFlow detectado: {evt.RuntimeAttributeType}");
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

                _runtimeAttributeContext.Modify(type, perTick, RuntimeAttributeChangeSource.AutoFlow);

                EventBus<RuntimeAttributeAutoFlowEffectEvent>.Raise(
                    new RuntimeAttributeAutoFlowEffectEvent(_runtimeAttributeContext.EntityId, type, perTick, Vector3.zero)
                );
            }
        }

        private float CalculateDelta(RuntimeAttributeAutoFlowConfig cfg, RuntimeAttributeType type)
        {
            float amount = cfg.usePercentage
                ? (_runtimeAttributeContext.Get(type)?.GetMaxValue() ?? 0) * cfg.amountPerTick / 100f
                : cfg.amountPerTick;

            float delta = 0f;
            if (cfg.autoFill) delta += amount;
            if (cfg.autoDrain) delta -= _linkService.ProcessLinkedDrain(_runtimeAttributeContext.EntityId, type, amount, _runtimeAttributeContext, RuntimeAttributeChangeSource.AutoFlow);
            return delta;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void Toggle() => IsPaused = !IsPaused;

        public void Dispose()
        {
            _runtimeAttributeContext.ResourceUpdated -= OnResourceUpdated;
            _timers.Clear();
            _configs.Clear();
        }
    }
}
