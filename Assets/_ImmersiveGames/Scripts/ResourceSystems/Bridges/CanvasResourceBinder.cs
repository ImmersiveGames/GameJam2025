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

        [Header("Canvas Positioning")]
        [SerializeField] private Vector3 canvasPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 canvasRotationOffset = Vector3.zero;
        [SerializeField] private bool applyOffsetOnStart = true;

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

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                var orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);
                _orchestrator = orchestrator;
            }
            if (!DependencyManager.Instance.TryGetGlobal(out _strategyFactory))
            {
                _strategyFactory = new ResourceSlotStrategyFactory();
                DependencyManager.Instance.RegisterGlobal(_strategyFactory);
            }
            _orchestrator.RegisterCanvas(this);
            DebugUtility.LogVerbose<CanvasResourceBinder>($"Registered '{CanvasId}' for actor '{GetComponentInParent<IActor>()?.ActorId}'.");
        }

        private void Start()
        {
            if (applyOffsetOnStart)
            {
                ApplyCanvasOffset();
            }
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
                maxSize: 20
            );

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
                // Já existe, apenas atualizar
                DebugUtility.LogVerbose<CanvasResourceBinder>($"[Binder] Slot para '{resourceType}' já existe para actor '{actorId}', atualizando");
                existing.Configure(data);
                return;
            }

            var slot = GetSlotFromPool();
            if (slot == null) return;

            // Aplicar apenas a ordenação do slot (se configurada)
            ApplySlotSorting(slot, instanceConfig);

            slot.InitializeForActorId(actorId, resourceType, instanceConfig);
            actorSlots[resourceType] = slot;

            slot.Configure(data);

            DebugUtility.LogVerbose<CanvasResourceBinder>($"Created slot for {resourceType}");
        }

        private void ApplySlotSorting(ResourceUISlot slot, ResourceInstanceConfig instanceConfig)
        {
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Aplicar ordenação apenas se configurada
            if (instanceConfig != null)
            {
                var canvas = rectTransform.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = rectTransform.gameObject.AddComponent<Canvas>();
                }
                canvas.overrideSorting = true;
                canvas.sortingOrder = instanceConfig.sortOrder;
            }
        }

        private void ApplyCanvasOffset()
        {
            // Aplicar offset de posição ao canvas inteiro
            if (canvasPositionOffset != Vector3.zero)
            {
                transform.localPosition += canvasPositionOffset;
            }

            // Aplicar offset de rotação ao canvas inteiro
            if (canvasRotationOffset != Vector3.zero)
            {
                transform.localRotation *= Quaternion.Euler(canvasRotationOffset);
            }

            DebugUtility.LogVerbose<CanvasResourceBinder>($"Applied canvas offset: Pos={canvasPositionOffset}, Rot={canvasRotationOffset}");
        }

        public void UpdateResourceForActor(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (_dynamicSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots) && 
                actorSlots.TryGetValue(resourceType, out var slot) && slot != null)
            {
                slot.Configure(data);
                DebugUtility.LogVerbose<CanvasResourceBinder>($"Updated slot for {resourceType} for actor '{actorId}'");
                return;
            }

            CreateSlotForActor(actorId, resourceType, data);
        }

        private void RemoveSlotsForActor(string actorId)
        {
            if (!_dynamicSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots)) return;
            
            foreach (var slot in actorSlots.Values.Where(slot => slot != null))
            {
                ReturnSlotToPool(slot);
            }
            _dynamicSlots.Remove(actorId);
        }
        
        private void ClearAllSlots()
        {
            StopAllCoroutines();
        
            foreach (string actorId in _dynamicSlots.Keys.ToList())
                RemoveSlotsForActor(actorId);

            _pool?.Clear();
            _dynamicSlots.Clear();
        }

        private void OnDestroy()
        {
            if (!DependencyManager.HasInstance || DependencyManager.Instance == null)
            {
                ClearAllSlots();
                return;
            }

            ClearAllSlots();
            _orchestrator?.UnregisterCanvas(CanvasId);

            if (_pool == null) return;
            _pool.Clear();
            _pool = null;
        }

        [ContextMenu("Apply Canvas Offset")]
        private void ContextApplyCanvasOffset()
        {
            ApplyCanvasOffset();
        }

        [ContextMenu("Reset Canvas Position")]
        private void ContextResetCanvasPosition()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Debug.Log("Canvas position and rotation reset");
        }

        [ContextMenu("Debug Slots State")]
        private void DebugSlots()
        {
            DebugUtility.Log<CanvasResourceBinder>($"Canvas {CanvasId} -> {_dynamicSlots.Count} actors");
            foreach (var actorEntry in _dynamicSlots)
            {
                DebugUtility.Log<CanvasResourceBinder>($"  Actor {actorEntry.Key}:");
                foreach (var slotEntry in actorEntry.Value)
                {
                    var slot = slotEntry.Value;
                    if (slot != null)
                    {
                        var rectTransform = slot.GetComponent<RectTransform>();
                        DebugUtility.Log<CanvasResourceBinder>($"    - {slotEntry.Key}: Pos={rectTransform.localPosition}, SortOrder={slot.GetComponent<Canvas>()?.sortingOrder ?? 0}");
                    }
                }
            }
        }
    }
}