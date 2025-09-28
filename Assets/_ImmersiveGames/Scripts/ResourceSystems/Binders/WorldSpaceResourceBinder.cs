using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class WorldSpaceResourceBinder : MonoBehaviour
    {
        [SerializeField] private Canvas worldCanvas;

        private readonly Dictionary<ResourceType, ResourceUISlot> _worldSlots = new();

        private void Awake()
        {
            if (worldCanvas == null) worldCanvas = GetComponent<Canvas>();
            DiscoverWorldSlots();
        }

        private void Start()
        {
            if (worldCanvas != null && worldCanvas.worldCamera == null)
                worldCanvas.worldCamera = Camera.main;
        }

        private void DiscoverWorldSlots()
        {
            var slots = GetComponentsInChildren<ResourceUISlot>(true);
            foreach (var s in slots)
            {
                _worldSlots[s.Type] = s;
                s.Clear();
            }
        }

        public void Bind(ResourceType type, IResourceValue data)
        {
            if (_worldSlots.TryGetValue(type, out var slot))
                slot.Configure(data);
        }

        public void UnbindAll()
        {
            foreach (var s in _worldSlots.Values) s.Clear();
        }
    }
}