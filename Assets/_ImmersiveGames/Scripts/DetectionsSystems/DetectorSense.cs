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
        private readonly List<IDetectable> _detectedObj = new();
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
                var planet = GetComponentInParent(_detectionResults[i]);
                if (planet is { IsActive: true })
                {
                    planets.Add(planet);
                }
            }

            return planets;
        }

        private IDetectable GetComponentInParent(Component component)
        {
            if (!component) return null;

            if (component.TryGetComponent<IDetectable>(out var obj))
            {
                return obj;
            }

            Transform current = component.transform.parent;
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
            if (obj is not { IsActive: true })
            {
                return false;
            }

            // Verifica se o planeta está dentro do raio usando Physics.OverlapSphere
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
            foreach (var item in currentObj)
            {
                if (_detectedObj.Contains(item)) continue;

                _detectedObj.Add(item);
                EventBus<SensorDetectedEvent>.Raise(new SensorDetectedEvent(item, DetectorEntity, SensorName));
                item.OnDetectableRanged(DetectorEntity, SensorName);
                DetectorEntity?.OnObjectDetected(item, DetectorEntity, SensorName); // Adiciona SensorName
                if (DebugMode)
                {
                    DebugUtility.Log(typeof(DetectorSense),$"[{SensorName}] Planeta detectado: {item.Name}", "green");
                }
            }

            _detectedObj.RemoveAll(obj =>
            {
                if (currentObj.Contains(obj)) return false;

                EventBus<SensorLostEvent>.Raise(new SensorLostEvent(obj, DetectorEntity, SensorName));
                obj.OnDetectableLost(DetectorEntity, SensorName);
                DetectorEntity?.OnPlanetLost(obj, DetectorEntity, SensorName); // Adiciona SensorName
                if (DebugMode)
                {
                    DebugUtility.Log(typeof(DetectorSense),$"[{SensorName}] Planeta perdido: {obj.Name}", "red");
                }
                return true;
            });
        }
        public void ForceImmediateDetection()
        {
            if (!IsEnabled) return;

            ProcessPlanets(DetectPlanets());
            _detectionTimer = 0f; // Reseta o temporizador para manter consistência

            if (DebugMode)
            {
                DebugUtility.Log(typeof(DetectorSense), $"[{SensorName}] Detecção imediata forçada.", "blue");
            }
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

            Gizmos.color = IsEnabled ? (_detectedObj.Count > 0 ? Color.green : Color.yellow) : Color.gray;
            Gizmos.DrawWireSphere(Origin.position, Radius);

            foreach (var planet in _detectedObj)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Origin.position, planet.Transform.position);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(planet.Transform.position + Vector3.up * 0.5f, $"[{SensorName}] Detectado: {planet.Name}");
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