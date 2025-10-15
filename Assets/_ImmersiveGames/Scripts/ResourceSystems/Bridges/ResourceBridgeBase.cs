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
        private IActor _actor;
        protected ResourceSystem resourceSystem;
        protected IActorResourceOrchestrator _orchestrator;
        protected bool initialized;
        private bool _isDestroyed;

        protected internal IActor Actor => _actor;
        public bool IsInitialized() => initialized;
        public bool IsDestroyed() => _isDestroyed;
        public ResourceSystem GetResourceSystem() => resourceSystem;

        protected virtual void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"Nenhum IActor encontrado em {name}. Desativando.");
                enabled = false;
                return;
            }

            DebugUtility.LogVerbose<ResourceBridgeBase>($"Awake chamado para ActorId: {_actor.ActorId}");
        }

        protected virtual void Start()
        {
            TryInitializeOnce();
        }

        /// <summary>
        /// Tenta inicializar uma única vez. Se falhar, desativa o componente.
        /// </summary>
        protected virtual void TryInitializeOnce()
        {
            if (!ShouldInitialize())
                return;

            string actorId = _actor.ActorId;
            DebugUtility.LogVerbose<ResourceBridgeBase>($"🚀 Tentando inicializar bridge para {actorId}");

            if (!TryInitializeService())
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"❌ Falha na inicialização de {actorId}. Desativando bridge.");
                OnInitializationFailed();
                enabled = false;
                return;
            }

            initialized = true;
            DebugUtility.LogVerbose<ResourceBridgeBase>($"✅ Bridge inicializado com sucesso para {actorId}");
            OnServiceInitialized();
        }

        protected virtual bool TryInitializeService()
        {
            string actorId = _actor.ActorId;

            if (!DependencyManager.Instance)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>("DependencyManager não está pronto. Desativando bridge.");
                return false;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                DebugUtility.LogWarning<ResourceBridgeBase>("Orchestrator global não encontrado no DependencyManager.");
                return false;
            }

            if (!_orchestrator.IsActorRegistered(actorId))
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"Actor {actorId} não está registrado no orchestrator.");
                return false;
            }

            resourceSystem = _orchestrator.GetActorResourceSystem(actorId);

            if (resourceSystem == null &&
                !DependencyManager.Instance.TryGetForObject(actorId, out resourceSystem))
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"ResourceSystem não encontrado para {actorId}.");
                return false;
            }

            return resourceSystem != null;
        }

        protected abstract void OnServiceInitialized();
        protected abstract void OnServiceDispose();

        protected virtual void OnInitializationFailed()
        {
            DebugUtility.LogWarning<ResourceBridgeBase>($"Inicialização falhou para {_actor?.ActorId}");
        }

        protected virtual bool ShouldInitialize() => true;

        protected virtual void OnDestroy()
        {
            _isDestroyed = true;
            OnServiceDispose();
            resourceSystem = null;
        }
    }
}
