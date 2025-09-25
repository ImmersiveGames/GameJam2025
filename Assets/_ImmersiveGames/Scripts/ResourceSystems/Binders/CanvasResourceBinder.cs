using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class CanvasResourceBinder : MonoBehaviour, ICanvasResourceBinder
    {
        [SerializeField] private string canvasId;
        
        private readonly Dictionary<string, ResourceUISlot> _slots = new();
        private EventBinding<ResourceUpdateEvent> _updateBinding;

        public string CanvasId => canvasId;

        private void Awake()
        {
            if (string.IsNullOrEmpty(canvasId))
                canvasId = gameObject.name;

            DiscoverSlots();
            RegisterGlobal();
            RegisterEventListeners();
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 CanvasBinder inicializado: {canvasId} com {_slots.Count} slots");
        }

        private void DiscoverSlots()
        {
            ResourceUISlot[] childSlots = GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var slot in childSlots)
            {
                string slotId = slot.SlotId;
                if (!_slots.TryAdd(slotId, slot)) continue;
                slot.Clear(); // Inicia oculto
                DebugUtility.LogVerbose<CanvasResourceBinder>($"📋 Slot descoberto: {slotId}");
            }
        }

        private void RegisterGlobal()
        {
            if (DependencyManager.Instance == null) return;
            DependencyManager.Instance.RegisterGlobal<ICanvasResourceBinder>(this);
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🌐 Registrado globalmente: {canvasId}");
        }

        private void RegisterEventListeners()
        {
            _updateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_updateBinding);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            UpdateResource(evt.ActorId, evt.ResourceType, evt.NewValue);
        }

        public bool TryBindActor(string actorId, ResourceType type, IResourceValue data)
        {
            string slotId = $"{actorId}_{type}";
            if (!_slots.TryGetValue(slotId, out var slot)) return false;
            slot.Configure(data);
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔗 Actor vinculado: {actorId}.{type} → {canvasId}");
            return true;
        }

        public void UnbindActor(string actorId)
        {
            foreach (var slot in _slots.Values.Where(slot => slot.ExpectedActorId == actorId))
            {
                slot.Clear();
            }
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔓 Actor desvinculado: {actorId} de {canvasId}");
        }

        public void UpdateResource(string actorId, ResourceType type, IResourceValue data)
        {
            string slotId = $"{actorId}_{type}";
            if (_slots.TryGetValue(slotId, out var slot))
            {
                slot.Configure(data);
            }
        }

        private void OnDestroy()
        {
            if (_updateBinding != null)
                EventBus<ResourceUpdateEvent>.Unregister(_updateBinding);
        }

        [ContextMenu("Debug Slots")]
        public void DebugSlots()
        {
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 Canvas {canvasId} Slots ({_slots.Count}):");
            foreach (var slot in _slots.Values)
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"   {slot.SlotId}");
            }
        }
    }
}