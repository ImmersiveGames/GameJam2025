using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class WanderState : IState
    {
        private readonly Transform _transform;
        private readonly float _minSpeed, _maxSpeed, _changeInterval;
        private float _timer;
        private Vector3 _direction;
        private float _currentSpeed;

        public WanderState(Transform transform, float minSpeed, float maxSpeed, float changeInterval)
        {
            _transform = transform;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
            _changeInterval = changeInterval;
        }

        public void FixedUpdate()
        {
            //
        }
        public void OnEnter()
        {
            PickNewDirection();
        }

        public void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _changeInterval)
                PickNewDirection();

            _transform.Translate(_direction * _currentSpeed * Time.deltaTime, Space.World);
        }

        public void OnExit() { }

        private void PickNewDirection()
        {
            _timer = 0f;
            _direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            _currentSpeed = Random.Range(_minSpeed, _maxSpeed);
        }
    }
}