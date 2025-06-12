using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    internal class WanderingState : IState
    {
        private float _timer;
        private Vector3 _direction;
        private Rect _gameArea;
        private readonly EaterConfigSo _config;
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        public WanderingState(EaterMovement eaterMovement, EaterConfigSo config)
        {
            _config = config;
            _gameArea = GameManager.Instance.GameConfig.gameArea;
            _eaterMovement = eaterMovement ?? throw new ArgumentNullException(nameof(eaterMovement), "EaterMovement cannot be null.");
            _transform = eaterMovement.transform;
        }
        public void Update()
        {
            _timer += Time.deltaTime;
            float currentSpeed = Random.Range(_config.MinSpeed, _config.MaxSpeed);
            if (_timer >= _config.DirectionChangeInterval)
            {
                ChooseNewDirection();
                _timer = 0f;
            }

            var targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * _config.RotationSpeed);

            float moveAmount = currentSpeed * Time.deltaTime;
            _transform.Translate(Vector3.forward * moveAmount, Space.Self);
            KeepWithinBounds();
        }
        public void FixedUpdate() { }
        public void OnEnter()
        {
            _timer = 0;
            _eaterMovement.IsOrbiting = false;
            ChooseNewDirection();
            DebugUtility.Log<WanderingState>($"Entering Wandering State for EaterMovement: {nameof(EaterMovement)}");
        }
        public void OnExit() { }
        private void ChooseNewDirection()
        {
            _direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        }
        private void KeepWithinBounds()
        {
            var pos = _transform.position;
            pos.x = Mathf.Clamp(pos.x, _gameArea.xMin, _gameArea.xMax);
            pos.z = Mathf.Clamp(pos.z, _gameArea.yMin, _gameArea.yMax);
            _transform.position = pos;
        }
    }

    internal class ChasingState : IState
    {
        private readonly EaterMovement _eaterMovement;
        private readonly Transform _transform;
        private readonly EaterConfigSo _config;
        private float _currentSpeed;
        public ChasingState(EaterMovement eaterMovement, EaterConfigSo config)
        {
            _eaterMovement = eaterMovement;
            _transform = eaterMovement.transform;
            _config = config;
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
            DebugUtility.Log<ChasingState>($"Entering Chasing State for EaterMovement: {nameof(EaterMovement)}");
        }
    }


}