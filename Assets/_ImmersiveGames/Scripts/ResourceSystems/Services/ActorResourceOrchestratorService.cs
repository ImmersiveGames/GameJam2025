using System.Collections.Generic;

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

        public IReadOnlyCollection<string> RegisteredActors => _actors.Keys;
        public IReadOnlyCollection<string> RegisteredCanvases => _canvases.Keys;

        public void RegisterActor(ResourceSystemService service)
        {
            if (service == null) return;
            if (!_actors.TryAdd(service.EntityId, service)) return;

            service.ResourceUpdated += OnResourceUpdated;

            foreach (var canvas in _canvases.Values)
                CreateSlotsForActorInCanvas(service, canvas);
        }

        public void UnregisterActor(string actorId)
        {
            if (!_actors.TryGetValue(actorId, out var svc)) return;
            svc.ResourceUpdated -= OnResourceUpdated;
            _actors.Remove(actorId);
        }

        public ResourceSystemService GetActorService(string actorId) => _actors.GetValueOrDefault(actorId);

        public void RegisterCanvas(CanvasResourceBinder binder)
        {
            if (binder == null) return;
            if (!_canvases.TryAdd(binder.CanvasId, binder)) return;

            foreach (var actor in _actors.Values)
                CreateSlotsForActorInCanvas(actor, binder);
        }

        public void UnregisterCanvas(string canvasId)
        {
            _canvases.Remove(canvasId);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            var actorId = evt.ActorId;
            var resourceType = evt.ResourceType;
            var actorSvc = GetActorService(actorId);
            if (actorSvc == null) return;

            var instanceConfig = actorSvc.GetInstanceConfig(resourceType);
            string targetCanvasId = instanceConfig != null ? instanceConfig.targetCanvasId : "MainUI";

            if (!string.IsNullOrEmpty(targetCanvasId) && _canvases.TryGetValue(targetCanvasId, out var canvas))
            {
                canvas.UpdateResourceForActor(actorId, resourceType, evt.NewValue);
            }
            else
            {
                foreach (var c in _canvases.Values)
                    c.UpdateResourceForActor(actorId, resourceType, evt.NewValue);
            }
        }

        private void CreateSlotsForActorInCanvas(ResourceSystemService actorSvc, CanvasResourceBinder canvas)
        {
            if (actorSvc == null || canvas == null) return;

            foreach (var kv in actorSvc.GetAll())
            {
                var resourceType = kv.Key;
                var instanceConfig = actorSvc.GetInstanceConfig(resourceType);
                string targetCanvasId = instanceConfig != null ? instanceConfig.targetCanvasId : "MainUI";

                if (targetCanvasId == canvas.CanvasId)
                    canvas.CreateSlotForActor(actorSvc.EntityId, resourceType, kv.Value, instanceConfig);
            }
        }
    }
}
