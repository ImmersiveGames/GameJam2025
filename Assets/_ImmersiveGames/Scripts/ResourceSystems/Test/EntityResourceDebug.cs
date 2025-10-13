using System.Collections;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceDebug : MonoBehaviour, IInjectableComponent
    {
        public enum TestMode { Passive, Active, Hybrid }

        [Header("Test Settings")]
        [SerializeField] private bool autoTestOnReady;
        [SerializeField] private TestMode testMode = TestMode.Hybrid;
        [SerializeField] private float testDamage = 10f;
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
        public string GetObjectId() => _actor?.ActorId;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
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

        [ContextMenu("🎯 Run Test Routine")]
        public void RunTestRoutine() => StartCoroutine(DelayedTestRoutine());

        private IEnumerator DelayedTestRoutine()
        {
            Debug.Log($"[EntityResourceDebug] 🔄 Starting delayed test for {_actor?.ActorId} (mode={testMode})");

            yield return new WaitForSeconds(initializationDelay);

            yield return StartCoroutine(ResolveResourceSystemWithRetry(overallTimeout));

            if (_resourceSystem == null)
            {
                Debug.LogError($"[EntityResourceDebug] ❌ Failed to resolve ResourceSystem for {_actor?.ActorId} within timeout ({overallTimeout}s)");
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
                    Debug.Log($"[EntityResourceDebug] ✅ ResourceSystem resolved for {_actor.ActorId}");
                    yield break;
                }

                yield return new WaitForSeconds(0.05f);
            }
            _resourceSystemResolved = false;
        }

        private IEnumerator PassiveObservationRoutine(float observationTime)
        {
            Debug.Log($"[EntityResourceDebug] 🕵️ Passive observation for {_actor.ActorId} ({observationTime}s)");
            _resourceUpdateEventsSeen = 0;
            _canvasBindRequestsSeen = 0;

            float start = Time.time;
            while (Time.time - start < observationTime)
            {
                yield return null;
            }

            Debug.Log($"[EntityResourceDebug] 🕵️ Passive observation finished. ResourceUpdates={_resourceUpdateEventsSeen}, CanvasBindRequests={_canvasBindRequestsSeen}");
        }

        private IEnumerator ActiveTestRoutine()
        {
            Debug.Log($"[EntityResourceDebug] 🔨 Active test for {_actor.ActorId}");
            yield return LogState("INITIAL");

            if (_resourceSystem == null)
            {
                Debug.LogError("[EntityResourceDebug] ❌ ResourceSystem is null - aborting active test");
                yield break;
            }

            ApplyDamage(testDamage);
            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER FIRST DAMAGE");

            ApplyDamage(testDamage);
            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER SECOND DAMAGE");
        }

        private IEnumerator HybridRoutine()
        {
            float passiveTime = Mathf.Min(1.0f, overallTimeout * 0.4f);
            float remaining = overallTimeout - passiveTime;

            Debug.Log($"[EntityResourceDebug] Hybrid: passive {passiveTime}s then active (if needed)");
            yield return StartCoroutine(PassiveObservationRoutine(passiveTime));

            if (_resourceUpdateEventsSeen > 0)
            {
                Debug.Log("[EntityResourceDebug] 🔁 Observed natural resource updates — skipping active steps");
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

            Debug.Log(sb.ToString());
            yield return null;
        }

        private void ApplyDamage(float amount)
        {
            if (_resourceSystem == null)
            {
                _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
                if (_resourceSystem == null)
                {
                    Debug.LogError("[EntityResourceDebug] ❌ Still cannot access ResourceSystem after retry! Aborting damage application.");
                    return;
                }
            }

            var health = _resourceSystem.Get(ResourceType.Health);
            if (health == null)
            {
                Debug.LogError("[EntityResourceDebug] ❌ Health resource not found!");
                return;
            }

            float before = health.GetCurrentValue();
            _resourceSystem.Modify(ResourceType.Health, -amount);
            float after = health.GetCurrentValue();
            Debug.Log($"💥 Damage Applied: {before:F1} → {after:F1}");
        }

        private void OnResourceUpdateEvent(ResourceUpdateEvent evt)
        {
            if (evt.ActorId != _actor.ActorId) return;
            _resourceUpdateEventsSeen++;

            if (verboseEvents)
            {
                Debug.Log($"[EntityResourceDebug] 🔔 ResourceUpdateEvent observed for {evt.ActorId}.{evt.ResourceType} -> {evt.NewValue?.GetCurrentValue():F1}");
            }
        }

        private void OnCanvasBindRequest(ResourceEventHub.CanvasBindRequest req)
        {
            if (req.actorId != _actor.ActorId) return;
            _canvasBindRequestsSeen++;

            if (verboseEvents)
            {
                Debug.Log($"[EntityResourceDebug] 🔗 CanvasBindRequest observed for {req.actorId}.{req.resourceType} -> {req.targetCanvasId}");
            }
        }

        private void OnActorRegistered(ResourceEventHub.ActorRegisteredEvent evt)
        {
            if (evt.actorId != _actor.ActorId) return;
            _actorRegisteredEventSeen = true;
            if (verboseEvents) Debug.Log($"[EntityResourceDebug] ➕ ActorRegistered observed for {evt.actorId}");
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
            Debug.Log(sb.ToString());
        }

        [ContextMenu("🔍 Quick Status")]
        public void QuickStatus()
        {
            _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);

            if (_resourceSystem == null)
            {
                Debug.Log("❌ ResourceSystem missing");
                return;
            }

            var health = _resourceSystem.Get(ResourceType.Health);
            Debug.Log($"📋 Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
        }

        [ContextMenu("🔄 Re-resolve ResourceSystem")]
        public void ReresolveResourceSystem()
        {
            _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
            Debug.Log($"[EntityResourceDebug] Re-resolved ResourceSystem for {_actor.ActorId}: {_resourceSystem != null}");
        }

        [ContextMenu("📋 Debug Orchestrator Access")]
        public void DebugOrchestratorAccess()
        {
            Debug.Log($"[EntityResourceDebug] 📋 ORCHESTRATOR ACCESS DEBUG for {_actor.ActorId}");
            Debug.Log($"- Orchestrator: {_orchestrator != null}");
            Debug.Log($"- Local ResourceSystem: {_resourceSystem != null}");

            if (_orchestrator != null)
            {
                bool isRegistered = _orchestrator.IsActorRegistered(_actor.ActorId);
                Debug.Log($"- Actor Registered in Orchestrator: {isRegistered}");

                if (isRegistered)
                {
                    var orchestratorSystem = _orchestrator.GetActorResourceSystem(_actor.ActorId);
                    Debug.Log($"- ResourceSystem from Orchestrator: {orchestratorSystem != null}");

                    if (orchestratorSystem != null)
                    {
                        var health = orchestratorSystem.Get(ResourceType.Health);
                        Debug.Log($"- Health from Orchestrator: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
                    }
                }
            }
        }
    }
}