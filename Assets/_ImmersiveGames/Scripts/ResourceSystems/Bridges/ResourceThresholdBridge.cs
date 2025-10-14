using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceThresholdBridge : ResourceBridgeBase
    {
        private ResourceThresholdService _thresholdService;
        private EventBinding<ResourceThresholdEvent> _thresholdBinding;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("ResourceSystem é null na inicialização do Threshold");
                return false;
            }

            IReadOnlyDictionary<ResourceType, IResourceValue> allResources = resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("ResourceSystem não tem recursos configurados");
                return false;
            }

            _thresholdService = new ResourceThresholdService(resourceSystem);
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ ThresholdService criado com {allResources.Count} recursos");

            bool hasThresholds = CheckForThresholdConfigurations();
            if (!hasThresholds)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>("Nenhum threshold configurado. Serviço criado mas sem trabalho.");
            }

            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdEvent);
            FilteredEventBus<ResourceThresholdEvent>.Register(_thresholdBinding, Actor.ActorId); // Registrado com scope = actorId

            DebugUtility.LogVerbose<ResourceThresholdBridge>("✅ Threshold event binding registrado com scope");
            return true;
        }

        protected override void OnServiceInitialized()
        {
            _thresholdService?.ForceCheck();
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"🚀 ThresholdService inicializado para {Actor.ActorId}");
        }

        private void OnThresholdEvent(ResourceThresholdEvent evt)
        {
            if (evt.ActorId != resourceSystem.EntityId)
                return;

            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"🔔 Threshold cruzado: {evt.ResourceType} -> {evt.Threshold:P0} ({(evt.IsAscending ? "↑" : "↓")})");

            // Emite evento visual opcional para UI, integrável com pooling
            EventBus<ResourceVisualFeedbackEvent>.Raise(
                new ResourceVisualFeedbackEvent(evt.ActorId, evt.ResourceType, evt.Threshold, evt.IsAscending));
        }

        protected override void OnServiceDispose()
        {
            FilteredEventBus<ResourceThresholdEvent>.Unregister(Actor.ActorId); // Unregister com scope

            _thresholdService?.Dispose();
            _thresholdService = null;
            DebugUtility.LogVerbose<ResourceThresholdBridge>("ThresholdService disposed");
        }

        protected override void Update()
        {
            base.Update();

            if (initialized && _thresholdService != null && _orchestrator.IsCanvasRegisteredForActor(Actor.ActorId))
            {
                // Lógica passiva - já é gerenciada por eventos filtrados
            }
        }
        // Novo método para debug
        public bool HasThresholdBinding()
        {
            return _thresholdBinding != null;
        }

        internal bool CheckForThresholdConfigurations()
        {
            if (resourceSystem == null) return false;

            foreach (var (resourceType, _) in resourceSystem.GetAll())
            {
                var config = resourceSystem.GetInstanceConfig(resourceType);
                if (config?.thresholdConfig != null && config.thresholdConfig.thresholds.Length > 0)
                {
                    DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ Thresholds encontrados para {resourceType}");
                    return true;
                }
            }

            DebugUtility.LogVerbose<ResourceThresholdBridge>("⚠️ Nenhuma configuração de threshold encontrada");
            return false;
        }

        [ContextMenu("🔔 Force Threshold Check")]
        public void ContextForce()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("Tentando inicializar via ContextMenu...");
                StartCoroutine(InitializeWithRetry());
                return;
            }

            if (initialized && _thresholdService != null)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>("🔔 Forçando verificação de thresholds via ContextMenu");
                _thresholdService.ForceCheck();
            }
            else
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("❌ Não foi possível forçar verificação - serviço não inicializado");
            }
        }
    }
}