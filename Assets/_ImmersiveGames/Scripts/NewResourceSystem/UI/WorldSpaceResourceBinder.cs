// WorldSpaceResourceBinder.cs

using System.Collections.Generic;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.NewResourceSystem.UI
{
    [DebugLevel(DebugLevel.Verbose)]
    public class WorldSpaceResourceBinder : MonoBehaviour
    {
        [SerializeField] private Canvas worldCanvas;
        
        private string actorId;
        private EntityResourceSystem resourceSystem;
        private Dictionary<ResourceType, ResourceUISlot> worldSlots = new();

        public void Initialize(string ownerActorId, EntityResourceSystem ownerResourceSystem)
        {
            actorId = ownerActorId;
            resourceSystem = ownerResourceSystem;
            DiscoverWorldSlots();
            
            DebugUtility.LogVerbose<WorldSpaceResourceBinder>($"🌍 WorldBinder inicializado: {actorId}");
        }

        private void DiscoverWorldSlots()
        {
            if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
            if (worldCanvas == null) return;

            var slots = worldCanvas.GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var slot in slots)
            {
                if (slot.ExpectedActorId == actorId)
                {
                    worldSlots[slot.ExpectedType] = slot;
                    slot.Clear();
                }
            }
        }

        public void BindResource(ResourceType type, IResourceValue data)
        {
            if (worldSlots.TryGetValue(type, out var slot))
            {
                slot.Configure(data);
            }
        }

        public void UnbindAll()
        {
            foreach (var slot in worldSlots.Values)
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