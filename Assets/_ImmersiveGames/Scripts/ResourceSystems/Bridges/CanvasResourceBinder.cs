using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.Pool;
// Reaproveita pooling nativo da Unity

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class CanvasResourceBinder : MonoBehaviour
    {
        [Header("Identification")]
        [SerializeField] private string canvasId;
        [SerializeField] private bool autoGenerateCanvasId;

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
        private IResourceSlotStrategyFactory _strategyFactory;

        private void Awake()
        {
            if (!DependencyManager.HasInstance || DependencyManager.Instance == null)
            {
                DebugUtility.LogWarning<CanvasResourceBinder>("DependencyManager não está disponível. CanvasResourceBinder não será inicializado.");
                return;
            }
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
            if (!DependencyManager.Instance.TryGetGlobal(out _strategyFactory))
            {
                _strategyFactory = new ResourceSlotStrategyFactory();
                // Opcional: registrar para reuso
                DependencyManager.Instance.RegisterGlobal(_strategyFactory);
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
                DependencyManager.Instance.RegisterGlobal(factory);
            }

            _canvasIdResolved = autoGenerateCanvasId ? factory.GenerateId(gameObject, "Canvas") : $"{gameObject.scene.name}_{gameObject.name}";
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

            if (!_dynamicSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots))
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
            if (_dynamicSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots) && actorSlots.TryGetValue(resourceType, out var slot) && slot != null)
            {
                slot.Configure(data); // strategy will animate or snap
                return;
            }

            // create if missing
            CreateSlotForActor(actorId, resourceType, data);
        }
        private void RemoveSlotsForActor(string actorId)
        {
            if (!_dynamicSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots)) return;
            foreach (var slot in actorSlots.Values.ToList().Where(slot => slot != null))
            {
                ReturnSlotToPool(slot);
            }
            _dynamicSlots.Remove(actorId);
        }
        
        private void ClearAllSlots()
        {
            // Para todas as coroutines ativas primeiro
            StopAllCoroutines();
        
            foreach (string actorId in _dynamicSlots.Keys.ToList())
                RemoveSlotsForActor(actorId);

            // Limpa o pool
            _pool?.Clear();

            _dynamicSlots.Clear();
        }

        private void OnDestroy()
        {
            // Verifica se ainda temos instâncias válidas
            if (!DependencyManager.HasInstance || DependencyManager.Instance == null)
            {
                ClearAllSlots();
                return;
            }

            ClearAllSlots();

            _orchestrator?.UnregisterCanvas(CanvasId);

            // Limpeza adicional
            if (_pool == null) return;
            _pool.Clear();
            _pool = null;
        }

        [ContextMenu("Debug Slots State")]
        private void DebugSlots()
        {
            DebugUtility.Log<CanvasResourceBinder>($"Canvas {CanvasId} -> dynamic actors: {_dynamicSlots.Count} - pooled initial capacity: {initialPoolSize}");
            foreach (KeyValuePair<string, Dictionary<ResourceType, ResourceUISlot>> kv in _dynamicSlots)
                DebugUtility.Log<CanvasResourceBinder>($"  Actor {kv.Key}: {kv.Value.Count} slots");
        }
    }
}
