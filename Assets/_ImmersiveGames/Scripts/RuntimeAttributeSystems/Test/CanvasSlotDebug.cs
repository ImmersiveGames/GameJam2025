using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.UI;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Test
{
    
    public class CanvasDebugUtility : MonoBehaviour
    {
        [Inject] private IRuntimeAttributeOrchestrator _orchestrator;

        [ContextMenu("🔍 Find and Debug All Slots")]
        public void FindAndDebugAllSlots()
        {
            var slots = FindObjectsByType<RuntimeAttributeUISlot>(FindObjectsSortMode.None);
            DebugUtility.LogVerbose<CanvasDebugUtility>($"🎯 Found {slots.Length} ResourceUISlots in scene");

            foreach (var slot in slots)
            {
                DebugSlotState(slot);
            }
        }

        [ContextMenu("🔄 Force Update All Slots")]
        public void ForceUpdateAllSlots()
        {
            var slots = FindObjectsByType<RuntimeAttributeUISlot>(FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                slot.ForceVisualUpdate();
            }
            DebugUtility.LogVerbose<CanvasDebugUtility>($"🔧 Force updated {slots.Length} slots");
        }

        [ContextMenu("📊 Check Canvas Components")]
        public void CheckCanvasComponents()
        {
            var canvasBinders = FindObjectsByType<RuntimeAttributeActorCanvas>(FindObjectsSortMode.None);
            DebugUtility.LogVerbose<CanvasDebugUtility>($"🎨 Found {canvasBinders.Length} Canvas Binders");

            foreach (var binder in canvasBinders)
            {
                DebugCanvas(binder);
            }

            var slots = FindObjectsByType<RuntimeAttributeUISlot>(FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                DebugSlotState(slot);
            }
        }

        [ContextMenu("🔍 Debug Style Flow")]
        public void DebugStyleFlow()
        {
            if (!DependencyManager.Provider.TryGetGlobal(out IRuntimeAttributeOrchestrator orchestrator))
            {
                DebugUtility.LogWarning<CanvasDebugUtility>("Orchestrator not found for style flow debug");
                return;
            }

            IReadOnlyCollection<string> actorIds = orchestrator.GetRegisteredActorIds();
            DebugUtility.LogVerbose<CanvasDebugUtility>($"🎨 STYLE FLOW DEBUG: {actorIds.Count} actors registered");

            foreach (string actorId in actorIds)
            {
                DebugUtility.LogVerbose<CanvasDebugUtility>($"\n👤 Actor: {actorId}");
                foreach (RuntimeAttributeType resourceType in System.Enum.GetValues(typeof(RuntimeAttributeType)))
                {
                    var config = ResolveInstanceConfig(actorId, resourceType);
                    DebugUtility.LogVerbose<CanvasDebugUtility>($"   - {resourceType}: Config={config != null}, Style={config?.slotStyle != null} ({config?.slotStyle?.name})");

                    if (config == null) continue;

                    var canvasBinders = FindObjectsByType<RuntimeAttributeActorCanvas>(FindObjectsSortMode.None);
                    foreach (var binder in canvasBinders)
                    {
                        if (binder.TryGetSlot(actorId, resourceType, out var slot) && slot != null)
                        {
                            var slotConfig = slot.InstanceConfig;
                            DebugUtility.LogVerbose<CanvasDebugUtility>($"     Slot Style in {binder.CanvasId}: {slotConfig?.slotStyle != null} ({slotConfig?.slotStyle?.name})");
                        }
                    }
                }
            }
        }

        private void DebugCanvas(RuntimeAttributeActorCanvas binder)
        {
            DebugUtility.LogVerbose<CanvasDebugUtility>($"🎨 CANVAS DEBUG: '{binder.CanvasId}'");
            DebugUtility.LogVerbose<CanvasDebugUtility>($"- State: {binder.State}, Injection: {binder.InjectionState}");
            DebugUtility.LogVerbose<CanvasDebugUtility>($"- Type: {binder.Type}, CanAcceptBinds: {binder.CanAcceptBinds()}");
            DebugUtility.LogVerbose<CanvasDebugUtility>($"- Actor Slots: {binder.GetActorSlotsCount()} actors");

            foreach ((string actorId, var slots) in binder.GetActorSlots())
            {
                DebugUtility.LogVerbose<CanvasDebugUtility>($"  - Actor '{actorId}': {slots.Count} slots");
                foreach (var (resourceType, slot) in slots)
                {
                    DebugUtility.LogVerbose<CanvasDebugUtility>($"    - {resourceType}: {(slot != null ? "Active" : "Null")}");
                    if (slot != null)
                    {
                        var rect = slot.GetComponent<RectTransform>();
                        var canvas = slot.GetComponent<Canvas>();
                        DebugUtility.LogVerbose<CanvasDebugUtility>($"      - Pos: {rect?.anchoredPosition}, Order: {canvas?.sortingOrder ?? 0}");
                    }
                }
            }
        }

        private void DebugSlotState(RuntimeAttributeUISlot slot)
        {
            DebugUtility.LogWarning<RuntimeAttributeUISlot>(
                $"🔍 Slot Debug - {slot.Type}:\n" +
                $"Current Fill: {slot.GetCurrentFill()}\n" +
                $"Style: {slot.GetCurrentStyle()?.name ?? "None"}\n" +
                $"Fill Image: {slot.FillImage != null} (color: {slot.FillImage?.color}, amount: {slot.FillImage?.fillAmount})\n" +
                $"Pending Image: {slot.PendingFillImage != null} (color: {slot.PendingFillImage?.color}, amount: {slot.PendingFillImage?.fillAmount})"
            );
        }

        private RuntimeAttributeInstanceConfig ResolveInstanceConfig(string actorId, RuntimeAttributeType runtimeAttributeType)
        {
            if (_orchestrator == null || !_orchestrator.TryGetActorResource(actorId, out var svc)) return null;
            return svc.GetInstanceConfig(runtimeAttributeType);
        }
    }
}