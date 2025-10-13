using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class InjectableEntityResourceBridge : MonoBehaviour, IInjectableComponent
    {
        [SerializeField] private ResourceInstanceConfig[] resourceInstances = Array.Empty<ResourceInstanceConfig>();

        [Inject] private IActorResourceOrchestrator _orchestrator;

        private IActor _actor;
        private ResourceSystem _service;
        private bool _isDestroyed;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => _actor?.ActorId ?? gameObject.name;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<InjectableEntityResourceBridge>($"No IActor found on {gameObject.name}");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Awake called for {_actor.ActorId}");
        }

        public void OnDependenciesInjected()
        {
            if (_isDestroyed) return;

            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                if (DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem existingService))
                {
                    Debug.LogWarning($"[EntityBridge] ResourceSystem already exists for {_actor.ActorId}, reusing");
                    _service = existingService;
                }
                else
                {
                    _service = new ResourceSystem(_actor.ActorId, resourceInstances);
                    DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"✅ Created new ResourceSystem for '{_actor.ActorId}'");
                }

                if (_orchestrator != null && !_orchestrator.IsActorRegistered(_actor.ActorId))
                {
                    _orchestrator.RegisterActor(_service);
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"✅ Registered actor '{_actor.ActorId}' in orchestrator");
                }
                else
                {
                    DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Actor '{_actor.ActorId}' already registered in orchestrator");
                }

                InjectionState = DependencyInjectionState.Ready;

                DebugResourceSystemAccess();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableEntityResourceBridge>($"❌ Entity bridge failed for '{_actor.ActorId}': {ex}");
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void DebugResourceSystemAccess()
        {
            StartCoroutine(DebugResourceSystemCoroutine());
        }

        private System.Collections.IEnumerator DebugResourceSystemCoroutine()
        {
            yield return new WaitForSeconds(1f);

            Debug.Log($"[EntityBridge] 🔍 POST-INIT CHECK for {_actor.ActorId}:");
            Debug.Log($"  - Local _service: {_service != null}");
            Debug.Log($"  - InjectionState: {InjectionState}");

            bool hasInDm = DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem dmService);
            Debug.Log($"  - In DependencyManager: {hasInDm}, Service: {dmService != null}");

            if (_orchestrator != null)
            {
                bool hasInOrchestrator = _orchestrator.IsActorRegistered(_actor.ActorId);
                Debug.Log($"  - In Orchestrator: {hasInOrchestrator}");

                if (hasInOrchestrator)
                {
                    var orchestratorService = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    Debug.Log($"  - Orchestrator Service: {orchestratorService != null}");
                }
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"OnDestroy called for {_actor?.ActorId}");

            if (_orchestrator != null && _actor != null)
            {
                _orchestrator.UnregisterActor(_actor.ActorId);
                DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Unregistered actor '{_actor.ActorId}' from orchestrator");
            }

            if (_actor != null)
            {
                DependencyManager.Instance.ClearObjectServices(_actor.ActorId);
                DebugUtility.LogVerbose<InjectableEntityResourceBridge>($"Cleared services for '{_actor.ActorId}' from DependencyManager");
            }

            _service?.Dispose();
            _service = null;
        }

        [ContextMenu("🔍 DEBUG BRIDGE STATUS")]
        public void DebugBridgeStatus()
        {
            Debug.Log($"🔧 ENTITY BRIDGE STATUS: {_actor?.ActorId}");
            Debug.Log($"- Actor: {_actor?.ActorId}");
            Debug.Log($"- Service: {_service != null}");
            Debug.Log($"- Injection: {InjectionState}");
            Debug.Log($"- Destroyed: {_isDestroyed}");

            if (_service != null)
            {
                var health = _service.Get(ResourceType.Health);
                Debug.Log($"- Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }

            if (DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator))
            {
                bool isRegistered = orchestrator.IsActorRegistered(_actor?.ActorId);
                Debug.Log($"- Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorService = orchestrator.GetActorResourceSystem(_actor?.ActorId);
                    Debug.Log($"- Orchestrator Service: {orchestratorService != null}");
                }
            }

            bool hasInDm = DependencyManager.Instance.TryGetForObject(_actor?.ActorId, out ResourceSystem dmService);
            Debug.Log($"- In DependencyManager: {hasInDm}, Service: {dmService != null}");
        }

        [ContextMenu("🔄 TEST RESOURCE ACCESS")]
        public void TestResourceAccess()
        {
            StartCoroutine(TestResourceAccessCoroutine());
        }

        private System.Collections.IEnumerator TestResourceAccessCoroutine()
        {
            Debug.Log($"=== 🎯 TESTING RESOURCE ACCESS ({_actor.ActorId}) ===");

            Debug.Log("📊 Test 1 - Local Access:");
            Debug.Log($"  - Local _service: {_service != null}");
            if (_service != null)
            {
                var health = _service.Get(ResourceType.Health);
                Debug.Log($"  - Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }

            yield return null;

            Debug.Log("📊 Test 2 - DependencyManager Access:");
            bool dmSuccess = DependencyManager.Instance.TryGetForObject(_actor.ActorId, out ResourceSystem dmService);
            Debug.Log($"  - DM Success: {dmSuccess}");
            Debug.Log($"  - DM Service: {dmService != null}");
            if (dmService != null)
            {
                var health = dmService.Get(ResourceType.Health);
                Debug.Log($"  - Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }

            yield return null;

            Debug.Log("📊 Test 3 - Orchestrator Access:");
            if (_orchestrator != null)
            {
                bool orchestratorSuccess = _orchestrator.IsActorRegistered(_actor.ActorId);
                Debug.Log($"  - Orchestrator Registered: {orchestratorSuccess}");
                if (orchestratorSuccess)
                {
                    var orchestratorService = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    Debug.Log($"  - Orchestrator Service: {orchestratorService != null}");
                    if (orchestratorService != null)
                    {
                        var health = orchestratorService.Get(ResourceType.Health);
                        Debug.Log($"  - Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
                    }
                }
            }

            Debug.Log($"=== ✅ TEST COMPLETE ===");
        }

        #region Métodos originais (mantidos para compatibilidade)

        [ContextMenu("Debug/Print Resources")]
        public void DebugPrintResources()
        {
            if (_service == null)
            {
                DebugUtility.LogWarning<InjectableEntityResourceBridge>("ResourceSystem not available");
                return;
            }

            IReadOnlyDictionary<ResourceType, IResourceValue> all = _service.GetAll();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📊 RESOURCES FOR ACTOR: {_actor.ActorId}");
            sb.AppendLine($"Injection State: {InjectionState}");
            sb.AppendLine($"Total Resources: {all.Count}");
            sb.AppendLine("────────────────────────");

            foreach (KeyValuePair<ResourceType, IResourceValue> kv in all)
            {
                var resource = kv.Value;
                var instanceConfig = _service.GetInstanceConfig(kv.Key);
                string canvasTarget = "Unknown";

                if (instanceConfig != null)
                {
                    canvasTarget = instanceConfig.canvasTargetMode switch
                    {
                        CanvasTargetMode.ActorSpecific => $"{_actor.ActorId}_Canvas",
                        CanvasTargetMode.Custom => instanceConfig.customCanvasId ?? "MainUI",
                        _ => "MainUI"
                    };
                }

                sb.AppendLine($"🔹 {kv.Key}:");
                sb.AppendLine($"   Value: {resource.GetCurrentValue():F1}/{resource.GetMaxValue():F1}");
                sb.AppendLine($"   Percentage: {(resource.GetCurrentValue() / resource.GetMaxValue()):P1}");
                sb.AppendLine($"   Canvas Target: {canvasTarget}");

                if (instanceConfig?.hasAutoFlow ?? false)
                    sb.AppendLine($"   AutoFlow: ✅ (Rate: {instanceConfig.autoFlowConfig.tickInterval})");

                if (instanceConfig?.thresholdConfig != null)
                    sb.AppendLine($"   Thresholds: ✅ ({instanceConfig.thresholdConfig?.thresholds.Length ?? 0} thresholds)");
            }

            DebugUtility.LogWarning<InjectableEntityResourceBridge>(sb.ToString());
        }

        #endregion
    }


}