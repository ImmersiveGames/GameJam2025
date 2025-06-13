using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    public class SensorController : MonoBehaviour
    {
        [SerializeField, Tooltip("Configurações para os sensores")]
        private List<SensorConfig> sensorConfigs = new();

        private readonly List<DetectorSense> _sensors = new();
        
        [System.Serializable]
        public class SensorConfig
        {
            public SensorTypes sensorName = SensorTypes.OtherSensor;
            public LayerMask planetLayer;
            public float radius = 10f;
            public float minDetectionFrequency = 0.1f;
            public float maxDetectionFrequency = 0.5f;
            public bool debugMode;
        }
        public DetectionsSystems.SensorConfig GetSensorConfig(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor != null) return sensor.GetConfig();
            DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
            return null;
        }
         private void Awake()
        {
            InitializeSensors();
        }

        private void InitializeSensors()
        {
            foreach (var sensor in sensorConfigs.Select(config => new DetectorSense(
                    transform,
                    config.planetLayer,
                    config.radius,
                    config.minDetectionFrequency,
                    config.maxDetectionFrequency,
                    debugMode: config.debugMode,
                    sensorName: config.sensorName
                )))
            {
                _sensors.Add(sensor);
            }
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            foreach (var sensor in _sensors)
            {
                sensor.Update(Time.deltaTime);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var sensor in _sensors)
            {
                sensor.DrawGizmos();
            }
        }
        
        public ReadOnlyCollection<IDetectable> GetDetectedPlanets(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor != null) return sensor.GetDetectedPlanets();
            DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
            return new List<IDetectable>().AsReadOnly(); // Retorna lista vazia
        }
        
        public bool IsPlanetInSensorRange(IDetectable planet, SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor == null)
            {
                DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
                return false;
            }

            bool isInRange = sensor.IsObjectInRange(planet);
            if (sensor.DebugMode)
            {
                DebugUtility.LogVerbose<SensorController>(
                    $"[{sensorName}] Verificação: Planeta {(planet != null ? planet.Name : "null")} {(isInRange ? "está" : "não está")} no alcance.",
                    isInRange ? "green" : "red"
                );
            }
            return isInRange;
        }
        // Métodos para ativar/desativar sensores por nome
        public void EnableSensor(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            sensor?.EnableSensor();
        }

        public void DisableSensor(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            sensor?.DisableSensor();
        }
    }
}