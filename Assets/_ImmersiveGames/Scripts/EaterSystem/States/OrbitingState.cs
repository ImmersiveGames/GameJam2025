using System.Collections;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal class OrbitingState : IState
    {
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;

        private float _currentAngle; // Ângulo atual da órbita
        private float _targetAngularSpeed; // Velocidade angular constante
        private float _orbitRadius; // Raio da órbita
        private bool _isTransitioning; // Flag para indicar transição ativa

        private const float OrbitPeriod = 5f; // Tempo para uma órbita completa (segundos)
        private const float TransitionDuration = 1.2f; // Duração da transição
        private const float OrbitOffset = 2f; // Offset adicional ao raio do alvo

        public OrbitingState(EaterMovement eaterMovement)
        {
            _eaterMovement = eaterMovement;
            _transform = eaterMovement.transform;
            _targetAngularSpeed = 360f / OrbitPeriod; // Velocidade angular em graus/segundo
            _currentAngle = 0f;
            _isTransitioning = false;
        }

        public void OnEnter()
        {
            _eaterMovement.IsOrbiting = true;
            _isTransitioning = true;

            var target = _eaterMovement.TargetTransform;
            if (target == null)
            {
                Debug.LogWarning("TargetTransform is null in OrbitingState!");
                return;
            }

            _orbitRadius = CalculateOrbitRadius(target);
            _currentAngle = GetAngleToTarget(target.position);

            // Calcula o raio inicial (distância atual ao alvo no plano XZ)
            float initialRadius = Vector3.Distance(
                new Vector3(_transform.position.x, 0f, _transform.position.z),
                new Vector3(target.position.x, 0f, target.position.z)
            );

            // Inicia a transição suave
            _eaterMovement.StartCoroutine(TransitionToOrbit(initialRadius, _orbitRadius, TransitionDuration));

            DebugUtility.Log<OrbitingState>($"Entered Orbiting State: radius={_orbitRadius}, initialAngle={_currentAngle}");
        }

        public void Update()
        {
            var target = _eaterMovement.TargetTransform;
            if (target == null) return;

            if (_isTransitioning) return;

            // Avança o ângulo com velocidade constante
            _currentAngle = (_currentAngle + _targetAngularSpeed * Time.deltaTime) % 360f;

            // Calcula a posição orbital
            Vector3 orbitPosition = CalculateOrbitPosition(target.position, _currentAngle, _orbitRadius);
            _transform.position = orbitPosition;

            // Rotação suave para olhar o alvo
            Vector3 lookPos = new Vector3(target.position.x, _transform.position.y, target.position.z);
            Quaternion targetRotation = Quaternion.LookRotation(lookPos - _transform.position);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        public void OnExit()
        {
            _eaterMovement.IsOrbiting = false;
            _isTransitioning = false;
            DebugUtility.Log<OrbitingState>("Exited Orbiting State");
        }

        private IEnumerator TransitionToOrbit(float initialRadius, float targetRadius, float duration)
        {
            Vector3 startPosition = _transform.position;
            Quaternion startRotation = _transform.rotation;
            float startAngle = _currentAngle;
            float elapsed = 0f;
            var target = _eaterMovement.TargetTransform;
            while (elapsed < duration)
            {
                target = _eaterMovement.TargetTransform;
                if (target == null) yield break;

                float t = elapsed / duration;
                // Curva Hermite para suavizar raio e posição
                float smoothT = t * t * t * (t * (6f * t - 15f) + 10f);

                // Interpola o raio
                float currentRadius = Mathf.Lerp(initialRadius, targetRadius, smoothT);

                // Avança o ângulo com velocidade constante
                float currentAngle = startAngle + (_targetAngularSpeed * elapsed);
                Vector3 targetPosition = CalculateOrbitPosition(target.position, currentAngle, currentRadius);

                // Interpola a posição
                _transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

                // Interpola a rotação
                Vector3 lookPos = new Vector3(target.position.x, _transform.position.y, target.position.z);
                Quaternion targetRotation = Quaternion.LookRotation(lookPos - _transform.position);
                _transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

                // Atualiza o ângulo global para continuidade
                _currentAngle = currentAngle % 360f;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Finaliza a transição
            if (target != null)
            {
                _transform.position = CalculateOrbitPosition(target.position, _currentAngle, _orbitRadius);
            }
            _isTransitioning = false;

            DebugUtility.Log<OrbitingState>("Transition to orbit completed");
        }

        private Vector3 CalculateOrbitPosition(Vector3 center, float angle, float radius)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
            return center + offset;
        }

        private float GetAngleToTarget(Vector3 targetPosition)
        {
            Vector3 offset = _transform.position - targetPosition;
            offset.y = 0f;
            return Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }

        private float CalculateOrbitRadius(Transform target)
        {
            var modelRoot = target.GetComponentInChildren<ModelRoot>();
            if (modelRoot == null)
            {
                Debug.LogWarning("ModelRoot not found on target!");
                return 3f;
            }

            if (modelRoot.transform.childCount == 0)
            {
                Debug.LogWarning("ModelRoot has no children!");
                return 3f;
            }

            var model = modelRoot.transform.GetChild(0);
            var collider = model.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("Collider not found on target model!");
                return 3f;
            }

            float maxRadius = collider.bounds.extents.magnitude;
            float finalRadius = maxRadius + OrbitOffset;

            DebugUtility.Log<OrbitingState>($"Calculated radius: {finalRadius} (collider: {collider.GetType().Name}, extents: {collider.bounds.extents})");

            return finalRadius;
        }
    }
}