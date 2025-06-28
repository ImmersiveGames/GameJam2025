using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems.EventsBus
{
    public class PlanetMarkedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetMarkedEvent(IDetectable detected) => Detected = detected;
    }

    public class PlanetUnmarkedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetUnmarkedEvent(IDetectable detected) => Detected = detected;
    }
    public class PlanetCreatedEvent : IEvent
    {
        public IDetectable Detected { get; }

        public PlanetCreatedEvent(IDetectable detected)
        {
            Detected = detected;
        }
    }

    public class PlanetDestroyedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public IActor ByActor { get; }
        public PlanetDestroyedEvent(IDetectable detected, IActor byActor)
        {
            Detected = detected;
            ByActor = byActor;
        }
    }
    
}