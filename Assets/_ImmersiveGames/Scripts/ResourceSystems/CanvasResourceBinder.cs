using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool; // Reaproveita pooling nativo da Unity
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bridges;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DefaultExecutionOrder(-50)]
    public class CanvasResourceBinder : MonoBehaviour
    {
        [Header("Identification")]
        [SerializeField] private string canvasId;
        [SerializeField] private bool autoGenerateCanvasId = false;

        [Header("Pool & Prefab")]
        [SerializeField] private ResourceUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private bool persistAcrossScenes;

        private readonly Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> _dynamicSlots = new();
        private ObjectPool<ResourceUISlot> _pool;
        private IActorResourceOrchestrator _orchestrator;
        private string _canvasIdResolved;
        public string CanvasId => _canvasIdResolved;

        private void Awake()
        {
            SetupCanvasId();
            
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
            if (dynamicSlotsParent == null) dynamicSlotsParent = transform;
            if (slotPrefab == null) DebugUtility.LogWarning<CanvasResourceBinder>($"slotPrefab not assigned on {name}");

            InitializePool();

            // Setup orchestrator
            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                var orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);
                _orchestrator = orchestrator;
            }

            _orchestrator.RegisterCanvas(this);
            DebugUtility.LogVerbose<CanvasResourceBinder>($"Registered '{CanvasId}' for actor '{GetComponentInParent<IActor>()?.ActorId}'.");
        }
        
        private void SetupCanvasId()
        {
            if (!string.IsNullOrEmpty(canvasId))
            {
                _canvasIdResolved = canvasId;
                return;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out IUniqueIdFactory factory))
            {
                factory = new UniqueIdFactory();
                DependencyManager.Instance.RegisterGlobal<IUniqueIdFactory>(factory);
            }

            if (autoGenerateCanvasId)
                _canvasIdResolved = factory.GenerateId(gameObject, "Canvas");
            else
                _canvasIdResolved = $"{gameObject.scene.name}_{gameObject.name}";
        }
        
        private void InitializePool()
        {
            _pool = new ObjectPool<ResourceUISlot>(
                createFunc: () => Instantiate(slotPrefab, dynamicSlotsParent),
                actionOnGet: s => s.gameObject.SetActive(true),
                actionOnRelease: s => { s.Clear(); s.gameObject.SetActive(false); },
                actionOnDestroy: s => Destroy(s.gameObject),
                collectionCheck: false,
                defaultCapacity: initialPoolSize,
                maxSize: 10
            );

            // Prewarm
            for (int i = 0; i < initialPoolSize; i++)
            {
                var tmp = _pool.Get();
                _pool.Release(tmp);
            }
        }
        
        private ResourceUISlot GetSlotFromPool() => _pool?.Get();
        private void ReturnSlotToPool(ResourceUISlot slot) => _pool?.Release(slot);
        
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
                // already exists
                return;
            }

            var slot = GetSlotFromPool();
            if (slot == null) return;

            slot.InitializeForActorId(actorId, resourceType, instanceConfig);
            actorSlots[resourceType] = slot;

            // Configure initial state (delegates to strategy)
            slot.Configure(data);
        }
        public void UpdateResourceForActor(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (_dynamicSlots.TryGetValue(actorId, out var actorSlots) && actorSlots.TryGetValue(resourceType, out var slot) && slot != null)
            {
                slot.Configure(data); // strategy will animate or snap
                return;
            }

            // create if missing
            CreateSlotForActor(actorId, resourceType, data);
        }
        private void RemoveSlotsForActor(string actorId)
        {
            if (!_dynamicSlots.TryGetValue(actorId, out var actorSlots)) return;
            foreach (var slot in actorSlots.Values.ToList())
            {
                if (slot != null) ReturnSlotToPool(slot);
            }
            _dynamicSlots.Remove(actorId);
        }
        
        private void ClearAllSlots()
        {
            foreach (var actorId in _dynamicSlots.Keys.ToList())
                RemoveSlotsForActor(actorId);

            // clear pool
            if (_pool == null) return;
            // Destroy pooled objects
            while (true)
            {
                // no direct enumerator on ObjectPool; we assume pool's Clear will destroy items via actionOnDestroy
                _pool.Clear();
                break;
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
            Debug.Log($"Canvas {CanvasId} -> dynamic actors: {_dynamicSlots.Count} - pooled initial capacity: {initialPoolSize}");
            foreach (var kv in _dynamicSlots)
                Debug.Log($"  Actor {kv.Key}: {kv.Value.Count} slots");
        }
    }
}
