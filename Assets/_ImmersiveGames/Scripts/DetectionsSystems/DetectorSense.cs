using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    public sealed class DetectorSense
    {
        private const int MaxDetectionResults = 10;
        private readonly Collider[] _detectionResults = new Collider[MaxDetectionResults];
        private readonly List<IDetectable> _detectedPlanets = new();
        private float _detectionTimer;

        public LayerMask PlanetLayer { get; }
        public float Radius { get; }
        public float MinDetectionFrequency { get; }
        public float MaxDetectionFrequency { get; }
        public bool DebugMode { get; }
        public Transform Origin { get; }
        public float CurrentDetectionFrequency { get; private set; }
        public bool IsEnabled { get; private set; }
        public IDetector DetectorEntity { get; }
        public SensorTypes SensorName { get; }

        public DetectorSense(Transform origin, LayerMask planetLayer, float radius = 10f,
            float minDetectionFrequency = 0.1f, float maxDetectionFrequency = 0.5f,
            bool debugMode = false, SensorTypes sensorName = SensorTypes.OtherSensor)
        {
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            PlanetLayer = planetLayer;
            Radius = radius;
            MinDetectionFrequency = minDetectionFrequency;
            MaxDetectionFrequency = maxDetectionFrequency;
            DebugMode = debugMode;
            SensorName = sensorName;
            CurrentDetectionFrequency = maxDetectionFrequency;
            IsEnabled = true;

            DetectorEntity = Origin.GetComponent<IDetector>();
            if (DetectorEntity == null)
            {
                Debug.LogError($"IDetector não encontrado no GameObject {Origin.name}.");
                IsEnabled = false;
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            _detectionTimer += deltaTime;
            if (_detectionTimer < CurrentDetectionFrequency) return;

            ProcessPlanets(DetectPlanets());
            _detectionTimer = 0f;
        }

        private List<IDetectable> DetectPlanets()
        {
            var planets = new List<IDetectable>();
            int hitCount = Physics.OverlapSphereNonAlloc(Origin.position, Radius, _detectionResults, PlanetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var planet = GetPlanetsMasterInParent(_detectionResults[i]);
                if (planet is { IsActive: true })
                {
                    planets.Add(planet);
                }
            }

            return planets;
        }

        private IDetectable GetPlanetsMasterInParent(Component component)
        {
            if (!component) return null;

            if (component.TryGetComponent<IDetectable>(out var planet))
            {
                return planet;
            }

            Transform current = component.transform.parent;
            while (current)
            {
                if (current.TryGetComponent(out planet))
                {
                    return planet;
                }
                current = current.parent;
            }

            return null;
        }
        public bool IsPlanetInRange(IDetectable planet)
        {
            if (!IsEnabled || planet is not { IsActive: true })
            {
                return false;
            }

            // Verifica se o planeta está dentro do raio usando Physics.OverlapSphere
            int hitCount = Physics.OverlapSphereNonAlloc(Origin.position, Radius, _detectionResults, PlanetLayer);
            for (int i = 0; i < hitCount; i++)
            {
                var detectedPlanet = GetPlanetsMasterInParent(_detectionResults[i]);
                if (ReferenceEquals(detectedPlanet, planet))
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessPlanets(List<IDetectable> planets)
        {
            UpdateDetectedPlanets(planets);
            AdjustDetectionFrequency(_detectedPlanets.Count);
        }

        private void UpdateDetectedPlanets(List<IDetectable> currentPlanets)
        {
            foreach (var planet in currentPlanets)
            {
                if (_detectedPlanets.Contains(planet)) continue;

                _detectedPlanets.Add(planet);
                EventBus<SensorDetectedEvent>.Raise(new SensorDetectedEvent(planet, DetectorEntity, SensorName));
                planet.OnDetectableRanged(DetectorEntity, SensorName);
                DetectorEntity?.OnObjectDetected(planet, DetectorEntity, SensorName); // Adiciona SensorName
                if (DebugMode)
                {
                    DebugUtility.Log(typeof(DetectorSense),$"[{SensorName}] Planeta detectado: {planet.Name}", "green");
                }
            }

            _detectedPlanets.RemoveAll(planet =>
            {
                if (currentPlanets.Contains(planet)) return false;

                EventBus<SensorLostEvent>.Raise(new SensorLostEvent(planet, DetectorEntity, SensorName));
                planet.OnDetectableLost(DetectorEntity, SensorName);
                DetectorEntity?.OnPlanetLost(planet, DetectorEntity, SensorName); // Adiciona SensorName
                if (DebugMode)
                {
                    DebugUtility.Log(typeof(DetectorSense),$"[{SensorName}] Planeta perdido: {planet.Name}", "red");
                }
                return true;
            });
        }

        private void AdjustDetectionFrequency(int planetCount)
        {
            CurrentDetectionFrequency = planetCount > 0
                ? Mathf.Lerp(MaxDetectionFrequency, MinDetectionFrequency, planetCount / 5f)
                : MaxDetectionFrequency;
        }

        public void EnableSensor()
        {
            if (IsEnabled) return;
            IsEnabled = true;
            _detectionTimer = 0f;
            if (DebugMode)
            {
                Debug.Log($"[{SensorName}] Sensor ativado.");
            }
        }

        public void DisableSensor()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            if (DebugMode)
            {
                Debug.Log($"[{SensorName}] Sensor desativado.");
            }
        }

        public void DrawGizmos()
        {
            if (!DebugMode || !Origin) return;

            Gizmos.color = IsEnabled ? (_detectedPlanets.Count > 0 ? Color.green : Color.yellow) : Color.gray;
            Gizmos.DrawWireSphere(Origin.position, Radius);

            foreach (var planet in _detectedPlanets)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Origin.position, planet.Transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.Transform.position + Vector3.up * 0.5f, $"[{SensorName}] Detectado: {planet.Name}");
#endif
            }
        }

        public ReadOnlyCollection<IDetectable> GetDetectedPlanets()
        {
            return _detectedPlanets.AsReadOnly();
        }

        public SensorConfig GetConfig()
        {
            return new SensorConfig
            {
                SensorName = SensorName,
                PlanetLayer = PlanetLayer,
                Radius = Radius,
                MinDetectionFrequency = MinDetectionFrequency,
                MaxDetectionFrequency = MaxDetectionFrequency,
                DebugMode = DebugMode
            };
        }
    }

    public class SensorConfig
    {
        public SensorTypes SensorName { get; set; }
        public LayerMask PlanetLayer { get; set; }
        public float Radius { get; set; }
        public float MinDetectionFrequency { get; set; }
        public float MaxDetectionFrequency { get; set; }
        public bool DebugMode { get; set; }
        
        public Color DebugColor { get; set; }
    }
}