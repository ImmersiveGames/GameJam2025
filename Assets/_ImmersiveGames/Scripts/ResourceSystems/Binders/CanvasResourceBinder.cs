// CanvasResourceBinder.cs
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
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
            RegisterForScene();
            RegisterEventListeners();
            
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 CanvasBinder inicializado: {canvasId} com {_slots.Count} slots");
        }

        private void DiscoverSlots()
        {
            ResourceUISlot[] childSlots = GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var slot in childSlots)
            {
                string slotId = slot.SlotId.ToLower().Trim();
                if (!_slots.TryAdd(slotId, slot))
                {
                    DebugUtility.LogWarning<CanvasResourceBinder>($"⚠️ Slot duplicado ignorado: {slotId}");
                    continue;
                }
                slot.Clear();
                DebugUtility.LogVerbose<CanvasResourceBinder>($"📋 Slot descoberto: {slotId}");
            }

            if (_slots.Count == 0)
            {
                DebugUtility.LogWarning<CanvasResourceBinder>($"⚠️ Nenhum slot encontrado para o canvas: {canvasId}");
            }
        }

        private void RegisterForScene()
        {
            string sceneName = gameObject.scene.name;
            DependencyManager.Instance.RegisterForScene<ICanvasResourceBinder>(sceneName, this, allowOverride: true);
            EventBus<CanvasBinderRegisteredEvent>.Raise(new CanvasBinderRegisteredEvent(this));
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🌐 Registrado na cena {sceneName}: {canvasId}");
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
            string slotId = $"{actorId}_{type}".ToLower().Trim();
            if (_slots.TryGetValue(slotId, out var slot))
            {
                slot.Configure(data);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔗 Actor vinculado: {actorId}.{type} → {canvasId}");
                return true;
            }

            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔍 Slot não encontrado para {slotId} no canvas {canvasId}");
            return false;
        }

        public void UnbindActor(string actorId)
        {
            foreach (var slot in _slots.Values.Where(slot => slot.ExpectedActorId.ToLower().Trim() == actorId.ToLower().Trim()))
            {
                slot.Clear();
            }
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🔓 Actor desvinculado: {actorId} de {canvasId}");
        }

        public void UpdateResource(string actorId, ResourceType type, IResourceValue data)
        {
            string slotId = $"{actorId}_{type}".ToLower().Trim();
            if (_slots.TryGetValue(slotId, out var slot))
            {
                slot.Configure(data);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔄 Slot atualizado: {slotId} no canvas {canvasId}");
            }
            else
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"🔍 Slot não encontrado para atualização: {slotId} no canvas {canvasId}");
            }
        }

        private void OnDestroy()
        {
            if (_updateBinding != null)
                EventBus<ResourceUpdateEvent>.Unregister(_updateBinding);
            DebugUtility.LogVerbose<CanvasResourceBinder>($"♻️ CanvasBinder destruído: {canvasId}");
        }

        [ContextMenu("Debug Slots")]
        public void DebugSlots()
        {
            DebugUtility.LogVerbose<CanvasResourceBinder>($"🎨 Canvas {canvasId} Slots ({_slots.Count}):");
            foreach (var slot in _slots)
            {
                DebugUtility.LogVerbose<CanvasResourceBinder>($"   {slot.Key} (Actor: {slot.Value.ExpectedActorId}, Type: {slot.Value.ExpectedType})");
            }
        }
    }
}