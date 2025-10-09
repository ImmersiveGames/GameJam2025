#if UNITY_EDITOR
using _ImmersiveGames.Scripts.DetectionsSystems.Runtime;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Mono
{
    [ExecuteInEditMode]
    public class SensorDebugVisualizer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showDebugRays;
        [SerializeField] private SensorController sensorController;

        [Header("Visual Settings")]
        [SerializeField] private bool showDetectionLines = true;
        [SerializeField] private bool showObjectMarkers = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField, Range(4, 12)] private int coneSegments = 6;

        // Cache variables
        private Vector3[] _cachedConePoints;
        private int _cachedSegments = -1;
        private float _cachedConeAngle = -1;
        private float _cachedRadius = -1;
        private Vector3 _cachedPosition;
        private Quaternion _cachedRotation;

        private void OnValidate()
        {
            sensorController ??= GetComponent<SensorController>();
        }

        private void Update()
        {
            if (showDebugRays && Application.isPlaying) DrawDebugRays();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || sensorController?.Collection == null) return;

            var collection = sensorController.Collection;
            if (Application.isPlaying && sensorController.Service != null)
            {
                DrawSensorGizmosRuntime(sensorController.Service);
            }
            else
            {
                DrawSensorGizmosEditor(collection);
            }
        }

        private void DrawDebugRays()
        {
            foreach (var sensor in sensorController.Service.GetSensors())
            {
                if (!sensor.Config.DebugMode) continue;

                var color = sensor.IsDetecting ? sensor.Config.DetectingColor : sensor.Config.IdleColor;
                if (sensor.Config.DetectionMode == SensorDetectionMode.Spherical)
                {
                    Debug.DrawRay(sensor.Origin.position, sensor.Origin.forward * sensor.Config.Radius, color, 0.1f);
                }
                else
                {
                    Vector3[] edgeDirections = sensor.GetConeEdgeDirections();
                    if (edgeDirections.Length == 2)
                    {
                        Debug.DrawRay(sensor.Origin.position, edgeDirections[0] * sensor.Config.Radius, color, 0.1f);
                        Debug.DrawRay(sensor.Origin.position, edgeDirections[1] * sensor.Config.Radius, color, 0.1f);
                    }
                }
            }
        }

        private void DrawSensorGizmosEditor(SensorCollection collection)
        {
            foreach (var config in collection.Sensors)
            {
                if (config?.DebugMode == true)
                {
                    DrawSingleSensorGizmo(config, config.IdleColor, false, false);
                }
            }
        }

        private void DrawSensorGizmosRuntime(DetectorService service)
        {
            foreach (var sensor in service.GetSensors())
            {
                if (!sensor.Config.DebugMode) continue;

                bool isDetecting = sensor.IsDetecting;
                var gizmoColor = isDetecting ? sensor.Config.DetectingColor : sensor.Config.IdleColor;
                DrawSingleSensorGizmo(sensor.Config, gizmoColor, isDetecting, true);

                if (isDetecting && showDetectionLines)
                {
                    DrawDetectionLines(sensor);
                }
            }
        }

        private void DrawSingleSensorGizmo(SensorConfig config, Color color, bool isDetecting, bool showInfo)
        {
            Gizmos.color = color;

            if (config.DetectionMode == SensorDetectionMode.Spherical)
            {
                Gizmos.DrawWireSphere(sensorController.transform.position, config.Radius);
            }
            else
            {
                DrawConeGizmo(config, color);
            }

            if (isDetecting)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
                Gizmos.DrawSphere(sensorController.transform.position, 0.15f);
            }

            if (showInfo && showLabels)
            {
                string status = isDetecting ? $"{GetDetectionCountForConfig(config, sensorController.Service)}" : "-";
                UnityEditor.Handles.Label(
                    sensorController.transform.position + Vector3.up * (config.Radius + 0.2f),
                    $"{config.DetectionType?.TypeName}\n{status}"
                );
            }
        }

        private int GetDetectionCountForConfig(SensorConfig config, DetectorService service)
        {
            if (service == null) return 0;

            foreach (var sensor in service.GetSensors())
            {
                if (sensor.Config == config) return sensor.CurrentlyDetected.Count;
            }
            return 0;
        }

        private void DrawDetectionLines(Sensor sensor)
        {
            if (sensor.CurrentlyDetected.Count == 0) return;

            Gizmos.color = sensor.Config.DetectingColor;
            if (sensor.CurrentlyDetected[0] is MonoBehaviour mono)
            {
                Gizmos.DrawLine(sensorController.transform.position, mono.transform.position);
                if (showObjectMarkers)
                {
                    Gizmos.DrawWireCube(mono.transform.position, Vector3.one * 0.2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || sensorController?.Collection == null) return;

            foreach (var config in sensorController.Collection.Sensors)
            {
                if (config?.DebugMode == true)
                {
                    Gizmos.color = config.SelectedColor;
                    if (config.DetectionMode == SensorDetectionMode.Spherical)
                    {
                        Gizmos.DrawWireSphere(sensorController.transform.position, config.Radius);
                    }
                    else
                    {
                        DrawDetailedConeGizmo(config);
                    }
                }
            }
        }

        private void DrawConeGizmo(SensorConfig config, Color color)
        {
            var coneWorldDirection = sensorController.transform.TransformDirection(config.ConeDirection);
            float halfAngle = config.ConeAngle * 0.5f;

            var upAxis = sensorController.transform.up;
            var leftRotation = Quaternion.AngleAxis(-halfAngle, upAxis);
            var rightRotation = Quaternion.AngleAxis(halfAngle, upAxis);

            var leftEdge = leftRotation * coneWorldDirection * config.Radius;
            var rightEdge = rightRotation * coneWorldDirection * config.Radius;

            Gizmos.DrawRay(sensorController.transform.position, leftEdge);
            Gizmos.DrawRay(sensorController.transform.position, rightEdge);

            DrawConeArcOptimized(coneWorldDirection, config.Radius, config.ConeAngle, color, upAxis);
        }

        private void DrawConeArcOptimized(Vector3 direction, float radius, float angle, Color color, Vector3 rotationAxis)
        {
            if (_cachedSegments != coneSegments || !Mathf.Approximately(_cachedConeAngle, angle) || !Mathf.Approximately(_cachedRadius, radius) ||
                _cachedPosition != sensorController.transform.position || _cachedRotation != sensorController.transform.rotation)
            {
                _cachedConePoints = CalculateConeArcPoints(direction, radius, angle, rotationAxis);
                _cachedSegments = coneSegments;
                _cachedConeAngle = angle;
                _cachedRadius = radius;
                _cachedPosition = sensorController.transform.position;
                _cachedRotation = sensorController.transform.rotation;
            }

            Gizmos.color = color;
            for (int i = 1; i < _cachedConePoints.Length; i++)
            {
                Gizmos.DrawLine(_cachedConePoints[i - 1], _cachedConePoints[i]);
            }
        }

        private Vector3[] CalculateConeArcPoints(Vector3 direction, float radius, float angle, Vector3 rotationAxis)
        {
            var points = new Vector3[coneSegments + 1];
            float segmentAngle = angle / coneSegments;

            for (int i = 0; i <= coneSegments; i++)
            {
                float currentAngle = -angle * 0.5f + segmentAngle * i;
                var rotation = Quaternion.AngleAxis(currentAngle, rotationAxis);
                var rotatedDirection = rotation * direction;
                points[i] = sensorController.transform.position + rotatedDirection * radius;
            }

            return points;
        }

        private void DrawDetailedConeGizmo(SensorConfig config)
        {
            var coneWorldDirection = sensorController.transform.TransformDirection(config.ConeDirection);
            float halfAngle = config.ConeAngle * 0.5f;

            var upAxis = sensorController.transform.up;
            var leftRotation = Quaternion.AngleAxis(-halfAngle, upAxis);
            var rightRotation = Quaternion.AngleAxis(halfAngle, upAxis);

            var leftEdge = leftRotation * coneWorldDirection * config.Radius;
            var rightEdge = rightRotation * coneWorldDirection * config.Radius;

            Gizmos.DrawRay(sensorController.transform.position, leftEdge);
            Gizmos.DrawRay(sensorController.transform.position, rightEdge);
            DrawConeArcOptimized(coneWorldDirection, config.Radius, config.ConeAngle, config.SelectedColor, upAxis);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
            Gizmos.DrawRay(sensorController.transform.position, coneWorldDirection * config.Radius);
        }
    }
}
#endif