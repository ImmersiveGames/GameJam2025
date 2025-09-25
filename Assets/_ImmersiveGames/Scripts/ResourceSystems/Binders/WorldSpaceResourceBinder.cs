using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class WorldSpaceResourceBinder : MonoBehaviour
    {
        [SerializeField] private Canvas worldCanvas;
        
        private string _actorId;
        private EntityResourceSystem _resourceSystem;
        private readonly Dictionary<ResourceType, ResourceUISlot> _worldSlots = new();

        public void Initialize(string ownerActorId, EntityResourceSystem ownerResourceSystem)
        {
            _actorId = ownerActorId;
            _resourceSystem = ownerResourceSystem;
            DiscoverWorldSlots();
            
            DebugUtility.LogVerbose<WorldSpaceResourceBinder>($"🌍 WorldBinder inicializado: {_actorId}");
        }

        private void DiscoverWorldSlots()
        {
            if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
            if (worldCanvas == null) return;

            ResourceUISlot[] slots = worldCanvas.GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var slot in slots)
            {
                if (slot.ExpectedActorId != _actorId) continue;
                _worldSlots[slot.ExpectedType] = slot;
                slot.Clear();
            }
        }

        public void BindResource(ResourceType type, IResourceValue data)
        {
            if (_worldSlots.TryGetValue(type, out var slot))
            {
                slot.Configure(data);
            }
        }

        public void UnbindAll()
        {
            foreach (var slot in _worldSlots.Values)
            {
                slot.Clear();
            }
        }

        private void Update()
        {
            // Opcional: Implementar follow de câmera para world UI
            if (worldCanvas != null && worldCanvas.worldCamera == null)
            {
                worldCanvas.worldCamera = Camera.main;
            }
        }
    }
}