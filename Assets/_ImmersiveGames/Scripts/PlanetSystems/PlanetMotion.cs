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
        private float _initialAngle; // Para manter a posição inicial no plano XZ

        private Tween _orbitTween;

        public void Initialize(
            Vector3 center,
            float radius,
            float orbitSpeedDegPerSec,
            bool orbitClockwise,
            float selfRotationSpeedDegPerSec)
        {
            _orbitCenter = center;
            _orbitRadius = radius;
            _orbitSpeed = orbitSpeedDegPerSec;
            _orbitClockwise = orbitClockwise;
            _selfRotationSpeed = selfRotationSpeedDegPerSec;

            // Calcular o ângulo inicial com base na posição atual no plano XZ
            Vector3 offset = transform.position - _orbitCenter;
            offset.y = 0; // Ignorar o eixo Y
            _initialAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

            StartOrbit();
        }

        private void StartOrbit()
        {
            float direction = _orbitClockwise ? -1f : 1f;

            _orbitTween?.Kill();

            // Usar um ângulo relativo a partir da posição inicial
            _orbitTween = DOTween.To(() => 0f, angle => UpdateOrbitPosition(angle), 360f * direction, 360f / _orbitSpeed)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void Update()
        {
            // Autorrotação do planeta no eixo Y
            transform.Rotate(Vector3.up, _selfRotationSpeed * Time.deltaTime, Space.Self);
        }

        private void UpdateOrbitPosition(float angle)
        {
            // Calcular a posição no plano XZ com base no ângulo
            float rad = (angle + _initialAngle) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * _orbitRadius;
            transform.position = _orbitCenter + offset; // Manter Y constante
        }

        private void OnDisable()
        {
            _orbitTween?.Kill();
        }

        private void OnDestroy()
        {
            _orbitTween?.Kill();
        }
    }
}