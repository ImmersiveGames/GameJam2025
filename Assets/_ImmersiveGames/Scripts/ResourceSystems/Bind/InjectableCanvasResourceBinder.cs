using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.Pool;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class InjectableCanvasResourceBinder : MonoBehaviour, ICanvasBinder 
    {
        [Header("Identification")]
        [SerializeField] private string canvasId;
        [SerializeField] private bool autoGenerateCanvasId = true;
    
        [Header("Dependencies")]
        [Inject] private IActorResourceOrchestrator _orchestrator;
        [Inject] private IResourceSlotStrategyFactory _strategyFactory;
        [Inject] private IUniqueIdFactory _idFactory;
        
        [Header("Pool & Prefab")]
        [SerializeField] private ResourceUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private int initialPoolSize = 5;

        [Header("Canvas Configuration")]
        [SerializeField] private CanvasType canvasType = CanvasType.Scene;
        [SerializeField] private Vector3 canvasPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 canvasRotationOffset = Vector3.zero;
        [SerializeField] private bool applyOffsetOnStart = true;

        [Header("Bind Timing")]
        [SerializeField] private float maxBindWaitSeconds = 2f;

        // CORREÇÃO: Slots organizados por ator e resourceType
        private readonly Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> _actorSlots = new();
        private ObjectPool<ResourceUISlot> _pool;
        
        private string _canvasIdResolved;
        
        // CORREÇÃO: Propriedades implementadas corretamente
        public string CanvasId => _canvasIdResolved;
        public virtual CanvasType Type => canvasType;
        public CanvasInitializationState State { get; set; }
        public DependencyInjectionState InjectionState { get; set; }
    
        public string GetObjectId() => CanvasId;
    
        private void Awake()
        {
            InjectionState = DependencyInjectionState.Pending;
            State = CanvasInitializationState.Pending;
            SetupCanvasId();
            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }
    
        public virtual void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Injecting;
            State = CanvasInitializationState.Injecting;

            // Validações essenciais
            if (slotPrefab == null)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"❌ Slot prefab not assigned for Canvas '{CanvasId}'. Binds will fail.");
                State = CanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
                return;
            }

            if (dynamicSlotsParent == null)
            {
                dynamicSlotsParent = transform;
                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Dynamic slots parent not assigned for Canvas '{CanvasId}', using self transform.");
            }

            InitializePool();
            EnsureStrategyFactoryRegistered();

            // Registrar no orchestrator
            try
            {
                if (_orchestrator != null)
                {
                    _orchestrator.RegisterCanvas(this);
                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"✅ Canvas '{CanvasId}' registered in orchestrator");
                }
                else
                {
                    DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Orchestrator not available for Canvas '{CanvasId}'");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Exception registering canvas '{CanvasId}': {ex}");
            }

            // Aplicar offset se configurado
            if (applyOffsetOnStart) 
            {
                ApplyCanvasOffset();
            }

            InjectionState = DependencyInjectionState.Ready;
            State = CanvasInitializationState.Ready;
        
            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"✅ Canvas '{CanvasId}' fully initialized as {Type}");
        }

        private void EnsureStrategyFactoryRegistered()
        {
            try
            {
                if (!DependencyManager.Instance.TryGetGlobal(out IResourceSlotStrategyFactory existingFactory) || existingFactory == null)
                {
                    var fallback = new ResourceSlotStrategyFactory();
                    DependencyManager.Instance.RegisterGlobal<IResourceSlotStrategyFactory>(fallback);
                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Registered fallback strategy factory for Canvas '{CanvasId}'");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Failed to ensure strategy factory: {ex}");
            }
        }

        public virtual bool CanAcceptBinds() => State == CanvasInitializationState.Ready;

        public virtual  void ForceReady() 
        {
            State = CanvasInitializationState.Ready;
            DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Canvas '{CanvasId}' forced to ready state");
        }

        public virtual void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (string.IsNullOrEmpty(actorId))
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"ScheduleBind called with empty actorId on Canvas '{CanvasId}'");
                return;
            }

            if (data == null)
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"ScheduleBind called with null data for {actorId}.{resourceType}");
                return;
            }

            // Resolver a configuração da instância
            ResourceInstanceConfig instanceConfig = ResolveInstanceConfig(actorId, resourceType);

            if (CanAcceptBinds())
            {
                CreateSlotForActor(actorId, resourceType, data, instanceConfig);
            }
            else
            {
                StartCoroutine(DelayedBind(actorId, resourceType, data, instanceConfig, maxBindWaitSeconds));
            }
        }

        private ResourceInstanceConfig ResolveInstanceConfig(string actorId, ResourceType resourceType)
        {
            try
            {
                ResourceSystem actorSvc = null;
                if (_orchestrator != null)
                {
                    actorSvc = _orchestrator.GetActorResourceSystem(actorId);
                }
                else
                {
                    DependencyManager.Instance.TryGetForObject(actorId, out actorSvc);
                }

                return actorSvc?.GetInstanceConfig(resourceType);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error resolving config for {actorId}.{resourceType}: {ex}");
                return null;
            }
        }

        private IEnumerator DelayedBind(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig, float timeoutSeconds)
        {
            float startTime = Time.time;
            
            while (!CanAcceptBinds() && (Time.time - startTime) < timeoutSeconds)
            {
                yield return null;
            }

            if (CanAcceptBinds())
            {
                CreateSlotForActor(actorId, resourceType, data, instanceConfig);
            }
            else
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Timeout binding {actorId}.{resourceType} to Canvas '{CanvasId}'");
                // Fallback: reencaminhar para o pipeline
                FallbackToPipeline(actorId, resourceType, data);
            }
        }

        private void FallbackToPipeline(string actorId, ResourceType resourceType, IResourceValue data)
        {
            try
            {
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.ScheduleBind(actorId, resourceType, data, CanvasId);
                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Fallback to pipeline for {actorId}.{resourceType}");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Failed fallback for {actorId}.{resourceType}: {ex}");
            }
        }

        #region Implementação do Canvas Resource Binder

        private void SetupCanvasId()
        {
            if (!string.IsNullOrEmpty(canvasId))
            {
                _canvasIdResolved = canvasId;
                return;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out IUniqueIdFactory factory) || factory == null)
            {
                factory = new UniqueIdFactory();
                DependencyManager.Instance.RegisterGlobal(factory);
            }

            _canvasIdResolved = autoGenerateCanvasId ? 
                factory.GenerateId(gameObject, "Canvas") : 
                $"{gameObject.scene.name}_{gameObject.name}";

            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Canvas ID resolved: {_canvasIdResolved}");
        }
        
        private void InitializePool()
        {
            if (initialPoolSize < 0) initialPoolSize = 0;

            _pool = new ObjectPool<ResourceUISlot>(
                createFunc: () =>
                {
                    if (slotPrefab == null) return null;
                    var slot = Instantiate(slotPrefab, dynamicSlotsParent);
                    slot.gameObject.SetActive(false);
                    return slot;
                },
                actionOnGet: slot => 
                { 
                    if (slot != null) 
                    {
                        slot.gameObject.SetActive(true);
                        slot.transform.SetAsLastSibling();
                    }
                },
                actionOnRelease: slot => 
                { 
                    if (slot != null) 
                    {
                        slot.Clear();
                        slot.gameObject.SetActive(false);
                    }
                },
                actionOnDestroy: slot => 
                { 
                    if (slot != null) Destroy(slot.gameObject); 
                },
                collectionCheck: false,
                defaultCapacity: Math.Max(1, initialPoolSize),
                maxSize: 100
            );

            // Pré-aquecer o pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                var slot = _pool.Get();
                if (slot != null) _pool.Release(slot);
            }

            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Pool initialized for Canvas '{CanvasId}' (Size: {initialPoolSize})");
        }
        
        private ResourceUISlot GetSlotFromPool()
        {
            try
            {
                return _pool?.Get();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error getting slot from pool: {ex}");
                return null;
            }
        }

        private void ReturnSlotToPool(ResourceUISlot slot)
        {
            if (slot == null) return;
            
            try
            {
                _pool?.Release(slot);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error returning slot to pool: {ex}");
                Destroy(slot.gameObject);
            }
        }
        
        public void CreateSlotForActor(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig = null)
        {
            if (string.IsNullOrEmpty(actorId))
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"CreateSlotForActor called with empty actorId");
                return;
            }

            if (slotPrefab == null)
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Cannot create slot - slotPrefab is null");
                return;
            }

            // CORREÇÃO: Garantir estrutura para o ator
            if (!_actorSlots.TryGetValue(actorId, out var actorSlotDict))
            {
                actorSlotDict = new Dictionary<ResourceType, ResourceUISlot>();
                _actorSlots[actorId] = actorSlotDict;
            }

            // Verificar se slot já existe para este resourceType
            if (actorSlotDict.TryGetValue(resourceType, out var existingSlot) && existingSlot != null)
            {
                // Apenas atualizar o slot existente
                existingSlot.Configure(data);
                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Updated existing slot: {actorId}.{resourceType}");
                return;
            }

            // Criar novo slot
            var newSlot = GetSlotFromPool();
            if (newSlot == null)
            {
                DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Failed to get slot from pool for {actorId}.{resourceType}");
                return;
            }

            try
            {
                // Configurar o slot
                ApplySlotSorting(newSlot, instanceConfig);
                newSlot.InitializeForActorId(actorId, resourceType, instanceConfig);
                newSlot.Configure(data);

                // Registrar o slot
                actorSlotDict[resourceType] = newSlot;

                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"✅ Created slot: {actorId}.{resourceType} on Canvas '{CanvasId}'");
                
                // Log detalhado da configuração
                if (instanceConfig != null)
                {
                    var animType = instanceConfig.fillAnimationType;
                    var styleName = instanceConfig.slotStyle != null ? instanceConfig.slotStyle.name : "default";
                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"  Config: Animation={animType}, Style={styleName}, SortOrder={instanceConfig.sortOrder}");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error creating slot for {actorId}.{resourceType}: {ex}");
                ReturnSlotToPool(newSlot);
            }
        }

        private void ApplySlotSorting(ResourceUISlot slot, ResourceInstanceConfig instanceConfig)
        {
            if (slot == null || instanceConfig == null) return;

            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            var canvas = slot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = slot.gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = instanceConfig.sortOrder;

            // Opcional: aplicar ordenação por sibling baseada no sortOrder
            slot.transform.SetSiblingIndex(instanceConfig.sortOrder);
        }

        public void ApplyCanvasOffset()
        {
            if (canvasPositionOffset != Vector3.zero)
            {
                transform.localPosition += canvasPositionOffset;
            }

            if (canvasRotationOffset != Vector3.zero)
            {
                transform.localRotation *= Quaternion.Euler(canvasRotationOffset);
            }

            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Applied canvas offset for '{CanvasId}': Pos={canvasPositionOffset}, Rot={canvasRotationOffset}");
        }

        // CORREÇÃO: Método para remover slots de um ator específico
        public void RemoveActorSlots(string actorId)
        {
            if (_actorSlots.TryGetValue(actorId, out var actorSlots))
            {
                foreach (var slot in actorSlots.Values.Where(slot => slot != null))
                {
                    ReturnSlotToPool(slot);
                }
                _actorSlots.Remove(actorId);
                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Removed all slots for actor '{actorId}' from Canvas '{CanvasId}'");
            }
        }

        private void ClearAllSlots()
        {
            StopAllCoroutines();
            
            foreach (var actorId in _actorSlots.Keys.ToList())
            {
                RemoveActorSlots(actorId);
            }

            _pool?.Clear();
            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Cleared all slots and pool for Canvas '{CanvasId}'");
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            ClearAllSlots();

            try
            {
                if (_orchestrator != null && !string.IsNullOrEmpty(CanvasId))
                {
                    _orchestrator.UnregisterCanvas(CanvasId);
                }

                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error during destruction: {ex}");
            }
        }

        [ContextMenu("🔍 DEBUG CANVAS")]
        public void DebugCanvas()
        {
            Debug.Log($"🎨 CANVAS DEBUG: '{CanvasId}'");
            Debug.Log($"- State: {State}, Injection: {InjectionState}");
            Debug.Log($"- Type: {Type}, CanAcceptBinds: {CanAcceptBinds()}");
            Debug.Log($"- Actor Slots: {_actorSlots.Count} actors");
            
            foreach (var (actorId, slots) in _actorSlots)
            {
                Debug.Log($"  - Actor '{actorId}': {slots.Count} slots");
                foreach (var (resourceType, slot) in slots)
                {
                    Debug.Log($"    - {resourceType}: {(slot != null ? "Active" : "Null")}");
                }
            }

            Debug.Log($"- Pool: {_pool?.CountAll ?? 0} total, {_pool?.CountActive ?? 0} active");
        }

        [ContextMenu("Debug/Print Slot Details")]
        public void DebugSlotDetails()
        {
            DebugUtility.Log<InjectableCanvasResourceBinder>($"📊 SLOT DETAILS for Canvas '{CanvasId}':");
            foreach (var (actorId, slots) in _actorSlots)
            {
                DebugUtility.Log<InjectableCanvasResourceBinder>($"  Actor: {actorId}");
                foreach (var (resourceType, slot) in slots)
                {
                    if (slot != null)
                    {
                        var rect = slot.GetComponent<RectTransform>();
                        var canvas = slot.GetComponent<Canvas>();
                        DebugUtility.Log<InjectableCanvasResourceBinder>($"    - {resourceType}: Pos={rect.anchoredPosition}, Order={canvas?.sortingOrder ?? 0}");
                    }
                }
            }
        }
        
        #endregion
    }
}