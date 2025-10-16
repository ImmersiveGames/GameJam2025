using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(25)]
    public abstract class ResourceBridgeBase : MonoBehaviour
    {
        protected IActorResourceOrchestrator orchestrator;
        protected ResourceSystem resourceSystem;
        protected IActor actor;

        private bool _initialized;
        private bool _destroyed;
        
        public IActor Actor => actor;
        public bool isInitialized => _initialized;

        protected virtual void Awake()
        {
            actor = GetComponent<IActor>();
            if (actor == null)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"{name} não possui IActor — bridge desativado.");
                enabled = false;
            }
        }

        protected virtual void Start()
        {
            if (!enabled) return;

            TryInitialize();
        }

        protected virtual void TryInitialize()
        {
            if (_initialized || actor == null) return;

            if (!DependencyManager.Instance.TryGetGlobal(out orchestrator))
            {
                DebugUtility.LogWarning<ResourceBridgeBase>("Orchestrator não disponível no DependencyManager.");
                enabled = false;
                return;
            }

            if (!orchestrator.TryGetActorResource(actor.ActorId, out resourceSystem) || resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"ResourceSystem não encontrado para {actor.ActorId}.");
                enabled = false;
                return;
            }

            _initialized = true;
            DebugUtility.LogVerbose<ResourceBridgeBase>($"✅ Bridge inicializado para {actor.ActorId}");
            OnServiceInitialized();
        }

        protected abstract void OnServiceInitialized();
        protected abstract void OnServiceDispose();

        protected virtual void OnDestroy()
        {
            _destroyed = true;
            if (_initialized) OnServiceDispose();
            resourceSystem = null;
            orchestrator = null;
        }

        public bool IsInitialized => _initialized;
        public bool IsDestroyed => _destroyed;
        public ResourceSystem GetResourceSystem() => resourceSystem;
    }
}
