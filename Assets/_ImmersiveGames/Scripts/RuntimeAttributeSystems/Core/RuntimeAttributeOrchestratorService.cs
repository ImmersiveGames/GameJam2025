using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core.Events;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Binders;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core
{
    public interface IRuntimeAttributeOrchestrator
    {
        void RegisterActor(RuntimeAttributeContext actor);
        void UnregisterActor(string actorId);
        RuntimeAttributeContext GetActorResourceSystem(string actorId);
        bool IsActorRegistered(string actorId);
        IReadOnlyCollection<string> GetRegisteredActorIds();
        bool TryGetActorResource(string actorId, out RuntimeAttributeContext runtimeAttributeContext);

        void RegisterCanvas(IRuntimeAttributeCanvasBinder attributeCanvas);
        void UnregisterCanvas(string canvasId);

        bool IsCanvasRegisteredForActor(string actorId);
        void ProcessPendingFirstUpdatesForCanvas(string canvasId);
    }

    public class RuntimeAttributeOrchestratorService : IRuntimeAttributeOrchestrator, IInjectableComponent
    {
        private readonly Dictionary<string, RuntimeAttributeContext> _actors = new();
        private readonly Dictionary<string, IRuntimeAttributeCanvasBinder> _canvases = new();

        private readonly Dictionary<(string actorId, RuntimeAttributeType resourceType), string> _canvasIdCache = new();
        private readonly Dictionary<string, Dictionary<(string actorId, RuntimeAttributeType resourceType), RuntimeAttributeUpdateEvent>> _pendingFirstUpdates = new();
        private readonly HashSet<(string actorId, RuntimeAttributeType resourceType)> _processedFirstUpdates = new();

        private readonly IRuntimeAttributeCanvasRoutingStrategy _routingStrategy;
        private const string MainUICanvasId = "MainUI";

        [Inject] private RuntimeAttributeCanvasPipelineManager _pipelineManager;

        public DependencyInjectionState InjectionState { get; set; }

        public RuntimeAttributeOrchestratorService(IRuntimeAttributeCanvasRoutingStrategy routingStrategy = null)
        {
            _routingStrategy = routingStrategy ?? new RuntimeAttributeCanvasRoutingStrategy();
        }

        public string GetObjectId() => "RuntimeAttributeOrchestratorService";

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>(
                "✅ Orchestrator Service fully initialized with dependencies",
                DebugUtility.Colors.CrucialInfo);

            // Se já existirem Canvas Binders na cena, registrar automaticamente.
            var existingBinders = UnityEngine.Object.FindObjectsByType<RuntimeAttributeActorCanvas>(
                UnityEngine.FindObjectsInactive.Include,
                UnityEngine.FindObjectsSortMode.None
            );
            foreach (var binder in existingBinders)
            {
                try
                {
                    RegisterCanvas(binder);
                    DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"🖼️ Auto-registered attributeCanvas '{binder.CanvasId}' on orchestrator initialization");
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<RuntimeAttributeOrchestratorService>($"Failed auto-registering attributeCanvas {binder.CanvasId}: {ex}");
                }
            }
        }

        public void RegisterActor(RuntimeAttributeContext service)
        {
            if (service == null) return;

            if (!_actors.TryAdd(service.EntityId, service))
            {
                DebugUtility.LogWarning<RuntimeAttributeOrchestratorService>($"Actor '{service.EntityId}' already registered");
                return;
            }

            service.ResourceUpdated += OnResourceUpdated;

            CreateInitialSlotsForActor(service);

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"Registered actor '{service.EntityId}'");

            ProcessPendingFirstUpdatesForActor(service.EntityId);

            // Notify hub (compat) that an actor exists
            RuntimeAttributeEventHub.NotifyActorRegistered(service.EntityId);
        }

        private void CreateInitialSlotsForActor(RuntimeAttributeContext actorSvc)
        {
            if (actorSvc == null) return;

            foreach (var (resourceType, resourceValue) in actorSvc.GetAll())
            {
                var instanceConfig = actorSvc.GetInstanceConfig(resourceType);
                string targetCanvasId = ResolveTargetCanvasId(instanceConfig, actorSvc.EntityId, resourceType);

                if (!string.IsNullOrEmpty(targetCanvasId))
                {
                    if (IsCanvasReady(targetCanvasId))
                    {
                        ScheduleBindForActor(actorSvc.EntityId, resourceType, resourceValue, targetCanvasId);
                        DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"✅ Immediate initial slot for {actorSvc.EntityId}.{resourceType}");
                    }
                    else
                    {
                        CacheInitialSlotCreation(actorSvc.EntityId, resourceType, resourceValue, targetCanvasId);
                    }
                }
            }
        }

        private bool IsCanvasReady(string canvasId)
        {
            return _canvases.ContainsKey(canvasId) && _canvases[canvasId].CanAcceptBinds();
        }

        private void CacheInitialSlotCreation(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data, string targetCanvasId)
        {
            if (!_pendingFirstUpdates.TryGetValue(targetCanvasId, out var canvasEvents))
            {
                canvasEvents = new Dictionary<(string actorId, RuntimeAttributeType resourceType), RuntimeAttributeUpdateEvent>();
                _pendingFirstUpdates[targetCanvasId] = canvasEvents;
            }

            var evt = new RuntimeAttributeUpdateEvent(actorId, runtimeAttributeType, data);
            canvasEvents[(actorId, runtimeAttributeType)] = evt;

            // Registrar também no hub para compatibilidade global
            try
            {
                var bindRequest = new RuntimeAttributeEventHub.CanvasBindRequest(actorId, runtimeAttributeType, data, targetCanvasId);
                RuntimeAttributeEventHub.RegisterPendingBind(bindRequest);
                DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"📦 (Hub) Registered pending bind for {actorId}.{runtimeAttributeType} -> {targetCanvasId}");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<RuntimeAttributeOrchestratorService>($"Failed registering pending bind in hub for {actorId}.{runtimeAttributeType}: {ex}");
            }

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"📦 Cached initial slot for {actorId}.{runtimeAttributeType} -> {targetCanvasId}");
        }

        public void RegisterCanvas(IRuntimeAttributeCanvasBinder attributeCanvas)
        {
            if (attributeCanvas == null) return;

            if (!_canvases.TryAdd(attributeCanvas.CanvasId, attributeCanvas))
            {
                DebugUtility.LogWarning<RuntimeAttributeOrchestratorService>($"Canvas '{attributeCanvas.CanvasId}' already registered");
                return;
            }

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>(
                $"✅ Canvas '{attributeCanvas.CanvasId}' registered",
                DebugUtility.Colors.Success);

            ProcessPendingFirstUpdatesForCanvas(attributeCanvas.CanvasId);
        }

        private void OnResourceUpdated(RuntimeAttributeUpdateEvent evt)
        {
            var cacheKey = (evt.ActorId, ResourceType: evt.RuntimeAttributeType);
            string targetCanvasId = _canvasIdCache.GetValueOrDefault(cacheKey) ?? ResolveTargetCanvasId(null, evt.ActorId, evt.RuntimeAttributeType);

            if (string.IsNullOrEmpty(targetCanvasId)) return;

            if (IsCanvasReady(targetCanvasId))
            {
                ScheduleBindForActor(evt.ActorId, evt.RuntimeAttributeType, evt.NewValue, targetCanvasId);
            }
            else
            {
                CachePendingFirstUpdate(evt, targetCanvasId);
            }
        }

        private void CachePendingFirstUpdate(RuntimeAttributeUpdateEvent evt, string targetCanvasId)
        {
            if (!_pendingFirstUpdates.TryGetValue(targetCanvasId, out var canvasUpdates))
            {
                canvasUpdates = new Dictionary<(string actorId, RuntimeAttributeType resourceType), RuntimeAttributeUpdateEvent>();
                _pendingFirstUpdates[targetCanvasId] = canvasUpdates;
            }

            var key = (evt.ActorId, ResourceType: evt.RuntimeAttributeType);
            canvasUpdates[key] = evt;

            // Registrar no hub para pendentes
            try
            {
                var bindRequest = new RuntimeAttributeEventHub.CanvasBindRequest(evt.ActorId, evt.RuntimeAttributeType, evt.NewValue, targetCanvasId);
                RuntimeAttributeEventHub.RegisterPendingBind(bindRequest);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<RuntimeAttributeOrchestratorService>($"Failed registering pending first update in hub for {evt.ActorId}.{evt.RuntimeAttributeType}: {ex}");
            }

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"📦 Cached first update for {evt.ActorId}.{evt.RuntimeAttributeType} -> {targetCanvasId}");
        }

        private void ScheduleBindForActor(string actorId, RuntimeAttributeType runtimeAttributeType, IRuntimeAttributeValue data, string targetCanvasId)
        {
            var request = new RuntimeAttributeEventHub.CanvasBindRequest(actorId, runtimeAttributeType, data, targetCanvasId);

            // Event-driven: publish and let the pipeline handles execution (and hub keeps pendentes)
            EventBus<RuntimeAttributeEventHub.CanvasBindRequest>.Raise(request);
        }

        public void ProcessPendingFirstUpdatesForCanvas(string canvasId)
        {
            if (!_pendingFirstUpdates.TryGetValue(canvasId, out var actorEvents))
                return;

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"🔄 Processing {actorEvents.Count} pending updates for attributeCanvas '{canvasId}'");

            foreach (var (key, evt) in actorEvents.ToList())
            {
                if (_actors.ContainsKey(key.actorId))
                {
                    ScheduleBindForActor(evt.ActorId, evt.RuntimeAttributeType, evt.NewValue, canvasId);
                    actorEvents.Remove(key);
                    DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"✅ Processed cached update: {evt.ActorId}.{evt.RuntimeAttributeType}");
                }
            }

            if (actorEvents.Count == 0)
            {
                _pendingFirstUpdates.Remove(canvasId);
            }
        }

        private void ProcessPendingFirstUpdatesForActor(string actorId)
        {
            var canvasesToProcess = _pendingFirstUpdates
                .Where(pair => pair.Value.Keys.Any(key => key.actorId == actorId))
                .Select(pair => pair.Key)
                .ToList();

            foreach (string canvasId in canvasesToProcess)
            {
                ProcessPendingFirstUpdatesForCanvas(canvasId);
            }
        }

        private string ResolveTargetCanvasId(RuntimeAttributeInstanceConfig config, string actorId, RuntimeAttributeType runtimeAttributeType)
        {
            var cacheKey = (actorId, resourceType: runtimeAttributeType);

            if (_canvasIdCache.TryGetValue(cacheKey, out string cachedCanvasId))
                return cachedCanvasId;

            string resolved = config == null ?
                MainUICanvasId :
                _routingStrategy.ResolveCanvasId(config, actorId);

            _canvasIdCache[cacheKey] = resolved;
            return resolved;
        }

        public void UnregisterActor(string actorId)
        {
            if (!_actors.TryGetValue(actorId, out var svc)) return;

            svc.ResourceUpdated -= OnResourceUpdated;
            _actors.Remove(actorId);

            var keysToRemove = _canvasIdCache.Keys.Where(k => k.actorId == actorId).ToList();
            foreach (var key in keysToRemove)
                _canvasIdCache.Remove(key);

            foreach (var (canvasId, canvasUpdates) in _pendingFirstUpdates.ToList())
            {
                var actorKeysToRemove = canvasUpdates.Keys.Where(key => key.actorId == actorId).ToList();
                foreach (var key in actorKeysToRemove)
                {
                    canvasUpdates.Remove(key);
                }

                if (canvasUpdates.Count == 0)
                {
                    _pendingFirstUpdates.Remove(canvasId);
                }
            }

            var processedToRemove = _processedFirstUpdates.Where(k => k.actorId == actorId).ToList();
            foreach (var key in processedToRemove)
            {
                _processedFirstUpdates.Remove(key);
            }

            DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"Unregistered actor '{actorId}'");
        }

        public RuntimeAttributeContext GetActorResourceSystem(string actorId) => _actors.GetValueOrDefault(actorId);

        public bool IsActorRegistered(string actorId) => _actors.ContainsKey(actorId);

        public IReadOnlyCollection<string> GetRegisteredActorIds() => _actors.Keys;

        public bool IsCanvasRegisteredForActor(string actorId)
        {
            return _canvases.Values.Any(canvas => canvas.GetActorSlots().ContainsKey(actorId));
        }

        public void UnregisterCanvas(string canvasId)
        {
            if (_canvases.Remove(canvasId))
            {
                _pendingFirstUpdates.Remove(canvasId);
                DebugUtility.LogVerbose<RuntimeAttributeOrchestratorService>($"Unregistered attributeCanvas '{canvasId}'");
            }
        }

        public bool TryGetActorResource(string actorId, out RuntimeAttributeContext runtimeAttributeContext) =>
            _actors.TryGetValue(actorId, out runtimeAttributeContext);

        public bool IsCanvasRegistered(string canvasId) => _canvases.ContainsKey(canvasId);

        public IReadOnlyCollection<string> GetRegisteredCanvasIds() => _canvases.Keys;
    }
}
