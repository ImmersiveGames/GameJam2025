using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    
    public class EntityDebugUtility : MonoBehaviour, IInjectableComponent
    {
        private const string TestsMenuRoot = "Tests/";
        private const string ResourcesMenuRoot = "Resources/";
        private const string DiagnosticsMenuRoot = "Diagnostics/";
        private const string BridgesMenuRoot = "Bridges/";

        private static readonly FieldInfo DamageReceiverTargetField =
            typeof(DamageReceiver).GetField("targetResource", BindingFlags.Instance | BindingFlags.NonPublic);

        public enum TestMode { Passive, Active, Hybrid }

        [Header("Test Settings")]
        [SerializeField] private bool autoTestOnReady;
        [SerializeField] private TestMode testMode = TestMode.Hybrid;
        [SerializeField] private float testDamage = 10f;
        [SerializeField] private ResourceType damageResourceType = ResourceType.Health; // Novo: Escolha o tipo de recurso para os testes
        [SerializeField] private float initializationDelay = 0.5f;
        [SerializeField] private float overallTimeout = 5f;
        [SerializeField] private bool verboseEvents = true;

        [Inject] private IActorResourceOrchestrator _orchestrator;
        private IActor _actor;
        private ResourceSystem _resourceSystem;
        private bool _resourceSystemResolved;

        private int _resourceUpdateEventsSeen;
        private int _canvasBindRequestsSeen;
        private bool _actorRegisteredEventSeen;

        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private EventBinding<ResourceEventHub.CanvasBindRequest> _canvasBindRequestBinding;
        private EventBinding<ResourceEventHub.ActorRegisteredEvent> _actorRegisteredBinding;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => _actor?.ActorId ?? gameObject.name;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<EntityDebugUtility>($"No IActor found on {gameObject.name}. Disabling.");
                enabled = false;
                return;
            }

            InjectionState = DependencyInjectionState.Pending;
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;

            _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdateEvent);
            EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);

            _canvasBindRequestBinding = new EventBinding<ResourceEventHub.CanvasBindRequest>(OnCanvasBindRequest);
            EventBus<ResourceEventHub.CanvasBindRequest>.Register(_canvasBindRequestBinding);

            _actorRegisteredBinding = new EventBinding<ResourceEventHub.ActorRegisteredEvent>(OnActorRegistered);
            EventBus<ResourceEventHub.ActorRegisteredEvent>.Register(_actorRegisteredBinding);

            if (autoTestOnReady)
                StartCoroutine(DelayedTestRoutine());
        }

        private void OnDestroy()
        {
            if (_resourceUpdateBinding != null) EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            if (_canvasBindRequestBinding != null) EventBus<ResourceEventHub.CanvasBindRequest>.Unregister(_canvasBindRequestBinding);
            if (_actorRegisteredBinding != null) EventBus<ResourceEventHub.ActorRegisteredEvent>.Unregister(_actorRegisteredBinding);
        }

        [ContextMenu(TestsMenuRoot + "Run Test Routine")]
        public void RunTestRoutine() => StartCoroutine(DelayedTestRoutine());

        private IEnumerator DelayedTestRoutine()
        {
            DebugUtility.Log<EntityDebugUtility>($"🔄 Starting delayed test for {_actor?.ActorId} (mode={testMode})");

            yield return new WaitForSeconds(initializationDelay);

            yield return StartCoroutine(ResolveResourceSystemWithRetry(overallTimeout));

            if (_resourceSystem == null)
            {
                DebugUtility.LogError<EntityDebugUtility>($"❌ Failed to resolve ResourceSystem for {_actor?.ActorId} within timeout ({overallTimeout}s)");
                ReportSummary();
                yield break;
            }

            switch (testMode)
            {
                case TestMode.Passive:
                    yield return StartCoroutine(PassiveObservationRoutine(overallTimeout));
                    break;
                case TestMode.Active:
                    yield return StartCoroutine(ActiveTestRoutine());
                    break;
                case TestMode.Hybrid:
                    yield return StartCoroutine(HybridRoutine());
                    break;
            }

            ReportSummary();
        }

        private IEnumerator ResolveResourceSystemWithRetry(float timeout)
        {
            float start = Time.time;
            while (Time.time - start < timeout)
            {
                _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);

                if (_resourceSystem != null)
                {
                    _resourceSystemResolved = true;
                    DebugUtility.Log<EntityDebugUtility>($"✅ ResourceSystem resolved for {_actor.ActorId}");
                    yield break;
                }

                yield return new WaitForSeconds(0.05f);
            }
            _resourceSystemResolved = false;
        }

        private IEnumerator PassiveObservationRoutine(float observationTime)
        {
            DebugUtility.Log<EntityDebugUtility>($"🕵️ Passive observation for {_actor.ActorId} ({observationTime}s)");
            _resourceUpdateEventsSeen = 0;
            _canvasBindRequestsSeen = 0;

            float start = Time.time;
            while (Time.time - start < observationTime)
            {
                yield return null;
            }

            DebugUtility.Log<EntityDebugUtility>($"🕵️ Passive observation finished. ResourceUpdates={_resourceUpdateEventsSeen}, CanvasBindRequests={_canvasBindRequestsSeen}");
        }

        private IEnumerator ActiveTestRoutine()
        {
            DebugUtility.Log<EntityDebugUtility>($"🔨 Active test for {_actor.ActorId}");
            yield return LogState("INITIAL");

            if (!ExecuteDamagePipeline(testDamage, damageResourceType, "Active Test - First Damage"))
            {
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER FIRST DAMAGE");

            if (!ExecuteDamagePipeline(testDamage, damageResourceType, "Active Test - Second Damage"))
            {
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER SECOND DAMAGE");
        }

        private IEnumerator HybridRoutine()
        {
            float passiveTime = Mathf.Min(1.0f, overallTimeout * 0.4f);
            float remaining = overallTimeout - passiveTime;

            DebugUtility.Log<EntityDebugUtility>($"Hybrid: passive {passiveTime}s then active (if needed)");
            yield return StartCoroutine(PassiveObservationRoutine(passiveTime));

            if (_resourceUpdateEventsSeen > 0)
            {
                DebugUtility.Log<EntityDebugUtility>("🔁 Observed natural resource updates — skipping active steps");
                yield break;
            }

            yield return StartCoroutine(ActiveTestRoutine());
        }

        private IEnumerator LogState(string phase)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 {phase}");
            sb.AppendLine($"Actor: {_actor?.ActorId}");
            sb.AppendLine($"ResourceSystem Local: {_resourceSystem != null}");
            sb.AppendLine($"Orchestrator Available: {_orchestrator != null}");
            sb.AppendLine($"ResourceSystem Resolved: {_resourceSystemResolved}");
            sb.AppendLine($"ResourceUpdateEventsSeen: {_resourceUpdateEventsSeen}");
            sb.AppendLine($"CanvasBindRequestsSeen: {_canvasBindRequestsSeen}");
            sb.AppendLine($"ActorRegisteredEventSeen: {_actorRegisteredEventSeen}");

            if (_resourceSystem != null)
            {
                foreach (var pair in _resourceSystem.GetAll())
                {
                    var value = pair.Value.GetCurrentValue();
                    var max = pair.Value.GetMaxValue();
                    sb.AppendLine($"  {pair.Key}: {value:F1}/{max:F1} ({(value / (max > 0 ? max : 1)):P1})");
                }
            }
            else
            {
                var temp = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
                sb.AppendLine($"  Re-check via Orchestrator: {temp != null}");
            }

            DebugUtility.Log<EntityDebugUtility>(sb.ToString());
            yield return null;
        }

        [ContextMenu(ResourcesMenuRoot + "Damage Selected Resource")]
        public void DamageSelectedResource()
        {
            if (testDamage <= Mathf.Epsilon)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("⚠️ Configure um valor de dano maior que zero antes de executar o teste.");
                return;
            }

            ExecuteDamagePipeline(testDamage, damageResourceType, "Damage Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Recover Selected Resource")]
        public void RecoverSelectedResource()
        {
            if (!TryGetResourceMetrics(damageResourceType, out var current, out var max))
            {
                return;
            }

            float missing = Mathf.Max(0f, max - current);
            if (missing <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"✅ {damageResourceType} já está cheio — nada para recuperar.");
                return;
            }

            float amount = -Mathf.Min(testDamage, missing);
            ExecuteDamagePipeline(amount, damageResourceType, "Recover Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Deplete Selected Resource")]
        public void DepleteSelectedResource()
        {
            if (!TryGetResourceMetrics(damageResourceType, out var current, out _))
            {
                return;
            }

            if (current <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"⚠️ {damageResourceType} já está zerado.");
                return;
            }

            ExecuteDamagePipeline(current, damageResourceType, "Deplete Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Restock Selected Resource")]
        public void RestockSelectedResource()
        {
            if (!TryGetResourceMetrics(damageResourceType, out var current, out var max))
            {
                return;
            }

            float missing = Mathf.Max(0f, max - current);
            if (missing <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"✅ {damageResourceType} já está no máximo.");
                return;
            }

            ExecuteDamagePipeline(-missing, damageResourceType, "Restock Selected Resource");
        }

        private bool ExecuteDamagePipeline(float amount, ResourceType requestedResource, string contextLabel)
        {
            if (Mathf.Abs(amount) <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"⚠️ {contextLabel}: valor zero ignorado.");
                return false;
            }

            if (!TryResolveDamageReceiver(requestedResource, out var receiver, out var resolvedResource))
            {
                return false;
            }

            if (!TryGetResource(resolvedResource, out var resource))
            {
                return false;
            }

            float before = resource.GetCurrentValue();
            var attackerId = _actor?.ActorId ?? gameObject.name;
            var ctx = new DamageContext(attackerId, receiver.GetReceiverId(), amount, resolvedResource);

            receiver.ReceiveDamage(ctx);

            float after = resource.GetCurrentValue();
            float delta = after - before;

            DebugUtility.Log<EntityDebugUtility>($"🧪 {contextLabel}: {resolvedResource} {before:F1} → {after:F1} (Δ={delta:+0.0;-0.0;0.0})");

            if (resolvedResource != requestedResource)
            {
                DebugUtility.LogWarning<EntityDebugUtility>(
                    $"⚠️ Receiver direcionado para {resolvedResource}, diferente do recurso solicitado {requestedResource}. Ajuste a configuração se necessário.");
            }

            return true;
        }

        private bool TryResolveDamageReceiver(ResourceType requestedResource, out IDamageReceiver receiver, out ResourceType resolvedResource)
        {
            var receivers = GetComponents<IDamageReceiver>();
            if (receivers == null || receivers.Length == 0)
            {
                DebugUtility.LogError<EntityDebugUtility>("❌ Nenhum IDamageReceiver encontrado no ator para executar o pipeline de dano.");
                receiver = null;
                resolvedResource = requestedResource;
                return false;
            }

            foreach (var candidate in receivers)
            {
                if (candidate is DamageReceiver typed && TryGetDamageReceiverTarget(typed, out var target) && target == requestedResource)
                {
                    receiver = candidate;
                    resolvedResource = target;
                    return true;
                }
            }

            receiver = receivers[0];
            if (receiver is DamageReceiver defaultReceiver && TryGetDamageReceiverTarget(defaultReceiver, out var fallbackTarget))
            {
                resolvedResource = fallbackTarget;
            }
            else
            {
                resolvedResource = requestedResource;
            }

            if (resolvedResource != requestedResource)
            {
                DebugUtility.LogWarning<EntityDebugUtility>(
                    $"⚠️ Usando o primeiro DamageReceiver disponível configurado para {resolvedResource}. Selecione um recurso compatível ou adicione um receiver dedicado.");
            }

            return true;
        }

        private static bool TryGetDamageReceiverTarget(DamageReceiver receiver, out ResourceType resourceType)
        {
            resourceType = ResourceType.Health;
            if (receiver == null || DamageReceiverTargetField == null)
            {
                return false;
            }

            try
            {
                resourceType = (ResourceType)DamageReceiverTargetField.GetValue(receiver);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetResource(ResourceType resourceType, out IResourceValue resource)
        {
            resource = null;
            if (!EnsureResourceSystem())
            {
                return false;
            }

            resource = _resourceSystem.Get(resourceType);
            if (resource != null)
            {
                return true;
            }

            DebugUtility.LogError<EntityDebugUtility>($"❌ Resource {resourceType} não encontrado para {_actor?.ActorId}.");
            return false;
        }

        private bool TryGetResourceMetrics(ResourceType resourceType, out float current, out float max)
        {
            current = 0f;
            max = 0f;

            if (!TryGetResource(resourceType, out var resource))
            {
                return false;
            }

            current = resource.GetCurrentValue();
            max = resource.GetMaxValue();
            return true;
        }

        private void OnResourceUpdateEvent(ResourceUpdateEvent evt)
        {
            if (evt.ActorId != _actor.ActorId) return;
            _resourceUpdateEventsSeen++;

            if (verboseEvents)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"🔔 ResourceUpdateEvent observed for {evt.ActorId}.{evt.ResourceType} -> {evt.NewValue?.GetCurrentValue():F1}");
            }
        }

        private void OnCanvasBindRequest(ResourceEventHub.CanvasBindRequest req)
        {
            if (req.actorId != _actor.ActorId) return;
            _canvasBindRequestsSeen++;

            if (verboseEvents)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"🔗 CanvasBindRequest observed for {req.actorId}.{req.resourceType} -> {req.targetCanvasId}");
            }
        }

        private void OnActorRegistered(ResourceEventHub.ActorRegisteredEvent evt)
        {
            if (evt.actorId != _actor.ActorId) return;
            _actorRegisteredEventSeen = true;
            if (verboseEvents) DebugUtility.Log<EntityDebugUtility>($"➕ ActorRegistered observed for {evt.actorId}");
        }

        private void ReportSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== 📋 TEST SUMMARY for {_actor?.ActorId} ===");
            sb.AppendLine($"Mode: {testMode}");
            sb.AppendLine($"ResourceSystemResolved: {_resourceSystemResolved}");
            sb.AppendLine($"ResourceUpdateEventsSeen: {_resourceUpdateEventsSeen}");
            sb.AppendLine($"CanvasBindRequestsSeen: {_canvasBindRequestsSeen}");
            sb.AppendLine($"ActorRegisteredEventSeen: {_actorRegisteredEventSeen}");

            bool suspicious = false;
            if (_canvasBindRequestsSeen > 0 && _resourceUpdateEventsSeen == 0)
            {
                suspicious = true;
                sb.AppendLine("⚠️ Heuristic: Canvas binds observed but no resource updates - investigate who published those binds.");
            }

            if (!suspicious)
                sb.AppendLine("✅ Heuristic: No suspicious forced-bind pattern detected.");

            sb.AppendLine("=== END SUMMARY ===");
            DebugUtility.LogVerbose<EntityDebugUtility>(sb.ToString());
        }

        [ContextMenu(DiagnosticsMenuRoot + "Quick Status")]
        public void QuickStatus()
        {
            _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);

            if (_resourceSystem == null)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("❌ ResourceSystem missing");
                return;
            }

            var health = _resourceSystem.Get(ResourceType.Health);
            DebugUtility.Log<EntityDebugUtility>($"📋 Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Re-resolve ResourceSystem")]
        public void ReresolveResourceSystem()
        {
            _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            DebugUtility.Log<EntityDebugUtility>($"Re-resolved ResourceSystem for {_actor.ActorId}: {_resourceSystem != null}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Debug Orchestrator Access")]
        public void DebugOrchestratorAccess()
        {
            DebugUtility.Log<EntityDebugUtility>($"📋 ORCHESTRATOR ACCESS DEBUG for {_actor.ActorId}");
            DebugUtility.Log<EntityDebugUtility>($"- Orchestrator: {_orchestrator != null}");
            DebugUtility.Log<EntityDebugUtility>($"- Local ResourceSystem: {_resourceSystem != null}");

            if (_orchestrator != null)
            {
                bool isRegistered = _orchestrator.IsActorRegistered(_actor.ActorId);
                DebugUtility.Log<EntityDebugUtility>($"- Actor Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    DebugUtility.Log<EntityDebugUtility>($"- ResourceSystem from Orchestrator: {orchestratorSystem != null}");

                    if (orchestratorSystem != null)
                    {
                        var health = orchestratorSystem.Get(ResourceType.Health);
                        DebugUtility.Log<EntityDebugUtility>($"- Health from Orchestrator: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
                    }
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "Injectable Bridge Status")]
        public void DebugBridgeStatus()
        {
            if (!TryGetComponentForDebug(out InjectableEntityResourceBridge bridge, "Debug Bridge Status"))
            {
                return;
            }

            LogInjectableBridgeStatus(bridge);
        }

        private void LogInjectableBridgeStatus(InjectableEntityResourceBridge bridge)
        {
            var actorId = bridge.GetObjectId();
            var service = bridge.GetResourceSystem();

            DebugUtility.Log<EntityDebugUtility>($"🔧 ENTITY BRIDGE STATUS: {actorId}");
            DebugUtility.Log<EntityDebugUtility>($"- Actor: {actorId}");
            DebugUtility.Log<EntityDebugUtility>($"- Service: {service != null}");
            DebugUtility.Log<EntityDebugUtility>($"- Injection: {bridge.InjectionState}");

            if (service != null)
            {
                var health = service.Get(ResourceType.Health);
                DebugUtility.Log<EntityDebugUtility>($"- Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }

            if (DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator))
            {
                bool isRegistered = orchestrator.IsActorRegistered(actorId);
                DebugUtility.Log<EntityDebugUtility>($"- Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorService = orchestrator.GetActorResourceSystem(actorId);
                    DebugUtility.Log<EntityDebugUtility>($"- Orchestrator Service: {orchestratorService != null}");
                }
            }

            bool hasInDm = DependencyManager.Instance.TryGetForObject(actorId, out ResourceSystem dmService);
            DebugUtility.Log<EntityDebugUtility>($"- In DependencyManager: {hasInDm}, Service: {dmService != null}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Print Resources")]
        public void DebugPrintResources()
        {
            if (!EnsureResourceSystem())
            {
                return;
            }

            IReadOnlyDictionary<ResourceType, IResourceValue> all = _resourceSystem.GetAll();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📊 RESOURCES FOR ACTOR: {_actor?.ActorId}");
            sb.AppendLine($"Total Resources: {all.Count}");
            sb.AppendLine("────────────────────────");

            foreach (KeyValuePair<ResourceType, IResourceValue> kv in all)
            {
                var resource = kv.Value;
                var instanceConfig = _resourceSystem.GetInstanceConfig(kv.Key);
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

            DebugUtility.Log<EntityDebugUtility>(sb.ToString());
        }

        [ContextMenu(BridgesMenuRoot + "Resource Bridge Status")]
        public void DebugResourceBridgeStatus()
        {
            var bridges = GetComponents<ResourceBridgeBase>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Nenhum ResourceBridgeBase encontrado para depurar.");
                return;
            }

            foreach (var bridge in bridges)
            {
                LogResourceBridgeStatus(bridge);
            }
        }

        private void LogResourceBridgeStatus(ResourceBridgeBase bridge)
        {
            string actorId = bridge.Actor?.ActorId ?? "null";
            bool orchestratorFound = DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator);
            bool actorRegistered = orchestratorFound && orchestrator.IsActorRegistered(actorId);

            DebugUtility.LogWarning<EntityDebugUtility>(
                $"🔧 BRIDGE STATUS - {bridge.GetType().Name}:\n" +
                $" - Actor: {actorId}\n" +
                $" - Orchestrator: {orchestratorFound}\n" +
                $" - Actor Registrado: {actorRegistered}\n" +
                $" - ResourceSystem: {bridge.GetResourceSystem() != null}\n" +
                $" - DependencyManager Ready: {DependencyManager.Instance}");

            if (orchestratorFound)
            {
                IReadOnlyCollection<string> actorIds = orchestrator.GetRegisteredActorIds();
                DebugUtility.LogWarning<EntityDebugUtility>($"📋 Atores registrados: {string.Join(", ", actorIds)}");
            }

            bool inDependencyManager = DependencyManager.Instance.TryGetForObject(actorId, out ResourceSystem dmSystem);
            DebugUtility.LogWarning<EntityDebugUtility>($"- In DependencyManager: {inDependencyManager}, Service: {dmSystem != null}");
        }

        [ContextMenu(BridgesMenuRoot + "Active Links")]
        public void DebugActiveLinks()
        {
            var bridges = GetComponents<ResourceLinkBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Serviço de links não disponível ou não inicializado");
                return;
            }

            foreach (var bridge in bridges)
            {
                var actorId = bridge.Actor?.ActorId;
                var resourceLinks = bridge.GetAllLinks();
                if (resourceLinks == null || actorId == null)
                {
                    DebugUtility.LogWarning<EntityDebugUtility>("Serviço de links não disponível ou não inicializado");
                    continue;
                }

                DebugUtility.LogWarning<EntityDebugUtility>($"🔗 Active resource links for {actorId}:");
                foreach (var linkConfig in resourceLinks)
                {
                    if (linkConfig == null) continue;
                    bool isActive = bridge.HasLink(linkConfig.sourceResource);
                    DebugUtility.LogWarning<EntityDebugUtility>($"  {linkConfig.sourceResource} -> {linkConfig.targetResource}: {(isActive ? "✅ ACTIVE" : "❌ INACTIVE")}");
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "Force Re-register Links")]
        public void ForceReregisterLinks()
        {
            var bridges = GetComponents<ResourceLinkBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Serviço de links não disponível ou não inicializado");
                return;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out IResourceLinkService linkService))
            {
                DebugUtility.LogWarning<EntityDebugUtility>("IResourceLinkService não encontrado no DependencyManager");
                return;
            }

            foreach (var bridge in bridges)
            {
                var actorId = bridge.Actor?.ActorId;
                if (actorId == null)
                {
                    DebugUtility.LogWarning<EntityDebugUtility>("Serviço de links não disponível ou não inicializado");
                    continue;
                }

                var resourceLinks = bridge.GetAllLinks();
                linkService.UnregisterAllLinks(actorId);

                foreach (var linkConfig in resourceLinks)
                {
                    if (linkConfig != null)
                    {
                        bridge.AddLink(linkConfig);
                    }
                }

                DebugUtility.LogWarning<EntityDebugUtility>($"🔄 Links re-registrados com sucesso para {actorId}");
            }
        }

        [ContextMenu(BridgesMenuRoot + "Threshold Status")]
        public void DebugThresholdStatus()
        {
            var bridges = GetComponents<ResourceThresholdBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("ResourceSystem não disponível ou não inicializado");
                return;
            }

            foreach (var bridge in bridges)
            {
                var resourceSystem = bridge.GetResourceSystem();
                if (resourceSystem != null && bridge.isInitialized)
                {
                    DebugUtility.LogWarning<EntityDebugUtility>($"📊 Threshold Service Status:");
                    DebugUtility.LogWarning<EntityDebugUtility>($" - Resources: {resourceSystem.GetAll().Count}");

                    foreach (var (resourceType, _) in resourceSystem.GetAll())
                    {
                        var config = resourceSystem.GetInstanceConfig(resourceType);
                        if (config?.thresholdConfig != null && config.thresholdConfig.thresholds.Length > 0)
                        {
                            DebugUtility.LogWarning<EntityDebugUtility>($"   - {resourceType}: {config.thresholdConfig.thresholds.Length} thresholds");
                        }
                    }
                }
                else
                {
                    DebugUtility.LogWarning<EntityDebugUtility>("ResourceSystem não disponível ou não inicializado");
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "AutoFlow Status")]
        public void DebugAutoFlowStatus()
        {
            var bridges = GetComponents<ResourceAutoFlowBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("ResourceSystem não disponível ou não inicializado");
                return;
            }

            foreach (var bridge in bridges)
            {
                var resourceSystem = bridge.GetResourceSystem();
                if (resourceSystem != null && bridge.isInitialized)
                {
                    int autoFlowCount = CountAutoFlowResources(resourceSystem);
                    DebugUtility.LogWarning<EntityDebugUtility>($"📊 Recursos com AutoFlow: {autoFlowCount}");

                    if (autoFlowCount > 0)
                    {
                        DebugUtility.LogWarning<EntityDebugUtility>("🔧 Recursos com AutoFlow configurado:");
                        foreach (var (resourceType, _) in resourceSystem.GetAll())
                        {
                            var inst = resourceSystem.GetInstanceConfig(resourceType);
                            if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                            {
                                var cfg = inst.autoFlowConfig;
                                DebugUtility.LogWarning<EntityDebugUtility>($"   - {resourceType}: " +
                                    $"Fill: {cfg.autoFill}, " +
                                    $"Drain: {cfg.autoDrain}, " +
                                    $"Interval: {cfg.tickInterval}s, " +
                                    $"Amount: {cfg.amountPerTick}" +
                                    $"{(cfg.usePercentage ? "%" : "")}");
                            }
                        }
                    }
                }
                else
                {
                    DebugUtility.LogWarning<EntityDebugUtility>("ResourceSystem não disponível ou não inicializado");
                }
            }
        }

        private int CountAutoFlowResources(ResourceSystem resourceSystem)
        {
            int count = 0;
            if (resourceSystem == null) return count;

            foreach (var (resourceType, _) in resourceSystem.GetAll())
            {
                var inst = resourceSystem.GetInstanceConfig(resourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Recupera um componente do ator atual e gera log amigável quando o componente não está presente.
        /// </summary>
        private bool TryGetComponentForDebug<T>(out T component, string context) where T : Component
        {
            component = GetComponent<T>();
            if (component != null)
            {
                return true;
            }

            DebugUtility.LogWarning<EntityDebugUtility>($"⚠️ {context}: componente {typeof(T).Name} não encontrado em {gameObject.name}.");
            return false;
        }

        private bool EnsureResourceSystem()
        {
            if (_resourceSystem != null) return true;

            _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            if (_resourceSystem != null) return true;

            DebugUtility.LogError<EntityDebugUtility>("❌ ResourceSystem is null - cannot continue the requested operation.");
            return false;
        }

    }
}
