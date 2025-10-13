using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public interface IActorResourceOrchestrator
    {
        void RegisterActor(ResourceSystem actor);
        void UnregisterActor(string actorId);
        ResourceSystem GetActorResourceSystem(string actorId);
        bool IsActorRegistered(string actorId);
        IReadOnlyCollection<string> GetRegisteredActorIds();
        bool TryGetActorResource(string actorId, out ResourceSystem resourceSystem);

        // CORREÇÃO: Usar ICanvasBinder em vez de CanvasResourceBinder
        void RegisterCanvas(ICanvasBinder canvas);
        void UnregisterCanvas(string canvasId);
    
        // NOVOS: Métodos para canvas
        bool IsCanvasRegistered(string canvasId);
        IReadOnlyCollection<string> GetRegisteredCanvasIds();
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
            DebugUtility.LogVerbose<ActorResourceOrchestratorService>("✅ Orchestrator Service fully initialized with dependencies");
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

            // NOVO: Registrar também no hub para garantir que o pipeline global tenha o bind pendente
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

            ProcessPendingFirstUpdatesForCanvas(canvas.CanvasId);
            CreateSlotsForCanvas(canvas);

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Registered canvas '{canvas.CanvasId}'");
        }

        private void CreateSlotsForCanvas(ICanvasBinder canvas)
        {
            if (canvas == null) return;

            foreach (var actor in _actors.Values)
            {
                foreach (var (resourceType, resourceValue) in actor.GetAll())
                {
                    var instanceConfig = actor.GetInstanceConfig(resourceType);
                    string targetCanvasId = ResolveTargetCanvasId(instanceConfig, actor.EntityId, resourceType);

                    if (targetCanvasId == canvas.CanvasId)
                    {
                        ScheduleBindForActor(actor.EntityId, resourceType, resourceValue, targetCanvasId);
                    }
                }
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_actors.TryGetValue(evt.ActorId, out var actorSvc)) return;

            var instCfg = actorSvc.GetInstanceConfig(evt.ResourceType);
            string targetCanvasId = ResolveTargetCanvasId(instCfg, evt.ActorId, evt.ResourceType);

            if (string.IsNullOrEmpty(targetCanvasId)) return;

            var firstUpdateKey = (evt.ActorId, evt.ResourceType);

            if (_processedFirstUpdates.Add(firstUpdateKey))
            {
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"🎯 FIRST UPDATE for {evt.ActorId}.{evt.ResourceType}");

                if (!IsCanvasReady(targetCanvasId))
                {
                    CacheFirstUpdate(evt, targetCanvasId);
                    return;
                }
            }

            // Always schedule bind for updates
            ScheduleBindForActor(evt.ActorId, evt.ResourceType, evt.NewValue, targetCanvasId);
        }

        private void CacheFirstUpdate(ResourceUpdateEvent evt, string targetCanvasId)
        {
            if (!_pendingFirstUpdates.ContainsKey(targetCanvasId))
            {
                _pendingFirstUpdates[targetCanvasId] = new Dictionary<string, ResourceUpdateEvent>();
            }
    
            _pendingFirstUpdates[targetCanvasId][evt.ActorId] = evt;

            // NOVO: registrar no hub
            try
            {
                var request = new ResourceEventHub.CanvasBindRequest(evt.ActorId, evt.ResourceType, evt.NewValue, targetCanvasId);
                ResourceEventHub.RegisterPendingBind(request);
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"📦 (Hub) Registered pending first update for {evt.ActorId}.{evt.ResourceType} -> {targetCanvasId}");
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

            // Publicar no EventBus (princípio: event-driven).
            // O CanvasPipelineManager está registrado como listener no EventBus e irá tentar processar imediatamente,
            // mantendo compatibilidade com a pipeline sem necessidade de chamada direta duplicada.
            EventBus<ResourceEventHub.CanvasBindRequest>.Raise(request);

            // NOTE: Removida a chamada direta a _pipelineManager.ScheduleBind(...) para evitar duplicação
            // (o pipeline é listener do EventBus e fará o TryExecuteBind).
        }

        private void ProcessPendingFirstUpdatesForCanvas(string canvasId)
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
        public void DebugActorRegistration(string actorId)
        {
            Debug.Log($"[Orchestrator] 🎭 DEBUG ACTOR REGISTRATION: {actorId}");
            Debug.Log($"- Is Registered: {_actors.ContainsKey(actorId)}");
            Debug.Log($"- Total Actors: {_actors.Count}");

            if (_actors.TryGetValue(actorId, out var resourceSystem))
            {
                Debug.Log($"- ResourceSystem: {resourceSystem != null}");
                Debug.Log($"- EntityId: {resourceSystem?.EntityId}");

                var health = resourceSystem?.Get(ResourceType.Health);
                Debug.Log($"- Health: {health?.GetCurrentValue():F1}/{health?.GetMaxValue():F1}");
            }
            else
            {
                Debug.Log($"- Actor NOT found in _actors dictionary");
                Debug.Log($"- Available actors: {string.Join(", ", _actors.Keys)}");
            }
        }
    }

}