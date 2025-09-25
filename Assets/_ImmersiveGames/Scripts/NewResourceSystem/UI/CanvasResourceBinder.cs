// CanvasResourceBinder.cs

using System.Collections.Generic;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.NewResourceSystem.UI
{
    [DebugLevel(DebugLevel.Verbose)]
    public class CanvasResourceBinder : MonoBehaviour, ICanvasResourceBinder
    {
        [SerializeField] private string canvasId;
        
        private Dictionary<string, ResourceUISlot> slots = new();
        private EventBinding<ResourceUpdateEvent> updateBinding;

        public string CanvasId => canvasId;

        private void Awake()
        {
            if (string.IsNullOrEmpty(canvasId))
                canvasId = gameObject.name;

            DiscoverSlots();
            RegisterGlobal();
            RegisterEventListeners();
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 CanvasBinder inicializado: {canvasId} com {slots.Count} slots");
        }

        private void DiscoverSlots()
        {
            var childSlots = GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var slot in childSlots)
            {
                string slotId = slot.SlotId;
                if (!slots.ContainsKey(slotId))
                {
                    slots[slotId] = slot;
                    slot.Clear(); // Inicia oculto
                    DebugUtility.LogVerbose<CanvasResourceBinder>($"📋 Slot descoberto: {slotId}");
                }
            }
        }

        private void RegisterGlobal()
        {
            if (DependencyManager.Instance != null)
            {
                DependencyManager.Instance.RegisterGlobal<ICanvasResourceBinder>(this);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🌐 Registrado globalmente: {canvasId}");
            }
        }

        private void RegisterEventListeners()
        {
            updateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(updateBinding);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            UpdateResource(evt.ActorId, evt.ResourceType, evt.NewValue);
        }

        public bool TryBindActor(string actorId, ResourceType type, IResourceValue data)
        {
            string slotId = $"{actorId}_{type}";
            if (slots.TryGetValue(slotId, out var slot))
            {
                slot.Configure(data);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔗 Actor vinculado: {actorId}.{type} → {canvasId}");
                return true;
            }
            return false;
        }

        public void UnbindActor(string actorId)
        {
            foreach (var slot in slots.Values)
            {
                if (slot.ExpectedActorId == actorId)
                {
                    slot.Clear();
                }
            }
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔓 Actor desvinculado: {actorId} de {canvasId}");
        }

        public void UpdateResource(string actorId, ResourceType type, IResourceValue data)
        {
            string slotId = $"{actorId}_{type}";
            if (slots.TryGetValue(slotId, out var slot))
            {
                slot.Configure(data);
            }
        }

        private void OnDestroy()
        {
            if (updateBinding != null)
                EventBus<ResourceUpdateEvent>.Unregister(updateBinding);
        }

        [ContextMenu("Debug Slots")]
        public void DebugSlots()
        {
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 Canvas {canvasId} Slots ({slots.Count}):");
            foreach (var slot in slots.Values)
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"   {slot.SlotId}");
            }
        }
    }
}