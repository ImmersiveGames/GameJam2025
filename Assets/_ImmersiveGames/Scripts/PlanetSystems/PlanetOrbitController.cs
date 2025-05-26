using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetOrbitController : MonoBehaviour
    {
        private Transform _orbitCenter;
        private float _orbitSpeed;
        private Tween _orbitTween;

        public void Initialize(Transform center, float orbitSpeed, float unused, bool orbitClockwise)
        {
            if (!center)
            {
                Debug.LogError($"Centro de órbita não definido para {gameObject.name}.", this);
                return;
            }

            _orbitCenter = center;
            _orbitSpeed = orbitSpeed;
            StartOrbit(orbitClockwise);
        }

        private void StartOrbit(bool orbitClockwise)
        {
            Vector3 relativePos = transform.position - _orbitCenter.position;
            float orbitRadius = new Vector3(relativePos.x, 0, relativePos.z).magnitude;

            float direction = orbitClockwise ? -1f : 1f;
            _orbitTween?.Kill();
            _orbitTween = DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(0, 360 * direction, 0), 360f / _orbitSpeed, RotateMode.FastBeyond360)
                    .SetRelative(true)
                    .SetEase(Ease.Linear))
                .SetLoops(-1, LoopType.Incremental)
                .OnUpdate(() =>
                {
                    Vector3 orbitPos = _orbitCenter.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(relativePos.x, 0, relativePos.z).normalized * orbitRadius);
                    transform.position = new Vector3(orbitPos.x, _orbitCenter.position.y, orbitPos.z);
                });
        }

        public void StopOrbit()
        {
            _orbitTween?.Kill();
            _orbitTween = null;
        }

        public void ResetState()
        {
            StopOrbit();
            _orbitCenter = null;
            _orbitSpeed = 0f;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}