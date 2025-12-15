using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.Pool;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Values;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.UI;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind
{
    public abstract class RuntimeAttributeActorCanvas : MonoBehaviour, IRuntimeAttributeCanvasBinder
    {
        [Header("Identification")]
        [SerializeField] private string canvasId;
        [SerializeField] private bool autoGenerateCanvasId = true;

        [Header("Dependencies")]
        [Inject] protected IRuntimeAttributeOrchestrator orchestrator;
        [Inject] protected IUniqueIdFactory idFactory;

        [Header("Pool & Prefab")]
        [SerializeField] private RuntimeAttributeUISlot slotPrefab;
        [SerializeField] private Transform dynamicSlotsParent;
        [SerializeField] private int initialPoolSize = 5;

        [Header("Canvas Configuration")]
        [SerializeField] private AttributeCanvasType attributeCanvasType = AttributeCanvasType.Scene;

        private readonly Dictionary<string, Dictionary<RuntimeAttributeType, RuntimeAttributeUISlot>> _actorSlots = new();
        private ObjectPool<RuntimeAttributeUISlot> _pool;
        private string _canvasIdResolved;

        public string CanvasId => _canvasIdResolved;
        public virtual AttributeCanvasType Type => attributeCanvasType;
        public AttributeCanvasInitializationState State { get; private set; }
        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => CanvasId;

        protected virtual void Awake()
        {
            InjectionState = DependencyInjectionState.Pending;
            State = AttributeCanvasInitializationState.Pending;
            SetupCanvasId();

            RuntimeAttributeBootstrapper.Instance.RegisterForInjection(this);
        }

        public virtual void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Injecting;
            State = AttributeCanvasInitializationState.Injecting;

            if (slotPrefab == null)
            {
                DebugUtility.LogError<RuntimeAttributeActorCanvas>($"❌ Slot prefab not assigned for Canvas '{CanvasId}'.");
                State = AttributeCanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
                return;
            }

            dynamicSlotsParent ??= transform;
            InitializePool();

            try
            {
                orchestrator?.RegisterCanvas(this);
                State = AttributeCanvasInitializationState.Ready;
                InjectionState = DependencyInjectionState.Ready;

                DebugUtility.LogVerbose<RuntimeAttributeActorCanvas>(
                    $"✅ Canvas '{CanvasId}' Ready ({Type})",
                    DebugUtility.Colors.CrucialInfo);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RuntimeAttributeActorCanvas>($"❌ Error initializing attributeCanvas '{CanvasId}': {ex}");
                State = AttributeCanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        private void SetupCanvasId()
        {
            if (autoGenerateCanvasId)
            {
                var actor = GetComponentInParent<IActor>();
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

        public virtual bool CanAcceptBinds() => State == AttributeCanvasInitializationState.Ready;

        public virtual void ScheduleBind(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data)
        {
            if (!CanAcceptBinds()) return;
            CreateSlotForActor(actorId, runtimeAttributeType, data);
        }

        private void CreateSlotForActor(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data, RuntimeAttributeInstanceConfig instanceConfig = null)
        {
            if (!_actorSlots.TryGetValue(actorId, out var actorDict))
            {
                actorDict = new Dictionary<RuntimeAttributeType, RuntimeAttributeUISlot>();
                _actorSlots[actorId] = actorDict;
            }

            if (actorDict.TryGetValue(runtimeAttributeType, out var existingSlot) && existingSlot != null)
            {
                existingSlot.Configure(data);
                return;
            }

            var newSlot = _pool.Get();
            try
            {
                instanceConfig ??= ResolveInstanceConfig(actorId, runtimeAttributeType);
                newSlot.InitializeForActorId(actorId, runtimeAttributeType, instanceConfig);
                newSlot.Configure(data);
                actorDict[runtimeAttributeType] = newSlot;

                DebugUtility.LogVerbose<RuntimeAttributeActorCanvas>($"🎨 Bound {actorId}.{runtimeAttributeType} on '{CanvasId}'");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RuntimeAttributeActorCanvas>($"❌ Error creating slot for {actorId}.{runtimeAttributeType}: {ex}");
                _pool.Release(newSlot);
            }
        }

        private RuntimeAttributeInstanceConfig ResolveInstanceConfig(string actorId, RuntimeAttributeType runtimeAttributeType)
        {
            return orchestrator != null && orchestrator.TryGetActorResource(actorId, out var svc)
                ? svc.GetInstanceConfig(runtimeAttributeType)
                : null;
        }

        private void InitializePool()
        {
            _pool = new ObjectPool<RuntimeAttributeUISlot>(
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
                if (RuntimeAttributeCanvasPipelineManager.HasInstance)
                    RuntimeAttributeCanvasPipelineManager.Instance.UnregisterCanvas(CanvasId);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RuntimeAttributeActorCanvas>($"Error during destroy: {ex}");
            }
        }

        public IReadOnlyDictionary<string, Dictionary<RuntimeAttributeType, RuntimeAttributeUISlot>> GetActorSlots() => _actorSlots;
        public bool TryGetSlot(string actorId, RuntimeAttributeType runtimeAttributeType, out RuntimeAttributeUISlot slot)
        {
            slot = null;
            return _actorSlots.TryGetValue(actorId, out var actorDict) &&
                actorDict.TryGetValue(runtimeAttributeType, out slot) && slot != null;
        }
        public int GetActorSlotsCount() => _actorSlots.Count;
    }
}
