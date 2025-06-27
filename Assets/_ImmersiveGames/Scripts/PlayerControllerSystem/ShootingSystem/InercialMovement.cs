using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class InercialMovement : IMovementStrategy
    {
        private Transform _self;
        private Transform _target;
        private Vector3 _currentDirection;
        private readonly ProjectilesData _config;
        private float _elapsed;

        public InercialMovement(ProjectilesData data)
        {
            _config = data;
        }

        public void Initialize(Transform self, Transform target)
        {
            _self = self;
            _target = target;
            _elapsed = 0f;

            _config.movementCurve.preWrapMode = WrapMode.Loop;
            _config.movementCurve.postWrapMode = WrapMode.Loop;

            _currentDirection = (target.position - self.position).normalized;
        }

        public void Tick()
        {
            if (_target == null || _self == null) return;

            _elapsed += Time.deltaTime;

            Vector3 toTarget = _target.position - _self.position;
            toTarget.y = 0;
            Vector3 desiredDir = toTarget.normalized;

            _currentDirection = Vector3.RotateTowards(
                _currentDirection,
                desiredDir,
                _config.missileRotationSpeed * Mathf.Deg2Rad * Time.deltaTime,
                1f
            );

            float curveTime = _elapsed / _config.curveDuration;
            float curveMultiplier = _config.movementCurve.Evaluate(curveTime);
            float currentSpeed = _config.moveSpeed * curveMultiplier;

            _self.position += _currentDirection * (currentSpeed * Time.deltaTime);
            _self.forward = _currentDirection;
        }
    }
}