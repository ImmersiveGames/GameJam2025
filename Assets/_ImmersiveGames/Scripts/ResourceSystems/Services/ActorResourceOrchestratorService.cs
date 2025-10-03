using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public interface IActorResourceOrchestrator
    {
        void RegisterActor(ResourceSystem actor);
        void UnregisterActor(string actorId);

        // NOVO: Método para obter ResourceSystem de um ator
        ResourceSystem GetActorResourceSystem(string actorId);
        
        // NOVO: Método para verificar se um ator está registrado
        bool IsActorRegistered(string actorId);
        
        // NOVO: Método para obter todos os ActorIds registrados (para debug)
        IReadOnlyCollection<string> GetRegisteredActorIds();

        void RegisterCanvas(CanvasResourceBinder canvas);
        void UnregisterCanvas(string canvasId);
        bool TryGetActorResource(string actorId, out ResourceSystem resourceSystem);
    }

    /// <summary>
    /// Orchestrator como serviço puro. Registra atores (ResourceSystem) e canvases (CanvasResourceBinder).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class ActorResourceOrchestratorService : IActorResourceOrchestrator
    {
        private readonly Dictionary<string, ResourceSystem> _actors = new();
        private readonly Dictionary<string, CanvasResourceBinder> _canvases = new();
        private readonly Dictionary<string, string> _canvasIdCache = new();

        private readonly ICanvasRoutingStrategy _routingStrategy;
        private const string MainUICanvasId = "MainUI";

        public ActorResourceOrchestratorService(ICanvasRoutingStrategy routingStrategy = null)
        {
            _routingStrategy = routingStrategy ?? new DefaultCanvasRoutingStrategy();
        }

        public void RegisterActor(ResourceSystem service)
        {
            if (service == null) return;
            if (!_actors.TryAdd(service.EntityId, service)) return;
            service.ResourceUpdated += OnResourceUpdated;

            foreach (var canvas in _canvases.Values)
                CreateSlotsForActorInCanvas(service, canvas);

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Registered actor '{service.EntityId}'");
        }

        public void UnregisterActor(string actorId)
        {
            if (!_actors.TryGetValue(actorId, out var svc)) return;
            svc.ResourceUpdated -= OnResourceUpdated;
            _actors.Remove(actorId);
            // Limpar cache para ator
            var keysToRemove = _canvasIdCache.Keys.Where(k => k.StartsWith(actorId)).ToList();
            foreach (var key in keysToRemove) _canvasIdCache.Remove(key);
            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered actor '{actorId}'");
        
        }

        // NOVA IMPLEMENTAÇÃO: Obter ResourceSystem de um ator
        public ResourceSystem GetActorResourceSystem(string actorId)
        {
            _actors.TryGetValue(actorId, out var resourceSystem);
            return resourceSystem;
        }

        // NOVA IMPLEMENTAÇÃO: Verificar se ator está registrado
        public bool IsActorRegistered(string actorId)
        {
            return _actors.ContainsKey(actorId);
        }

        // NOVA IMPLEMENTAÇÃO: Obter todos os ActorIds
        public IReadOnlyCollection<string> GetRegisteredActorIds()
        {
            return _actors.Keys;
        }

        public void RegisterCanvas(CanvasResourceBinder binder)
        {
            if (binder == null) return;
            if (!_canvases.TryAdd(binder.CanvasId, binder)) return;

            // create slots for existing actors that target this canvas
            foreach (var actor in _actors.Values)
                CreateSlotsForActorInCanvas(actor, binder);

            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Registered canvas '{binder.CanvasId}'");
        }

        public void UnregisterCanvas(string canvasId)
        {
            if (_canvases.Remove(canvasId))
            {
                // Limpar cache para canvas
                var keysToRemove = _canvasIdCache.Keys.Where(k => k.EndsWith(canvasId)).ToList();
                foreach (var key in keysToRemove) _canvasIdCache.Remove(key);
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered canvas '{canvasId}'");
            }
        }
        public bool TryGetActorResource(string actorId, out ResourceSystem resourceSystem)
        {
            return _actors.TryGetValue(actorId, out resourceSystem);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_actors.TryGetValue(evt.ActorId, out var actorSvc)) return;

            var instCfg = actorSvc.GetInstanceConfig(evt.ResourceType);
            string targetCanvasId = ResolveTargetCanvasId(instCfg, evt.ActorId);

            if (!string.IsNullOrEmpty(targetCanvasId) && _canvases.TryGetValue(targetCanvasId, out var canvas))
            {
                canvas.UpdateResourceForActor(evt.ActorId, evt.ResourceType, evt.NewValue);
            }
            else
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"Target canvas '{targetCanvasId}' not found for actor '{evt.ActorId}'.");
            }
        }
        
        private string ResolveTargetCanvasId(ResourceInstanceConfig config, string actorId)
        {
            string cacheKey = $"{actorId}_{config?.resourceDefinition.type}";
            if (_canvasIdCache.TryGetValue(cacheKey, out var cachedCanvasId))
            {
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"[Orchestrator] Cached CanvasId '{cachedCanvasId}' for actor '{actorId}'");
                return cachedCanvasId;
            }

            string resolved = config == null ? MainUICanvasId : _routingStrategy.ResolveCanvasId(config, actorId);
            _canvasIdCache[cacheKey] = resolved;
            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Resolved target CanvasId '{resolved}' for actor '{actorId}'");
            return resolved;
        }

        private void CreateSlotsForActorInCanvas(ResourceSystem actorSvc, CanvasResourceBinder canvas)
        {
            if (actorSvc == null || canvas == null) return;

            foreach (var (resourceType, resourceValue) in actorSvc.GetAll())
            {
                var instanceConfig = actorSvc.GetInstanceConfig(resourceType);
                string resolvedTarget = ResolveTargetCanvasId(instanceConfig, actorSvc.EntityId);

                if (resolvedTarget == canvas.CanvasId)
                    canvas.CreateSlotForActor(actorSvc.EntityId, resourceType, resourceValue, instanceConfig);
            }
        }
    }
}