using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(-50)]
    public class CanvasResourceBinder : MonoBehaviour
    {
        [SerializeField] private string canvasId;
        [SerializeField] private ResourceUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private bool persistAcrossScenes;
        [SerializeField] private int initialPoolSize = 8;

        private readonly Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> _dynamicSlots = new();
        private readonly Queue<ResourceUISlot> _slotPool = new();

        private IActorResourceOrchestrator _orchestrator;
        private ResourceBarAnimator _animator;

        public string CanvasId => string.IsNullOrEmpty(canvasId) ? $"{gameObject.scene.name}_{gameObject.name}" : canvasId;

        private void Awake()
        {
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
            if (dynamicSlotsParent == null) dynamicSlotsParent = transform;

            if (slotPrefab == null)
                Debug.LogWarning($"CanvasResourceBinder on {name} has no slotPrefab assigned.");

            for (int i = 0; i < initialPoolSize && slotPrefab != null; i++)
            {
                var s = Instantiate(slotPrefab, dynamicSlotsParent);
                s.gameObject.SetActive(false);
                _slotPool.Enqueue(s);
            }

            // animator lookup
            _animator = GetComponentInParent<ResourceBarAnimator>() ?? FindFirstObjectByType<ResourceBarAnimator>();

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                var orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);
                _orchestrator = orchestrator;
            }

            _orchestrator.RegisterCanvas(this);
        }

        private ResourceUISlot GetSlotFromPool()
        {
            while (_slotPool.Count > 0)
            {
                var s = _slotPool.Dequeue();
                if (s != null) { s.gameObject.SetActive(true); return s; }
            }

            if (slotPrefab == null) return null;
            return Instantiate(slotPrefab, dynamicSlotsParent);
        }

        private void ReturnSlotToPool(ResourceUISlot slot)
        {
            if (slot == null) return;
            slot.Clear();
            slot.gameObject.SetActive(false);
            _slotPool.Enqueue(slot);
        }

        public void CreateSlotForActor(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig = null)
        {
            if (string.IsNullOrEmpty(actorId) || slotPrefab == null) return;

            if (!_dynamicSlots.TryGetValue(actorId, out var actorSlots))
            {
                actorSlots = new Dictionary<ResourceType, ResourceUISlot>();
                _dynamicSlots[actorId] = actorSlots;
            }

            if (actorSlots.TryGetValue(resourceType, out var existing) && existing != null)
            {
                existing.Configure(data);
                return;
            }

            var slot = GetSlotFromPool();
            if (slot == null) return;

            slot.InitializeForActorId(actorId, resourceType, instanceConfig);

            // Prefer animator if instance wants animation and animator exists
            if (instanceConfig is { enableAnimation: true } && _animator != null)
            {
                // set immediate values then animate
                slot.SetFillValues(data.GetPercentage(), data.GetPercentage());
                _animator.StartAnimation(slot, data.GetPercentage(), instanceConfig.animationStyle ?? slot.DefaultStyle);
            }
            else
            {
                slot.Configure(data);
            }

            actorSlots[resourceType] = slot;
        }

        public void UpdateResourceForActor(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (_dynamicSlots.TryGetValue(actorId, out var actorSlots) && actorSlots.TryGetValue(resourceType, out var slot) && slot != null)
            {
                var inst = slot.InstanceConfig;
                if (inst is { enableAnimation: true } && _animator != null)
                {
                    _animator.StartAnimation(slot, data.GetPercentage(), inst.animationStyle ?? slot.DefaultStyle);
                }
                else
                {
                    slot.Configure(data);
                }
                return;
            }

            // create if missing
            CreateSlotForActor(actorId, resourceType, data);
        }

        private void RemoveSlotsForActor(string actorId)
        {
            if (!_dynamicSlots.TryGetValue(actorId, out var actorSlots)) return;

            foreach (var slot in actorSlots.Values.ToList())
                if (slot != null) ReturnSlotToPool(slot);

            _dynamicSlots.Remove(actorId);
        }

        private void ClearAllSlots()
        {
            foreach (var actorId in _dynamicSlots.Keys.ToList())
                RemoveSlotsForActor(actorId);

            while (_slotPool.Count > 0)
            {
                var s = _slotPool.Dequeue();
                if (s != null) Destroy(s.gameObject);
            }
        }

        private void OnDestroy()
        {
            ClearAllSlots();
            _orchestrator?.UnregisterCanvas(CanvasId);
        }

        [ContextMenu("Debug Slots State")]
        private void DebugSlots()
        {
            Debug.Log($"Canvas {CanvasId} -> dynamic actors: {_dynamicSlots.Count} - poolSize: {_slotPool.Count}");
            foreach (var kv in _dynamicSlots)
                Debug.Log($"  Actor {kv.Key}: {kv.Value.Count} slots");
        }
    }
}
