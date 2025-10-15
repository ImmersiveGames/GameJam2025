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

        // NOTE: _canvasIdResolved is what the world sees. It may be provisional in Awake and reconciled at injection.
        private string _canvasIdResolved;
        private string _provisionalCanvasId;

        public string CanvasId => _canvasIdResolved;
        public virtual CanvasType Type => canvasType;
        public CanvasInitializationState State { get; private set; }
        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => _canvasIdResolved;

        private void Awake()
        {
            InjectionState = DependencyInjectionState.Pending;
            State = CanvasInitializationState.Pending;

            // create a provisional id so ResourceInitializationManager can register this object.
            // IMPORTANT: do not depend on injected services here.
            _provisionalCanvasId = GenerateProvisionalCanvasId();
            _canvasIdResolved = _provisionalCanvasId;

            ResourceInitializationManager.Instance.RegisterForInjection(this);
        }

        private string GenerateProvisionalCanvasId()
        {
            // Prefer gameObject name (keeps logs readable) plus GUID to avoid collisions
            return $"{gameObject.name}_{Guid.NewGuid():N}";
        }

        public virtual void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Injecting;
            State = CanvasInitializationState.Injecting;

            ReconcileId();

            if (!ValidatePrefabAndParent())
                return;

            InitializePool();
            EnsureStrategyFactoryRegistered();

            RegisterInOrchestrator();

            InjectionState = DependencyInjectionState.Ready;
            State = CanvasInitializationState.Ready;

            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>(
                $"✅ Canvas '{CanvasId}' fully initialized as {Type}");

            // -------- Local helpers (melhoram legibilidade sem mudar a API) --------
            void ReconcileId()
            {
                string finalCanvasId = ResolveFinalCanvasId();
                if (string.Equals(finalCanvasId, _canvasIdResolved, StringComparison.Ordinal))
                    return;

                string oldId = _canvasIdResolved;
                _canvasIdResolved = finalCanvasId;

                try
                {
                    if (_orchestrator != null)
                    {
                        // Unregister old if present (silently ignore if not present)
                        try { _orchestrator.UnregisterCanvas(oldId); } catch { /* ignore */ }
                    }

                    if (CanvasPipelineManager.HasInstance)
                    {
                        // ensure no stale registration remains in pipeline
                        CanvasPipelineManager.Instance.UnregisterCanvas(oldId);
                    }

                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Canvas id reconciled: {oldId} -> {_canvasIdResolved}");
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<InjectableCanvasResourceBinder>($"Failed to reconcile canvas id: {ex}");
                }
            }

            bool ValidatePrefabAndParent()
            {
                if (slotPrefab == null)
                {
                    DebugUtility.LogError<InjectableCanvasResourceBinder>(
                        $"❌ Slot prefab not assigned for Canvas '{CanvasId}'.");
                    State = CanvasInitializationState.Failed;
                    InjectionState = DependencyInjectionState.Failed;
                    return false;
                }

                if (dynamicSlotsParent == null)
                    dynamicSlotsParent = transform;

                return true;
            }

            void RegisterInOrchestrator()
            {
                try
                {
                    _orchestrator?.RegisterCanvas(this);
                    DebugUtility.LogVerbose<InjectableCanvasResourceBinder>(
                        $"✅ Canvas '{CanvasId}' registered in orchestrator");
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError<InjectableCanvasResourceBinder>(
                        $"Exception registering canvas '{CanvasId}': {ex}");
                }
            }
        }

        private string ResolveFinalCanvasId()
        {
            if (!autoGenerateCanvasId)
            {
                // explicit canvasId set in inspector wins
                return string.IsNullOrEmpty(canvasId) ? gameObject.name : canvasId;
            }

            // If there's an IActor in parent, and it has ActorId, prefer actor-specific canvas id
            var actor = GetComponentInParent<ActorSystems.IActor>();
            if (actor != null && !string.IsNullOrEmpty(actor.ActorId))
            {
                return $"{actor.ActorId}_Canvas";
            }

            // Otherwise prefer idFactory (now injected) if available; fallback to provisional
            try
            {
                if (_idFactory != null)
                {
                    return _idFactory.GenerateId(gameObject);
                }
            }
            catch { /* ignore factory errors and fallthrough to provisional */ }

            return _provisionalCanvasId;
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
        protected virtual void ForceReady() => State = CanvasInitializationState.Ready;

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

            var instanceConfig = ResolveInstanceConfig(actorId, resourceType);

            if (CanAcceptBinds())
                CreateSlotForActor(actorId, resourceType, data, instanceConfig);
            else
                StartCoroutine(DelayedBind(actorId, resourceType, data, instanceConfig, maxBindWaitSeconds));
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
                instanceConfig ??= ResolveInstanceConfig(actorId, resourceType);

                newSlot.InitializeForActorId(actorId, resourceType, instanceConfig);
                newSlot.Configure(data);
                actorDict[resourceType] = newSlot;

                ApplySlotSorting(newSlot, instanceConfig);

                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>(
                    $"✅ Slot created: {actorId}.{resourceType}, Anim: {instanceConfig?.fillAnimationType}, Style: {instanceConfig?.slotStyle?.name}");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error creating slot for {actorId}.{resourceType}: {ex}");
                ReturnSlotToPool(newSlot);
            }
        }

        private ResourceInstanceConfig ResolveInstanceConfig(string actorId, ResourceType resourceType)
        {
            if (_orchestrator == null || !_orchestrator.TryGetActorResource(actorId, out var svc))
                return null;

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
            // Normaliza tamanho inicial
            if (initialPoolSize < 0) initialPoolSize = 0;

            // Cria o pool com funções nomeadas (melhor legibilidade)
            _pool = new ObjectPool<ResourceUISlot>(
                createFunc: Create,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroySlot,
                collectionCheck: false,
                defaultCapacity: Mathf.Max(1, initialPoolSize),
                maxSize: 100
            );

            // Pré-aquecimento do pool
            Prewarm(initialPoolSize);

            DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Pool initialized for Canvas '{CanvasId}' (Size: {initialPoolSize})");

            // -------- Local helpers --------
            ResourceUISlot Create()
            {
                if (slotPrefab == null) return null;

                var slot = Instantiate(slotPrefab, dynamicSlotsParent);
                slot.gameObject.SetActive(false);
                return slot;
            }

            void OnGet(ResourceUISlot slot)
            {
                if (slot == null) return;
                slot.gameObject.SetActive(true);
                slot.transform.SetAsLastSibling();
            }

            void OnRelease(ResourceUISlot slot)
            {
                if (slot == null) return;
                slot.Clear();
                slot.gameObject.SetActive(false);
            }

            void OnDestroySlot(ResourceUISlot slot)
            {
                if (slot == null) return;
                Destroy(slot.gameObject);
            }

            void Prewarm(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var s = _pool.Get();
                    if (s != null) _pool.Release(s);
                }
            }
        }

        private ResourceUISlot GetSlotFromPool() => _pool.Get();

        private void ReturnSlotToPool(ResourceUISlot slot)
        {
            if (slot == null) return;
            slot.Clear();
            _pool.Release(slot);
        }

        private void ApplySlotSorting(ResourceUISlot slot, ResourceInstanceConfig instanceConfig)
        {
            if (slot == null || instanceConfig == null) return;

            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            var canvas = slot.GetComponent<Canvas>();
            if (canvas == null)
                canvas = slot.gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = instanceConfig.sortOrder;

            slot.transform.SetSiblingIndex(instanceConfig.sortOrder);
        }

        private void RemoveActorSlots(string actorId)
        {
            if (_actorSlots.TryGetValue(actorId, out Dictionary<ResourceType, ResourceUISlot> actorSlots))
            {
                foreach (var slot in actorSlots.Values.Where(slot => slot != null))
                    ReturnSlotToPool(slot);

                _actorSlots.Remove(actorId);
                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"Removed all slots for actor '{actorId}' from Canvas '{CanvasId}'");
            }
        }

        private void ClearAllSlots()
        {
            StopAllCoroutines();

            foreach (string actorId in _actorSlots.Keys.ToList())
                RemoveActorSlots(actorId);

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
                    _orchestrator.UnregisterCanvas(CanvasId);

                if (CanvasPipelineManager.HasInstance)
                    CanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error during destruction: {ex}");
            }
        }

        // Debug helpers
        public int GetActorSlotsCount() => _actorSlots.Count;
        public IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots() => _actorSlots;
        public int GetPoolCountTotal() => _pool?.CountAll ?? 0;
        public int GetPoolCountActive() => _pool?.CountActive ?? 0;
        public bool TryGetSlot(string actorId, ResourceType resourceType, out ResourceUISlot slot)
        {
            slot = null;
            return _actorSlots.TryGetValue(actorId, out var actorDict) &&
                   actorDict.TryGetValue(resourceType, out slot) && slot != null;
        }
    }
}
