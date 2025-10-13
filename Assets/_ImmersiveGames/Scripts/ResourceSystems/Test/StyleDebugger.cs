using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    public class StyleDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logVerbose = true;
        
        [ContextMenu("🎯 Debug Style Resolution Flow")]
        public void DebugStyleResolutionFlow()
        {
            Debug.Log("=== 🎯 STYLE RESOLUTION FLOW DEBUG ===");
            
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(FindObjectsSortMode.None);
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            
            if (orchestrator == null)
            {
                Debug.LogError("❌ Orchestrator not found in DependencyManager");
                return;
            }
            
            var actorIds = orchestrator.GetRegisteredActorIds();
            
            foreach (var actorId in actorIds)
            {
                Debug.Log($"\n👤 ACTOR: {actorId}");
                
                // Verificar ResourceSystem do ator
                var resourceSystem = orchestrator.GetActorResourceSystem(actorId);
                if (resourceSystem != null)
                {
                    Debug.Log($"✅ ResourceSystem: Found");
                    
                    // Verificar configurações de recursos
                    foreach (ResourceType resourceType in System.Enum.GetValues(typeof(ResourceType)))
                    {
                        var config = resourceSystem.GetInstanceConfig(resourceType);
                        if (config != null)
                        {
                            Debug.Log($"📋 {resourceType}:");
                            Debug.Log($"   - Config: Found");
                            Debug.Log($"   - Style: {config.slotStyle != null} ({config.slotStyle?.name})");
                            Debug.Log($"   - Animation: {config.fillAnimationType}");
                            Debug.Log($"   - Canvas Mode: {config.canvasTargetMode}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"❌ ResourceSystem: Not found");
                }

                // Verificar em cada canvas
                foreach (var binder in canvasBinders)
                {
                    Debug.Log($"\n🎨 CANVAS: {binder.CanvasId} ({binder.GetType().Name})");
                    
                    // Testar resolução para Health
                    var config = ResolveInstanceConfigFromBinder(binder, actorId, ResourceType.Health);
                    if (config != null)
                    {
                        Debug.Log($"✅ Health Config: Found");
                        Debug.Log($"   - Style: {config.slotStyle != null} ({config.slotStyle?.name})");
                        Debug.Log($"   - Animation: {config.fillAnimationType}");
                        
                        // Verificar se há slot criado
                        var slot = GetSlotForActor(binder, actorId, ResourceType.Health);
                        if (slot != null)
                        {
                            var slotConfig = slot.GetInstanceConfig();
                            Debug.Log($"   - Slot Created: ✅");
                            Debug.Log($"   - Slot Config: {slotConfig != null}");
                            Debug.Log($"   - Slot Style: {slotConfig?.slotStyle != null} ({slotConfig?.slotStyle?.name})");
                            Debug.Log($"   - Current Fill: {(slotConfig != null ? "N/A" : "No config")}");
                        }
                        else
                        {
                            Debug.Log($"   - ❌ No slot created");
                        }
                    }
                    else
                    {
                        Debug.Log($"❌ Health Config: Not found in this canvas");
                    }
                }
            }
            
            Debug.Log("=== ✅ DEBUG COMPLETE ===");
        }

        [ContextMenu("🔍 Debug All Canvas Binders")]
        public void DebugAllCanvasBinders()
        {
            Debug.Log("=== 🔍 ALL CANVAS BINDERS DEBUG ===");
            
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(FindObjectsSortMode.None);
            
            foreach (var binder in canvasBinders)
            {
                Debug.Log($"\n🎨 {binder.GetType().Name}: '{binder.CanvasId}'");
                Debug.Log($"   - State: {binder.State}");
                Debug.Log($"   - Injection: {binder.InjectionState}");
                Debug.Log($"   - CanAcceptBinds: {binder.CanAcceptBinds()}");
                
                // Usar reflection para acessar slots privados
                var actorSlots = GetActorSlotsFromBinder(binder);
                if (actorSlots != null)
                {
                    Debug.Log($"   - Actors with slots: {actorSlots.Count}");
                    foreach (var actorEntry in actorSlots)
                    {
                        Debug.Log($"     👤 {actorEntry.Key}: {actorEntry.Value.Count} slots");
                        foreach (var slotEntry in actorEntry.Value)
                        {
                            var slot = slotEntry.Value;
                            if (slot != null)
                            {
                                var config = slot.GetInstanceConfig();
                                Debug.Log($"       - {slotEntry.Key}: Style={config?.slotStyle != null} ({config?.slotStyle?.name})");
                            }
                        }
                    }
                }
            }
            
            Debug.Log("=== ✅ DEBUG COMPLETE ===");
        }

        [ContextMenu("🔄 Force Reinitialize All Slots")]
        public void ForceReinitializeAllSlots()
        {
            Debug.Log("=== 🔄 FORCE REINITIALIZE ALL SLOTS ===");
            
            var slots = FindObjectsByType<ResourceUISlot>(FindObjectsSortMode.None);
            int reinitializedCount = 0;
            int styleAppliedCount = 0;
            
            foreach (var slot in slots)
            {
                var config = slot.GetInstanceConfig();
                if (config != null)
                {
                    // Chamar RefreshStyle se existir
                    var refreshMethod = slot.GetType().GetMethod("RefreshStyle");
                    if (refreshMethod != null)
                    {
                        refreshMethod.Invoke(slot, null);
                        reinitializedCount++;
                        
                        if (config.slotStyle != null)
                        {
                            styleAppliedCount++;
                            if (logVerbose)
                                Debug.Log($"✅ Reinitialized: {slot.Type} with style {config.slotStyle.name}");
                        }
                        else
                        {
                            if (logVerbose)
                                Debug.Log($"⚠️ Reinitialized: {slot.Type} but no style assigned");
                        }
                    }
                    else
                    {
                        // Fallback: chamar ForceVisualUpdate
                        var forceUpdateMethod = slot.GetType().GetMethod("ForceVisualUpdate");
                        if (forceUpdateMethod != null)
                        {
                            forceUpdateMethod.Invoke(slot, null);
                            reinitializedCount++;
                            if (logVerbose)
                                Debug.Log($"🔧 ForceUpdated: {slot.Type}");
                        }
                    }
                }
                else
                {
                    if (logVerbose)
                        Debug.Log($"❌ No config for: {slot.Type}");
                }
            }
            
            Debug.Log($"✅ Reinitialized {reinitializedCount} slots ({styleAppliedCount} with styles)");
        }

        [ContextMenu("🎨 Test Style Application")]
        public void TestStyleApplication()
        {
            Debug.Log("=== 🎨 TEST STYLE APPLICATION ===");
            
            var slots = FindObjectsByType<ResourceUISlot>(FindObjectsSortMode.None);
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            
            foreach (var slot in slots)
            {
                Debug.Log($"\n🔍 Testing Slot: {slot.gameObject.name}");
                Debug.Log($"   - Type: {slot.Type}");
                
                var config = slot.GetInstanceConfig();
                if (config != null)
                {
                    Debug.Log($"   - Config: ✅ Found");
                    Debug.Log($"   - Style: {config.slotStyle != null} ({config.slotStyle?.name})");
                    Debug.Log($"   - Animation: {config.fillAnimationType}");
                    
                    // Verificar componentes UI
                    var fillImage = slot.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (fillImage != null)
                    {
                        Debug.Log($"   - Fill Image: ✅ (color: {fillImage.color}, amount: {fillImage.fillAmount})");
                    }
                    else
                    {
                        Debug.Log($"   - ❌ Fill Image: Not found");
                    }
                }
                else
                {
                    Debug.Log($"   - ❌ Config: Not found");
                }
            }
            
            Debug.Log("=== ✅ TEST COMPLETE ===");
        }

        #region Helper Methods

        private ResourceInstanceConfig ResolveInstanceConfigFromBinder(InjectableCanvasResourceBinder binder, string actorId, ResourceType resourceType)
        {
            var method = binder.GetType().GetMethod("ResolveInstanceConfig", 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                return method.Invoke(binder, new object[] { actorId, resourceType }) as ResourceInstanceConfig;
            }
            return null;
        }

        private ResourceUISlot GetSlotForActor(InjectableCanvasResourceBinder binder, string actorId, ResourceType resourceType)
        {
            var actorSlots = GetActorSlotsFromBinder(binder);
            if (actorSlots != null && actorSlots.TryGetValue(actorId, out var slots) && slots.TryGetValue(resourceType, out var slot))
            {
                return slot;
            }
            return null;
        }

        private Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlotsFromBinder(InjectableCanvasResourceBinder binder)
        {
            var field = typeof(InjectableCanvasResourceBinder).GetField("_actorSlots", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                return field.GetValue(binder) as Dictionary<string, Dictionary<ResourceType, ResourceUISlot>>;
            }
            return null;
        }

        #endregion

        #region Debug Commands for Specific Issues

        [ContextMenu("🚨 Debug Missing Styles")]
        public void DebugMissingStyles()
        {
            Debug.Log("=== 🚨 DEBUG MISSING STYLES ===");
            
            var slots = FindObjectsByType<ResourceUISlot>(FindObjectsSortMode.None);
            int missingStyles = 0;
            
            foreach (var slot in slots)
            {
                var config = slot.GetInstanceConfig();
                if (config == null || config.slotStyle == null)
                {
                    missingStyles++;
                    Debug.Log($"❌ {slot.gameObject.name}:");
                    Debug.Log($"   - Config: {config != null}");
                    Debug.Log($"   - Style: {config?.slotStyle != null}");
                    Debug.Log($"   - Type: {slot.Type}");
                }
            }
            
            if (missingStyles == 0)
            {
                Debug.Log("✅ All slots have styles assigned!");
            }
            else
            {
                Debug.Log($"⚠️ Found {missingStyles} slots with missing styles");
            }
        }

        [ContextMenu("🔧 Fix All Slot Styles")]
        public void FixAllSlotStyles()
        {
            Debug.Log("=== 🔧 FIXING ALL SLOT STYLES ===");
            
            var slots = FindObjectsByType<ResourceUISlot>(FindObjectsSortMode.None);
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(FindObjectsSortMode.None);
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            
            int fixedCount = 0;
            
            foreach (var slot in slots)
            {
                // Tentar encontrar o config correto para este slot
                var slotName = slot.gameObject.name;
                if (slotName.Contains("_"))
                {
                    var parts = slotName.Split('_');
                    if (parts.Length >= 2)
                    {
                        var actorId = parts[0];
                        var resourceTypeStr = parts[1];
                        
                        if (System.Enum.TryParse<ResourceType>(resourceTypeStr, out var resourceType))
                        {
                            // Encontrar o config correto
                            var resourceSystem = orchestrator?.GetActorResourceSystem(actorId);
                            if (resourceSystem != null)
                            {
                                var config = resourceSystem.GetInstanceConfig(resourceType);
                                if (config != null && config.slotStyle != null)
                                {
                                    // Aqui precisaríamos de uma maneira de atualizar o config do slot
                                    // Como não temos um método público, vamos apenas logar
                                    Debug.Log($"🔧 Would fix: {slotName} with style {config.slotStyle.name}");
                                    fixedCount++;
                                }
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"✅ Would fix {fixedCount} slots (implementation needed)");
        }

        #endregion
    }
}