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

        public bool HasAutoFlowService => _autoFlow != null;
        public bool IsAutoFlowActive => _autoFlow != null && !_autoFlow.IsPaused;

        protected override void OnServiceInitialized()
        {
            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>($"ResourceSystem null em {actor.ActorId}");
                enabled = false;
                return;
            }

            if (!HasAutoFlowResources())
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"Nenhum recurso AutoFlow em {actor.ActorId} — Bridge desativado.");
                enabled = false;
                return;
            }

            _autoFlow = new ResourceAutoFlowService(resourceSystem, startPaused);
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"🚀 AutoFlow inicializado para {actor.ActorId}");

            if (!startPaused) _autoFlow.Resume();
        }

        private void Update()
        {
            if (_autoFlow != null && IsInitialized && !_autoFlow.IsPaused)
                _autoFlow.Process(Time.deltaTime);
        }

        public bool ResumeAutoFlow()
        {
            if (_autoFlow == null)
            {
                return false;
            }

            if (!_autoFlow.IsPaused)
            {
                return false;
            }

            _autoFlow.Resume();
            string actorId = actor?.ActorId ?? name;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"▶️ AutoFlow retomado para {actorId}.");
            return true;
        }

        public bool PauseAutoFlow()
        {
            if (_autoFlow == null)
            {
                return false;
            }

            if (_autoFlow.IsPaused)
            {
                return false;
            }

            _autoFlow.Pause();
            string actorId = actor?.ActorId ?? name;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"⏸️ AutoFlow pausado para {actorId}.");
            return true;
        }

        protected override void OnServiceDispose()
        {
            _autoFlow?.Dispose();
            _autoFlow = null;
        }

        private bool HasAutoFlowResources()
        {
            foreach (var (type, _) in resourceSystem.GetAll())
            {
                var cfg = resourceSystem.GetInstanceConfig(type);
                if (cfg is { hasAutoFlow: true } && cfg.autoFlowConfig != null)
                    return true;
            }
            return false;
        }

        // ContextMenu removidos — debug deve ser via DebugUtility/Inspector Customizado.
    }
}