using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceThresholdBridge : ResourceBridgeBase
    {
        private ResourceThresholdService _thresholdService;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            // Verificar se o ResourceSystem tem recursos configurados
            IReadOnlyDictionary<ResourceType, IResourceValue> allResources = resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>("ResourceSystem não tem recursos configurados");
                return false;
            }

            _thresholdService = new ResourceThresholdService(resourceSystem);
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ ThresholdService criado com {allResources.Count} recursos");

            OnServiceInitialized();
            return true;
        }

        protected override void OnServiceInitialized()
        {
            // Forçar verificação inicial
            _thresholdService?.ForceCheck();
        }

        protected override void OnServiceDispose()
        {
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