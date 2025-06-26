using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using DG.Tweening;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    [DebugLevel(DebugLevel.Logs)]
    public class AdvancedProjectileMovement : MonoBehaviour, IObjectMovement
    {
        [SerializeField] private ProjectilesData config;
        private Transform _target;
        private bool _isMoving;

        private void Awake()
        {
            DOTween.SetTweensCapacity(200, 50);
            if (config == null)
            {
                DebugUtility.LogError<AdvancedProjectileMovement>("ProjectilesData não atribuído no Inspector.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!_isMoving) return;

            MoveWithRotation();
        }

        public void Initialize(Vector3? direction, float speed, Transform target = null)
        {
            _target = target;
            _isMoving = true;

            if (_target == null)
            {
                DebugUtility.LogWarning<AdvancedProjectileMovement>("Nenhum alvo fornecido. Desativando movimento.", this);
                StopMovement();
                return;
            }

            if (config.faceTarget && _target == null)
            {
                DebugUtility.LogWarning<AdvancedProjectileMovement>("faceTarget ativado, mas nenhum alvo fornecido. Desativando faceTarget.", this);
                config.faceTarget = false;
            }

            if (config.movementType == MovementType.Curve)
            {
                // Nenhuma inicialização extra necessária
            }
            else
            {
                DebugUtility.LogWarning<AdvancedProjectileMovement>(
                    $"Tipo de movimento {config.movementType} não implementado.",
                    this);
                StopMovement();
            }
        }

        private void MoveWithRotation()
        {
            if (!_isMoving || _target == null) return;

            // Recalcular targetPosXZ a cada frame com base na posição atual do alvo
            var targetPosXZ = new Vector3(_target.position.x, transform.position.y, _target.position.z) + GetTargetVariation();
            var directionXZ = (targetPosXZ - transform.position).normalized;
            float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(targetPosXZ.x, 0, targetPosXZ.z));

            // Rotação ajustada com amortecimento próximo ao alvo
            if (directionXZ != Vector3.zero && config.faceTarget)
            {
                var targetRotation = Quaternion.LookRotation(directionXZ, Vector3.up);
                float rotationFactor = (distance < 1f) ? Mathf.Clamp01(distance) : 1f; // Reduz rotação quando perto
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, config.rotationSpeed * Time.fixedDeltaTime * rotationFactor);
            }

            // Movimento para frente
            var moveDirection = transform.forward;
            var newPosition = transform.position + moveDirection * (config.moveSpeed * Time.fixedDeltaTime);
            newPosition.y = transform.position.y; // Manter Y constante no plano XZ
            transform.position = newPosition;
        }

        private Vector3 GetTargetVariation()
        {
            return Random.insideUnitSphere * config.errorRadius; // Variação aleatória no alvo
        }

        public void StopMovement()
        {
            _isMoving = false;
            DebugUtility.LogVerbose<AdvancedProjectileMovement>("Movimento parado.", "yellow", this);
        }

        private void OnDisable()
        {
            StopMovement();
        }
    }
}