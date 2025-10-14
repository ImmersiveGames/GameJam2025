using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using System.Collections;
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

        protected IActor Actor => _actor;
        public IActor GetActor() => _actor;
        public ResourceSystem GetResourceSystem() => resourceSystem;
        public bool IsInitialized() => initialized;
        public bool IsDestroyed() => _isDestroyed;

        protected virtual void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"No IActor found on {name}. Disabling.");
                enabled = false;
                return;
            }

            DebugUtility.LogVerbose<ResourceBridgeBase>($"Awake chamado para ActorId: {_actor.ActorId}");
        }

        protected virtual void Start()
        {
            StartCoroutine(InitializeWithRetry());
        }

        protected virtual IEnumerator InitializeWithRetry()
        {
            string actorId = _actor.ActorId;
            int maxAttempts = 15;
            int attempt = 0;

            DebugUtility.LogVerbose<ResourceBridgeBase>($"🚀 Iniciando inicialização para {actorId}");

            while (!initialized && attempt < maxAttempts && _actor != null && !_isDestroyed)
            {
                attempt++;

                if (attempt > 1)
                    yield return new WaitForSeconds(0.1f);
                else
                    yield return new WaitForEndOfFrame();

                DebugUtility.LogVerbose<ResourceBridgeBase>($"Tentativa {attempt} de inicialização para {actorId}");

                if (TryInitializeService())
                {
                    initialized = true;
                    DebugUtility.LogVerbose<ResourceBridgeBase>($"✅ Inicializado com sucesso na tentativa {attempt}");
                    OnServiceInitialized();
                    break;
                }
            }

            if (!initialized && _actor != null && !_isDestroyed)
            {
                DebugUtility.LogWarning<ResourceBridgeBase>($"❌ Falha após {maxAttempts} tentativas para {actorId}. Desativando.");
                OnInitializationFailed();
                enabled = false;
            }
        }

        protected virtual bool TryInitializeService()
        {
            string actorId = _actor.ActorId;

            if (!DependencyManager.Instance)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("DependencyManager não está pronto");
                return false;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("Orchestrator não encontrado no DependencyManager");
                return false;
            }

            if (!_orchestrator.IsActorRegistered(actorId))
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>($"Actor {actorId} não está registrado no orchestrator");
                return false;
            }

            resourceSystem = _orchestrator.GetActorResourceSystem(actorId);

            if (resourceSystem == null)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem não encontrado via orchestrator");

                if (!TryFindResourceSystem(actorId))
                {
                    return false;
                }
            }

            if (resourceSystem == null)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem ainda é null após todas as tentativas");
                return false;
            }

            return true;
        }

        protected virtual bool TryFindResourceSystem(string actorId)
        {
            if (DependencyManager.Instance.TryGetForObject(actorId, out resourceSystem))
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem obtido via DependencyManager");
                return true;
            }

            DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem não encontrado no DependencyManager");
            return false;
        }

        protected virtual void Update()
        {
            // Coroutine handles initialization
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
            StopAllCoroutines();
            OnServiceDispose();
            resourceSystem = null;
        }
    }
}