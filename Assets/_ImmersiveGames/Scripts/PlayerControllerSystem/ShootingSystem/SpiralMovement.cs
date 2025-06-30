using _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class SpiralMovement : IMovementStrategy
    {
        private Transform _self;
        private Transform _target;
        private float _angle;
        private float _elapsed;
        private readonly ProjectilesData _config;

        public SpiralMovement(ProjectilesData data)
        {
            _config = data;
        }

        public void Initialize(Transform self, Transform target)
        {
            _self = self;
            _target = target;
            _angle = 0f;
            _elapsed = 0f;

            _config.movementCurve.preWrapMode = WrapMode.Loop;
            _config.movementCurve.postWrapMode = WrapMode.Loop;
        }

        public void Tick()
        {
            if (_target == null || _self == null) return;

            _elapsed += Time.deltaTime;

            Vector3 toTarget = _target.position - _self.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f) return;

            Vector3 forward = toTarget.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // curva de aceleração
            float curveTime = _elapsed / _config.curveDuration;
            float curveMultiplier = _config.movementCurve.Evaluate(curveTime);
            float currentSpeed = _config.moveSpeed * curveMultiplier;

            // avanço para frente
            Vector3 forwardMovement = forward * (currentSpeed * Time.deltaTime);

            // rotação espiral
            _angle += _config.spiralRotationPerSecond * 360f * Time.deltaTime;
            float radians = _angle * Mathf.Deg2Rad;

            Vector3 spiralOffset = right * Mathf.Cos(radians) + Vector3.Cross(Vector3.up, right) * Mathf.Sin(radians);
            spiralOffset *= _config.spiralRadius;

            Vector3 move = forwardMovement + spiralOffset * Time.deltaTime;

            _self.position += move;
            _self.forward = forward;
        }
    }
}