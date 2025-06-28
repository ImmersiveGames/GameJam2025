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
    
    public class PlanetConsumedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetConsumedEvent(IDetectable detected)
        {
            Detected = detected;
        }
    }
    
}