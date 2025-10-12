using System.Collections.Generic;
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
}