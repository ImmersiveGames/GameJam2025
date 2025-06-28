using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    public class SensorDetectedEvent : IEvent
    {
        public SensorTypes SensorName { get; }
        public IDetectable Planet { get; }
        public IDetector Owner { get; }

        public SensorDetectedEvent(IDetectable planet, IDetector owner, SensorTypes sensorName)
        {
            SensorName = sensorName;
            Planet = planet;
            Owner = owner;
        }
    }

    public class SensorLostEvent : IEvent
    {
        public SensorTypes SensorName { get; }
        public IDetectable Planet { get; }
        public IDetector Owner { get; }

        public SensorLostEvent(IDetectable planet, IDetector owner, SensorTypes sensorName)
        {
            SensorName = sensorName;
            Planet = planet;
            Owner = owner;
        }
    }
}