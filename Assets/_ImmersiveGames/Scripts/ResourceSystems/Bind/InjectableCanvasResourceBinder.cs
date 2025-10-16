using System;
using System.Collections.Generic;
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
        [Inject] protected IActorResourceOrchestrator orchestrator;
        [Inject] protected IResourceSlotStrategyFactory strategyFactory;
        [Inject] protected IUniqueIdFactory idFactory;

        [Header("Pool & Prefab")]
        [SerializeField] private ResourceUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private int initialPoolSize = 5;

        [Header("Canvas Configuration")]
        [SerializeField] private CanvasType canvasType = CanvasType.Scene;

        private readonly Dictionary<string, Dictionary<ResourceType, ResourceUISlot>> _actorSlots = new();
        private ObjectPool<ResourceUISlot> _pool;
        private string _canvasIdResolved;

        public string CanvasId => _canvasIdResolved;
        public virtual CanvasType Type => canvasType;
        public CanvasInitializationState State { get; private set; }
        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => CanvasId;

        protected virtual void Awake()
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
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"❌ Slot prefab not assigned for Canvas '{CanvasId}'.");
                State = CanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
                return;
            }

            dynamicSlotsParent ??= transform;
            InitializePool();

            try
            {
                orchestrator?.RegisterCanvas(this);
                State = CanvasInitializationState.Ready;
                InjectionState = DependencyInjectionState.Ready;

                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"✅ Canvas '{CanvasId}' Ready ({Type})");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"❌ Error initializing canvas '{CanvasId}': {ex}");
                State = CanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        protected void SetupCanvasId()
        {
            if (autoGenerateCanvasId)
            {
                var actor = GetComponentInParent<_ImmersiveGames.Scripts.ActorSystems.IActor>();
                if (actor != null && !string.IsNullOrEmpty(actor.ActorId))
                    _canvasIdResolved = $"{actor.ActorId}_Canvas";
                else
                    _canvasIdResolved = idFactory?.GenerateId(gameObject) ?? Guid.NewGuid().ToString();
            }
            else
                _canvasIdResolved = canvasId;

            if (string.IsNullOrEmpty(_canvasIdResolved))
                _canvasIdResolved = gameObject.name;
        }

        public virtual bool CanAcceptBinds() => State == CanvasInitializationState.Ready;

        public virtual void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (!CanAcceptBinds()) return;
            CreateSlotForActor(actorId, resourceType, data);
        }

        protected void CreateSlotForActor(string actorId, ResourceType resourceType, IResourceValue data, ResourceInstanceConfig instanceConfig = null)
        {
            if (!_actorSlots.TryGetValue(actorId, out var actorDict))
            {
                actorDict = new Dictionary<ResourceType, ResourceUISlot>();
                _actorSlots[actorId] = actorDict;
            }

            if (actorDict.TryGetValue(resourceType, out var existingSlot) && existingSlot != null)
            {
                existingSlot.Configure(data);
                return;
            }

            var newSlot = _pool.Get();
            try
            {
                instanceConfig ??= ResolveInstanceConfig(actorId, resourceType);
                newSlot.InitializeForActorId(actorId, resourceType, instanceConfig);
                newSlot.Configure(data);
                actorDict[resourceType] = newSlot;

                DebugUtility.LogVerbose<InjectableCanvasResourceBinder>($"🎨 Bound {actorId}.{resourceType} on '{CanvasId}'");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"❌ Error creating slot for {actorId}.{resourceType}: {ex}");
                _pool.Release(newSlot);
            }
        }

        private ResourceInstanceConfig ResolveInstanceConfig(string actorId, ResourceType resourceType)
        {
            return orchestrator != null && orchestrator.TryGetActorResource(actorId, out var svc)
                ? svc.GetInstanceConfig(resourceType)
                : null;
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<ResourceUISlot>(
                () =>
                {
                    var slot = Instantiate(slotPrefab, dynamicSlotsParent);
                    slot.gameObject.SetActive(false);
                    return slot;
                },
                slot => slot.gameObject.SetActive(true),
                slot =>
                {
                    slot.Clear();
                    slot.gameObject.SetActive(false);
                },
                slot => Destroy(slot.gameObject),
                false, initialPoolSize, 100
            );

            for (int i = 0; i < initialPoolSize; i++)
                _pool.Release(_pool.Get());
        }

        protected virtual void OnDestroy()
        {
            try
            {
                orchestrator?.UnregisterCanvas(CanvasId);
                if (CanvasPipelineManager.HasInstance)
                    CanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<InjectableCanvasResourceBinder>($"Error during destroy: {ex}");
            }
        }

        public IReadOnlyDictionary<string, Dictionary<ResourceType, ResourceUISlot>> GetActorSlots() => _actorSlots;
        public bool TryGetSlot(string actorId, ResourceType resourceType, out ResourceUISlot slot)
        {
            slot = null;
            return _actorSlots.TryGetValue(actorId, out var actorDict) &&
                actorDict.TryGetValue(resourceType, out slot) && slot != null;
        }
        public int GetActorSlotsCount() => _actorSlots.Count;
    }
}
