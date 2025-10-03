using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Classe base abstrata para todos os bridges de recurso.
    /// Gerencia a inicialização comum: IActor, ResourceSystem e ordenação de execução.
    /// </summary>
    [DefaultExecutionOrder(20)] // Ordem comum para todos os bridges
    public abstract class ResourceBridgeBase : MonoBehaviour
    {
        [Header("Resource Bridge Base")]
        [SerializeField] private bool enableDebugLogs = true;

        private IActor _actor;
        protected ResourceSystem resourceSystem;
        private IActorResourceOrchestrator _orchestrator;
        protected bool initialized;
        
        protected IActor Actor => _actor;

        protected virtual void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                LogWarning($"No IActor found on {name}. Disabling.");
                enabled = false;
                return;
            }

            LogVerbose($"Awake chamado para ActorId: {_actor.ActorId}");
        }

        protected virtual void Start()
        {
            StartCoroutine(InitializeWithRetry());
        }

        protected virtual IEnumerator InitializeWithRetry()
        {
            string actorId = _actor.ActorId;
            int maxAttempts = 10;
            int attempt = 0;

            while (!initialized && attempt < maxAttempts && _actor != null)
            {
                attempt++;
                LogVerbose($"Tentativa {attempt} de inicialização para {actorId}");

                if (TryInitializeService())
                {
                    initialized = true;
                    LogVerbose($"✅ Inicializado com sucesso na tentativa {attempt}");
                    break;
                }

                yield return new WaitForEndOfFrame();
            }

            if (!initialized && _actor != null)
            {
                LogWarning($"Falha após {maxAttempts} tentativas. Desativando.");
                enabled = false;
            }
        }

        protected virtual bool TryInitializeService()
        {
            string actorId = _actor.ActorId;

            // Obter o orchestrator
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                LogVerbose("Orchestrator não encontrado");
                return false;
            }

            // Usar o método da interface para obter o ResourceSystem
            resourceSystem = _orchestrator.GetActorResourceSystem(actorId);

            if (resourceSystem == null)
            {
                LogVerbose("ResourceSystem não encontrado via orchestrator");
                
                // Fallback: tentar outras formas
                if (!TryFindResourceSystem(actorId))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool TryFindResourceSystem(string actorId)
        {
            // Tentativa 1: DependencyManager
            if (DependencyManager.Instance.TryGetForObject(actorId, out resourceSystem))
            {
                LogVerbose("ResourceSystem obtido via DependencyManager");
                return true;
            }

            // Tentativa 2: EntityResourceBridge direto
            var bridge = GetComponent<EntityResourceBridge>();
            if (bridge != null)
            {
                resourceSystem = bridge.GetService();
                if (resourceSystem != null)
                {
                    LogVerbose("ResourceSystem obtido via EntityResourceBridge");
                    return true;
                }
            }

            // Tentativa 3: Buscar em parent
            bridge = GetComponentInParent<EntityResourceBridge>();
            if (bridge != null)
            {
                resourceSystem = bridge.GetService();
                if (resourceSystem != null)
                {
                    LogVerbose("ResourceSystem obtido via EntityResourceBridge (parent)");
                    return true;
                }
            }

            return false;
        }

        protected virtual void Update()
        {
            // Fallback: se ainda não inicializou, tentar uma vez por frame
            if (!initialized && _actor != null)
            {
                initialized = TryInitializeService();
            }
        }

        // Métodos auxiliares para logging
        protected void LogVerbose(string message)
        {
            if (enableDebugLogs)
            {
                DebugUtility.LogVerbose(GetType(), message);
            }
        }

        protected void LogWarning(string message)
        {
            DebugUtility.LogWarning(GetType(), message);
        }

        protected void LogError(string message)
        {
            DebugUtility.LogError(GetType(), message);
        }

        // Métodos abstratos que as classes derivadas devem implementar
        protected abstract void OnServiceInitialized();
        protected abstract void OnServiceDispose();

        // Métodos virtuais para override se necessário
        protected virtual void OnInitializationFailed() { }
        protected virtual bool ShouldInitialize() => true;

        [ContextMenu("Debug Status")]
        protected virtual void DebugStatus()
        {
            string actorId = _actor?.ActorId ?? "null";
            bool orchestratorFound = DependencyManager.Instance.TryGetGlobal(out _orchestrator);
            bool actorRegistered = orchestratorFound && _orchestrator.IsActorRegistered(actorId);
            
            DebugUtility.LogWarning(GetType(), $"Status:\n" +
                     $" - Initialized: {initialized}\n" +
                     $" - Actor: {actorId}\n" +
                     $" - Orchestrator: {orchestratorFound}\n" +
                     $" - Actor Registrado: {actorRegistered}\n" +
                     $" - ResourceSystem: {resourceSystem != null}");

            if (orchestratorFound)
            {
                var actorIds = _orchestrator.GetRegisteredActorIds();
                DebugUtility.LogWarning(GetType(), $"Atores registrados: {string.Join(", ", actorIds)}");
            }
        }

        protected virtual void OnDestroy()
        {
            OnServiceDispose();
            resourceSystem = null;
        }
    }
    
    /*
     Exemplo de uso:
    public class ResourceAlertBridge : ResourceBridgeBase
    {
        [SerializeField] private float lowResourceThreshold = 0.2f;
        [SerializeField] private float criticalResourceThreshold = 0.1f;
        
        private ResourceAlertService _alertService;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            _alertService = new ResourceAlertService(_resourceSystem, lowResourceThreshold, criticalResourceThreshold);
            LogVerbose("✅ ResourceAlertService criado");

            OnServiceInitialized();
            return true;
        }

        protected override void OnServiceInitialized()
        {
            // Configurações específicas do alert service
        }

        protected override void OnServiceDispose()
        {
            _alertService?.Dispose();
            _alertService = null;
        }

        // Métodos específicos do alert bridge...
    }  */
}