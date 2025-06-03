using UnityEngine;
using DG.Tweening;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetMotion : MonoBehaviour
    {
        private Vector3 _orbitCenter;
        private float _orbitRadius;
        private float _orbitSpeed;
        private bool _orbitClockwise;
        private float _selfRotationSpeed;
        private float _currentAngle;
        private Tween _orbitTween;
        private bool _isPaused;
        public float OrbitSpeedDegPerSec => _orbitSpeed;
        public float SelfRotationSpeedDegPerSec => _selfRotationSpeed;

        public void Initialize(Vector3 center, float radius, float orbitSpeedDegPerSec, bool orbitClockwise, float selfRotationSpeedDegPerSec, float initialAngleRad = 0f)
        {
            if (radius <= 0f)
            {
                Debug.LogWarning($"Raio de órbita inválido ({radius}) para {gameObject.name}. Usando valor padrão.", this);
                radius = 1f;
            }
            if (orbitSpeedDegPerSec == 0f)
            {
                Debug.LogWarning($"Velocidade orbital inválida para {gameObject.name}. Usando valor padrão.", this);
                orbitSpeedDegPerSec = 10f;
            }

            _orbitCenter = center;
            _orbitRadius = radius;
            _orbitSpeed = orbitSpeedDegPerSec;
            _orbitClockwise = orbitClockwise;
            _selfRotationSpeed = selfRotationSpeedDegPerSec;
            _currentAngle = initialAngleRad; // Usar ângulo inicial fornecido

            // Definir posição inicial com base no ângulo fornecido
            UpdateOrbitPosition(_currentAngle);

            StartOrbit();
            Debug.Log($"PlanetMotion inicializado para {gameObject.name}: centro {_orbitCenter}, raio {_orbitRadius}, ângulo inicial {_currentAngle * Mathf.Rad2Deg} graus, velocidade orbital {_orbitSpeed}, rotação própria {_selfRotationSpeed}.");
        }

        private void StartOrbit()
        {
            float direction = _orbitClockwise ? -1f : 1f;

            _orbitTween?.Kill();
            _orbitTween = DOTween.To(() => _currentAngle, angle => _currentAngle = angle, 360f * direction * Mathf.Deg2Rad, 360f / Mathf.Abs(_orbitSpeed))
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .OnUpdate(() => UpdateOrbitPosition(_currentAngle));
        }

        private void UpdateOrbitPosition(float angle)
        {
            var offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * _orbitRadius;
            transform.position = _orbitCenter + offset;
        }

        private void Update()
        {
            if (_selfRotationSpeed != 0f)
            {
                transform.Rotate(Vector3.up, _selfRotationSpeed * Time.deltaTime, Space.Self);
            }
        }

        public void PauseOrbit()
        {
            if (_isPaused) return;
            _orbitTween?.Pause();
            _isPaused = true;
            Debug.Log($"Órbita pausada para {gameObject.name}.");
        }

        public void ResumeOrbit()
        {
            if (!_isPaused) return;
            _orbitTween?.Play();
            _isPaused = false;
            Debug.Log($"Órbita retomada para {gameObject.name}.");
        }

        private void OnDisable()
        {
            _orbitTween?.Kill();
            Debug.Log($"PlanetMotion desativado para {gameObject.name}.");
        }

        private void OnDestroy()
        {
            _orbitTween?.Kill();
            Debug.Log($"PlanetMotion destruído para {gameObject.name}.");
        }
    }
}