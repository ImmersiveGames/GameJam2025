using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    public class ConfigFlowDebugger : MonoBehaviour
    {
        [ContextMenu("🔍 Debug Config Flow")]
        public void DebugConfigFlow()
        {
            Debug.Log("🔄 DEBUGGING CONFIG FLOW");

            // 1. Verificar todos os ResourceSystems registrados
            Debug.Log("1. 📊 REGISTERED RESOURCE SYSTEMS:");
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            var actorIds = orchestrator.GetRegisteredActorIds();
            
            foreach (var actorId in actorIds)
            {
                var resourceSystem = orchestrator.GetActorResourceSystem(actorId);
                if (resourceSystem != null)
                {
                    Debug.Log($"   - {actorId}: {resourceSystem != null}");
                    // Chamar o método de debug do ResourceSystem
                    resourceSystem.DebugInstanceConfigs();
                }
            }

            // 2. Verificar Canvas Binders e seus slots
            Debug.Log("2. 🎨 CANVAS BINDERS AND SLOTS:");
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(FindObjectsSortMode.None);
            foreach (var binder in canvasBinders)
            {
                Debug.Log($"   - Canvas: {binder.CanvasId}");
                
                // Usar reflection para acessar _actorSlots se for privado
                var field = typeof(InjectableCanvasResourceBinder).GetField("_actorSlots", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    if (field.GetValue(binder) is Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> actorSlots)
                    {
                        foreach (var actorEntry in actorSlots)
                        {
                            foreach (var slotEntry in actorEntry.Value)
                            {
                                var slot = slotEntry.Value;
                                if (slot != null)
                                {
                                    var config = slot.GetInstanceConfig();
                                    Debug.Log($"     - Slot: {actorEntry.Key}.{slotEntry.Key}");
                                    Debug.Log($"       Config: {config != null}, Style: {config?.slotStyle != null} ({config?.slotStyle?.name})");
                                }
                            }
                        }
                    }
                }
            }

            // 3. Testar resolução de config para um actor específico
            Debug.Log("3. 🧪 TEST CONFIG RESOLUTION:");
            foreach (var actorId in actorIds)
            {
                TestConfigResolutionForActor(actorId, ResourceType.Health);
            }
        }

        private void TestConfigResolutionForActor(string actorId, ResourceType resourceType)
        {
            Debug.Log($"   🧪 Testing {actorId}.{resourceType}:");
            
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            var resourceSystem = orchestrator.GetActorResourceSystem(actorId);
            
            if (resourceSystem != null)
            {
                var config = resourceSystem.GetInstanceConfig(resourceType);
                Debug.Log($"     - ResourceSystem: {resourceSystem != null}");
                Debug.Log($"     - Config: {config != null}");
                Debug.Log($"     - Style: {config?.slotStyle != null} ({config?.slotStyle?.name})");
                
                // Verificar também via DependencyManager
                ResourceSystem dmSystem;
                if (DependencyManager.Instance.TryGetForObject(actorId, out dmSystem))
                {
                    var dmConfig = dmSystem.GetInstanceConfig(resourceType);
                    Debug.Log($"     - Via DM: Config={dmConfig != null}, Style={dmConfig?.slotStyle != null}");
                }
            }
            else
            {
                Debug.Log($"     - ❌ ResourceSystem not found");
            }
        }

        [ContextMenu("🔄 Test All Canvas Binders")]
        public void TestAllCanvasBinders()
        {
            Debug.Log("🎯 TESTING ALL CANVAS BINDERS CONFIG RESOLUTION");
            
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(FindObjectsSortMode.None);
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            
            foreach (var binder in canvasBinders)
            {
                Debug.Log($"\n🔧 Testing Canvas: {binder.CanvasId}");
                
                // Usar reflection para chamar ResolveInstanceConfig
                var method = typeof(InjectableCanvasResourceBinder).GetMethod("ResolveInstanceConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    var actorIds = orchestrator.GetRegisteredActorIds();
                    foreach (var actorId in actorIds)
                    {
                        var config = method.Invoke(binder, new object[] { actorId, ResourceType.Health }) as ResourceInstanceConfig;
                        Debug.Log($"   - {actorId}.Health: Config={config != null}, Style={config?.slotStyle != null}");
                    }
                }
            }
        }
    }
}