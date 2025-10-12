using System.Collections;
using System.Text;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceDebug : MonoBehaviour, IInjectableComponent
    {
        [Header("Test Settings")]
        [SerializeField] private bool autoTestOnReady = true;
        [SerializeField] private float testDamage = 10f;
        [SerializeField] private float initializationDelay = 0.5f; // CORREÇÃO: Delay para garantir registro

        [Inject] private IActorResourceOrchestrator _orchestrator;
        private IActor _actor;
        private ResourceSystem _resourceSystem;
        private bool _resourceSystemResolved = false;

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
            
            // CORREÇÃO: Não tentar obter ResourceSystem imediatamente
            // Aguardar o InjectableEntityResourceBridge registrar primeiro
            if (autoTestOnReady)
                StartCoroutine(DelayedTestRoutine());
        }

        [ContextMenu("🎯 Run Test Routine")]
        public void RunTestRoutine() => StartCoroutine(DelayedTestRoutine());

        // CORREÇÃO: Nova rotina com delay para garantir que o ResourceSystem está registrado
        private IEnumerator DelayedTestRoutine()
        {
            Debug.Log($"[EntityResourceDebug] 🔄 Starting delayed test for {_actor.ActorId}");
            
            // Tentar resolver o ResourceSystem com retry
            yield return StartCoroutine(ResolveResourceSystemWithRetry());
            
            if (_resourceSystem == null)
            {
                Debug.LogError($"[EntityResourceDebug] ❌ Failed to resolve ResourceSystem for {_actor.ActorId} after retry");
                yield break;
            }

            yield return StartCoroutine(TestRoutine());
        }

        // CORREÇÃO: Método para resolver ResourceSystem com múltiplas tentativas
        private IEnumerator ResolveResourceSystemWithRetry()
        {
            int maxAttempts = 10;
            float delayBetweenAttempts = 0.1f;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
                
                if (_resourceSystem != null)
                {
                    _resourceSystemResolved = true;
                    Debug.Log($"[EntityResourceDebug] ✅ ResourceSystem resolved for {_actor.ActorId} on attempt {attempt}");
                    yield break;
                }

                Debug.Log($"[EntityResourceDebug] ⏳ Attempt {attempt}/{maxAttempts} - ResourceSystem not yet available for {_actor.ActorId}");
                
                if (attempt < maxAttempts)
                    yield return new WaitForSeconds(delayBetweenAttempts);
            }

            Debug.LogError($"[EntityResourceDebug] ❌ Failed to resolve ResourceSystem for {_actor.ActorId} after {maxAttempts} attempts");
        }

        private IEnumerator TestRoutine()
        {
            Debug.Log($"=== 🎯 ENTITY RESOURCE DEBUG ({_actor?.ActorId}) ===");
            yield return LogState("INITIAL");

            if (_resourceSystem == null)
            {
                Debug.LogError("❌ ResourceSystem is null - cannot proceed with test");
                yield break;
            }

            ApplyDamage(testDamage);
            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER FIRST DAMAGE");

            ApplyDamage(testDamage);
            yield return new WaitForSeconds(0.3f);
            yield return LogState("AFTER SECOND DAMAGE");

            Debug.Log($"=== ✅ TEST COMPLETE ({_actor?.ActorId}) ===");
        }

        private IEnumerator LogState(string phase)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📊 {phase}");
            sb.AppendLine($"Actor: {_actor?.ActorId}");
            sb.AppendLine($"ResourceSystem: {_resourceSystem != null}");
            sb.AppendLine($"Orchestrator: {_orchestrator != null}");
            sb.AppendLine($"ResourceSystem Resolved: {_resourceSystemResolved}");

            if (_resourceSystem != null)
            {
                foreach (var pair in _resourceSystem.GetAll())
                {
                    var value = pair.Value.GetCurrentValue();
                    var max = pair.Value.GetMaxValue();
                    sb.AppendLine($"  {pair.Key}: {value:F1}/{max:F1} ({(value / max):P1})");
                }
            }
            else
            {
                // CORREÇÃO: Tentar obter novamente via orchestrator
                var tempSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
                sb.AppendLine($"  Re-check via Orchestrator: {tempSystem != null}");
            }

            Debug.Log(sb.ToString());
            yield return null;
        }

        private void ApplyDamage(float amount)
        {
            if (_resourceSystem == null)
            {
                Debug.LogError("❌ ResourceSystem is null!");
                
                // CORREÇÃO: Tentar obter novamente
                _resourceSystem = _orchestrator?.GetActorResourceSystem(_actor.ActorId);
                if (_resourceSystem == null)
                {
                    Debug.LogError("❌ Still cannot access ResourceSystem after retry!");
                    return;
                }
            }

            var health = _resourceSystem.Get(ResourceType.Health);
            if (health == null)
            {
                Debug.LogError("❌ Health resource not found!");
                return;
            }

            float before = health.GetCurrentValue();
            _resourceSystem.Modify(ResourceType.Health, -amount);
            float after = health.GetCurrentValue();
            Debug.Log($"💥 Damage Applied: {before:F1} → {after:F1}");
        }

        [ContextMenu("🔍 Quick Status")]
        public void QuickStatus()
        {
            // CORREÇÃO: Sempre tentar obter o ResourceSystem atualizado
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

        // CORREÇÃO: Novo método para debug detalhado
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