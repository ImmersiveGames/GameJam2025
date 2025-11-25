using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    
    public class DetectorService
    {
        private readonly List<Sensor> _sensors = new();

        public DetectorService(Transform origin, IDetector detector, SensorCollection collection)
        {
            DebugUtility.Log<DetectorService>($"Criando serviço com {collection.Sensors.Count} configurações em {origin.name}");

            foreach (var config in collection.Sensors)
            {
                if (config == null)
                {
                    DebugUtility.LogError<DetectorService>("SensorConfig nulo encontrado na collection");
                    continue;
                }

                _sensors.Add(new Sensor(origin, detector, config));
            }

            DebugUtility.Log<DetectorService>($"Serviço criado com {_sensors.Count} sensores ativos em {origin.name}");
        }

        public void Update(float deltaTime)
        {
            foreach (var sensor in _sensors)
            {
                sensor.Update(deltaTime);
            }
        }

        public IReadOnlyList<Sensor> GetSensors() => _sensors;

        public bool IsAnySensorDetecting() => _sensors.Any(sensor => sensor.IsDetecting);

        public int GetTotalDetections() => _sensors.Sum(sensor => sensor.CurrentlyDetected.Count);

        public bool TryGetSensor(DetectionType detectionType, out Sensor sensor)
        {
            sensor = null;

            if (detectionType == null)
            {
                return false;
            }

            for (int i = 0; i < _sensors.Count; i++)
            {
                Sensor candidate = _sensors[i];
                if (candidate?.Config?.DetectionType == null)
                {
                    continue;
                }

                if (candidate.Config.DetectionType == detectionType)
                {
                    sensor = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}