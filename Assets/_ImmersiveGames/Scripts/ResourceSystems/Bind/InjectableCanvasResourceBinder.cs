using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.Pool;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public abstract class InjectableCanvasResourceBinder : MonoBehaviour, ICanvasBinder
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

        [Header("Bind Timing")]
        [SerializeField] private float maxBindWaitSeconds = 2f;

        private readonly Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> _actorSlots = new();
        private ObjectPool<ResourceUISlot> _pool;

        private string _canvasIdResolved;

        public string CanvasId => _canvasIdResolved;
        public virtual CanvasType Type => canvasType;
        public CanvasInitializationState State { get; protected set; }
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

        public virtual void ForceReady()
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

        protected void CreateSlotForActor(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig = null)
        {
            if (!_actorSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorDict))
            {
                actorDict = new Dictionary<ResourceType, ResourceUISlot>();
                _actorSlots[actorId] = actorDict;
            }

            if (actorDict.TryGetValue(resourceType, out var existingSlot) && existingSlot != null)
            {
                existingSlot.Configure(data);
                return;
            }

            var newSlot = GetSlotFromPool();

            try
            {
                if (instanceConfig == null)
                    instanceConfig = ResolveInstanceConfig(actorId, resourceType);

                Debug.Log($"[CreateSlot] {actorId}.{resourceType}: Config={instanceConfig != null}, Style={instanceConfig?.slotStyle != null}");

                newSlot.InitializeForActorId(actorId, resourceType, instanceConfig);
                newSlot.Configure(data);
                actorDict[resourceType] = newSlot;

                ApplySlotSorting(newSlot, instanceConfig);

                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"✅ Slot created: {actorId}.{resourceType}, Anim: {instanceConfig?.fillAnimationType}, Style: {instanceConfig?.slotStyle?.name}");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error creating slot for {actorId}.{resourceType}: {ex}");
                ReturnSlotToPool(newSlot);
            }
        }

        protected ResourceInstanceConfig ResolveInstanceConfig(string actorId, ResourceType resourceType)
        {
            if (_orchestrator == null || !_orchestrator.TryGetActorResource(actorId, out var svc)) return null;
            return svc.GetInstanceConfig(resourceType);
        }

        private IEnumerator DelayedBind(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig, float maxWait)
        {
            float elapsed = 0f;
            while (elapsed < maxWait)
            {
                if (CanAcceptBinds())
                {
                    CreateSlotForActor(actorId, resourceType, data, instanceConfig);
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Timeout waiting for bind {actorId}.{resourceType} on '{CanvasId}'");
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<ResourceUISlot>(
                () => Instantiate(slotPrefab, dynamicSlotsParent),
                slot => { slot.gameObject.SetActive(true); },
                slot => { slot.gameObject.SetActive(false); },
                Destroy, true, initialPoolSize, 20
            );
        }

        private ResourceUISlot GetSlotFromPool() => _pool.Get();

        private void ReturnSlotToPool(ResourceUISlot slot)
        {
            if (slot == null) return;
            slot.Clear();
            _pool.Release(slot);
        }

        private void SetupCanvasId()
        {
            _canvasIdResolved = autoGenerateCanvasId ? _idFactory?.GenerateId(gameObject) ?? Guid.NewGuid().ToString() : canvasId;
            if (string.IsNullOrEmpty(_canvasIdResolved)) _canvasIdResolved = gameObject.name;
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

            slot.transform.SetSiblingIndex(instanceConfig.sortOrder);
        }

        private void RemoveActorSlots(string actorId)
        {
            if (_actorSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots))
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

            foreach (string actorId in _actorSlots.Keys.ToList())
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
        // Métodos públicos para debug
        public int GetActorSlotsCount() => _actorSlots.Count;
        public IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots() => _actorSlots;
        public int GetPoolCountTotal() => _pool?.CountAll ?? 0;
        public int GetPoolCountActive() => _pool?.CountActive ?? 0;
        public bool TryGetSlot(string actorId, ResourceType resourceType, out ResourceUISlot slot)
        {
            slot = null;
            return _actorSlots.TryGetValue(actorId, out var actorDict) && actorDict.TryGetValue(resourceType, out slot) && slot != null;
        }
    }
}