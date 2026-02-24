using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    
    public class Sensor
    {
        private readonly Collider[] _results = new Collider[5];
        private readonly List<IDetectable> _detected = new();
        private readonly List<IDetectable> _currentDetections = new(); // Lista temporária reutilizada para armazenar detecções no frame atual
        private readonly List<IDetectable> _cleanupBuffer = new(); // Buffer reaproveitado para evitar GC por frame
        private readonly Transform _origin;
        private readonly IDetector _detector;
        
        // Cache por objeto por frame - mais específico
        private readonly Dictionary<IDetectable, int> _enterEventFrameCache = new();
        private readonly Dictionary<IDetectable, int> _exitEventFrameCache = new();
        
        private float _timer;
        private bool _isEnabled = true;

        public SensorConfig Config { get; }
        private DetectionType DetectionType => Config.DetectionType;
        public ReadOnlyCollection<IDetectable> CurrentlyDetected => _detected.AsReadOnly();
        public bool IsDetecting => _detected.Count > 0;
        public bool IsEnabled => _isEnabled;
        public Transform Origin => _origin;

        public Sensor(Transform origin, IDetector detector, SensorConfig config)
        {
            _origin = origin;
            _detector = detector;
            Config = config;

            string mode = config.DetectionMode == SensorDetectionMode.Spherical ? "Esférico" : "Cônico";
            DebugUtility.Log<Sensor>($"Criado em {origin.name}: Tipo={config.DetectionType?.TypeName}, Modo={mode}, Raio={config.Radius}");
        }

        public void Update(float deltaTime)
        {
            if (!_isEnabled)
            {
                return;
            }

            _timer += deltaTime;
            if (_timer < Config.MaxFrequency) return;

            DetectObjects();
            ProcessDetections(_currentDetections);
            _timer = 0f;
        }

        public void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled)
            {
                return;
            }

            _isEnabled = enabled;

            if (!enabled)
            {
                ForceClearDetections();
                _timer = 0f;
                return;
            }

            // Força uma atualização rápida assim que o sensor volta a ficar ativo.
            _timer = Config.MaxFrequency;
        }

        private void DetectObjects()
        {
            _currentDetections.Clear();
            DetectUsingPhysics();
            DetectUsingRegistry();
        }

        private void DetectUsingPhysics()
        {
            int hits = Physics.OverlapSphereNonAlloc(_origin.position, Config.Radius, _results, Config.TargetLayer);

            for (int i = 0; i < hits; i++)
            {
                var collider = _results[i];
                var detectable = GetDetectableFromCollider(collider);

                if (detectable == null) continue;
                if (IsSelfOrChild(detectable, _detector)) continue;
                if (Config.DetectionMode == SensorDetectionMode.Conical && !IsInCone(collider.transform.position)) continue;

                if (!_currentDetections.Contains(detectable))
                {
                    _currentDetections.Add(detectable);
                }
            }
        }

        private void DetectUsingRegistry()
        {
            IReadOnlyCollection<IDetectable> detectables = DetectableRegistry.GetByType(DetectionType);
            if (detectables.Count == 0)
            {
                return;
            }

            foreach (var detectable in detectables)
            {
                if (ShouldSkipDetectable(detectable)) continue;
                if (!TryGetDetectableMono(detectable, out var detectableMono)) continue;
                if (!IsMonoEligible(detectableMono)) continue;

                var targetPosition = GetDetectablePosition(detectable, detectableMono);
                if (!IsWithinDetectionVolume(targetPosition)) continue;

                _currentDetections.Add(detectable);
            }
        }

        private bool ShouldSkipDetectable(IDetectable detectable)
        {
            if (detectable == null) return true;
            if (_currentDetections.Contains(detectable)) return true;
            if (IsSelfOrChild(detectable, _detector)) return true;
            return false;
        }

        private bool TryGetDetectableMono(IDetectable detectable, out MonoBehaviour detectableMono)
        {
            detectableMono = detectable as MonoBehaviour;
            return detectableMono != null;
        }

        private bool IsMonoEligible(MonoBehaviour detectableMono)
        {
            if (!detectableMono.isActiveAndEnabled || !detectableMono.gameObject.activeInHierarchy) return false;
            if (!MatchesLayer(detectableMono.gameObject.layer)) return false;
            return true;
        }

        private bool IsWithinDetectionVolume(Vector3 targetPosition)
        {
            if (!IsWithinRadius(targetPosition)) return false;
            if (Config.DetectionMode == SensorDetectionMode.Conical && !IsInCone(targetPosition)) return false;
            return true;
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
            if (collider == null) return null;

            // Utiliza a hierarquia completa porque os colliders residem em filhos dos detectables.
            return collider.GetComponentInParent<IDetectable>();
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

        private bool MatchesLayer(int objectLayer)
        {
            int targetMask = Config.TargetLayer;
            int layerMask = 1 << objectLayer;
            return (targetMask & layerMask) != 0;
        }

        private bool IsWithinRadius(Vector3 targetPosition)
        {
            float maxDistanceSqr = Config.Radius * Config.Radius;
            float distanceSqr = (_origin.position - targetPosition).sqrMagnitude;
            return distanceSqr <= maxDistanceSqr;
        }

        private Vector3 GetDetectablePosition(IDetectable detectable, MonoBehaviour detectableMono)
        {
            // Utiliza o Transform do ator quando disponível para manter consistência com o sistema de atores.
            return detectable.Owner?.Transform != null
                ? detectable.Owner.Transform.position
                : detectableMono.transform.position;
        }

        private void ProcessDetections(List<IDetectable> current)
        {
            int currentFrame = Time.frameCount;

            HandleNewDetections(current, currentFrame);
            HandleLostDetections(current, currentFrame);

            // Limpar caches antigos (mais de 1 frame atrás) para evitar memory leak
            CleanupFrameCaches(currentFrame);
        }

        private void HandleNewDetections(List<IDetectable> current, int currentFrame)
        {
            foreach (var detectable in current.Where(detectable => !_detected.Contains(detectable)).Where(detectable => !AlreadyProcessedThisFrame(_enterEventFrameCache, detectable, currentFrame)))
            {
                _detected.Add(detectable);
                _enterEventFrameCache[detectable] = currentFrame;

                DebugUtility.LogVerbose<Sensor>($" ? NOVA DETECÇÃO: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionEnterEvent>.Raise(new DetectionEnterEvent(detectable, _detector, DetectionType));
            }
        }

        private void HandleLostDetections(List<IDetectable> current, int currentFrame)
        {
            for (int i = _detected.Count - 1; i >= 0; i--)
            {
                var detectable = _detected[i];
                if (current.Contains(detectable)) continue;
                if (AlreadyProcessedThisFrame(_exitEventFrameCache, detectable, currentFrame)) continue;

                _detected.RemoveAt(i);
                _exitEventFrameCache[detectable] = currentFrame;

                DebugUtility.LogVerbose<Sensor>($" ? PERDA DE DETECÇÃO: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                // APENAS EventBus
                EventBus<DetectionExitEvent>.Raise(new DetectionExitEvent(detectable, _detector, DetectionType));
            }
        }

        private static bool AlreadyProcessedThisFrame(Dictionary<IDetectable, int> cache, IDetectable detectable, int currentFrame)
        {
            return cache.TryGetValue(detectable, out int lastFrame) && lastFrame == currentFrame;
        }

        private void ForceClearDetections()
        {
            if (_detected.Count == 0)
            {
                return;
            }

            int currentFrame = Time.frameCount;

            for (int i = _detected.Count - 1; i >= 0; i--)
            {
                var detectable = _detected[i];
                _detected.RemoveAt(i);
                _exitEventFrameCache[detectable] = currentFrame;

                DebugUtility.LogVerbose<Sensor>(
                    $" ? LIMPEZA MANUAL: {GetName(detectable)} por {GetName(_detector)} [Frame {currentFrame}]");

                EventBus<DetectionExitEvent>.Raise(new DetectionExitEvent(detectable, _detector, DetectionType));
            }

            CleanupFrameCaches(currentFrame);
            _currentDetections.Clear();
        }

        private void CleanupFrameCaches(int currentFrame)
        {
            // Remove entradas com mais de 1 frame de idade
            _cleanupBuffer.Clear();

            foreach (KeyValuePair<IDetectable, int> kvp in _enterEventFrameCache)
            {
                if (kvp.Value < currentFrame - 1)
                {
                    _cleanupBuffer.Add(kvp.Key);
                }
            }

            foreach (var key in _cleanupBuffer)
                _enterEventFrameCache.Remove(key);

            _cleanupBuffer.Clear();

            foreach (KeyValuePair<IDetectable, int> kvp in _exitEventFrameCache)
            {
                if (kvp.Value < currentFrame - 1)
                {
                    _cleanupBuffer.Add(kvp.Key);
                }
            }

            foreach (var key in _cleanupBuffer)
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
