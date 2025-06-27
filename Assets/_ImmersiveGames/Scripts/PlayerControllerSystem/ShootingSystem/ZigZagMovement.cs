using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class ZigZagMovement : IMovementStrategy
    {
        private Transform _self;
        private Transform _target;
        private float _elapsed;

        private readonly ProjectilesData _config;

        public ZigZagMovement(ProjectilesData data)
        {
            _config = data;
        }

        public void Initialize(Transform self, Transform target)
        {
            _self = self;
            _target = target;
            _elapsed = 0f;

            // 🔁 Garante que a curva será avaliada corretamente em loop
            _config.movementCurve.preWrapMode = WrapMode.Loop;
            _config.movementCurve.postWrapMode = WrapMode.Loop;
        }

        public void Tick()
        {
            if (_target == null || _self == null) return;

            _elapsed += Time.deltaTime;

            var toTarget = _target.position - _self.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f) return;

            var forward = toTarget.normalized;
            var right = Vector3.Cross(Vector3.up, forward);

            // 🎯 Avalia a curva baseada no tempo e duração de ciclo
            float curveTime = _elapsed / _config.curveDuration;
            float curveMultiplier = _config.movementCurve.Evaluate(curveTime);

            float currentSpeed = _config.moveSpeed * curveMultiplier;
            var forwardMovement = forward * (currentSpeed * Time.deltaTime);

            // Zig-zag lateral continua sendo baseado em senoide (ou também poderia usar curva)
            float lateralOffset = Mathf.Sin(_elapsed * _config.zigzagFrequency * Mathf.PI * 2f) * _config.zigzagAmplitude;
            var offset = right * lateralOffset * Time.deltaTime;

            _self.position += forwardMovement + offset;
            _self.forward = forward;
        }
    }
}