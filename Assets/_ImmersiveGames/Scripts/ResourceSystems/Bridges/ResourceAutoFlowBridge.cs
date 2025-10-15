using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceAutoFlowBridge : ResourceBridgeBase
    {
        [SerializeField] private bool startPaused = true;

        private ResourceAutoFlowService _autoFlow;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>(
                    $"ResourceSystem é null na inicialização do AutoFlow ({Actor.ActorId})");
                return false;
            }

            bool hasAutoFlowResources = CheckForAutoFlowResources();
            if (!hasAutoFlowResources)
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>(
                    $"Nenhum recurso com AutoFlow configurado em {Actor.ActorId}. Desativando bridge (sem erro).");
                enabled = false;
                return true; // ✅ Nenhum erro, só sem necessidade
            }

            _autoFlow = new ResourceAutoFlowService(resourceSystem, _orchestrator, startPaused);
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>(
                $"✅ AutoFlowService criado com {CountAutoFlowResources()} recursos com autoflow");
            return true;
        }

        protected override void OnServiceInitialized()
        {
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"🚀 AutoFlowService inicializado para {Actor.ActorId}");
            if (!startPaused)
                _autoFlow?.Resume();
        }

        protected override void OnServiceDispose()
        {
            _autoFlow?.Dispose();
            _autoFlow = null;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlowService disposed");
        }

        protected void Update()
        {
            if (initialized && _autoFlow != null)
                _autoFlow.Process(Time.deltaTime);
        }

        private bool CheckForAutoFlowResources()
        {
            if (resourceSystem == null) return false;

            foreach (var (type, _) in resourceSystem.GetAll())
            {
                var inst = resourceSystem.GetInstanceConfig(type);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                    return true;
            }

            return false;
        }

        private int CountAutoFlowResources()
        {
            int count = 0;
            if (resourceSystem == null) return count;

            foreach (var (type, _) in resourceSystem.GetAll())
            {
                var inst = resourceSystem.GetInstanceConfig(type);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                    count++;
            }
            return count;
        }

        [ContextMenu("⏸️ Pause AutoFlow")]
        public void ContextPause()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("AutoFlow ainda não inicializado.");
                return;
            }
            _autoFlow?.Pause();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow pausado");
        }

        [ContextMenu("▶️ Resume AutoFlow")]
        public void ContextResume()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("AutoFlow ainda não inicializado.");
                return;
            }
            _autoFlow?.Resume();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow retomado");
        }

        [ContextMenu("🔄 Toggle AutoFlow")]
        public void ContextToggle()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("AutoFlow ainda não inicializado.");
                return;
            }
            _autoFlow?.Toggle();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>(
                $"AutoFlow alternado. Pausado: {_autoFlow?.IsPaused}");
        }

        [ContextMenu("🔄 Reset AutoFlow Timers")]
        public void ContextReset()
        {
            if (!initialized)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("AutoFlow ainda não inicializado.");
                return;
            }
            _autoFlow?.ResetTimers();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Timers resetados");
        }
    }
}
