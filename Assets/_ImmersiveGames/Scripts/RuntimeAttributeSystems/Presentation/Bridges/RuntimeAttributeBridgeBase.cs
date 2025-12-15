using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges
{
    /// <summary>
    /// Base comum para bridges que ligam RuntimeAttributeContext a serviços ou UI.
    /// Cuida da resolução do ator, obtenção de orchestrator/contexto e ciclo de vida seguro.
    /// </summary>
    [DefaultExecutionOrder(25)]
    public abstract class RuntimeAttributeBridgeBase : MonoBehaviour, IRuntimeAttributeBridge
    {
        protected IRuntimeAttributeOrchestrator orchestrator;
        protected RuntimeAttributeContext runtimeAttributeContext;
        protected IActor actor;

        private bool _initialized;
        private bool _destroyed;
        
        public IActor Actor => actor;
        public bool IsInitialized => _initialized;

        protected virtual void Awake()
        {
            actor = GetComponent<IActor>();
            if (actor == null)
            {
                DebugUtility.LogWarning<RuntimeAttributeBridgeBase>($"{name} não possui IActor — component desativado.");
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

            if (!DependencyManager.Provider.TryGetGlobal(out orchestrator))
            {
                DebugUtility.LogWarning<RuntimeAttributeBridgeBase>("Orchestrator não disponível no DependencyManager.");
                enabled = false;
                return;
            }

            if (!orchestrator.TryGetActorResource(actor.ActorId, out runtimeAttributeContext) || runtimeAttributeContext == null)
            {
                DebugUtility.LogWarning<RuntimeAttributeBridgeBase>($"RuntimeAttributeContext não encontrado para {actor.ActorId}.");
                enabled = false;
                return;
            }

            _initialized = true;
            DebugUtility.LogVerbose<RuntimeAttributeBridgeBase>(
                $"✅ Component inicializado para {actor.ActorId}",
                DebugUtility.Colors.CrucialInfo);
            OnServiceInitialized();
        }

        protected abstract void OnServiceInitialized();
        protected abstract void OnServiceDispose();

        protected virtual void OnDestroy()
        {
            _destroyed = true;
            if (_initialized) OnServiceDispose();
            runtimeAttributeContext = null;
            orchestrator = null;
        }

        public bool IsDestroyed => _destroyed;
        public RuntimeAttributeContext GetResourceSystem() => runtimeAttributeContext;
    }
}
