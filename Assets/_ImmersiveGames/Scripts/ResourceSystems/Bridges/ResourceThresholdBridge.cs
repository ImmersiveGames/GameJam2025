using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(20)] // Executar DEPOIS de todos os sistemas de recurso
    public class ResourceThresholdBridge : MonoBehaviour
    {
        private ResourceThresholdService _thresholdService;
        private ResourceSystem _resourceSystem;
        private IActor _actor;
        private IActorResourceOrchestrator _orchestrator;
        private bool _initialized;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor != null) return;
            DebugUtility.LogWarning<ResourceThresholdBridge>($"No IActor found on {name}. Disabling.");
            enabled = false;
        }

        private void Start()
        {
            StartCoroutine(InitializeWithRetry());
        }

        private IEnumerator InitializeWithRetry()
        {
            string actorId = _actor.ActorId;
            const int maxAttempts = 10;
            int attempt = 0;

            while (!_initialized && attempt < maxAttempts)
            {
                attempt++;
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"Tentativa {attempt} de inicialização para {actorId}");

                if (TryInitializeThresholdService())
                {
                    _initialized = true;
                    DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ Inicializado com sucesso na tentativa {attempt}");
                    break;
                }

                // Aguardar próximo frame antes de tentar novamente
                yield return new WaitForEndOfFrame();
            }

            if (_initialized) yield break;
            DebugUtility.LogWarning<ResourceThresholdBridge>($"Falha após {maxAttempts} tentativas. Desativando.");
            enabled = false;
        }

        private bool TryInitializeThresholdService()
        {
            string actorId = _actor.ActorId;

            // Obter o orchestrator
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"Orchestrator não encontrado");
                return false;
            }

            // CORREÇÃO PRINCIPAL: Usar o método novo da interface
            _resourceSystem = _orchestrator.GetActorResourceSystem(actorId);

            if (_resourceSystem == null)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"ResourceSystem não encontrado via orchestrator");
                return false;
            }

            // Verificar se o ResourceSystem tem recursos configurados
            IReadOnlyDictionary<ResourceType, IResourceValue> allResources = _resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"ResourceSystem não tem recursos configurados");
                return false;
            }

            _thresholdService = new ResourceThresholdService(_resourceSystem);
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"✅ ThresholdService criado com {allResources.Count} recursos");

            // Forçar verificação inicial
            _thresholdService.ForceCheck();
            
            return true;
        }

        private void Update()
        {
            // Fallback: se ainda não inicializou, tentar uma vez por frame
            if (!_initialized && _actor != null)
            {
                _initialized = TryInitializeThresholdService();
            }
        }

        private void OnDestroy()
        {
            _thresholdService?.Dispose();
            _thresholdService = null;
            _resourceSystem = null;
        }

        [ContextMenu("Force Threshold Check")]
        private void ContextForce() 
        {
            if (!_initialized)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"Tentando inicializar via ContextMenu...");
                _initialized = TryInitializeThresholdService();
            }
            
            if (_initialized)
            {
                DebugUtility.LogVerbose<ResourceThresholdBridge>($"Forçando verificação via ContextMenu");
                _thresholdService?.ForceCheck();
            }
            else
            {
                DebugUtility.LogWarning<ResourceThresholdBridge>($"Não foi possível inicializar para forçar verificação");
            }
        }

        [ContextMenu("Debug Status")]
        private void DebugStatus()
        {
            string actorId = _actor?.ActorId ?? "null";
            bool orchestratorFound = DependencyManager.Instance.TryGetGlobal(out _orchestrator);
            bool actorRegistered = orchestratorFound && _orchestrator.IsActorRegistered(actorId);
            
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"Status:\n" +
                     $" - Initialized: {_initialized}\n" +
                     $" - Actor: {actorId}\n" +
                     $" - Orchestrator: {orchestratorFound}\n" +
                     $" - Actor Registrado: {actorRegistered}\n" +
                     $" - ResourceSystem: {_resourceSystem != null}\n" +
                     $" - ThresholdService: {_thresholdService != null}");

            if (!orchestratorFound) return;
            IReadOnlyCollection<string> actorIds = _orchestrator.GetRegisteredActorIds();
            DebugUtility.LogVerbose<ResourceThresholdBridge>($"Atores registrados: {string.Join(", ", actorIds)}");
        }
    }
}