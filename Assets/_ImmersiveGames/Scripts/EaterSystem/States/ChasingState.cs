using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Warning)]
    internal class ChasingState : IState
    {
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;
        private readonly SensorController _sensorController;
        private float _currentSpeed;
        public ChasingState(EaterMovement eaterMovement, EaterConfigSo config, SensorController sensorController)
        {
            _eaterMovement = eaterMovement;
            _transform = eaterMovement.transform;
            _config = config;
            _sensorController = sensorController;
        }
        public void Update()
        {
            var target = _eaterMovement.TargetTransform;
            var direction = (target.position - _transform.position).normalized;

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);

                // ❗ Força alinhamento apenas no plano XZ (top-down)
                _transform.eulerAngles = new Vector3(0f, _transform.eulerAngles.y, 0f);
            }
            float moveAmount = _currentSpeed * Time.deltaTime;
            _transform.Translate(direction * moveAmount, Space.World);
        }
        
        public void OnEnter()
        {
            var targetMotion = _eaterMovement.TargetTransform.GetComponent<PlanetMotion>();
            _currentSpeed = targetMotion.GetOrbitSpeed() + _config.MinSpeed * 5f;
            DebugUtility.LogVerbose<ChasingState>($"Entering Chasing State for EaterMovement: {nameof(EaterMovement)}");
        }
    }
}