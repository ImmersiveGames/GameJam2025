using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public class DetectorsMaster : ActorMaster, IDetector
    {
        private readonly Dictionary<SensorTypes, List<IDetectable>> _detectedPlanets = new();
        private PlanetsManager _planetsManager;
        protected override void Awake()
        {
            base.Awake();
            _planetsManager = PlanetsManager.Instance;
        }
        public override void Reset()
        {
            _detectedPlanets.Clear();
        }
        public virtual void OnObjectDetected(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;

            if (!_detectedPlanets.ContainsKey(sensorName))
            {
                _detectedPlanets[sensorName] = new List<IDetectable>();
            }

            if (!_detectedPlanets[sensorName].Contains(interactable))
            {
                _detectedPlanets[sensorName].Add(interactable);
                DebugUtility.LogVerbose<DetectorsMaster>($"Planeta detectado por {sensorName}: {interactable.Name}", "green");
                if (_planetsManager.IsMarkedPlanet(interactable))
                {
                    DebugUtility.LogVerbose<DetectorsMaster>($"Planeta marcado detectado por {sensorName}: {interactable.Name}", "yellow");
                }
            }
        }
        public virtual void OnPlanetLost(IDetectable interactable, IDetector detector,SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;

            if (_detectedPlanets.ContainsKey(sensorName) && _detectedPlanets[sensorName].Remove(interactable))
            {
                DebugUtility.LogVerbose<DetectorsMaster>($"Planeta perdido por {sensorName}: {interactable.Name}", "red");
                if (!_detectedPlanets[sensorName].Any())
                {
                    _detectedPlanets.Remove(sensorName);
                }
            }
        }
        public IReadOnlyList<IDetectable> GetPlanetsBySensor(SensorTypes sensorName)
        {
            return _detectedPlanets.TryGetValue(sensorName, out var planets)
                ? planets.AsReadOnly()
                : new List<IDetectable>().AsReadOnly();
        }

        public IReadOnlyList<IDetectable> GetAllDetectedPlanets()
        {
            return _detectedPlanets.Values
                .SelectMany(p => p)
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }
}