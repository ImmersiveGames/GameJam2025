using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceSystemDebugManager : MonoBehaviour
    {
        [Header("Debug Configuration")]
        [SerializeField] private bool enableDebugFeatures = true;
        [SerializeField] private bool enableEventListening = true;
        [SerializeField] private bool autoInitialize = true;
        
        [Header("Integration Testing")]
        [SerializeField] private ResourceType testResourceType = ResourceType.Health;
        [SerializeField] private float testDamageAmount = -20f;
        [SerializeField] private float testHealAmount = 30f;
        [SerializeField] private float testThresholdValue = 50f;

        [Header("Event Monitoring")]
        [SerializeField] private bool logThresholdEvents = true;
        [SerializeField] private bool logResourceUpdates = true;
        
        // Component References
        private CanvasResourceBinder _canvasBinder;
        private EntityResourceBridge _entityBridge;
        private ResourceAutoFlowBridge _autoFlowBridge;
        private ResourceLinkBridge _linkBridge;
        private ResourceThresholdBridge _thresholdBridge;
        
        // Event System
        private EventBinding<ResourceThresholdEvent> _thresholdBinding;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private Queue<string> _eventLog = new Queue<string>();
        
        // Actor Reference
        private IActor _actor;
        private ResourceSystem _resourceSystem;
        private IActorResourceOrchestrator _orchestrator;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            
            if (!enableDebugFeatures)
            {
                DebugUtility.LogVerbose<ResourceSystemDebugManager>("Debug features disabled");
                enabled = false;
                return;
            }

            if (autoInitialize)
            {
                InitializeDebugManager();
            }
        }

        [ContextMenu("Debug/Initialize Debug Manager")]
        public void InitializeDebugManager()
        {
            GatherComponents();
            SetupEventListeners();
            DebugUtility.LogWarning<ResourceSystemDebugManager>("✅ Resource System Debug Manager Initialized");
        }

        private void GatherComponents()
        {
            _canvasBinder = GetComponent<CanvasResourceBinder>();
            _entityBridge = GetComponent<EntityResourceBridge>();
            _autoFlowBridge = GetComponent<ResourceAutoFlowBridge>();
            _linkBridge = GetComponent<ResourceLinkBridge>();
            _thresholdBridge = GetComponent<ResourceThresholdBridge>();

            // Tentar obter o resource system
            if (_actor != null)
            {
                DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out _resourceSystem);
            }

            DependencyManager.Instance.TryGetGlobal(out _orchestrator);

            DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Debug Manager initialized - Components: " +
                $"CanvasBinder: {_canvasBinder != null}, " +
                $"EntityBridge: {_entityBridge != null}, " +
                $"AutoFlow: {_autoFlowBridge != null}, " +
                $"LinkBridge: {_linkBridge != null}, " +
                $"ThresholdBridge: {_thresholdBridge != null}, " +
                $"ResourceSystem: {_resourceSystem != null}");
        }

        private void SetupEventListeners()
        {
            if (!enableEventListening) return;

            // Threshold Events
            _thresholdBinding = new EventBinding<ResourceThresholdEvent>(OnThresholdCrossed);
            EventBus<ResourceThresholdEvent>.Register(_thresholdBinding);
            
            // Resource Update Events (CORRIGIDO: ResourceUpdateEvent em vez de ResourceChangedEvent)
            _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
            
            DebugUtility.LogVerbose<ResourceSystemDebugManager>("✅ Event listeners initialized");
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

        private void LogEvent(string message)
        {
            _eventLog.Enqueue($"[{Time.time:F2}] {message}");
            
            // Manter apenas os últimos 15 eventos
            if (_eventLog.Count > 15)
                _eventLog.Dequeue();

            DebugUtility.LogVerbose<ResourceSystemDebugManager>(message);
        }

        #region 🎯 TESTES PRÁTICOS E INFORMATIVOS

        [ContextMenu("Tests/Integration/Validate System State")]
        public void ValidateSystemState()
        {
            StartCoroutine(ComprehensiveSystemValidation());
        }

        private IEnumerator ComprehensiveSystemValidation()
        {
            DebugUtility.LogWarning<ResourceSystemDebugManager>("🔍 STARTING SYSTEM VALIDATION");

            bool allValid = true;

            // Validar Resource System
            if (_resourceSystem != null)
            {
                var resources = _resourceSystem.GetAll();
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Found {resources.Count} resources");
                
                foreach (var resource in resources)
                {
                    var value = resource.Value.GetCurrentValue();
                    var max = resource.Value.GetMaxValue();
                    
                    if (value < 0 || value > max)
                    {
                        DebugUtility.LogError<ResourceSystemDebugManager>($"❌ INVALID RESOURCE: {resource.Key} = {value}/{max}");
                        allValid = false;
                    }
                    else
                    {
                        DebugUtility.LogVerbose<ResourceSystemDebugManager>($"✅ {resource.Key}: {value:F1}/{max:F1} ({(value/max):P1})");
                    }
                }
            }
            else
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("❌ RESOURCE SYSTEM NOT FOUND");
                allValid = false;
            }

            // Validar Dependencies
            if (!DependencyManager.HasInstance || DependencyManager.Instance == null)
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("❌ DEPENDENCY MANAGER NOT AVAILABLE");
                allValid = false;
            }
            else
            {
                DebugUtility.LogVerbose<ResourceSystemDebugManager>("✅ Dependency Manager available");
            }

            // Validar Orchestrator
            if (_orchestrator == null)
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("❌ ORCHESTRATOR NOT FOUND");
                allValid = false;
            }
            else
            {
                var actorIds = _orchestrator.GetRegisteredActorIds();
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"✅ Orchestrator available - {actorIds.Count} actors registered");
            }

            if (allValid)
            {
                DebugUtility.LogWarning<ResourceSystemDebugManager>("✅ SYSTEM VALIDATION PASSED");
            }
            else
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("❌ SYSTEM VALIDATION FAILED");
            }

            yield return null;
        }

        [ContextMenu("Tests/Integration/Test Resource Boundaries")]
        public void TestResourceBoundaries()
        {
            if (_resourceSystem == null) 
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("ResourceSystem not available");
                return;
            }

            DebugUtility.LogWarning<ResourceSystemDebugManager>("🎯 TESTING RESOURCE BOUNDARIES");

            var resources = _resourceSystem.GetAll();
            foreach (var resource in resources)
            {
                var originalValue = resource.Value.GetCurrentValue();
                var maxValue = resource.Value.GetMaxValue();
                
                // Testar underflow
                _resourceSystem.Set(resource.Key, -1000);
                float afterUnderflow = resource.Value.GetCurrentValue();
                
                // Testar overflow
                _resourceSystem.Set(resource.Key, maxValue + 1000);
                float afterOverflow = resource.Value.GetCurrentValue();

                // Restaurar valor original
                _resourceSystem.Set(resource.Key, originalValue);

                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"  {resource.Key}: " +
                    $"Underflow({-1000}→{afterUnderflow}) " +
                    $"Overflow({maxValue + 1000}→{afterOverflow}) " +
                    $"Bounds: [0, {maxValue}]");
            }
        }

        [ContextMenu("Tests/Integration/Test Dependency Injection")]
        public void TestDependencyInjection()
        {
            DebugUtility.LogWarning<ResourceSystemDebugManager>("🔧 TESTING DEPENDENCY INJECTION");

            bool orchestratorFound = DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator);
            bool factoryFound = DependencyManager.Instance.TryGetGlobal(out IUniqueIdFactory factory);
            bool linkServiceFound = DependencyManager.Instance.TryGetGlobal(out IResourceLinkService linkService);
            
            DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Orchestrator: {orchestratorFound}");
            DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Factory: {factoryFound}");
            DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Link Service: {linkServiceFound}");
            DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Actor: {_actor != null}");

            if (_actor != null)
            {
                bool resourceSystemFound = DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out _);
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"ResourceSystem for actor: {resourceSystemFound}");
            }
        }

        #endregion

        #region 🎮 CENÁRIOS PRÁTICOS

        [ContextMenu("Tests/Scenarios/Simulate Combat Scenario")]
        public void SimulateCombatScenario()
        {
            StartCoroutine(CombatScenarioSimulation());
        }

        private IEnumerator CombatScenarioSimulation()
        {
            DebugUtility.LogWarning<ResourceSystemDebugManager>("⚔️ STARTING COMBAT SCENARIO SIMULATION");

            // Fase 1: Dano gradual
            for (int i = 0; i < 3; i++)
            {
                ApplyDamage(15);
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.5f);

            // Fase 2: Cura
            for (int i = 0; i < 2; i++)
            {
                ApplyHeal(20);
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.5f);

            // Fase 3: Dano crítico
            ApplyDamage(40);

            DebugUtility.LogWarning<ResourceSystemDebugManager>("⚔️ COMBAT SCENARIO COMPLETED");
        }

        [ContextMenu("Tests/Scenarios/Test All Resources")]
        public void TestAllResources()
        {
            if (_resourceSystem == null) return;

            DebugUtility.LogWarning<ResourceSystemDebugManager>("🧪 TESTING ALL RESOURCES");

            var resources = _resourceSystem.GetAll();
            foreach (var resource in resources)
            {
                // Aplicar pequena modificação para testar
                _resourceSystem.Modify(resource.Key, -5);
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Tested {resource.Key}: -5 damage");
            }
        }

        [ContextMenu("Tests/Scenarios/Test AutoFlow Integration")]
        public void TestAutoFlowIntegration()
        {
            if (_autoFlowBridge == null) return;

            DebugUtility.LogWarning<ResourceSystemDebugManager>("🔄 TESTING AUTOFLOW INTEGRATION");

            // Testar pausa e retomada
            DebugAutoFlowPause();
            StartCoroutine(TestAutoFlowCycle());
        }

        private IEnumerator TestAutoFlowCycle()
        {
            yield return new WaitForSeconds(1f);
            DebugAutoFlowResume();
            yield return new WaitForSeconds(1f);
            DebugAutoFlowToggle();
            
            DebugUtility.LogWarning<ResourceSystemDebugManager>("🔄 AUTOFLOW INTEGRATION TEST COMPLETED");
        }

        #endregion

        #region 📊 MÉTRICAS E RELATÓRIOS

        [ContextMenu("Debug/Metrics/Show System Metrics")]
        public void ShowSystemMetrics()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📈 SYSTEM METRICS");
            sb.AppendLine("=================");

            // Métricas de recursos
            if (_resourceSystem != null)
            {
                var resources = _resourceSystem.GetAll();
                sb.AppendLine($"Total Resources: {resources.Count}");
                
                foreach (var resource in resources)
                {
                    float percentage = resource.Value.GetCurrentValue() / resource.Value.GetMaxValue();
                    sb.AppendLine($"  {resource.Key}: {resource.Value.GetCurrentValue():F1}/{resource.Value.GetMaxValue():F1} ({percentage:P1})");
                    
                    // Mostrar configuração de instance
                    var instanceConfig = _resourceSystem.GetInstanceConfig(resource.Key);
                    if (instanceConfig != null)
                    {
                        sb.AppendLine($"    Config: AutoFlow={instanceConfig.hasAutoFlow}, Thresholds={instanceConfig.thresholdConfig != null}");
                    }
                }
            }
            else
            {
                sb.AppendLine("No ResourceSystem available");
            }

            // Métricas de eventos
            sb.AppendLine($"Recent Events: {_eventLog.Count}");

            DebugUtility.LogWarning<ResourceSystemDebugManager>(sb.ToString());
        }

        [ContextMenu("Debug/Metrics/Show Recent Events")]
        public void ShowRecentEvents()
        {
            if (_eventLog.Count == 0)
            {
                DebugUtility.LogWarning<ResourceSystemDebugManager>("No recent events");
                return;
            }

            DebugUtility.LogWarning<ResourceSystemDebugManager>("📋 RECENT EVENTS:");
            foreach (string eventMsg in _eventLog)
            {
                DebugUtility.Log<ResourceSystemDebugManager>(eventMsg);
            }
        }

        #endregion

        #region 🔧 MÉTODOS DE DEBUG ORIGINAIS

        [ContextMenu("Debug/Canvas/Apply Canvas Offset")]
        public void DebugApplyCanvasOffset()
        {
            if (_canvasBinder != null)
            {
                _canvasBinder.ApplyCanvasOffset();
                DebugUtility.LogVerbose<ResourceSystemDebugManager>("Canvas offset applied via Debug Manager");
            }
        }

        [ContextMenu("Debug/Canvas/Reset Canvas Position")]
        public void DebugResetCanvasPosition()
        {
            if (_canvasBinder != null)
            {
                _canvasBinder.transform.localPosition = Vector3.zero;
                _canvasBinder.transform.localRotation = Quaternion.identity;
                DebugUtility.LogVerbose<ResourceSystemDebugManager>("Canvas position and rotation reset");
            }
        }

        [ContextMenu("Debug/Canvas/Show Slots State")]
        public void DebugSlotsState()
        {
            if (_canvasBinder != null)
            {
                _canvasBinder.DebugSlots();
            }
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
                // Fallback: mostrar recursos diretamente
                var resources = _resourceSystem.GetAll();
                DebugUtility.LogWarning<ResourceSystemDebugManager>("Current Resources:");
                foreach (var resource in resources)
                {
                    DebugUtility.LogVerbose<ResourceSystemDebugManager>($"  {resource.Key}: {resource.Value.GetCurrentValue():F1}/{resource.Value.GetMaxValue():F1}");
                }
            }
        }

        [ContextMenu("Debug/AutoFlow/Show Status")]
        public void DebugAutoFlowStatus()
        {
            if (_autoFlowBridge != null)
            {
                _autoFlowBridge.DebugAutoFlowStatus();
            }
        }

        [ContextMenu("Debug/AutoFlow/Pause")]
        public void DebugAutoFlowPause()
        {
            if (_autoFlowBridge != null)
            {
                _autoFlowBridge.ContextPause();
            }
        }

        [ContextMenu("Debug/AutoFlow/Resume")]
        public void DebugAutoFlowResume()
        {
            if (_autoFlowBridge != null)
            {
                _autoFlowBridge.ContextResume();
            }
        }

        [ContextMenu("Debug/AutoFlow/Toggle")]
        public void DebugAutoFlowToggle()
        {
            if (_autoFlowBridge != null)
            {
                _autoFlowBridge.ContextToggle();
            }
        }

        [ContextMenu("Debug/Links/Show Active Links")]
        public void DebugActiveLinks()
        {
            if (_linkBridge != null)
            {
                _linkBridge.DebugActiveLinks();
            }
        }

        [ContextMenu("Debug/Threshold/Force Check")]
        public void DebugForceThresholdCheck()
        {
            if (_thresholdBridge != null)
            {
                _thresholdBridge.ContextForce();
            }
        }

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

        [ContextMenu("Test/Modify Resource")]
        public void DebugModifyResource()
        {
            if (_resourceSystem != null)
            {
                _resourceSystem.Modify(testResourceType, testThresholdValue);
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Modified {testResourceType} by {testThresholdValue}");
            }
        }

        public void ApplyDamage(float amount, ResourceType resourceType = ResourceType.Health)
        {
            if (_actor == null) 
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("No Actor available");
                return;
            }
            
            if (DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out var svc))
            {
                svc.Modify(resourceType, -Math.Abs(amount));
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Damage applied: {amount} to {resourceType}");
            }
            else
            {
                DebugUtility.LogError<ResourceSystemDebugManager>($"ResourceSystem not found for actor {_actor.ActorId}");
            }
        }

        public void ApplyHeal(float amount, ResourceType resourceType = ResourceType.Health)
        {
            if (_actor == null) 
            {
                DebugUtility.LogError<ResourceSystemDebugManager>("No Actor available");
                return;
            }
            
            if (DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out var svc))
            {
                svc.Modify(resourceType, Math.Abs(amount));
                DebugUtility.LogVerbose<ResourceSystemDebugManager>($"Heal applied: {amount} to {resourceType}");
            }
            else
            {
                DebugUtility.LogError<ResourceSystemDebugManager>($"ResourceSystem not found for actor {_actor.ActorId}");
            }
        }

        [ContextMenu("Debug/Show All Status")]
        public void DebugAllComponentsStatus()
        {
            DebugUtility.LogWarning<ResourceSystemDebugManager>("=== COMPREHENSIVE DEBUG STATUS ===");
            
            if (_canvasBinder != null) _canvasBinder.DebugStatus();
            if (_autoFlowBridge != null) _autoFlowBridge.DebugStatus();
            if (_linkBridge != null) _linkBridge.DebugStatus();
            if (_thresholdBridge != null) _thresholdBridge.DebugStatus();
            
            ShowSystemMetrics();
            
            DebugUtility.LogWarning<ResourceSystemDebugManager>("=== END DEBUG STATUS ===");
        }

        [ContextMenu("Debug/Quick Test All Systems")]
        public void QuickTestAllSystems()
        {
            DebugUtility.LogWarning<ResourceSystemDebugManager>("🚀 STARTING QUICK TEST OF ALL SYSTEMS");
            
            DebugSlotsState();
            DebugPrintResources();
            DebugAutoFlowStatus();
            DebugActiveLinks();
            DebugAllComponentsStatus();
            
            DebugUtility.LogWarning<ResourceSystemDebugManager>("✅ QUICK TEST COMPLETED");
        }

        [ContextMenu("Debug/Toggle Debug Mode")]
        public void ToggleDebugMode()
        {
            enableDebugFeatures = !enableDebugFeatures;
            DebugUtility.LogWarning<ResourceSystemDebugManager>($"Debug mode {(enableDebugFeatures ? "ENABLED" : "DISABLED")}");
        }

        [ContextMenu("Debug/Clear Event Log")]
        public void ClearEventLog()
        {
            _eventLog.Clear();
            DebugUtility.LogVerbose<ResourceSystemDebugManager>("Event log cleared");
        }

        #endregion

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
        }
    }
}