using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class WanderState : IState
    {
        private readonly Transform _transform;
        private readonly float _minSpeed;
        private readonly float _maxSpeed;
        private readonly float _directionChangeInterval;
        private float _timer;
        private Vector3 _direction;
        private float _currentSpeed;

        public WanderState(Transform transform, float minSpeed, float maxSpeed, float directionChangeInterval)
        {
            _transform = transform;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
            _directionChangeInterval = directionChangeInterval;
        }

        public void OnEnter()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _timer = 0f;
            ChooseNewDirection();
            _currentSpeed = Random.Range(_minSpeed, _maxSpeed);
            DebugUtility.LogVerbose<WanderState>("Entrou no estado de vagar.");
        }

        public void OnExit()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _timer = 0f;
            DebugUtility.LogVerbose<WanderState>("Saiu do estado de vagar.");
        }

        public void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            _timer += Time.deltaTime;
            if (_timer >= _directionChangeInterval)
            {
                ChooseNewDirection();
                _currentSpeed = Random.Range(_minSpeed, _maxSpeed);
                _timer = 0f;
            }

            if (_direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(_direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            float moveAmount = _currentSpeed * Time.deltaTime;
            _transform.position += _transform.forward * moveAmount;
        }

        public void FixedUpdate() { }

        private void ChooseNewDirection()
        {
            _direction = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized;
            DebugUtility.LogVerbose<WanderState>($"Nova direção de vagar: {_direction}, velocidade: {_currentSpeed}.");
        }
    }
}