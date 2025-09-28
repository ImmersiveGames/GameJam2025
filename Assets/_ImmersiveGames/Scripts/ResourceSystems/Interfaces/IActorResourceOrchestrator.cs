using System.Collections.Generic;
namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    public interface IActorResourceOrchestrator
    {
        void RegisterActor(ResourceSystemService service);
        void UnregisterActor(string actorId);
        void RegisterCanvas(CanvasResourceBinder binder);
        void UnregisterCanvas(string canvasId);
        ResourceSystemService GetActorService(string actorId);
        
        IReadOnlyCollection<string> RegisteredActors { get; }
        IReadOnlyCollection<string> RegisteredCanvases { get; }
    }
}