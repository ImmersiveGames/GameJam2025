using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.SpawnSystems.EventBus;

namespace _ImmersiveGames.Scripts.DetectionsSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DetectorSense
    {
        private const int MaxDetectionResults = 10;
        private readonly Collider[] _detectionResults = new Collider[MaxDetectionResults];
        private readonly List<IDetectable> _detectedObj = new();
        private float _detectionTimer;

        private LayerMask PlanetLayer { get; }
        private float Radius { get; }
        private float MinDetectionFrequency { get; }
        private float MaxDetectionFrequency { get; }
        public bool DebugMode { get; }
        private Transform Origin { get; }
        private float CurrentDetectionFrequency { get; set; }
        public bool IsEnabled { get; set; }
        private IDetector DetectorEntity { get; }
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
            if (DetectorEntity != null) return;
            DebugUtility.LogError<DetectorSense>($"IDetector não encontrado no Actor {Origin.name}.");
            IsEnabled = false;
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
            var detected = new List<IDetectable>();
            int hitCount = Physics.OverlapSphereNonAlloc(Origin.position, Radius, _detectionResults, PlanetLayer);
            for (int i = 0; i < hitCount; i++)
            {
                var detectable = GetComponentInParent(_detectionResults[i]);
                if (detectable is { Detectable: { IsActive: true } })
                {
                    detected.Add(detectable);
                }
            }
            return detected;
        }

        private IDetectable GetComponentInParent(Component component)
        {
            if (!component) return null;

            if (component.TryGetComponent<IDetectable>(out var obj))
            {
                return obj;
            }

            var current = component.transform.parent;
            while (current)
            {
                if (current.TryGetComponent(out obj))
                {
                    return obj;
                }
                current = current.parent;
            }

            return null;
        }

        public bool IsObjectInRange(IDetectable obj)
        {
            if (obj is { Detectable: { IsActive: false } })
            {
                return false;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(Origin.position, Radius, _detectionResults, PlanetLayer);
            for (int i = 0; i < hitCount; i++)
            {
                var detectable = GetComponentInParent(_detectionResults[i]);
                if (ReferenceEquals(detectable, obj))
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessPlanets(List<IDetectable> objs)
        {
            UpdateDetectedObj(objs);
            AdjustDetectionFrequency(_detectedObj.Count);
        }

        private void UpdateDetectedObj(List<IDetectable> currentObj)
        {
            foreach (var item in currentObj.Where(item => !_detectedObj.Contains(item)))
            {
                _detectedObj.Add(item);
                SensorFilteredEventBus.RaiseFiltered(new SensorDetectedEvent(item, DetectorEntity, SensorName));
                item.OnDetectableRanged(DetectorEntity, SensorName);
                DetectorEntity?.OnObjectDetected(item, DetectorEntity, SensorName);
            }

            _detectedObj.RemoveAll(obj => {
                if (currentObj.Contains(obj)) return false;

                SensorFilteredEventBus.RaiseFiltered(new SensorLostEvent(obj, DetectorEntity, SensorName));
                obj.OnDetectableLost(DetectorEntity, SensorName);
                DetectorEntity?.OnPlanetLost(obj, DetectorEntity, SensorName);
                return true;
            });
        }

        public void ForceImmediateDetection()
        {
            if (!IsEnabled) return;

            ProcessPlanets(DetectPlanets());
            _detectionTimer = 0f;
        }

        private void AdjustDetectionFrequency(int objCount)
        {
            CurrentDetectionFrequency = objCount > 0
                ? Mathf.Lerp(MaxDetectionFrequency, MinDetectionFrequency, objCount / 5f)
                : MaxDetectionFrequency;
        }

        public void EnableSensor()
        {
            if (IsEnabled) return;
            IsEnabled = true;
            _detectionTimer = 0f;
            DebugUtility.LogVerbose<DetectorSense>($"[{SensorName}] Sensor ativado.");
        }

        public void DisableSensor()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            DebugUtility.LogVerbose<DetectorSense>($"[{SensorName}] Sensor desativado.");
        }

        public void DrawGizmos()
        {
            if (!DebugMode || !Origin) return;

            Gizmos.color = IsEnabled ? _detectedObj.Count > 0 ? Color.green : Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(Origin.position, Radius);

            foreach (var planet in _detectedObj)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Origin.position, planet.Detectable.Transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.Detectable.Transform.position + Vector3.up * 0.5f, $"[{SensorName}] Detectado: {planet.Detectable.Name}");
#endif
            }
        }

        public ReadOnlyCollection<IDetectable> GetDetectedSensors()
        {
            return _detectedObj.AsReadOnly();
        }

        public SensorConfig GetConfig()
        {
            return new SensorConfig
            {
                SensorName = SensorName,
                DetectLayer = PlanetLayer,
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
        public LayerMask DetectLayer { get; set; }
        public float Radius { get; set; }
        public float MinDetectionFrequency { get; set; }
        public float MaxDetectionFrequency { get; set; }
        public bool DebugMode { get; set; }
    }
}