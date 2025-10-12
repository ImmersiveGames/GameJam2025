using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceThresholdBridge : ResourceBridgeBase
    {
        private ResourceThresholdService _thresholdService;
        private EventBinding<ResourceThresholdEvent>? _thresholdBinding;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            IReadOnlyDictionary<ResourceType, IResourceValue> allResources = resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("ResourceSystem não tem recursos configurados");
                return false;
            }

            _thresholdService = new ResourceThresholdService(resourceSystem);
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ ThresholdService criado com {allResources.Count} recursos");

            // Cria binding usando EventBinding
            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdEvent);
            EventBus<ResourceThresholdEvent>.Register(_thresholdBinding);

            OnServiceInitialized();
            return true;
        }

        protected override void OnServiceInitialized()
        {
            _thresholdService?.ForceCheck();
        }

        private void OnThresholdEvent(ResourceThresholdEvent evt)
        {
            if (evt.ActorId != resourceSystem.EntityId)
                return;

            DebugUtility.LogVerbose<ResourceThresholdBridge>(
                $"🔔 Threshold cruzado: {evt.ResourceType} -> {evt.Threshold:P0} ({(evt.IsAscending ? "↑" : "↓")})");

            // Emite evento visual opcional para UI
            EventBus<ResourceVisualFeedbackEvent>.Raise(
                new ResourceVisualFeedbackEvent(evt.ActorId, evt.ResourceType, evt.Threshold, evt.IsAscending));
        }

        protected override void OnServiceDispose()
        {
            if (_thresholdBinding != null)
            {
                EventBus<ResourceThresholdEvent>.Unregister(_thresholdBinding);
                _thresholdBinding = null;
            }

            _thresholdService?.Dispose();
            _thresholdService = null;
        }

        public void ContextForce()
        {
            if (!initialized)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>("Tentando inicializar via ContextMenu...");
                initialized = TryInitializeService();
            }

            if (initialized)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>("Forçando verificação via ContextMenu");
                _thresholdService?.ForceCheck();
            }
            else
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("Não foi possível inicializar para forçar verificação");
            }
        }
    }
}
