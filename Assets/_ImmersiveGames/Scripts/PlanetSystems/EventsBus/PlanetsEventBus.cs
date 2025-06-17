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
        public IDetectable Detectable { get; }

        public PlanetCreatedEvent(IDetectable detected)
        {
            Detectable = detected;
        }
    }
    
    public class PlanetConsumedEvent : IEvent
    {
        public IDetectable Detectable { get; }
        //Todo: implementar quem consumiu para diferenciar o evento de consumo de planeta
        public PlanetConsumedEvent(IDetectable detected)
        {
            Detectable = detected;
        }
    }
    
    public class PlanetDetectedEvent
    {
        public IDetector Detector { get; }
        public SensorTypes Sensor { get; }

        public PlanetDetectedEvent(IDetector detector, SensorTypes sensor)
        {
            Detector = detector;
            Sensor = sensor;
        }
    }
    
}