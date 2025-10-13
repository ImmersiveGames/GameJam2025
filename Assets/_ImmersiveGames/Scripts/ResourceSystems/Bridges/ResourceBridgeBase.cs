using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Classe base abstrata para todos os bridges de recurso.
    /// Atualizada para funcionar com o novo sistema de injeção de dependências.
    /// </summary>
    [DefaultExecutionOrder(25)] // CORREÇÃO: Ordem após o InjectableEntityResourceBridge (20)
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class ResourceBridgeBase : MonoBehaviour
    {
        private IActor _actor;
        protected ResourceSystem resourceSystem;
        private IActorResourceOrchestrator _orchestrator;
        protected bool initialized;
        private bool _isDestroyed;
        
        protected IActor Actor => _actor;

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
            int maxAttempts = 15; // CORREÇÃO: Aumentado para dar tempo da injeção
            int attempt = 0;

            DebugUtility.LogVerbose<ResourceBridgeBase>($"🚀 Iniciando inicialização para {actorId}");

            while (!initialized && attempt < maxAttempts && _actor != null && !_isDestroyed)
            {
                attempt++;
                
                // CORREÇÃO: Esperar um pouco mais entre tentativas
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

            // CORREÇÃO: Verificar primeiro se o DependencyManager está pronto
            if (!DependencyManager.Instance)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("DependencyManager não está pronto");
                return false;
            }

            // CORREÇÃO: Tentar obter o orchestrator
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("Orchestrator não encontrado no DependencyManager");
                return false;
            }

            // CORREÇÃO: Verificar se o actor está registrado no orchestrator
            if (!_orchestrator.IsActorRegistered(actorId))
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>($"Actor {actorId} não está registrado no orchestrator");
                return false;
            }

            // CORREÇÃO: Usar o método da interface para obter o ResourceSystem
            resourceSystem = _orchestrator.GetActorResourceSystem(actorId);

            if (resourceSystem == null)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem não encontrado via orchestrator");
                
                // CORREÇÃO: Fallback atualizado - apenas DependencyManager
                if (!TryFindResourceSystem(actorId))
                {
                    return false;
                }
            }

            // CORREÇÃO: Verificação final
            if (resourceSystem == null)
            {
                DebugUtility.LogVerbose<ResourceBridgeBase>("ResourceSystem ainda é null após todas as tentativas");
                return false;
            }

            return true;
        }

        protected virtual bool TryFindResourceSystem(string actorId)
        {
            // CORREÇÃO: Apenas DependencyManager - remover referências ao bridge antigo
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
            // CORREÇÃO: Remover fallback do Update - pode causar problemas de performance
            // A inicialização deve ser feita apenas via corrotina
        }

        // Métodos abstratos que as classes derivadas devem implementar
        protected abstract void OnServiceInitialized();
        protected abstract void OnServiceDispose();

        // Métodos virtuais para override se necessário
        protected virtual void OnInitializationFailed() 
        {
            DebugUtility.LogWarning<ResourceBridgeBase>($"Inicialização falhou para {_actor?.ActorId}");
        }
        
        protected virtual bool ShouldInitialize() => true;

        [ContextMenu("🔧 Debug Bridge Status")]
        public virtual void DebugStatus()
        {
            string actorId = _actor?.ActorId ?? "null";
            bool orchestratorFound = DependencyManager.Instance.TryGetGlobal(out _orchestrator);
            bool actorRegistered = orchestratorFound && _orchestrator.IsActorRegistered(actorId);
            
            DebugUtility.LogWarning(GetType(), 
                $"🔧 BRIDGE STATUS - {GetType().Name}:\n" +
                $" - Actor: {actorId}\n" +
                $" - Initialized: {initialized}\n" +
                $" - Destroyed: {_isDestroyed}\n" +
                $" - Orchestrator: {orchestratorFound}\n" +
                $" - Actor Registrado: {actorRegistered}\n" +
                $" - ResourceSystem: {resourceSystem != null}\n" +
                $" - DependencyManager Ready: {DependencyManager.Instance}");

            if (orchestratorFound)
            {
                IReadOnlyCollection<string> actorIds = _orchestrator.GetRegisteredActorIds();
                DebugUtility.LogWarning(GetType(), $"📋 Atores registrados: {string.Join(", ", actorIds)}");
            }

            // CORREÇÃO: Verificar também no DependencyManager
            bool inDependencyManager = DependencyManager.Instance.TryGetForObject(actorId, out ResourceSystem dmSystem);
            DebugUtility.LogWarning(GetType(), $" - In DependencyManager: {inDependencyManager}, Service: {dmSystem != null}");
        }

        [ContextMenu("🔄 Force Reinitialize")]
        public virtual void ForceReinitialize()
        {
            if (_isDestroyed) return;
            
            DebugUtility.LogWarning<ResourceBridgeBase>($"🔄 Forçando reinicialização para {_actor?.ActorId}");
            StopAllCoroutines();
            initialized = false;
            resourceSystem = null;
            StartCoroutine(InitializeWithRetry());
        }

        protected virtual void OnDestroy()
        {
            _isDestroyed = true;
            StopAllCoroutines();
            OnServiceDispose();
            resourceSystem = null;
        }
    }
}