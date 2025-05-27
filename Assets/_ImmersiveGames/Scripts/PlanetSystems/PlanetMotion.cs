using UnityEngine;
using DG.Tweening;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetMotion : MonoBehaviour
    {
        private Vector3 orbitCenter;
        private float orbitRadius;
        private float orbitSpeed;
        private bool orbitClockwise;
        private float selfRotationSpeed;

        private Tween orbitTween;

        private Vector3 initialOffset;

        public void Initialize(
            Vector3 center,
            float radius,
            float orbitSpeedDegPerSec,
            bool orbitClockwise,
            float selfRotationSpeedDegPerSec)
        {
            this.orbitCenter = center;
            this.orbitRadius = radius;
            this.orbitSpeed = orbitSpeedDegPerSec;
            this.orbitClockwise = orbitClockwise;
            this.selfRotationSpeed = selfRotationSpeedDegPerSec;

            initialOffset = (transform.position - orbitCenter).normalized * orbitRadius;

            StartOrbit();
        }

        private void StartOrbit()
        {
            float direction = orbitClockwise ? -1f : 1f;

            orbitTween?.Kill();

            orbitTween = DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(0f, 360f * direction, 0f), 360f / orbitSpeed, RotateMode.FastBeyond360)
                    .SetRelative(true)
                    .SetEase(Ease.Linear))
                .SetLoops(-1, LoopType.Incremental)
                .OnUpdate(UpdateOrbitPosition);
        }

        private void Update()
        {
            // Autorrotação do planeta
            transform.Rotate(Vector3.up, selfRotationSpeed * Time.deltaTime, Space.Self);
        }

        private void UpdateOrbitPosition()
        {
            float angle = transform.eulerAngles.y * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * orbitRadius;
            Vector3 targetPos = orbitCenter + offset;

            transform.position = new Vector3(targetPos.x, orbitCenter.y, targetPos.z);
        }

        private void OnDisable()
        {
            orbitTween?.Kill();
        }

        private void OnDestroy()
        {
            orbitTween?.Kill();
        }
    }
}
