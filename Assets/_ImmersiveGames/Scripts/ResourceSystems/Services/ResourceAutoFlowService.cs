using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Evento para efeitos visuais de auto flow (ex.: partículas pooled).
    /// </summary>
    public class AutoFlowEffectEvent : IEvent
    {
        public string ActorId { get; }
        public ResourceType ResourceType { get; }
        public float Delta { get; }
        public Vector3 Position { get; } // Posição para efeito visual (ex.: de SkinSystem)

        public AutoFlowEffectEvent(string actorId, ResourceType resourceType, float delta, Vector3 position)
        {
            ActorId = actorId;
            ResourceType = resourceType;
            Delta = delta;
            Position = position;
        }
    }

    /// <summary>
    /// Serviço puro para auto flow (regen/drain), integrado com bind e pooling para efeitos.
    /// </summary>
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceAutoFlowService : IDisposable
    {
        private readonly ResourceSystem _resourceSystem;
        private readonly IActorResourceOrchestrator _orchestrator;
        private readonly IResourceLinkService _linkService;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private readonly Dictionary<ResourceType, ResourceAutoFlowConfig> _configs = new();

        public bool IsPaused { get; private set; }

        public ResourceAutoFlowService(ResourceSystem resourceSystem, IActorResourceOrchestrator orchestrator, bool startPaused = true)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            IsPaused = startPaused;

            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
                DependencyManager.Instance.RegisterGlobal<IResourceLinkService>(_linkService);
            }

            RefreshConfigsFromResourceSystem();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;

            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Inicializado com {_configs.Count} recursos com autoflow para Actor {_resourceSystem.EntityId}");
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
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

            foreach (var (resourceType, _) in _resourceSystem.GetAll())
            {
                var inst = _resourceSystem.GetInstanceConfig(resourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    _configs[resourceType] = inst.autoFlowConfig;
                    _timers[resourceType] = 0f;
                    DebugUtility.LogVerbose<ResourceAutoFlowService>($"Configurado autoflow para {resourceType}: " +
                        $"Fill={inst.autoFlowConfig.autoFill}, Drain={inst.autoFlowConfig.autoDrain}");
                }
            }
        }

        public void Process(float deltaTime)
        {
            if (IsPaused || _configs.Count == 0) return;

            // Sincronização com bind: Pausar se canvas não pronto
            if (!_orchestrator.IsCanvasRegisteredForActor(_resourceSystem.EntityId))
            {
                DebugUtility.LogVerbose<ResourceAutoFlowService>($"Aguardando canvas pronto para {_resourceSystem.EntityId}");
                return;
            }

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
                totalDelta -= _linkService.ProcessLinkedDrain(_resourceSystem.EntityId, resourceType, Mathf.Abs(perTickAmount) * ticks, _resourceSystem);
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

            // Disparar evento para efeitos visuais (integrável com pooling)
            var effectEvent = new AutoFlowEffectEvent(_resourceSystem.EntityId, resourceType, totalDelta, Vector3.zero); // Posição de SkinSystem
            EventBus<AutoFlowEffectEvent>.Raise(effectEvent);

            float perTickAmount = CalculatePerTickAmount(resourceType, cfg);
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Aplicado {totalDelta:F2} em {resourceType} " +
                $"({ticks} ticks de {perTickAmount:F2})");
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
            foreach (var key in _timers.Keys.ToList())
            {
                _timers[key] = 0f;
            }
            DebugUtility.LogVerbose<ResourceAutoFlowService>($"Timers resetados para {_timers.Count} recursos");
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