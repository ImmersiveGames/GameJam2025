using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetOrbit : MonoBehaviour
    {
        private Transform _orbitCenter;
        private float _orbitSpeed;
        private Tween _orbitTween;

        public void Initialize(Transform center, float minOrbitSpeed, float maxOrbitSpeed, bool orbitClockwise)
        {
            if (center == null)
            {
                Debug.LogWarning($"Centro de órbita não definido para {gameObject.name}.");
                return;
            }

            _orbitCenter = center;
            _orbitSpeed = Random.Range(minOrbitSpeed, maxOrbitSpeed);

            // Configura a órbita com DOTween
            StartOrbit(orbitClockwise);
        }

        private void StartOrbit(bool orbitClockwise)
        {
            // Calcula o raio da órbita (distância ao centro)
            Vector3 relativePos = transform.position - _orbitCenter.position;
            float orbitRadius = relativePos.magnitude;

            // Define a direção da órbita
            float direction = orbitClockwise ? -1f : 1f; // Horário = negativo, anti-horário = positivo

            // Cria uma sequência para a órbita
            _orbitTween?.Kill(); // Interrompe qualquer animação anterior
            _orbitTween = DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(0, 360 * direction, 0), 360f / _orbitSpeed, RotateMode.FastBeyond360)
                    .SetRelative(true)
                    .SetEase(Ease.Linear))
                .SetLoops(-1, LoopType.Incremental)
                .OnUpdate(() =>
                {
                    // Mantém o planeta na órbita circular ajustando sua posição
                    Vector3 orbitPos = _orbitCenter.position + (Quaternion.Euler(0, transform.eulerAngles.y, 0) * new Vector3(relativePos.x, 0, relativePos.z).normalized * orbitRadius);
                    transform.position = new Vector3(orbitPos.x, transform.position.y, orbitPos.z);
                });
        }

        public void StopOrbit()
        {
            if (_orbitTween != null)
            {
                _orbitTween.Kill();
                _orbitTween = null;
            }
        }

        private void OnDestroy()
        {
            StopOrbit();
        }
    }
}