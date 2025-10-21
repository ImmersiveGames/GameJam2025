using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    
    public class Sensor
    {
        private readonly Collider[] _results = new Collider[5];
        private readonly List<IDetectable> _detected = new();
        private readonly Transform _origin;
        private readonly IDetector _detector;
        
        // Cache por objeto por frame - mais específico
        private readonly Dictionary<IDetectable, int> _enterEventFrameCache = new();
        private readonly Dictionary<IDetectable, int> _exitEventFrameCache = new();
        
        private float _timer;

        public SensorConfig Config { get; }
        private DetectionType DetectionType => Config.DetectionType;
        public ReadOnlyCollection<IDetectable> CurrentlyDetected => _detected.AsReadOnly();
        public bool IsDetecting => _detected.Count > 0;
        public Transform Origin => _origin;

        public Sensor(Transform origin, IDetector detector, SensorConfig config)
        {
            _origin = origin;
            _detector = detector;
            Config = config;

            string mode = config.DetectionMode == SensorDetectionMode.Spherical ? "Esférico" : "Cônico";
            DebugUtility.LogVerbose<Sensor>($"Criado em {origin.name}: Tipo={config.DetectionType?.TypeName}, Modo={mode}, Raio={config.Radius}");
        }

        public void Update(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < Config.MaxFrequency) return;

            List<IDetectable> currentDetections = DetectObjects();
            ProcessDetections(currentDetections);
            _timer = 0f;
        }

        private List<IDetectable> DetectObjects()
        {
            var detected = new List<IDetectable>();
            int hits = Physics.OverlapSphereNonAlloc(_origin.position, Config.Radius, _results, Config.TargetLayer);

            for (int i = 0; i < hits; i++)
            {
                var collider = _results[i];
                var detectable = GetDetectableFromCollider(collider);

                if (detectable == null) continue;
                if (IsSelfOrChild(detectable, _detector)) continue;
                if (Config.DetectionMode == SensorDetectionMode.Conical && !IsInCone(collider.transform.position)) continue;

                if (!detected.Contains(detectable))
                {
                    detected.Add(detectable);
                }
            }

            return detected;
        }

        private bool IsInCone(Vector3 targetPosition)
        {
            var coneWorldDirection = _origin.TransformDirection(Config.ConeDirection);
            var directionToTarget = (targetPosition - _origin.position).normalized;
            float angleToTarget = Vector3.Angle(coneWorldDirection, directionToTarget);
            return angleToTarget <= (Config.ConeAngle / 2f);
        }

        private IDetectable GetDetectableFromCollider(Collider collider)
        {
            var root = collider.transform.root;
            return root.GetComponent<IDetectable>() ?? collider.GetComponent<IDetectable>();
        }

        private bool IsSelfOrChild(IDetectable detectable, IDetector detector)
        {
            if (detectable is MonoBehaviour detectableMono && detector is MonoBehaviour detectorMono)
            {
                return detectableMono.transform == detectorMono.transform ||
                       detectableMono.transform.IsChildOf(detectorMono.transform) ||
                       detectorMono.transform.IsChildOf(detectableMono.transform);
            }
            return detectable.Owner == detector.Owner;
        }

        private void ProcessDetections(List<IDetectable> current)
        {
            int currentFrame = Time.frameCount;

            // Novas detecções
            foreach (var detectable in current.Where(detectable => !_detected.Contains(detectable)))
            {
                // Verificar se já processamos este detectable neste frame
                if (_enterEventFrameCache.TryGetValue(detectable, out int lastFrame) && lastFrame == currentFrame)
                    continue;

                _detected.Add(detectable);
                _enterEventFrameCache[detectable] = currentFrame;

                DebugUtility.LogVerbose<Sensor>($" → NOVA DETECÇÃO: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionEnterEvent>.Raise(new DetectionEnterEvent(detectable, _detector, DetectionType));
            }

            // Detecções perdidas
            for (int i = _detected.Count - 1; i >= 0; i--)
            {
                var detectable = _detected[i];
                if (current.Contains(detectable)) continue;

                // Verificar se já processamos este detectable neste frame
                if (_exitEventFrameCache.TryGetValue(detectable, out int lastFrame) && lastFrame == currentFrame)
                    continue;

                _detected.RemoveAt(i);
                _exitEventFrameCache[detectable] = currentFrame;

                DebugUtility.LogVerbose<Sensor>($" → PERDA DE DETECÇÃO: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionExitEvent>.Raise(new DetectionExitEvent(detectable, _detector, DetectionType));
            }

            // Limpar caches antigos (mais de 1 frame atrás) para evitar memory leak
            CleanupFrameCaches(currentFrame);
        }

        private void CleanupFrameCaches(int currentFrame)
        {
            // Remove entradas com mais de 1 frame de idade
            var oldEnterKeys = _enterEventFrameCache.Where(kvp => kvp.Value < currentFrame - 1)
                                                   .Select(kvp => kvp.Key).ToList();
            var oldExitKeys = _exitEventFrameCache.Where(kvp => kvp.Value < currentFrame - 1)
                                                 .Select(kvp => kvp.Key).ToList();

            foreach (var key in oldEnterKeys)
                _enterEventFrameCache.Remove(key);
            
            foreach (var key in oldExitKeys)
                _exitEventFrameCache.Remove(key);
        }

        private string GetName(object obj)
        {
            return (obj as MonoBehaviour)?.gameObject.name ?? obj.ToString();
        }

        public bool IsDetectingObject(IDetectable detectable) => _detected.Contains(detectable);
        public void ClearDetections() => _detected.Clear();

        private Vector3 GetConeWorldDirection()
        {
            return _origin.TransformDirection(Config.ConeDirection);
        }

        public Vector3[] GetConeEdgeDirections()
        {
            if (Config.DetectionMode != SensorDetectionMode.Conical) return Array.Empty<Vector3>();

            var coneWorldDirection = GetConeWorldDirection();
            float halfAngle = Config.ConeAngle / 2f;
            var leftRotation = Quaternion.AngleAxis(-halfAngle, Vector3.up);
            var rightRotation = Quaternion.AngleAxis(halfAngle, Vector3.up);

            return new[] { leftRotation * coneWorldDirection, rightRotation * coneWorldDirection };
        }

        public Vector3[] GetConeArcPoints(int segments = 16)
        {
            if (Config.DetectionMode != SensorDetectionMode.Conical) return Array.Empty<Vector3>();

            var coneWorldDirection = GetConeWorldDirection();
            float segmentAngle = Config.ConeAngle / segments;
            var points = new List<Vector3>();

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = -Config.ConeAngle / 2 + segmentAngle * i;
                var rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
                var segmentDirection = rotation * coneWorldDirection;
                points.Add(_origin.position + segmentDirection * Config.Radius);
            }

            return points.ToArray();
        }
    }
}