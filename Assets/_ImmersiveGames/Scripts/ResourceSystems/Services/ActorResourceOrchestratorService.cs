using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public interface IActorResourceOrchestrator
    {
        void RegisterActor(ResourceSystem actor);
        void UnregisterActor(string actorId);
        ResourceSystem GetActorResourceSystem(string actorId);
        bool IsActorRegistered(string actorId);
        IReadOnlyCollection<string> GetRegisteredActorIds();
        bool TryGetActorResource(string actorId, out ResourceSystem resourceSystem);

        void RegisterCanvas(ICanvasBinder canvas);
        void UnregisterCanvas(string canvasId);

        bool IsCanvasRegisteredForActor(string actorId);
        void ProcessPendingFirstUpdatesForCanvas(string canvasId);
    }

    public class ActorResourceOrchestratorService : IActorResourceOrchestrator, IInjectableComponent
    {
        private readonly Dictionary<string, ResourceSystem> _actors = new();
        private readonly Dictionary<string, ICanvasBinder> _canvases = new();

        private readonly Dictionary<(string actorId, ResourceType resourceType), string> _canvasIdCache = new();
        private readonly Dictionary<string, Dictionary<string, ResourceUpdateEvent>> _pendingFirstUpdates = new();
        private readonly HashSet<(string actorId, ResourceType resourceType)> _processedFirstUpdates = new();

        private readonly ICanvasRoutingStrategy _routingStrategy;
        private const string MainUICanvasId = "MainUI";

        [Inject] private CanvasPipelineManager _pipelineManager;

        public DependencyInjectionState InjectionState { get; set; }

        public ActorResourceOrchestratorService(ICanvasRoutingStrategy routingStrategy = null)
        {
            _routingStrategy = routingStrategy ?? new DefaultCanvasRoutingStrategy();
        }

        public string GetObjectId() => "ActorResourceOrchestratorService";

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            DebugUtility.Log<ActorResourceOrchestratorService>(
                "✅ Orchestrator Service fully initialized with dependencies",
                DebugUtility.Colors.CrucialInfo);

            // Se já existirem Canvas Binders na cena, registrar automaticamente.
            var existingBinders = UnityEngine.Object.FindObjectsByType<InjectableCanvasResourceBinder>(
                UnityEngine.FindObjectsInactive.Include,
                UnityEngine.FindObjectsSortMode.None
            );
            foreach (var binder in existingBinders)
            {
                try
                {
                    RegisterCanvas(binder);
                    DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"🖼️ Auto-registered canvas '{binder.CanvasId}' on orchestrator initialization");
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Failed auto-registering canvas {binder.CanvasId}: {ex}");
                }
            }
        }

        public void RegisterActor(ResourceSystem service)
        {
            if (service == null) return;

            if (!_actors.TryAdd(service.EntityId, service))
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Actor '{service.EntityId}' already registered");
                return;
            }

            service.ResourceUpdated += OnResourceUpdated;

            CreateInitialSlotsForActor(service);

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Registered actor '{service.EntityId}'");

            ProcessPendingFirstUpdatesForActor(service.EntityId);

            // Notify hub (compat) that an actor exists
            ResourceEventHub.NotifyActorRegistered(service.EntityId);
        }

        private void CreateInitialSlotsForActor(ResourceSystem actorSvc)
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
                        DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"✅ Immediate initial slot for {actorSvc.EntityId}.{resourceType}");
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

        private void CacheInitialSlotCreation(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            if (!_pendingFirstUpdates.ContainsKey(targetCanvasId))
            {
                _pendingFirstUpdates[targetCanvasId] = new Dictionary<string, ResourceUpdateEvent>();
            }

            var evt = new ResourceUpdateEvent(actorId, resourceType, data);
            _pendingFirstUpdates[targetCanvasId][actorId] = evt;

            // Registrar também no hub para compatibilidade global
            try
            {
                var bindRequest = new ResourceEventHub.CanvasBindRequest(actorId, resourceType, data, targetCanvasId);
                ResourceEventHub.RegisterPendingBind(bindRequest);
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"📦 (Hub) Registered pending bind for {actorId}.{resourceType} -> {targetCanvasId}");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Failed registering pending bind in hub for {actorId}.{resourceType}: {ex}");
            }

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"📦 Cached initial slot for {actorId}.{resourceType} -> {targetCanvasId}");
        }

        public void RegisterCanvas(ICanvasBinder canvas)
        {
            if (canvas == null) return;

            if (!_canvases.TryAdd(canvas.CanvasId, canvas))
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Canvas '{canvas.CanvasId}' already registered");
                return;
            }

            DebugUtility.Log<ActorResourceOrchestratorService>(
                $"✅ Canvas '{canvas.CanvasId}' registered",
                DebugUtility.Colors.Success);

            ProcessPendingFirstUpdatesForCanvas(canvas.CanvasId);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            var cacheKey = (evt.ActorId, evt.ResourceType);
            string targetCanvasId = _canvasIdCache.GetValueOrDefault(cacheKey) ?? ResolveTargetCanvasId(null, evt.ActorId, evt.ResourceType);

            if (string.IsNullOrEmpty(targetCanvasId)) return;

            if (IsCanvasReady(targetCanvasId))
            {
                ScheduleBindForActor(evt.ActorId, evt.ResourceType, evt.NewValue, targetCanvasId);
            }
            else
            {
                CachePendingFirstUpdate(evt, targetCanvasId);
            }
        }

        private void CachePendingFirstUpdate(ResourceUpdateEvent evt, string targetCanvasId)
        {
            if (!_pendingFirstUpdates.TryGetValue(targetCanvasId, out var canvasUpdates))
            {
                canvasUpdates = new Dictionary<string, ResourceUpdateEvent>();
                _pendingFirstUpdates[targetCanvasId] = canvasUpdates;
            }

            string key = $"{evt.ActorId}_{evt.ResourceType}";
            canvasUpdates[key] = evt;

            // Registrar no hub para pendentes
            try
            {
                var bindRequest = new ResourceEventHub.CanvasBindRequest(evt.ActorId, evt.ResourceType, evt.NewValue, targetCanvasId);
                ResourceEventHub.RegisterPendingBind(bindRequest);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Failed registering pending first update in hub for {evt.ActorId}.{evt.ResourceType}: {ex}");
            }

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"📦 Cached first update for {evt.ActorId}.{evt.ResourceType} -> {targetCanvasId}");
        }

        private void ScheduleBindForActor(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            var request = new ResourceEventHub.CanvasBindRequest(actorId, resourceType, data, targetCanvasId);

            // Event-driven: publish and let the pipeline handles execution (and hub keeps pendentes)
            EventBus<ResourceEventHub.CanvasBindRequest>.Raise(request);
        }

        public void ProcessPendingFirstUpdatesForCanvas(string canvasId)
        {
            if (_pendingFirstUpdates.TryGetValue(canvasId, out Dictionary<string, ResourceUpdateEvent> actorEvents))
            {
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"🔄 Processing {actorEvents.Count} pending updates for canvas '{canvasId}'");

                foreach ((string actorId, var evt) in actorEvents.ToList())
                {
                    if (_actors.ContainsKey(actorId))
                    {
                        ScheduleBindForActor(evt.ActorId, evt.ResourceType, evt.NewValue, canvasId);
                        actorEvents.Remove(actorId);
                        DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"✅ Processed cached update: {evt.ActorId}.{evt.ResourceType}");
                    }
                }

                if (actorEvents.Count == 0)
                {
                    _pendingFirstUpdates.Remove(canvasId);
                }
            }
        }

        private void ProcessPendingFirstUpdatesForActor(string actorId)
        {
            var canvasesToProcess = _pendingFirstUpdates.Keys.Where(canvasId =>
                _pendingFirstUpdates[canvasId].ContainsKey(actorId)
            ).ToList();

            foreach (string canvasId in canvasesToProcess)
            {
                ProcessPendingFirstUpdatesForCanvas(canvasId);
            }
        }

        private string ResolveTargetCanvasId(ResourceInstanceConfig config, string actorId, ResourceType resourceType)
        {
            var cacheKey = (actorId, resourceType);

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

            foreach (Dictionary<string, ResourceUpdateEvent> canvasUpdates in _pendingFirstUpdates.Values)
            {
                canvasUpdates.Remove(actorId);
            }

            var emptyCanvases = _pendingFirstUpdates.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (string canvasId in emptyCanvases)
            {
                _pendingFirstUpdates.Remove(canvasId);
            }

            var processedToRemove = _processedFirstUpdates.Where(k => k.actorId == actorId).ToList();
            foreach (var key in processedToRemove)
            {
                _processedFirstUpdates.Remove(key);
            }

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered actor '{actorId}'");
        }

        public ResourceSystem GetActorResourceSystem(string actorId) => _actors.GetValueOrDefault(actorId);

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
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered canvas '{canvasId}'");
            }
        }

        public bool TryGetActorResource(string actorId, out ResourceSystem resourceSystem) =>
            _actors.TryGetValue(actorId, out resourceSystem);

        public bool IsCanvasRegistered(string canvasId) => _canvases.ContainsKey(canvasId);

        public IReadOnlyCollection<string> GetRegisteredCanvasIds() => _canvases.Keys;
    }
}
