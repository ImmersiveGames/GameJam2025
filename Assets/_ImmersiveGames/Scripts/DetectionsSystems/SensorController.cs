using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class SensorController : MonoBehaviour
    {
        [SerializeField] [Tooltip("Configurações para os sensores")]
        private List<SensorConfig> sensorConfigs = new();

        private readonly List<DetectorSense> _sensors = new();
        
        private Dictionary<(SensorTypes, IDetectable), bool> _rangeCheckCache = new();
        private int _lastFrameChecked = -1;

        [Serializable]
        public class SensorConfig
        {
            public SensorTypes sensorName = SensorTypes.OtherSensor;
            public LayerMask planetLayer;
            public float radius = 10f;
            public float minDetectionFrequency = 0.1f;
            public float maxDetectionFrequency = 0.5f;
            public bool debugMode;
        }

        private void Awake()
        {
            InitializeSensors();
        }

        private void InitializeSensors()
        {
            // Obter o IDetector (DetectorController) do Actor
            var detector = GetComponent<IDetector>();
            if (detector == null)
            {
                DebugUtility.LogError<SensorController>($"IDetector não encontrado em '{gameObject.name}'. SensorController desativado.", this);
                enabled = false;
                return;
            }

            foreach (var sensor in sensorConfigs.Select(config => new DetectorSense(
                    transform,
                    config.planetLayer,
                    config.radius,
                    config.minDetectionFrequency,
                    config.maxDetectionFrequency,
                    config.debugMode,
                    config.sensorName
                )))
            {
                _sensors.Add(sensor);
            }
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            // Limpa o cache se for um novo frame
            if (Time.frameCount != _lastFrameChecked)
            {
                _rangeCheckCache.Clear();
                _lastFrameChecked = Time.frameCount;
            }
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

        public DetectionsSystems.SensorConfig GetSensorConfig(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor != null) return sensor.GetConfig();
            DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
            return null;
        }

        public ReadOnlyCollection<IDetectable> GetDetectedSensor(SensorTypes sensorName)
        {
            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor != null) return sensor.GetDetectedSensors();
            DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
            return new List<IDetectable>().AsReadOnly();
        }

        public bool IsObjectInSensorRange(IDetectable obj, SensorTypes sensorName)
        {
            var cacheKey = (sensorName, obj);
            // Verifica se já temos um resultado em cache para este frame
            if (_rangeCheckCache.TryGetValue(cacheKey, out bool cachedResult))
            {
                if (sensorName == SensorTypes.EaterEatSensor && obj != null)
                {
                    DebugUtility.LogVerbose<SensorController>(
                        $"[{sensorName}] Usando cache: Planeta {obj.Detectable.Name} {(cachedResult ? "está" : "não está")} no alcance.",
                        cachedResult ? "green" : "red"
                    );
                }
                return cachedResult;
            }

            var sensor = _sensors.Find(s => s.SensorName == sensorName);
            if (sensor == null)
            {
                DebugUtility.LogWarning<SensorController>($"Sensor com nome '{sensorName}' não encontrado.");
                return false;
            }

            if (!sensor.IsEnabled)
            {
                if (sensor.DebugMode)
                {
                    DebugUtility.LogVerbose<SensorController>(
                        $"[{sensorName}] Sensor desativado, ignorando verificação de alcance para {(obj != null ? obj.Detectable.Name : "null")}.",
                        "gray"
                    );
                }
                return false;
            }

            bool isInRange = sensor.IsObjectInRange(obj);
            // Armazena o resultado no cache
            _rangeCheckCache[cacheKey] = isInRange;

            if (sensor.DebugMode)
            {
                DebugUtility.LogVerbose<SensorController>(
                    $"[{sensorName}] Verificação: Planeta {(obj != null ? obj.Detectable.Name : "null")} {(isInRange ? "está" : "não está")} no alcance.",
                    isInRange ? "green" : "red"
                );
            }
            return isInRange;
        }

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

        public void DisableAllSensors()
        {
            foreach (var sensor in Enum.GetValues(typeof(SensorTypes)).Cast<SensorTypes>())
            {
                DisableSensor(sensor);
            }
        }

        public void EnableAllSensors()
        {
            foreach (var sensor in Enum.GetValues(typeof(SensorTypes)).Cast<SensorTypes>())
            {
                EnableSensor(sensor);
            }
        }
    }
}