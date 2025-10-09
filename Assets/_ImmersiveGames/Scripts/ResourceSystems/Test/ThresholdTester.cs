using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    public class ThresholdTester : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType = ResourceType.Health;
        [SerializeField] private float testValue = 50f;
        
        private EventBinding<ResourceThresholdEvent> _binding;
        private IActorResourceOrchestrator _orchestrator;

        private void Start()
        {
            _binding = new EventBinding<ResourceThresholdEvent>(OnThresholdCrossed);
            EventBus<ResourceThresholdEvent>.Register(_binding);
            
            DependencyManager.Instance.TryGetGlobal(out _orchestrator);
            
            DebugUtility.LogVerbose<ThresholdTester>($"✅ Testador de thresholds inicializado");
        }

        private void OnThresholdCrossed(ResourceThresholdEvent evt)
        {
            DebugUtility.LogVerbose<ThresholdTester>($"🎯 EVENTO CAPTURADO:\n" +
                     $" - Actor: {evt.ActorId}\n" +
                     $" - Recurso: {evt.ResourceType}\n" +
                     $" - Threshold: {evt.Threshold:F2}\n" +
                     $" - Direção: {(evt.IsAscending ? "↑ SUBINDO" : "↓ DESCENDO")}\n" +
                     $" - Atual: {evt.CurrentPercentage:P1}");
        }

        [ContextMenu("Testar Modificação de Recurso")]
        private void TestResourceModification()
        {
            if (_orchestrator == null)
            {
                DebugUtility.LogError<ThresholdTester>($"Orchestrator não encontrado");
                return;
            }

            // Encontrar um ator para testar
            IReadOnlyCollection<string> actorIds = _orchestrator.GetRegisteredActorIds();
            if (actorIds.Count == 0)
            {
                DebugUtility.LogWarning<ThresholdTester>($"Nenhum ator registrado");
                return;
            }

            string testActorId = "";
            foreach (string actorId in actorIds)
            {
                var resourceSystem = _orchestrator.GetActorResourceSystem(actorId);
                if (resourceSystem?.Get(resourceType) != null)
                {
                    testActorId = actorId;
                    break;
                }
            }

            if (string.IsNullOrEmpty(testActorId))
            {
                DebugUtility.LogWarning<ThresholdTester>($"Nenhum ator com recurso {resourceType} encontrado");
                return;
            }

            var testSystem = _orchestrator.GetActorResourceSystem(testActorId);
            testSystem.Modify(resourceType, testValue);
            
            DebugUtility.LogVerbose<ThresholdTester>($"Modificado {resourceType} em {testValue} para {testActorId}");
        }

        private void OnDestroy()
        {
            if (_binding != null)
            {
                EventBus<ResourceThresholdEvent>.Unregister(_binding);
            }
        }
    }
}