using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-60)]
    
    public sealed class PlanetMotion : MonoBehaviour
    {
        [Header("Orbit Configuration")]
        [Tooltip("Transform utilizado como centro da órbita. Quando vazio, o objeto pai imediato será usado.")]
        [SerializeField] private Transform orbitCenter;

        [Tooltip("Raio atual da órbita em unidades do mundo.")]
        [SerializeField, Min(0f)] private float orbitRadius;

        [Tooltip("Velocidade angular da órbita em graus por segundo.")]
        [SerializeField] private float orbitAngularSpeed;

        [Tooltip("Define se a órbita acontece no sentido horário.")]
        [SerializeField] private bool orbitClockwise;

        [Header("Self Rotation")]
        [Tooltip("Velocidade de rotação própria do planeta em graus por segundo.")]
        [SerializeField] private float selfRotationSpeed;

        private float _currentOrbitAngle;
        private float _heightOffset;
        private bool _orbitConfigured;
        private bool _orbitPaused;

        private Transform OrbitCenter => orbitCenter != null ? orbitCenter : transform.parent;

        private void Update()
        {
            if (!_orbitConfigured || _orbitPaused)
            {
                return;
            }

            UpdateOrbitAngle();
            ApplyOrbitPosition();
            ApplySelfRotation();
        }

        /// <summary>
        /// Configura os parâmetros orbitais do planeta.
        /// </summary>
        /// <param name="center">Centro da órbita que será acompanhado.</param>
        /// <param name="radius">Raio desejado para a órbita.</param>
        /// <param name="startAngle">Ângulo inicial em radianos.</param>
        /// <param name="angularSpeed">Velocidade de rotação em graus por segundo.</param>
        /// <param name="selfSpinSpeed">Velocidade de rotação própria.</param>
        /// <param name="clockwise">Define se a órbita é no sentido horário.</param>
        public void ConfigureOrbit(Transform center, float radius, float startAngle, float angularSpeed, float selfSpinSpeed, bool clockwise)
        {
            orbitCenter = center;
            orbitRadius = Mathf.Max(0f, radius);
            orbitAngularSpeed = angularSpeed;
            selfRotationSpeed = selfSpinSpeed;
            orbitClockwise = clockwise;
            _currentOrbitAngle = startAngle;

            var effectiveCenter = OrbitCenter;
            Vector3 centerPosition = effectiveCenter != null ? effectiveCenter.position : Vector3.zero;
            _heightOffset = transform.position.y - centerPosition.y;

            _orbitConfigured = true;
            _orbitPaused = false;
            ApplyOrbitPosition();
        }

        /// <summary>
        /// Pausa imediatamente a atualização orbital e rotação própria do planeta.
        /// </summary>
        public void PauseMotion()
        {
            _orbitPaused = true;
        }

        /// <summary>
        /// Retoma a atualização orbital previamente pausada.
        /// </summary>
        public void ResumeMotion()
        {
            _orbitPaused = false;
        }

        /// <summary>
        /// Indica se o movimento orbital está pausado.
        /// </summary>
        public bool IsMotionPaused => _orbitPaused;

        private void UpdateOrbitAngle()
        {
            if (Mathf.Approximately(orbitAngularSpeed, 0f))
            {
                return;
            }

            float direction = orbitClockwise ? -1f : 1f;
            _currentOrbitAngle += Mathf.Deg2Rad * orbitAngularSpeed * direction * Time.deltaTime;
            _currentOrbitAngle = Mathf.Repeat(_currentOrbitAngle, Mathf.PI * 2f);
        }

        private void ApplyOrbitPosition()
        {
            Transform effectiveCenter = OrbitCenter;
            Vector3 centerPosition = effectiveCenter != null ? effectiveCenter.position : Vector3.zero;

            Vector3 orbitOffset = new(Mathf.Cos(_currentOrbitAngle) * orbitRadius, 0f, Mathf.Sin(_currentOrbitAngle) * orbitRadius);
            Vector3 heightOffset = Vector3.up * _heightOffset;
            transform.position = centerPosition + orbitOffset + heightOffset;
        }

        private void ApplySelfRotation()
        {
            if (Mathf.Approximately(selfRotationSpeed, 0f))
            {
                return;
            }

            transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_orbitConfigured && orbitRadius <= 0f)
            {
                return;
            }

            Transform effectiveCenter = OrbitCenter;
            Vector3 centerPosition = effectiveCenter != null ? effectiveCenter.position : Vector3.zero;
            float radius = Mathf.Max(orbitRadius, 0f);

            if (radius <= 0f)
            {
                return;
            }

            Gizmos.color = new Color(0.35f, 0.95f, 0.65f, 0.75f);
            DrawOrbitCircle(centerPosition, radius);
        }

        private static void DrawOrbitCircle(Vector3 center, float radius, int segments = 64)
        {
            Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
            float step = Mathf.PI * 2f / segments;

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
#endif
    }
}
