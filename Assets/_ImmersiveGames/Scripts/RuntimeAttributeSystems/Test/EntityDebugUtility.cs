using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Bind;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Test
{
    
    public class EntityDebugUtility : MonoBehaviour, IInjectableComponent
    {
        private const string TestsMenuRoot = "Tests/";
        private const string ResourcesMenuRoot = "Resources/";
        private const string DiagnosticsMenuRoot = "Diagnostics/";
        private const string BridgesMenuRoot = "Bridges/";
        private const float MinimalReviveAmount = 1f;

        private static readonly FieldInfo DamageReceiverTargetField =
            typeof(DamageReceiver).GetField("targetRuntimeAttribute", BindingFlags.Instance | BindingFlags.NonPublic);

        public enum TestMode { Passive, Active, Hybrid }

        [Header("Test Settings")]
        [SerializeField] private bool autoTestOnReady;
        [SerializeField] private TestMode testMode = TestMode.Hybrid;
        [SerializeField] private float testDamage = 10f;
        [SerializeField] private RuntimeAttributeType damageRuntimeAttributeType = RuntimeAttributeType.Health; // Novo: Escolha o tipo de recurso para os testes
        [SerializeField] private float initializationDelay = 0.5f;
        [SerializeField] private float overallTimeout = 5f;
        [SerializeField] private bool verboseEvents = true;

        [Inject] private IActorRuntimeAttributeOrchestrator _orchestrator;
        private IActor _actor;
        private RuntimeAttributeContext _runtimeAttributeContext;
        private bool _resourceSystemResolved;

        private int _resourceUpdateEventsSeen;
        private int _canvasBindRequestsSeen;
        private bool _actorRegisteredEventSeen;

        private EventBinding<RuntimeAttributeUpdateEvent> _resourceUpdateBinding;
        private EventBinding<RuntimeAttributeEventHub.CanvasBindRequest> _canvasBindRequestBinding;
        private EventBinding<RuntimeAttributeEventHub.ActorRegisteredEvent> _actorRegisteredBinding;

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
            RuntimeAttributeInitializationManager.Instance.RegisterForInjection(this);
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;

            _resourceUpdateBinding = new EventBinding<RuntimeAttributeUpdateEvent>(OnResourceUpdateEvent);
            EventBus<RuntimeAttributeUpdateEvent>.Register(_resourceUpdateBinding);

            _canvasBindRequestBinding = new EventBinding<RuntimeAttributeEventHub.CanvasBindRequest>(OnCanvasBindRequest);
            EventBus<RuntimeAttributeEventHub.CanvasBindRequest>.Register(_canvasBindRequestBinding);

            _actorRegisteredBinding = new EventBinding<RuntimeAttributeEventHub.ActorRegisteredEvent>(OnActorRegistered);
            EventBus<RuntimeAttributeEventHub.ActorRegisteredEvent>.Register(_actorRegisteredBinding);

            if (autoTestOnReady)
                StartCoroutine(DelayedTestRoutine());
        }

        private void OnDestroy()
        {
            if (_resourceUpdateBinding != null) EventBus<RuntimeAttributeUpdateEvent>.Unregister(_resourceUpdateBinding);
            if (_canvasBindRequestBinding != null) EventBus<RuntimeAttributeEventHub.CanvasBindRequest>.Unregister(_canvasBindRequestBinding);
            if (_actorRegisteredBinding != null) EventBus<RuntimeAttributeEventHub.ActorRegisteredEvent>.Unregister(_actorRegisteredBinding);
        }

        [ContextMenu(TestsMenuRoot + "Run Test Routine")]
        public void RunTestRoutine() => StartCoroutine(DelayedTestRoutine());

        private IEnumerator DelayedTestRoutine()
        {
            DebugUtility.LogVerbose<EntityDebugUtility>($"🔄 Starting delayed test for {_actor?.ActorId} (mode={testMode})");

            yield return new WaitForSeconds(initializationDelay);

            yield return StartCoroutine(ResolveResourceSystemWithRetry(overallTimeout));

            if (_runtimeAttributeContext == null)
            {
                DebugUtility.LogError<EntityDebugUtility>($"❌ Failed to resolve RuntimeAttributeContext for {_actor?.ActorId} within timeout ({overallTimeout}s)");
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
                _runtimeAttributeContext = _orchestrator?.GetActorResourceSystem(_actor.ActorId);

                if (_runtimeAttributeContext != null)
                {
                    _resourceSystemResolved = true;
                    DebugUtility.LogVerbose<EntityDebugUtility>($"✅ RuntimeAttributeContext resolved for {_actor.ActorId}");
                    yield break;
                }

                yield return new WaitForSeconds(0.05f);
            }
            _resourceSystemResolved = false;
        }

        private IEnumerator PassiveObservationRoutine(float observationTime)
        {
            DebugUtility.LogVerbose<EntityDebugUtility>($"🕵️ Passive observation for {_actor.ActorId} ({observationTime}s)");
            _resourceUpdateEventsSeen = 0;
            _canvasBindRequestsSeen = 0;

            float start = Time.time;
            while (Time.time - start < observationTime)
            {
                yield return null;
            }

            DebugUtility.LogVerbose<EntityDebugUtility>($"🕵️ Passive observation finished. ResourceUpdates={_resourceUpdateEventsSeen}, CanvasBindRequests={_canvasBindRequestsSeen}");
        }

        private IEnumerator ActiveTestRoutine()
        {
            DebugUtility.LogVerbose<EntityDebugUtility>($"🔨 Active test for {_actor.ActorId}");
            yield return LogState("INITIAL");

            if (!ExecuteDamagePipeline(testDamage, damageRuntimeAttributeType, "Active Test - First Damage"))
            {
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER FIRST DAMAGE");

            if (!ExecuteDamagePipeline(testDamage, damageRuntimeAttributeType, "Active Test - Second Damage"))
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

            DebugUtility.LogVerbose<EntityDebugUtility>($"Hybrid: passive {passiveTime}s then active (if needed)");
            yield return StartCoroutine(PassiveObservationRoutine(passiveTime));

            if (_resourceUpdateEventsSeen > 0)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>("🔁 Observed natural runtimeAttribute updates — skipping active steps");
                yield break;
            }

            yield return StartCoroutine(ActiveTestRoutine());
        }

        private IEnumerator LogState(string phase)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 {phase}");
            sb.AppendLine($"Actor: {_actor?.ActorId}");
            sb.AppendLine($"RuntimeAttributeContext Local: {_runtimeAttributeContext != null}");
            sb.AppendLine($"Orchestrator Available: {_orchestrator != null}");
            sb.AppendLine($"RuntimeAttributeContext Resolved: {_resourceSystemResolved}");
            sb.AppendLine($"ResourceUpdateEventsSeen: {_resourceUpdateEventsSeen}");
            sb.AppendLine($"CanvasBindRequestsSeen: {_canvasBindRequestsSeen}");
            sb.AppendLine($"ActorRegisteredEventSeen: {_actorRegisteredEventSeen}");

            if (_runtimeAttributeContext != null)
            {
                foreach (var pair in _runtimeAttributeContext.GetAll())
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

            DebugUtility.LogVerbose<EntityDebugUtility>(sb.ToString());
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

            ExecuteDamagePipeline(testDamage, damageRuntimeAttributeType, "Damage Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Recover Selected Resource")]
        public void RecoverSelectedResource()
        {
            if (!TryGetResourceMetrics(damageRuntimeAttributeType, out var current, out var max))
            {
                return;
            }

            float missing = Mathf.Max(0f, max - current);
            if (missing <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"✅ {damageRuntimeAttributeType} já está cheio — nada para recuperar.");
                return;
            }

            float amount = -Mathf.Min(testDamage, missing);
            ExecuteDamagePipeline(amount, damageRuntimeAttributeType, "Recover Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Deplete Selected Resource")]
        public void DepleteSelectedResource()
        {
            if (!TryGetResourceMetrics(damageRuntimeAttributeType, out var current, out _))
            {
                return;
            }

            if (current <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"⚠️ {damageRuntimeAttributeType} já está zerado.");
                return;
            }

            ExecuteDamagePipeline(current, damageRuntimeAttributeType, "Deplete Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Restock Selected Resource")]
        public void RestockSelectedResource()
        {
            if (!TryGetResourceMetrics(damageRuntimeAttributeType, out var current, out var max))
            {
                return;
            }

            float missing = Mathf.Max(0f, max - current);
            if (missing <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"✅ {damageRuntimeAttributeType} já está no máximo.");
                return;
            }

            ExecuteDamagePipeline(-missing, damageRuntimeAttributeType, "Restock Selected Resource");
        }

        [ContextMenu(ResourcesMenuRoot + "Revive Selected Resource")]
        public void ReviveSelectedResource()
        {
            if (!TryGetResourceMetrics(damageRuntimeAttributeType, out var current, out var max))
            {
                return;
            }

            if (current > Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"✅ {damageRuntimeAttributeType} já está acima de zero — ator ativo.");
                return;
            }

            float baseAmount = Mathf.Max(testDamage, MinimalReviveAmount);
            float healAmount = baseAmount;

            if (max > Mathf.Epsilon)
            {
                float missing = Mathf.Max(0f, max - current);
                if (missing <= Mathf.Epsilon)
                {
                    DebugUtility.LogVerbose<EntityDebugUtility>($"⚠️ {damageRuntimeAttributeType} já está no valor máximo.");
                    return;
                }

                healAmount = Mathf.Min(baseAmount, missing);
            }

            ExecuteDamagePipeline(-healAmount, damageRuntimeAttributeType, "Revive Selected Resource");
        }

        private bool ExecuteDamagePipeline(float amount, RuntimeAttributeType requestedRuntimeAttribute, string contextLabel)
        {
            if (Mathf.Abs(amount) <= Mathf.Epsilon)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"⚠️ {contextLabel}: valor zero ignorado.");
                return false;
            }

            if (!TryResolveDamageReceiver(requestedRuntimeAttribute, out var receiver, out var resolvedResource))
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

            DebugUtility.LogVerbose<EntityDebugUtility>($"🧪 {contextLabel}: {resolvedResource} {before:F1} → {after:F1} (Δ={delta:+0.0;-0.0;0.0})");

            if (resolvedResource != requestedRuntimeAttribute)
            {
                DebugUtility.LogWarning<EntityDebugUtility>(
                    $"⚠️ Receiver direcionado para {resolvedResource}, diferente do recurso solicitado {requestedRuntimeAttribute}. Ajuste a configuração se necessário.");
            }

            return true;
        }

        private bool TryResolveDamageReceiver(RuntimeAttributeType requestedRuntimeAttribute, out IDamageReceiver receiver, out RuntimeAttributeType resolvedRuntimeAttribute)
        {
            var receivers = GetComponents<IDamageReceiver>();
            if (receivers == null || receivers.Length == 0)
            {
                DebugUtility.LogError<EntityDebugUtility>("❌ Nenhum IDamageReceiver encontrado no ator para executar o pipeline de dano.");
                receiver = null;
                resolvedRuntimeAttribute = requestedRuntimeAttribute;
                return false;
            }

            foreach (var candidate in receivers)
            {
                if (candidate is DamageReceiver typed && TryGetDamageReceiverTarget(typed, out var target) && target == requestedRuntimeAttribute)
                {
                    receiver = candidate;
                    resolvedRuntimeAttribute = target;
                    return true;
                }
            }

            receiver = receivers[0];
            if (receiver is DamageReceiver defaultReceiver && TryGetDamageReceiverTarget(defaultReceiver, out var fallbackTarget))
            {
                resolvedRuntimeAttribute = fallbackTarget;
            }
            else
            {
                resolvedRuntimeAttribute = requestedRuntimeAttribute;
            }

            if (resolvedRuntimeAttribute != requestedRuntimeAttribute)
            {
                DebugUtility.LogWarning<EntityDebugUtility>(
                    $"⚠️ Usando o primeiro DamageReceiver disponível configurado para {resolvedRuntimeAttribute}. Selecione um recurso compatível ou adicione um receiver dedicado.");
            }

            return true;
        }

        private static bool TryGetDamageReceiverTarget(DamageReceiver receiver, out RuntimeAttributeType runtimeAttributeType)
        {
            runtimeAttributeType = RuntimeAttributeType.Health;
            if (receiver == null || DamageReceiverTargetField == null)
            {
                return false;
            }

            try
            {
                runtimeAttributeType = (RuntimeAttributeType)DamageReceiverTargetField.GetValue(receiver);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetResource(RuntimeAttributeType runtimeAttributeType, out IRuntimeAttributeValue runtimeAttribute)
        {
            runtimeAttribute = null;
            if (!EnsureResourceSystem())
            {
                return false;
            }

            runtimeAttribute = _runtimeAttributeContext.Get(runtimeAttributeType);
            if (runtimeAttribute != null)
            {
                return true;
            }

            DebugUtility.LogError<EntityDebugUtility>($"❌ Resource {runtimeAttributeType} não encontrado para {_actor?.ActorId}.");
            return false;
        }

        private bool TryGetResourceMetrics(RuntimeAttributeType runtimeAttributeType, out float current, out float max)
        {
            current = 0f;
            max = 0f;

            if (!TryGetResource(runtimeAttributeType, out var resource))
            {
                return false;
            }

            current = resource.GetCurrentValue();
            max = resource.GetMaxValue();
            return true;
        }

        private void OnResourceUpdateEvent(RuntimeAttributeUpdateEvent evt)
        {
            if (evt.ActorId != _actor.ActorId) return;
            _resourceUpdateEventsSeen++;

            if (verboseEvents)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"🔔 RuntimeAttributeUpdateEvent observed for {evt.ActorId}.{evt.RuntimeAttributeType} -> {evt.NewValue?.GetCurrentValue():F1}");
            }
        }

        private void OnCanvasBindRequest(RuntimeAttributeEventHub.CanvasBindRequest req)
        {
            if (req.actorId != _actor.ActorId) return;
            _canvasBindRequestsSeen++;

            if (verboseEvents)
            {
                DebugUtility.LogVerbose<EntityDebugUtility>($"🔗 CanvasBindRequest observed for {req.actorId}.{req.runtimeAttributeType} -> {req.targetCanvasId}");
            }
        }

        private void OnActorRegistered(RuntimeAttributeEventHub.ActorRegisteredEvent evt)
        {
            if (evt.actorId != _actor.ActorId) return;
            _actorRegisteredEventSeen = true;
            if (verboseEvents) DebugUtility.LogVerbose<EntityDebugUtility>($"➕ ActorRegistered observed for {evt.actorId}");
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
                sb.AppendLine("⚠️ Heuristic: Canvas binds observed but no runtimeAttribute updates - investigate who published those binds.");
            }

            if (!suspicious)
                sb.AppendLine("✅ Heuristic: No suspicious forced-bind pattern detected.");

            sb.AppendLine("=== END SUMMARY ===");
            DebugUtility.LogVerbose<EntityDebugUtility>(sb.ToString());
        }

        [ContextMenu(DiagnosticsMenuRoot + "Quick Status")]
        public void QuickStatus()
        {
            _runtimeAttributeContext = _orchestrator?.GetActorResourceSystem(_actor.ActorId);

            if (_runtimeAttributeContext == null)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("❌ RuntimeAttributeContext missing");
                return;
            }

            var health = _runtimeAttributeContext.Get(RuntimeAttributeType.Health);
            DebugUtility.LogVerbose<EntityDebugUtility>($"📋 Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Re-resolve RuntimeAttributeContext")]
        public void ReresolveResourceSystem()
        {
            _runtimeAttributeContext = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            DebugUtility.LogVerbose<EntityDebugUtility>($"Re-resolved RuntimeAttributeContext for {_actor.ActorId}: {_runtimeAttributeContext != null}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Debug Orchestrator Access")]
        public void DebugOrchestratorAccess()
        {
            DebugUtility.LogVerbose<EntityDebugUtility>($"📋 ORCHESTRATOR ACCESS DEBUG for {_actor.ActorId}");
            DebugUtility.LogVerbose<EntityDebugUtility>($"- Orchestrator: {_orchestrator != null}");
            DebugUtility.LogVerbose<EntityDebugUtility>($"- Local RuntimeAttributeContext: {_runtimeAttributeContext != null}");

            if (_orchestrator != null)
            {
                bool isRegistered = _orchestrator.IsActorRegistered(_actor.ActorId);
                DebugUtility.LogVerbose<EntityDebugUtility>($"- Actor Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    DebugUtility.LogVerbose<EntityDebugUtility>($"- RuntimeAttributeContext from Orchestrator: {orchestratorSystem != null}");

                    if (orchestratorSystem != null)
                    {
                        var health = orchestratorSystem.Get(RuntimeAttributeType.Health);
                        DebugUtility.LogVerbose<EntityDebugUtility>($"- Health from Orchestrator: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
                    }
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "Injectable Component Status")]
        public void DebugBridgeStatus()
        {
            if (!TryGetComponentForDebug(out RuntimeAttributeController bridge, "Debug Component Status"))
            {
                return;
            }

            LogInjectableBridgeStatus(bridge);
        }

        private void LogInjectableBridgeStatus(RuntimeAttributeController component)
        {
            var actorId = component.GetObjectId();
            var service = component.GetResourceSystem();

            DebugUtility.LogVerbose<EntityDebugUtility>($"🔧 ENTITY BRIDGE STATUS: {actorId}");
            DebugUtility.LogVerbose<EntityDebugUtility>($"- Actor: {actorId}");
            DebugUtility.LogVerbose<EntityDebugUtility>($"- Service: {service != null}");
            DebugUtility.LogVerbose<EntityDebugUtility>($"- Injection: {component.InjectionState}");

            if (service != null)
            {
                var health = service.Get(RuntimeAttributeType.Health);
                DebugUtility.LogVerbose<EntityDebugUtility>($"- Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }

            if (DependencyManager.Provider.TryGetGlobal(out IActorRuntimeAttributeOrchestrator orchestrator))
            {
                bool isRegistered = orchestrator.IsActorRegistered(actorId);
                DebugUtility.LogVerbose<EntityDebugUtility>($"- Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorService = orchestrator.GetActorResourceSystem(actorId);
                    DebugUtility.LogVerbose<EntityDebugUtility>($"- Orchestrator Service: {orchestratorService != null}");
                }
            }

            bool hasInDm = DependencyManager.Instance.TryGetForObject(actorId, out RuntimeAttributeContext dmService);
            DebugUtility.LogVerbose<EntityDebugUtility>($"- In DependencyManager: {hasInDm}, Service: {dmService != null}");
        }

        [ContextMenu(DiagnosticsMenuRoot + "Print Resources")]
        public void DebugPrintResources()
        {
            if (!EnsureResourceSystem())
            {
                return;
            }

            IReadOnlyDictionary<RuntimeAttributeType, IRuntimeAttributeValue> all = _runtimeAttributeContext.GetAll();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"📊 RESOURCES FOR ACTOR: {_actor?.ActorId}");
            sb.AppendLine($"Total Resources: {all.Count}");
            sb.AppendLine("────────────────────────");

            foreach (KeyValuePair<RuntimeAttributeType, IRuntimeAttributeValue> kv in all)
            {
                var resource = kv.Value;
                var instanceConfig = _runtimeAttributeContext.GetInstanceConfig(kv.Key);
                string canvasTarget = "Unknown";

                if (instanceConfig != null)
                {
                    canvasTarget = instanceConfig.attributeCanvasTargetMode switch
                    {
                        AttributeCanvasTargetMode.ActorSpecific => $"{_actor.ActorId}_Canvas",
                        AttributeCanvasTargetMode.Custom => instanceConfig.customCanvasId ?? "MainUI",
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

            DebugUtility.LogVerbose<EntityDebugUtility>(sb.ToString());
        }

        [ContextMenu(BridgesMenuRoot + "Resource Component Status")]
        public void DebugResourceBridgeStatus()
        {
            var bridges = GetComponents<RuntimeAttributeBridgeBase>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Nenhum RuntimeAttributeBridgeBase encontrado para depurar.");
                return;
            }

            foreach (var bridge in bridges)
            {
                LogResourceBridgeStatus(bridge);
            }
        }

        private void LogResourceBridgeStatus(RuntimeAttributeBridgeBase bridge)
        {
            string actorId = bridge.Actor?.ActorId ?? "null";
            bool orchestratorFound = DependencyManager.Provider.TryGetGlobal(out IActorRuntimeAttributeOrchestrator orchestrator);
            bool actorRegistered = orchestratorFound && orchestrator.IsActorRegistered(actorId);

            DebugUtility.LogWarning<EntityDebugUtility>(
                $"🔧 BRIDGE STATUS - {bridge.GetType().Name}:\n" +
                $" - Actor: {actorId}\n" +
                $" - Orchestrator: {orchestratorFound}\n" +
                $" - Actor Registrado: {actorRegistered}\n" +
                $" - RuntimeAttributeContext: {bridge.GetResourceSystem() != null}\n" +
                $" - DependencyManager Ready: {DependencyManager.Provider}");

            if (orchestratorFound)
            {
                IReadOnlyCollection<string> actorIds = orchestrator.GetRegisteredActorIds();
                DebugUtility.LogWarning<EntityDebugUtility>($"📋 Atores registrados: {string.Join(", ", actorIds)}");
            }

            bool inDependencyManager = DependencyManager.Provider.TryGetForObject(actorId, out RuntimeAttributeContext dmSystem);
            DebugUtility.LogWarning<EntityDebugUtility>($"- In DependencyManager: {inDependencyManager}, Service: {dmSystem != null}");
        }

        [ContextMenu(BridgesMenuRoot + "Active Links")]
        public void DebugActiveLinks()
        {
            var bridges = GetComponents<RuntimeAttributeLinkBridge>();
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

                DebugUtility.LogWarning<EntityDebugUtility>($"🔗 Active runtimeAttribute links for {actorId}:");
                foreach (var linkConfig in resourceLinks)
                {
                    if (linkConfig == null) continue;
                    bool isActive = bridge.HasLink(linkConfig.sourceRuntimeAttribute);
                    DebugUtility.LogWarning<EntityDebugUtility>($"  {linkConfig.sourceRuntimeAttribute} -> {linkConfig.targetRuntimeAttribute}: {(isActive ? "✅ ACTIVE" : "❌ INACTIVE")}");
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "Force Re-register Links")]
        public void ForceReregisterLinks()
        {
            var bridges = GetComponents<RuntimeAttributeLinkBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Serviço de links não disponível ou não inicializado");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out IRuntimeAttributeLinkService linkService))
            {
                DebugUtility.LogWarning<EntityDebugUtility>("IRuntimeAttributeLinkService não encontrado no DependencyManager");
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

        [ContextMenu(BridgesMenuRoot + "Resume AutoFlow")]
        public void ResumeAutoFlow()
        {
            var bridges = GetComponents<RuntimeAttributeAutoFlowBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Nenhum RuntimeAttributeAutoFlowBridge encontrado para retomar.");
                return;
            }

            foreach (var bridge in bridges)
            {
                bool resumed = bridge.ResumeAutoFlow();
                string actorId = bridge.Actor?.ActorId ?? bridge.name;
                DebugUtility.LogWarning<EntityDebugUtility>(
                    resumed
                        ? $"▶️ AutoFlow retomado manualmente para {actorId}."
                        : $"⏸️ AutoFlow permaneceu pausado para {actorId}. Verifique bloqueios automáticos ou pausas manuais.");
            }
        }

        [ContextMenu(BridgesMenuRoot + "Pause AutoFlow")]
        public void PauseAutoFlow()
        {
            var bridges = GetComponents<RuntimeAttributeAutoFlowBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("Nenhum RuntimeAttributeAutoFlowBridge encontrado para pausar.");
                return;
            }

            foreach (var bridge in bridges)
            {
                bool paused = bridge.PauseAutoFlow();
                string actorId = bridge.Actor?.ActorId ?? bridge.name;
                DebugUtility.LogWarning<EntityDebugUtility>(
                    paused
                        ? $"⏸️ AutoFlow pausado manualmente para {actorId}."
                        : $"⚠️ AutoFlow já estava pausado em {actorId} ou serviço indisponível.");
            }
        }

        [ContextMenu(BridgesMenuRoot + "Threshold Status")]
        public void DebugThresholdStatus()
        {
            var bridges = GetComponents<RuntimeAttributeThresholdBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("RuntimeAttributeContext não disponível ou não inicializado");
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
                    DebugUtility.LogWarning<EntityDebugUtility>("RuntimeAttributeContext não disponível ou não inicializado");
                }
            }
        }

        [ContextMenu(BridgesMenuRoot + "AutoFlow Status")]
        public void DebugAutoFlowStatus()
        {
            var bridges = GetComponents<RuntimeAttributeAutoFlowBridge>();
            if (bridges == null || bridges.Length == 0)
            {
                DebugUtility.LogWarning<EntityDebugUtility>("RuntimeAttributeContext não disponível ou não inicializado");
                return;
            }

            foreach (var bridge in bridges)
            {
                var resourceSystem = bridge.GetResourceSystem();
                if (resourceSystem != null && bridge.isInitialized)
                {
                    DebugUtility.LogWarning<EntityDebugUtility>(
                        $"🔄 AutoFlow Component '{bridge.name}': Service={(bridge.HasAutoFlowService ? "✅" : "❌")}, " +
                        $"StartPaused={bridge.StartPaused}, AutoResumeAllowed={bridge.AutoResumeAllowed}, " +
                        $"IsActive={bridge.IsAutoFlowActive}");

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
                    DebugUtility.LogWarning<EntityDebugUtility>("RuntimeAttributeContext não disponível ou não inicializado");
                }
            }
        }

        private int CountAutoFlowResources(RuntimeAttributeContext runtimeAttributeContext)
        {
            int count = 0;
            if (runtimeAttributeContext == null) return count;

            foreach (var (resourceType, _) in runtimeAttributeContext.GetAll())
            {
                var inst = runtimeAttributeContext.GetInstanceConfig(resourceType);
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
            if (_runtimeAttributeContext != null) return true;

            _runtimeAttributeContext = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            if (_runtimeAttributeContext != null) return true;

            DebugUtility.LogError<EntityDebugUtility>("❌ RuntimeAttributeContext is null - cannot continue the requested operation.");
            return false;
        }

    }
}
