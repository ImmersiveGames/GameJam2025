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
                DebugUtility.LogWarning<ResourceThresholdBridge>(
                    $"ResourceSystem é null na inicialização do Threshold ({Actor.ActorId})");
                return false;
            }

            IReadOnlyDictionary<ResourceType, IResourceValue> allResources = resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>(
                    $"ResourceSystem sem recursos para {Actor.ActorId}. Desativando bridge.");
                enabled = false;
                return true;
            }

            _thresholdService = new ResourceThresholdService(resourceSystem);
            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"✅ ThresholdService criado com {allResources.Count} recursos");

            bool hasThresholds = CheckForThresholdConfigurations();
            if (!hasThresholds)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>(
                    $"Nenhum threshold configurado para {Actor.ActorId}. Serviço criado, mas inativo.");
            }

            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdEvent);
            FilteredEventBus<ResourceThresholdEvent>.Register(_thresholdBinding, Actor.ActorId);
            DebugUtility.LogVerbose<ResourceThresholdBridge>("✅ Threshold event binding registrado com scope");

            return true;
        }

        protected override void OnServiceInitialized()
        {
            _thresholdService?.ForceCheck();
            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"🚀 ThresholdService inicializado para {Actor.ActorId}");
        }

        private void OnThresholdEvent(ResourceThresholdEvent evt)
        {
            if (evt.ActorId != resourceSystem.EntityId)
                return;

            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"🔔 Threshold cruzado: {evt.ResourceType} -> {evt.Threshold:P0} ({(evt.IsAscending ? "↑" : "↓")})");

            EventBus<ResourceVisualFeedbackEvent>.Raise(
                new ResourceVisualFeedbackEvent(evt.ActorId, evt.ResourceType, evt.Threshold, evt.IsAscending));
        }

        protected override void OnServiceDispose()
        {
            FilteredEventBus<ResourceThresholdEvent>.Unregister(Actor.ActorId);
            _thresholdService?.Dispose();
            _thresholdService = null;
            DebugUtility.LogVerbose<ResourceThresholdBridge>("ThresholdService disposed");
        }

        internal bool CheckForThresholdConfigurations()
        {
            if (resourceSystem == null) return false;

            bool hasAny = false;
            foreach (var (resourceType, _) in resourceSystem.GetAll())
            {
                var config = resourceSystem.GetInstanceConfig(resourceType);
                if (config?.thresholdConfig != null && config.thresholdConfig.thresholds.Length > 0)
                {
                    hasAny = true;
                    DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ Thresholds encontrados para {resourceType}");
                }
            }

            if (!hasAny)
                DebugUtility.LogVerbose<ResourceThresholdBridge>("⚠️ Nenhuma configuração de threshold encontrada");

            return hasAny;
        }

        [ContextMenu("🔔 Force Threshold Check")]
        public void ContextForce()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("Threshold ainda não inicializado.");
                return;
            }

            _thresholdService?.ForceCheck();
            DebugUtility.LogVerbose<ResourceThresholdBridge>("🔔 Forçando verificação de thresholds");
        }
    }
}
