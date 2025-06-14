using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    // Interface for entities that can detect planets (Player, EaterDetectable)
    public interface IDetector
    {
        void OnObjectDetected(IDetectable planetMaster, IDetector detectorContext, SensorTypes sensorName); // When planetMaster enters detection range
        void OnPlanetLost(IDetectable planetMaster, IDetector detectorContext, SensorTypes sensorName);    // When planetMaster exits detection range
    }

    // Interface for planets to handle interactions
    public interface IDetectable
    {
        bool IsActive { get; set; } // Indicates if the planet is active
        Transform Transform { get; }
        string Name { get; } // Planet name
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