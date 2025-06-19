using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetector
    {
        IActor Owner { get; }
        void OnObjectDetected(IDetectable detectable, IDetector detectorContext, SensorTypes sensorName); // When planetMaster enters detection range
        void OnPlanetLost(IDetectable planetMaster, IDetector detectorContext, SensorTypes sensorName);    // When planetMaster exits detection range
    }

    // Interface for planets to handle interactions
    public interface IDetectable
    {
        IActor Detectable { get; }
        void OnDetectableRanged(IDetector entity, SensorTypes sensorName); // Called when detected by player/EaterDetectable
        void OnDetectableLost(IDetector entity,SensorTypes sensorName); // Called when detected by player/EaterDetectable
        PlanetResourcesSo GetResource(); // Retrieve planetMaster resources
        PlanetData GetPlanetData();
        PlanetsMaster GetPlanetsMaster(); // Get the PlanetsMaster component
    }
    
    public enum SensorTypes
    {
        PlayerDetectorSensor,
        PlayerRecognizerSensor,
        EaterDetectorSensor,
        EaterEatSensor,
        OtherSensor
    }
}