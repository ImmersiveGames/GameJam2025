using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges
{
    
    public class RuntimeAttributeThresholdBridge : RuntimeAttributeBridgeBase
    {
        private RuntimeAttributeThresholdService _service;
        private EventBinding<RuntimeAttributeThresholdEvent> _binding;

        protected override void OnServiceInitialized()
        {
            if (runtimeAttributeContext == null)
            {
                DebugUtility.LogWarning<RuntimeAttributeThresholdBridge>($"RuntimeAttributeContext null para {actor.ActorId}");
                enabled = false;
                return;
            }

            IReadOnlyDictionary<RuntimeAttributeType, IRuntimeAttributeValue> all = runtimeAttributeContext.GetAll();
            if (all.Count == 0)
            {
                DebugUtility.LogVerbose<RuntimeAttributeThresholdBridge>($"Sem recursos em {actor.ActorId}. Desativando.");
                enabled = false;
                return;
            }

            _service = new RuntimeAttributeThresholdService(runtimeAttributeContext);
            _binding = new EventBinding<RuntimeAttributeThresholdEvent>(OnThresholdEvent);
            FilteredEventBus<RuntimeAttributeThresholdEvent>.Register(_binding, actor.ActorId);

            DebugUtility.LogVerbose<RuntimeAttributeThresholdBridge>(
                $"✅ ThresholdBridge ativo para {actor.ActorId}",
                DebugUtility.Colors.Success);
            _service.ForceCheck();
        }

        private void OnThresholdEvent(RuntimeAttributeThresholdEvent evt)
        {
            if (evt.ActorId != actor.ActorId) return;

            EventBus<RuntimeAttributeVisualFeedbackEvent>.Raise(
                new RuntimeAttributeVisualFeedbackEvent(evt.ActorId, evt.RuntimeAttributeType, evt.Threshold, evt.IsAscending));

            DebugUtility.LogVerbose<RuntimeAttributeThresholdBridge>(
                $"🔔 Threshold: {evt.RuntimeAttributeType} → {evt.Threshold:P0} ({(evt.IsAscending ? "↑" : "↓")})");
        }

        protected override void OnServiceDispose()
        {
            FilteredEventBus<RuntimeAttributeThresholdEvent>.Unregister(actor.ActorId);
            _service?.Dispose();
            _service = null;
        }

        [ContextMenu("🔔 Force Threshold Check")]
        private void Force() => _service?.ForceCheck();
    }
}
