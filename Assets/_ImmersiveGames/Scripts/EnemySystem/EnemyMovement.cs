using UnityEngine;
using _ImmersiveGames.Scripts.EnemySystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class EnemyMovement : MonoBehaviour
    {
        private EnemyData _data;
        private Transform _target;
        private float _movementTimer;

        public void Initialize(EnemyData data, Transform target)
        {
            if (data == null || target == null)
            {
                DebugUtility.LogWarning<EnemyMovement>($"Dados ou alvo inválidos para {gameObject.name}.", this);
                return;
            }

            _data = data;
            _target = target;
            _movementTimer = 0f;
        }

        private void Update()
        {
            if (_target == null || _data == null || !gameObject.activeSelf) return;

            _movementTimer += Time.deltaTime;

            Vector3 targetPosXZ = new Vector3(_target.position.x, 0f, _target.position.z);
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 direction = (targetPosXZ - currentPosXZ).normalized;

            UpdateMovement(direction);
        }

        private void UpdateMovement(Vector3 direction)
        {
            if (direction == Vector3.zero) return;

            switch (_data.movementType)
            {
                case EnemyMovementType.Linear:
                    ApplyLinearMovement(direction);
                    break;

                case EnemyMovementType.Sinusoidal:
                    ApplySinusoidalMovement(direction);
                    break;

                case EnemyMovementType.Homing:
                    ApplyHomingMovement(direction);
                    break;
            }
        }

        private void ApplyLinearMovement(Vector3 direction)
        {
            transform.position += direction * _data.speed * Time.deltaTime;
            ApplyRotation(direction, _data.rotationSpeed);
        }

        private void ApplySinusoidalMovement(Vector3 direction)
        {
            Vector3 sideDir = Vector3.Cross(direction, Vector3.up).normalized;
            float offset = Mathf.Sin(_movementTimer * _data.speed * _data.sinusoidalFrequency) 
                          * _data.sinusoidalAmplitude;
            transform.position += (direction * _data.speed + sideDir * offset) * Time.deltaTime;
            ApplyRotation(direction, _data.rotationSpeed);
        }

        private void ApplyHomingMovement(Vector3 direction)
        {
            transform.position += direction * _data.speed * Time.deltaTime;
            ApplyRotation(direction, _data.rotationSpeed * _data.homingRotationMultiplier);
        }

        private void ApplyRotation(Vector3 direction, float rotationSpeed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                Time.deltaTime * rotationSpeed);
        }

        public void ResetState()
        {
            _data = null;
            _target = null;
            _movementTimer = 0f;
            transform.position = Vector3.zero; // Reposicionar no pool
            transform.rotation = Quaternion.identity; // Resetar rotação
        }
    }
}