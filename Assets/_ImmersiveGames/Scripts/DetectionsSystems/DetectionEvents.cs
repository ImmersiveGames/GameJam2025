using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{

    public class SensorDetectedEvent : IEvent
    {
        public SensorTypes SensorName { get; }
        public IDetectable Planet { get; }
        public IDetector Detector { get; }

        public SensorDetectedEvent(IDetectable planet, IDetector detector, SensorTypes sensorName)
        {
            SensorName = sensorName;
            Planet = planet;
            Detector = detector;
        }
    }

    public class SensorLostEvent : IEvent
    {
        public SensorTypes SensorName { get; }
        public IDetectable Planet { get; }
        public IDetector Detector { get; }

        public SensorLostEvent(IDetectable planet, IDetector detector, SensorTypes sensorName)
        {
            SensorName = sensorName;
            Planet = planet;
            Detector = detector;
        }
    }
}