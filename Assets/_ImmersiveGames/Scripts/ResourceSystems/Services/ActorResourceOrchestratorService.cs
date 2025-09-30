using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    using ResourceSystems;

    /// <summary>
    /// Orchestrator como serviço puro. Registra atores (ResourceSystemService) e canvases (CanvasResourceBinder).
    /// </summary>
    public class ActorResourceOrchestratorService : IActorResourceOrchestrator
    {
        private readonly Dictionary<string, ResourceSystemService> _actors = new();
        private readonly Dictionary<string, CanvasResourceBinder> _canvases = new();
        
        private readonly ICanvasRoutingStrategy _routingStrategy;
        private const string MainUICanvasId = "MainUI";
        
        public IReadOnlyCollection<string> RegisteredActors => _actors.Keys;
        public IReadOnlyCollection<string> RegisteredCanvases => _canvases.Keys;
        
        
        public ActorResourceOrchestratorService(ICanvasRoutingStrategy routingStrategy = null)
        {
            _routingStrategy = routingStrategy ?? new DefaultCanvasRoutingStrategy();
        }
        public void RegisterActor(ResourceSystemService service)
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
            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered actor '{actorId}'");
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
                DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Unregistered canvas '{canvasId}'");
        }

        //public ResourceSystemService GetActorService(string actorId) => _actors.GetValueOrDefault(actorId);
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
            if (config == null) return MainUICanvasId;
            var resolved = _routingStrategy.ResolveCanvasId(config, actorId);
            DebugUtility.LogVerbose<ActorResourceOrchestratorService>($"Resolved target CanvasId '{resolved}' for actor '{actorId}'");
            return resolved;
        }
        

        private void CreateSlotsForActorInCanvas(ResourceSystemService actorSvc, CanvasResourceBinder canvas)
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
    public class DefaultCanvasRoutingStrategy : ICanvasRoutingStrategy
    {
        private const string MainUICanvasId = "MainUI";
        public string ResolveCanvasId(ResourceInstanceConfig config, string actorId)
        {
            if (config == null) return MainUICanvasId;
            return config.canvasTargetMode switch
            {
                CanvasTargetMode.ActorSpecific => $"{actorId}_Canvas",
                CanvasTargetMode.Custom => string.IsNullOrEmpty(config.customCanvasId) ? MainUICanvasId : config.customCanvasId,
                _ => MainUICanvasId
            };
        }
    }
}
