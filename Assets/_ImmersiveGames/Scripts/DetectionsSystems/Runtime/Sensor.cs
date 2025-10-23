using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime.Internal;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    
    public class Sensor
    {
        private Collider[] _results;
        private readonly DetectionSet _detected = new();
        private readonly List<IDetectable> _currentDetections = new(); // Lista temporária reutilizada para armazenar detecções no frame atual
        private readonly HashSet<IDetectable> _currentDetectionsLookup = new(); // Lookup para evitar Contains O(n)
        private readonly Transform _origin;
        private readonly IDetector _detector;
        private readonly FrameEventCache _enterEventFrameCache = new();
        private readonly FrameEventCache _exitEventFrameCache = new();
        private bool _hasLoggedColliderResize;
        
        private float _timer;

        public SensorConfig Config { get; }
        private DetectionType DetectionType => Config.DetectionType;
        public ReadOnlyCollection<IDetectable> CurrentlyDetected => _detected.ReadOnlyItems;
        public bool IsDetecting => _detected.Count > 0;
        public Transform Origin => _origin;

        public Sensor(Transform origin, IDetector detector, SensorConfig config)
        {
            _origin = origin;
            _detector = detector;
            Config = config;
            _results = new Collider[Mathf.Max(1, config.MaxColliders)];

            string mode = config.DetectionMode == SensorDetectionMode.Spherical ? "Esférico" : "Cônico";
            DebugUtility.LogVerbose<Sensor>($"Criado em {origin.name}: Tipo={config.DetectionType?.TypeName}, Modo={mode}, Raio={config.Radius}");
        }

        public void Update(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < Config.MaxFrequency) return;

            DetectObjects();
            ProcessDetections();
            _timer = 0f;
        }

        private void DetectObjects()
        {
            _currentDetections.Clear();
            _currentDetectionsLookup.Clear();

            int hits = CaptureColliders();

            for (int i = 0; i < hits; i++)
            {
                var collider = _results[i];
                if (collider == null) continue;

                var detectable = GetDetectableFromCollider(collider);

                if (detectable == null) continue;
                if (IsSelfOrChild(detectable, _detector)) continue;
                if (Config.DetectionMode == SensorDetectionMode.Conical && !IsInCone(collider.transform.position)) continue;

                if (_currentDetectionsLookup.Add(detectable))
                {
                    _currentDetections.Add(detectable);
                }
            }
        }

        private int CaptureColliders()
        {
            int hits = Physics.OverlapSphereNonAlloc(_origin.position, Config.Radius, _results, Config.TargetLayer);

            while (hits == _results.Length)
            {
                ExpandResultsBuffer();
                hits = Physics.OverlapSphereNonAlloc(_origin.position, Config.Radius, _results, Config.TargetLayer);
            }

            return hits;
        }

        private void ExpandResultsBuffer()
        {
            int previousLength = _results.Length;
            int newSize = Mathf.Max(previousLength * 2, previousLength + 1);
            Array.Resize(ref _results, newSize);

            if (!_hasLoggedColliderResize)
            {
                DebugUtility.LogWarning<Sensor>($"Buffer de colisores expandido de {previousLength} para {newSize} em {GetName(_detector)}. Ajuste o campo MaxColliders no SensorConfig para otimizar.");
                _hasLoggedColliderResize = true;
            }
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

        private void ProcessDetections()
        {
            int currentFrame = Time.frameCount;

            // Novas detecções
            for (int i = 0; i < _currentDetections.Count; i++)
            {
                var detectable = _currentDetections[i];
                if (_detected.Contains(detectable)) continue;
                if (!_enterEventFrameCache.TryRegister(detectable, currentFrame)) continue;

                _detected.Add(detectable);

                DebugUtility.LogVerbose<Sensor>($" → NOVA DETECÇÃO: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionEnterEvent>.Raise(new DetectionEnterEvent(detectable, _detector, DetectionType));
            }

            // Detecções perdidas
            for (int i = _detected.Count - 1; i >= 0; i--)
            {
                var detectable = _detected[i];
                if (_currentDetectionsLookup.Contains(detectable)) continue;
                if (!_exitEventFrameCache.TryRegister(detectable, currentFrame)) continue;

                var removed = _detected.RemoveAt(i);

                DebugUtility.LogVerbose<Sensor>($" → PERDA DE DETECÇÃO: {GetName(removed)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionExitEvent>.Raise(new DetectionExitEvent(removed, _detector, DetectionType));
            }

            // Limpar caches antigos (mais de 1 frame atrás) para evitar memory leak
            _enterEventFrameCache.Cleanup(currentFrame);
            _exitEventFrameCache.Cleanup(currentFrame);
        }

        private string GetName(object obj)
        {
            return (obj as MonoBehaviour)?.gameObject.name ?? obj.ToString();
        }

        public bool IsDetectingObject(IDetectable detectable) => _detected.Contains(detectable);

        public void ClearDetections()
        {
            _detected.Clear();
            _enterEventFrameCache.Clear();
            _exitEventFrameCache.Clear();
        }

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