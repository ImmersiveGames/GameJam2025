using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    
    public class ResourceThresholdBridge : ResourceBridgeBase
    {
        private ResourceThresholdService _service;
        private EventBinding<ResourceThresholdEvent> _binding;

        protected override void OnServiceInitialized()
        {
            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>($"ResourceSystem null para {actor.ActorId}");
                enabled = false;
                return;
            }

            IReadOnlyDictionary<ResourceType, IResourceValue> all = resourceSystem.GetAll();
            if (all.Count == 0)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"Sem recursos em {actor.ActorId}. Desativando.");
                enabled = false;
                return;
            }

            _service = new ResourceThresholdService(resourceSystem);
            _binding = new EventBinding<ResourceThresholdEvent>(OnThresholdEvent);
            FilteredEventBus<ResourceThresholdEvent>.Register(_binding, actor.ActorId);

            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"✅ ThresholdBridge ativo para {actor.ActorId}",
                DebugUtility.Colors.Success);
            _service.ForceCheck();
        }

        private void OnThresholdEvent(ResourceThresholdEvent evt)
        {
            if (evt.ActorId != actor.ActorId) return;

            EventBus<ResourceVisualFeedbackEvent>.Raise(
                new ResourceVisualFeedbackEvent(evt.ActorId, evt.ResourceType, evt.Threshold, evt.IsAscending));

            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"🔔 Threshold: {evt.ResourceType} → {evt.Threshold:P0} ({(evt.IsAscending ? "↑" : "↓")})");
        }

        protected override void OnServiceDispose()
        {
            FilteredEventBus<ResourceThresholdEvent>.Unregister(actor.ActorId);
            _service?.Dispose();
            _service = null;
        }

        [ContextMenu("🔔 Force Threshold Check")]
        private void Force() => _service?.ForceCheck();
    }
}
