using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{

    public class DetectorService
    {
        private readonly List<Sensor> _sensors = new();

        public DetectorService(Transform origin, IDetector detector, SensorCollection collection)
        {
            DebugUtility.Log<DetectorService>($"Criando servi�o com {collection.Sensors.Count} configura��es em {origin.name}");

            foreach (var config in collection.Sensors)
            {
                if (config == null)
                {
                    DebugUtility.LogError<DetectorService>("SensorConfig nulo encontrado na collection");
                    continue;
                }

                _sensors.Add(new Sensor(origin, detector, config));
            }

            DebugUtility.Log<DetectorService>($"Servi�o criado com {_sensors.Count} sensores ativos em {origin.name}");
        }

        public void Update(float deltaTime)
        {
            foreach (var sensor in _sensors)
            {
                sensor.Update(deltaTime);
            }
        }

        public IReadOnlyList<Sensor> GetSensors() => _sensors;
    }
}
