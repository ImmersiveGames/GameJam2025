using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    [DebugLevel(DebugLevel.Verbose)]
    public class NewResourceSystemDebugManager : MonoBehaviour, IInjectableComponent
    {
        [Header("Debug Configuration")]
        [SerializeField] private bool enableDebugFeatures = true;
        [SerializeField] private bool enableEventListening = true; [SerializeField] private bool autoInitialize = true;

        [Header("Integration Testing")]
        [SerializeField] private ResourceType testResourceType = ResourceType.Health;
        [SerializeField] private float testDamageAmount = -20f;
        [SerializeField] private float testHealAmount = 30f;
        [SerializeField] private float testThresholdValue = 50f;

        [Header("Event Monitoring")]
        [SerializeField] private bool logThresholdEvents = true;
        [SerializeField] private bool logResourceUpdates = true;
        [SerializeField] private bool logCanvasEvents = true;

        // NOVAS DEPENDÊNCIAS COM INJEÇÃO
        [Inject] private IActorResourceOrchestrator _orchestrator;
        [Inject] private CanvasPipelineManager _pipelineManager;
        [Inject] private IUniqueIdFactory _idFactory;

        // Component References
        private ICanvasBinder _canvasBinder;
        private InjectableEntityResourceBridge _entityBridge;

        // Event System
        private EventBinding<ResourceThresholdEvent> _thresholdBinding;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private EventBinding<CanvasBindRequest> _canvasBindBinding;

        private Queue<string> _eventLog = new Queue<string>();

        // Actor Reference
        private IActor _actor;
        private ResourceSystem _resourceSystem;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => _actor?.ActorId ?? "DebugManager";

        private void Awake()
        {
            _actor = GetComponent<IActor>();

            if (!enableDebugFeatures)
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Debug features disabled");
                enabled = false;
                return;
            }

            // NOVO: Registrar para injeção de dependências
            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;

            if (autoInitialize)
            {
                InitializeDebugManager();
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("✅ Debug Manager fully initialized with dependencies");
        }

        [ContextMenu("Debug/Initialize Debug Manager")]
        public void InitializeDebugManager()
        {
            GatherComponents();
            SetupEventListeners();
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("✅ New Resource System Debug Manager Initialized");
        }

        private void GatherComponents()
        {
            // NOVO: Buscar componentes usando as novas interfaces
            _canvasBinder = GetComponent<ICanvasBinder>();
            _entityBridge = GetComponent<InjectableEntityResourceBridge>();

            // Obter resource system via orchestrator
            if (_actor != null)
            {
                _resourceSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
            }

            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Debug Manager initialized - Components: " +
                $"CanvasBinder: {_canvasBinder != null} (Type: {_canvasBinder?.Type}), " +
                $"EntityBridge: {_entityBridge != null}, " +
                $"ResourceSystem: {_resourceSystem != null}, " +
                $"Orchestrator: {_orchestrator != null}, " +
                $"PipelineManager: {_pipelineManager != null}");
        }

        private void SetupEventListeners()
        {
            if (!enableEventListening) return;

            // Threshold Events
            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdCrossed);
            EventBus<ResourceThresholdEvent>.Register(_thresholdBinding);

            // Resource Update Events
            _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);

            // NOVO: Canvas Bind Events
            _canvasBindBinding = new EventBinding<CanvasBindRequest>(OnCanvasBindRequest);
            EventBus<CanvasBindRequest>.Register(_canvasBindBinding);

            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("✅ Event listeners initialized");
        }

        private void OnThresholdCrossed(ResourceThresholdEvent evt)
        {
            if (!logThresholdEvents) return;

            string eventMessage = $"🎯 THRESHOLD CROSSED:\n" +
                $" - Actor: {evt.ActorId}\n" +
                $" - Resource: {evt.ResourceType}\n" +
                $" - Threshold: {evt.Threshold:F2}\n" +
                $" - Direction: {(evt.IsAscending ? "↑ ASCENDING" : "↓ DESCENDING")}\n" +
                $" - Current: {evt.CurrentPercentage:P1}";

            LogEvent(eventMessage);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!logResourceUpdates) return;

            string eventMessage = $"🔄 RESOURCE UPDATED:\n" +
                $" - Actor: {evt.ActorId}\n" +
                $" - Resource: {evt.ResourceType}\n" +
                $" - Value: {evt.NewValue.GetCurrentValue():F1}/{evt.NewValue.GetMaxValue():F1}";

            LogEvent(eventMessage);
        }

        // NOVO: Monitorar requests de bind para canvas
        private void OnCanvasBindRequest(CanvasBindRequest evt)
        {
            if (!logCanvasEvents) return;

            string eventMessage = $"🎨 CANVAS BIND REQUEST:\n" +
                $" - Actor: {evt.actorId}\n" +
                $" - Resource: {evt.resourceType}\n" +
                $" - Target Canvas: {evt.targetCanvasId}\n" +
                $" - Pending Time: {Time.time - evt.requestTime:F2}s";

            LogEvent(eventMessage);
        }

        private void LogEvent(string message)
        {
            _eventLog.Enqueue($"[{Time.time:F2}] {message}");

            // Manter apenas os últimos 15 eventos
            if (_eventLog.Count > 15)
                _eventLog.Dequeue();

            DebugUtility.LogVerbose<NewResourceSystemDebugManager>(message);
        }

        #region 🎯 TESTES ESPECÍFICOS DO NOVO SISTEMA

        [ContextMenu("Tests/New System/Validate Injection System")]
        public void ValidateInjectionSystem()
        {
            StartCoroutine(ComprehensiveInjectionValidation());
        }

        private IEnumerator ComprehensiveInjectionValidation()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔍 STARTING INJECTION SYSTEM VALIDATION");

            bool allValid = true;

            // Validar estado de injeção
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Injection State: {InjectionState}");

            if (InjectionState != DependencyInjectionState.Ready)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ INJECTION NOT READY");
                allValid = false;
            }

            // Validar dependências injetadas
            if (_orchestrator == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ ORCHESTRATOR NOT INJECTED");
                allValid = false;
            }
            else
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>("✅ Orchestrator injected");
            }

            if (_pipelineManager == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ PIPELINE MANAGER NOT INJECTED");
                allValid = false;
            }
            else
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>("✅ Pipeline Manager injected");
            }

            // Validar componentes do novo sistema
            if (_canvasBinder == null)
            {
                DebugUtility.LogWarning<NewResourceSystemDebugManager>("⚠️ NO CANVAS BINDER FOUND (might be normal)");
            }
            else
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"✅ Canvas Binder: {_canvasBinder.CanvasId} (Type: {_canvasBinder.Type}, State: {_canvasBinder.State})");
            }

            if (_entityBridge == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ ENTITY BRIDGE NOT FOUND");
                allValid = false;
            }
            else
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"✅ Entity Bridge: {_entityBridge.InjectionState}");
            }

            if (allValid)
            {
                DebugUtility.LogWarning<NewResourceSystemDebugManager>("✅ INJECTION SYSTEM VALIDATION PASSED");
            }
            else
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ INJECTION SYSTEM VALIDATION FAILED");
            }

            yield return null;
        }

        [ContextMenu("Tests/New System/Test Canvas Pipeline")]
        public void TestCanvasPipeline()
        {
            StartCoroutine(CanvasPipelineTest());
        }

        private IEnumerator CanvasPipelineTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🎨 TESTING CANVAS PIPELINE");

            if (_resourceSystem == null || _actor == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("❌ Cannot test pipeline - missing dependencies");
                yield break;
            }

            // Obter informações sobre os recursos e seus canvas targets
            var resources = _resourceSystem.GetAll();
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Testing {resources.Count} resources");

            foreach (var resource in resources)
            {
                var instanceConfig = _resourceSystem.GetInstanceConfig(resource.Key);
                string targetCanvasId = "Unknown";

                if (instanceConfig != null)
                {
                    targetCanvasId = instanceConfig.canvasTargetMode switch
                    {
                        CanvasTargetMode.ActorSpecific => $"{_actor.ActorId}_Canvas",
                        CanvasTargetMode.Custom => instanceConfig.customCanvasId ?? "MainUI",
                        _ => "MainUI"
                    };
                }

                DebugUtility.LogVerbose<NewResourceSystemDebugManager>(
                    $"Resource {resource.Key} -> Canvas: {targetCanvasId}");

                // Verificar se o canvas está registrado
                bool canvasRegistered = _orchestrator.IsCanvasRegistered(targetCanvasId);
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>(
                    $"  Canvas Registered: {canvasRegistered}");

                if (canvasRegistered)
                {
                    // Forçar um update para testar o pipeline
                    _resourceSystem.Modify(resource.Key, -1f);
                    yield return new WaitForSeconds(0.2f);

                    _resourceSystem.Modify(resource.Key, 1f);
                    yield return new WaitForSeconds(0.2f);
                }
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🎨 CANVAS PIPELINE TEST COMPLETED");
        }

        [ContextMenu("Tests/New System/Test Dynamic Canvas Creation")]
        public void TestDynamicCanvasCreation()
        {
            StartCoroutine(DynamicCanvasCreationTest());
        }

        private IEnumerator DynamicCanvasCreationTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔄 TESTING DYNAMIC CANVAS CREATION");

            // Este teste requer um prefab de DynamicCanvasBinder no projeto
            // Você pode criar um método para spawnar canvas dinamicamente aqui

            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Note: Dynamic canvas test requires setup");
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Create a DynamicCanvasBinder prefab and reference it");

            yield return null;

            // Exemplo de como seria a criação dinâmica:
            /*
            if (dynamicCanvasPrefab != null)
            {
                var canvasGO = Instantiate(dynamicCanvasPrefab);
                var dynamicCanvas = canvasGO.GetComponent<DynamicCanvasBinder>();

                yield return new WaitUntil(() => dynamicCanvas.CanAcceptBinds());

                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"✅ Dynamic Canvas Ready: {dynamicCanvas.CanvasId}");

                // Testar bind com o novo canvas
                if (_resourceSystem != null)
                {
                    var health = _resourceSystem.Get(ResourceType.Health);
                    if (health != null)
                    {
                        _pipelineManager.ScheduleBind(_actor.ActorId, ResourceType.Health, health, dynamicCanvas.CanvasId);
                    }
                }
            }
            */

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔄 DYNAMIC CANVAS CREATION TEST COMPLETED");
        }

        [ContextMenu("Tests/New System/Test Multiple Actors")]
        public void TestMultipleActors()
        {
            StartCoroutine(MultipleActorsTest());
        }

        private IEnumerator MultipleActorsTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("👥 TESTING MULTIPLE ACTORS SYSTEM");

            var actorIds = _orchestrator.GetRegisteredActorIds();
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Found {actorIds.Count} registered actors");

            foreach (var actorId in actorIds)
            {
                var actorSystem = _orchestrator.GetActorResourceSystem(actorId);
                if (actorSystem != null)
                {
                    var resources = actorSystem.GetAll();
                    DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Actor {actorId}: {resources.Count} resources");

                    foreach (var resource in resources)
                    {
                        DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"  - {resource.Key}: {resource.Value.GetCurrentValue():F1}");
                    }
                }
            }

            // Testar interação entre múltiplos atores
            if (actorIds.Count > 1)
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Testing multi-actor resource modifications...");

                // Aplicar modificações em todos os atores
                foreach (var actorId in actorIds)
                {
                    var actorSystem = _orchestrator.GetActorResourceSystem(actorId);
                    actorSystem?.Modify(ResourceType.Health, -5f);
                }

                yield return new WaitForSeconds(0.5f);
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("👥 MULTIPLE ACTORS TEST COMPLETED");
        }

        #endregion

        #region 📊 MÉTRICAS DO NOVO SISTEMA

        [ContextMenu("Debug/Metrics/Show New System Metrics")]
        public void ShowNewSystemMetrics()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📈 NEW SYSTEM METRICS");
            sb.AppendLine("=====================");

            // Métricas de injeção
            sb.AppendLine($"Injection State: {InjectionState}");
            sb.AppendLine($"Orchestrator: {_orchestrator != null}");
            sb.AppendLine($"Pipeline Manager: {_pipelineManager != null}");

            // Métricas de atores
            var actorIds = _orchestrator.GetRegisteredActorIds();
            sb.AppendLine($"Registered Actors: {actorIds.Count}");
            foreach (var actorId in actorIds)
            {
                sb.AppendLine($"  - {actorId}");
            }

            // Métricas de canvas
            var canvasIds = _orchestrator.GetRegisteredCanvasIds();
            sb.AppendLine($"Registered Canvases: {canvasIds.Count}");
            foreach (var canvasId in canvasIds)
            {
                var canvas = _orchestrator.IsCanvasRegistered(canvasId);
                sb.AppendLine($"  - {canvasId} (Registered: {canvas})");
            }

            // Métricas de pipeline
            sb.AppendLine($"Pending Binds: {GetPendingBindsCount()}"); // Você precisaria expor isso no CanvasPipelineManager

            // Métricas de recursos
            if (_resourceSystem != null)
            {
                var resources = _resourceSystem.GetAll();
                sb.AppendLine($"Local Resources: {resources.Count}");
                foreach (var resource in resources)
                {
                    var config = _resourceSystem.GetInstanceConfig(resource.Key);
                    string canvasTarget = config?.canvasTargetMode.ToString() ?? "Default";
                    sb.AppendLine($"  - {resource.Key}: {resource.Value.GetCurrentValue():F1} (Target: {canvasTarget})");
                }
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>(sb.ToString());
        }

        private int GetPendingBindsCount()
        {
            // Este método precisaria acessar o estado interno do CanvasPipelineManager
            // Você pode expor essa informação através de um método público no CanvasPipelineManager
            return -1; // Placeholder
        }

        [ContextMenu("Debug/Metrics/Show Canvas States")]
        public void ShowCanvasStates()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("🎨 CANVAS STATES");
            sb.AppendLine("================");

            var canvasIds = _orchestrator.GetRegisteredCanvasIds();
            foreach (var canvasId in canvasIds)
            {
                // Para obter o estado detalhado, você precisaria de um método no orchestrator
                // para obter o ICanvasBinder pelo ID
                sb.AppendLine($"  - {canvasId}");
            }

            // Estado do canvas local
            if (_canvasBinder != null)
            {
                sb.AppendLine($"Local Canvas: {_canvasBinder.CanvasId}");
                sb.AppendLine($"  Type: {_canvasBinder.Type}");
                sb.AppendLine($"  State: {_canvasBinder.State}");
                sb.AppendLine($"  Can Accept Binds: {_canvasBinder.CanAcceptBinds()}");
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>(sb.ToString());
        }

        #endregion

        #region 🔧 MÉTODOS DE DEBUG PARA O NOVO SISTEMA

        [ContextMenu("Debug/New System/Force Canvas Ready")]
        public void DebugForceCanvasReady()
        {
            if (_canvasBinder != null)
            {
                _canvasBinder.ForceReady();
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Canvas {_canvasBinder.CanvasId} forced to ready state");
            }
        }

        [ContextMenu("Debug/New System/Test Dependency Injection")]
        public void DebugTestDependencyInjection()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔧 TESTING DEPENDENCY INJECTION");

            bool orchestratorInjected = _orchestrator != null;
            bool pipelineInjected = _pipelineManager != null;
            bool idFactoryInjected = _idFactory != null;

            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Orchestrator Injected: {orchestratorInjected}");
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Pipeline Manager Injected: {pipelineInjected}");
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"ID Factory Injected: {idFactoryInjected}");

            if (_actor != null)
            {
                bool resourceSystemAvailable = _orchestrator?.GetActorResourceSystem(_actor.ActorId) != null;
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"ResourceSystem for actor: {resourceSystemAvailable}");
            }
        }

        [ContextMenu("Debug/New System/Reinitialize Components")]
        public void DebugReinitializeComponents()
        {
            StartCoroutine(ReinitializeComponentsRoutine());
        }

        private IEnumerator ReinitializeComponentsRoutine()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔄 REINITIALIZING COMPONENTS");

            if (_entityBridge != null)
            {
                // Forçar reinicialização do entity bridge
                _entityBridge.InjectionState = DependencyInjectionState.Pending;
                ResourceInitializationManager.Instance.RegisterForInjection(_entityBridge);
            }

            yield return new WaitForSeconds(0.5f);

            if (_canvasBinder != null)
            {
                // Forçar reinicialização do canvas binder
                var canvas = _canvasBinder as MonoBehaviour;
                if (canvas != null)
                {
                    // Recriar o componente (extremo - apenas para debug)
                    DebugUtility.LogWarning<NewResourceSystemDebugManager>("Note: Canvas recreation would require component replacement");
                }
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔄 COMPONENT REINITIALIZATION COMPLETED");
        }

        #endregion

        #region 🎮 CENÁRIOS DE TESTE DO NOVO SISTEMA

        [ContextMenu("Tests/Scenarios/Test Scene Canvas Integration")]
        public void TestSceneCanvasIntegration()
        {
            StartCoroutine(SceneCanvasIntegrationTest());
        }

        private IEnumerator SceneCanvasIntegrationTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🏞️ TESTING SCENE CANVAS INTEGRATION");

            // Testar com canvas de cena (pré-existentes)
            if (_canvasBinder != null && _canvasBinder.Type == CanvasType.Scene)
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Testing Scene Canvas...");

                // Aplicar modificações para testar o bind
                for (int i = 0; i < 3; i++)
                {
                    ApplyDamage(10);
                    yield return new WaitForSeconds(0.3f);
                }

                yield return new WaitForSeconds(0.5f);

                for (int i = 0; i < 2; i++)
                {
                    ApplyHeal(15);
                    yield return new WaitForSeconds(0.3f);
                }
            }
            else
            {
                DebugUtility.LogWarning<NewResourceSystemDebugManager>("No Scene Canvas found on this object");
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🏞️ SCENE CANVAS INTEGRATION TEST COMPLETED");
        }

        [ContextMenu("Tests/Scenarios/Stress Test Resource System")]
        public void StressTestResourceSystem()
        {
            StartCoroutine(ResourceSystemStressTest());
        }

        private IEnumerator ResourceSystemStressTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("💥 STARTING RESOURCE SYSTEM STRESS TEST");

            int testIterations = 20;
            float delayBetweenOperations = 0.05f;

            for (int i = 0; i < testIterations; i++)
            {
                // Aplicar operações rápidas e alternadas
                ApplyDamage(5);
                yield return new WaitForSeconds(delayBetweenOperations);

                ApplyHeal(3);
                yield return new WaitForSeconds(delayBetweenOperations);

                if (i % 5 == 0)
                {
                    // Aplicar operação maior a cada 5 iterações
                    ApplyDamage(25);
                    yield return new WaitForSeconds(delayBetweenOperations);
                }
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("💥 RESOURCE SYSTEM STRESS TEST COMPLETED");
        }

        #endregion

        #region 🔄 MÉTODOS COMPATÍVEIS (mantidos do original)

        [ContextMenu("Test/Apply Damage")]
        public void DebugDamage()
        {
            ApplyDamage(testDamageAmount, testResourceType);
        }

        [ContextMenu("Test/Apply Heal")]
        public void DebugHeal()
        {
            ApplyHeal(testHealAmount, testResourceType);
        }

        [ContextMenu("Debug/Resources/Print All Resources")]
        public void DebugPrintResources()
        {
            if (_entityBridge != null)
            {
                _entityBridge.DebugPrintResources();
            }
            else if (_resourceSystem != null)
            {
                var resources = _resourceSystem.GetAll();
                DebugUtility.LogWarning<NewResourceSystemDebugManager>("Current Resources:");
                foreach (var resource in resources)
                {
                    DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"  {resource.Key}: {resource.Value.GetCurrentValue():F1}/{resource.Value.GetMaxValue():F1}");
                }
            }
        }

        public void ApplyDamage(float amount, ResourceType resourceType = ResourceType.Health)
        {
            if (_actor == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("No Actor available");
                return;
            }

            var resourceSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
            if (resourceSystem != null)
            {
                resourceSystem.Modify(resourceType, -Math.Abs(amount));
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Damage applied: {amount} to {resourceType}");
            }
            else
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>($"ResourceSystem not found for actor {_actor.ActorId}");
            }
        }

        public void ApplyHeal(float amount, ResourceType resourceType = ResourceType.Health)
        {
            if (_actor == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("No Actor available");
                return;
            }

            var resourceSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
            if (resourceSystem != null)
            {
                resourceSystem.Modify(resourceType, Math.Abs(amount));
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Heal applied: {amount} to {resourceType}");
            }
            else
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>($"ResourceSystem not found for actor {_actor.ActorId}");
            }
        }

        [ContextMenu("Debug/Show All Status")]
        public void DebugAllComponentsStatus()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("=== COMPREHENSIVE DEBUG STATUS ===");

            ShowNewSystemMetrics();
            ShowCanvasStates();

            if (_entityBridge != null)
            {
                _entityBridge.DebugPrintResources();
            }

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("=== END DEBUG STATUS ===");
        }

        [ContextMenu("Debug/Quick Test All Systems")]
        public void QuickTestAllSystems()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🚀 STARTING QUICK TEST OF ALL SYSTEMS");

            DebugTestDependencyInjection();
            DebugPrintResources();
            ShowNewSystemMetrics();
            DebugAllComponentsStatus();

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("✅ QUICK TEST COMPLETED");
        }

        [ContextMenu("Debug/Clear Event Log")]
        public void ClearEventLog()
        {
            _eventLog.Clear();
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Event log cleared");
        }

        #endregion

        [ContextMenu("Debug/System/Full Initialization Check")]
        public void DebugFullInitializationCheck()
        {
            StartCoroutine(ComprehensiveInitializationDiagnostic());
        }

        private IEnumerator ComprehensiveInitializationDiagnostic()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🔍 FULL INITIALIZATION DIAGNOSTIC");

            // 1. Verificar Dependency Manager
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Step 1: Checking Dependency Manager");
            DebugTestDependencyInjection();

            // 2. Verificar Component States
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Step 2: Checking Component States");
            yield return CheckComponentStates();

            // 3. Verificar Resource System
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Step 3: Checking Resource System");
            yield return CheckResourceSystemState();

            // 4. Verificar Canvas Pipeline
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Step 4: Checking Canvas Pipeline");
            yield return CheckCanvasPipelineState();

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("✅ DIAGNOSTIC COMPLETE");
        }

        private IEnumerator CheckComponentStates()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📋 COMPONENT STATES:");

            // Actor
            sb.AppendLine($"Actor: {_actor?.ActorId} ({(_actor != null ? "✅" : "❌")})");

            // Resource System
            var resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            sb.AppendLine($"Resource System: {(resourceSystem != null ? "✅" : "❌")}");

            // Canvas Binder
            sb.AppendLine($"Canvas Binder: {(_canvasBinder != null ? $"✅ ({_canvasBinder.State})" : "❌")}");

            // Injection States
            sb.AppendLine($"Debug Manager Injection: {InjectionState}");
            sb.AppendLine($"Entity Bridge Injection: {_entityBridge?.InjectionState ?? DependencyInjectionState.Failed}");

            DebugUtility.LogWarning<NewResourceSystemDebugManager>(sb.ToString());
            yield return null;
        }

        private IEnumerator CheckResourceSystemState()
        {
            if (_resourceSystem == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("ResourceSystem is null!");
                yield break;
            }

            var resources = _resourceSystem.GetAll();
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Resource System has {resources.Count} resources");

            foreach (var resource in resources)
            {
                var instanceConfig = _resourceSystem.GetInstanceConfig(resource.Key);
                string canvasTarget = instanceConfig?.canvasTargetMode.ToString() ?? "Unknown";

                DebugUtility.LogVerbose<NewResourceSystemDebugManager>(
                    $"{resource.Key}: {resource.Value.GetCurrentValue():F1}/{resource.Value.GetMaxValue():F1} " +
                    $"(Target: {canvasTarget})");
            }

            yield return null;
        }

        private IEnumerator CheckCanvasPipelineState()
        {
            var canvasIds = _orchestrator?.GetRegisteredCanvasIds();
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Registered Canvases: {canvasIds?.Count ?? 0}");

            if (canvasIds != null)
            {
                foreach (var canvasId in canvasIds)
                {
                    bool isReady = _orchestrator.IsCanvasRegistered(canvasId);
                    DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"  - {canvasId}: {(isReady ? "✅" : "❌")}");
                }
            }

            yield return null;
        }
        [ContextMenu("Debug/Test/First Damage Flow Test")]
        public void DebugFirstDamageFlowTest()
        {
            StartCoroutine(FirstDamageFlowTest());
        }

        private IEnumerator FirstDamageFlowTest()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🎯 FIRST DAMAGE FLOW TEST");

            // 1. Estado antes do dano
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("BEFORE DAMAGE:");
            yield return CheckResourceSystemState();

            // 2. Aplicar primeiro dano
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("APPLYING FIRST DAMAGE...");
            ApplyDamage(10f);
            yield return new WaitForSeconds(0.1f); // Pequeno delay para processamento

            // 3. Estado após primeiro dano
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("AFTER FIRST DAMAGE:");
            yield return CheckResourceSystemState();

            // 4. Verificar se evento foi disparado
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("CHECKING FOR EVENTS...");
            yield return CheckEventSystem();

            // 5. Aplicar segundo dano
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("APPLYING SECOND DAMAGE...");
            ApplyDamage(10f);
            yield return new WaitForSeconds(0.1f);

            // 6. Estado após segundo dano
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("AFTER SECOND DAMAGE:");
            yield return CheckResourceSystemState();

            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🎯 FIRST DAMAGE FLOW TEST COMPLETE");
        }

        private IEnumerator CheckEventSystem()
        {
            DebugUtility.LogVerbose<NewResourceSystemDebugManager>("Recent Events:");
            foreach (var eventMsg in _eventLog)
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"  {eventMsg}");
            }
            yield return null;
        }
        [ContextMenu("Debug/Slot/Animation Diagnosis")]
        public void DebugSlotAnimationDiagnosis()
        {
            StartCoroutine(SlotAnimationDiagnosis());
        }

        private IEnumerator SlotAnimationDiagnosis()
        {
            DebugUtility.LogWarning<NewResourceSystemDebugManager>("🎨 SLOT ANIMATION DIAGNOSIS");

            if (_canvasBinder == null)
            {
                DebugUtility.LogError<NewResourceSystemDebugManager>("No Canvas Binder found");
                yield break;
            }

            // Verificar estratégia de animação
            var slot = FindFirstResourceSlot();
            if (slot != null)
            {
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Slot Found: {slot.name}");
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Slot Type: {slot.GetType().Name}");

                // Verificar componentes UI
                var fillImage = slot.FillImage;
                var pendingImage = slot.PendingFillImage;
                var valueText = slot.ValueText;

                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Fill Image: {fillImage != null}");
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Pending Image: {pendingImage != null}");
                DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Value Text: {valueText != null}");

                if (fillImage != null)
                {
                    DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Fill Amount: {fillImage.fillAmount}");
                    DebugUtility.LogVerbose<NewResourceSystemDebugManager>($"Fill Color: {fillImage.color}");
                }
            }
            else
            {
                DebugUtility.LogWarning<NewResourceSystemDebugManager>("No Resource Slot found");
            }

            yield return null;
        }

        private ResourceUISlot FindFirstResourceSlot()
        {
            // Buscar o primeiro slot na cena para diagnóstico
            return FindObjectOfType<ResourceUISlot>();
        }
        private void OnDestroy()
        {
            if (_thresholdBinding != null)
            {
                EventBus<ResourceThresholdEvent>.Unregister(_thresholdBinding);
                _thresholdBinding = null;
            }

            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
                _resourceUpdateBinding = null;
            }

            if (_canvasBindBinding != null)
            {
                EventBus<CanvasBindRequest>.Unregister(_canvasBindBinding);
                _canvasBindBinding = null;
            }
        }
    }
}