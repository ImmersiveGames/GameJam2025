using _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class DirectMovement : IMovementStrategy
    {
        private Transform _self;
        private Transform _target;
        private readonly ProjectilesData _config;
        private float _elapsed;

        public DirectMovement(ProjectilesData data)
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
        }

        public void Tick()
        {
            if (_target == null || _self == null) return;

            _elapsed += Time.deltaTime;

            Vector3 toTarget = _target.position - _self.position;
            toTarget.y = 0;

            if (toTarget.magnitude <= _config.errorRadius) return;

            float curveTime = _elapsed / _config.curveDuration;
            float curveMultiplier = _config.movementCurve.Evaluate(curveTime);
            float currentSpeed = _config.moveSpeed * curveMultiplier;

            Vector3 direction = toTarget.normalized;
            _self.position += direction * (currentSpeed * Time.deltaTime);
            _self.forward = direction;
        }
    }
}