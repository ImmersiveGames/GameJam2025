using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    [DebugLevel(DebugLevel.Verbose)]
    public class CanvasSlotDebug : MonoBehaviour
    {
        [ContextMenu("🔍 Find and Debug All Slots")]
        public void FindAndDebugAllSlots()
        {
            var slots = FindObjectsByType<ResourceUISlot>(sortMode: FindObjectsSortMode.None);
            Debug.Log($"🎯 Found {slots.Length} ResourceUISlots in scene");
            
            foreach (var slot in slots)
            {
                slot.DebugSlotState();
            }
        }

        [ContextMenu("🔄 Force Update All Slots")]
        public void ForceUpdateAllSlots()
        {
            var slots = FindObjectsByType<ResourceUISlot>(sortMode: FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                slot.ForceVisualUpdate();
            }
            Debug.Log($"🔧 Force updated {slots.Length} slots");
        }

        [ContextMenu("📊 Check Canvas Components")]
        public void CheckCanvasComponents()
        {
            var canvasBinders = FindObjectsByType<InjectableCanvasResourceBinder>(sortMode: FindObjectsSortMode.None);
            Debug.Log($"🎨 Found {canvasBinders.Length} Canvas Binders");
            
            foreach (var binder in canvasBinders)
            {
                Debug.Log($"Canvas: {binder.CanvasId}, State: {binder.State}");
            }

            var slots = FindObjectsByType<ResourceUISlot>(sortMode: FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                Debug.Log($"Slot: {slot.Type}, Active: {slot.RootPanel.activeSelf}");
                
                if (slot.FillImage != null)
                {
                    Debug.Log($"  - Fill: {slot.FillImage.fillAmount}, Color: {slot.FillImage.color}");
                }
                if (slot.PendingFillImage != null)
                {
                    Debug.Log($"  - Pending: {slot.PendingFillImage.fillAmount}, Color: {slot.PendingFillImage.color}");
                }
            }
        }
        
    }
}