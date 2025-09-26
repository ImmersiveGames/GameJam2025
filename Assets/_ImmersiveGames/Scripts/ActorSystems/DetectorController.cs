using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class DetectorController
    {
        private readonly Dictionary<SensorTypes, List<IDetectable>> _detectedPlanets = new();

        private IDetector Owner { get; }

        public DetectorController(IDetector owner = null)
        {
            Owner = owner;
        }
        public void OnObjectDetected(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, Owner)) return;

            if (!_detectedPlanets.ContainsKey(sensorName))
            {
                _detectedPlanets[sensorName] = new List<IDetectable>();
            }

            if (_detectedPlanets[sensorName].Contains(interactable)) return;
            _detectedPlanets[sensorName].Add(interactable);
            DebugUtility.LogVerbose<DetectorController>($"Planeta detectado por {sensorName}: {interactable.Detectable.ActorName}", "green");
        }
        public void OnPlanetLost(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, Owner)) return;

            if (!_detectedPlanets.ContainsKey(sensorName) || !_detectedPlanets[sensorName].Remove(interactable)) return;
            DebugUtility.LogVerbose<DetectorController>($"Planeta perdido por {sensorName}: {interactable.Detectable.ActorName}", "red");
            if (!_detectedPlanets[sensorName].Any())
            {
                _detectedPlanets.Remove(sensorName);
            }
        }
        public IReadOnlyList<IDetectable> GetPlanetsBySensor(SensorTypes sensorName)
        {
            return _detectedPlanets.TryGetValue(sensorName, out List<IDetectable> planets)
                ? planets.AsReadOnly()
                : new List<IDetectable>(0).AsReadOnly();
        }

        public IReadOnlyList<IDetectable> GetAllDetectedPlanets()
        {
            return _detectedPlanets.Values
                .SelectMany(p => p)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }

        public void Reset()
        {
            _detectedPlanets.Clear();
        }
    }
}