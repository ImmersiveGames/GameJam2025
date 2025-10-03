using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    public enum SensorDetectionMode
    {
        Spherical,
        Conical
    }

    [CreateAssetMenu(fileName = "SensorConfig", menuName = "ImmersiveGames/Detection/SensorConfig", order = 2)]
    public class SensorConfig : ScriptableObject
    {
        [SerializeField] private DetectionType detectionType;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float radius = 10f;
        [SerializeField] private float minFrequency = 0.1f;
        [SerializeField] private float maxFrequency = 0.5f;
        [SerializeField] private bool debugMode;
        
        [Header("Detection Mode")]
        [SerializeField] private SensorDetectionMode detectionMode = SensorDetectionMode.Spherical;
        [SerializeField] private float coneAngle = 90f;
        [SerializeField] private Vector3 coneDirection = Vector3.forward;
        
        [Header("Gizmo Colors (Editor Only)")]
        [SerializeField] private Color idleColor = Color.red;
        [SerializeField] private Color detectingColor = Color.green;
        [SerializeField] private Color selectedColor = Color.cyan;

        public DetectionType DetectionType => detectionType;
        public LayerMask TargetLayer => targetLayer;
        public float Radius => radius;
        public float MinFrequency => minFrequency;
        public float MaxFrequency => maxFrequency;
        public bool DebugMode => debugMode;
        public SensorDetectionMode DetectionMode => detectionMode;
        public float ConeAngle => coneAngle;
        public Vector3 ConeDirection => coneDirection;
        public Color IdleColor => idleColor;
        public Color DetectingColor => detectingColor;
        public Color SelectedColor => selectedColor;
    }
}