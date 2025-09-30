using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IActorResourceOrchestrator
    {
        void RegisterActor(ResourceSystemService actorService);
        void UnregisterActor(string actorId);

        void RegisterCanvas(CanvasResourceBinder canvas);
        void UnregisterCanvas(string canvasId);
    }
    public interface ICanvasRoutingStrategy
    {
        string ResolveCanvasId(ResourceInstanceConfig config, string actorId);
    }
}