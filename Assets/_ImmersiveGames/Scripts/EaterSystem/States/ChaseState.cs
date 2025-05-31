using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class ChaseState : IState
    {
        private readonly Transform _transform;
        private readonly Transform _target;
        private readonly float _speed;
        private readonly float _reachDistance;

        public UnityEvent OnTargetReached { get; }

        public ChaseState(Transform transform, Transform target, float speed, float reachDistance, UnityEvent onTargetReached)
        {
            _transform = transform;
            _target = target;
            _speed = speed;
            _reachDistance = reachDistance;
            OnTargetReached = onTargetReached;
        }

        public void OnEnter() { }

        public void Update()
        {
            if (_target == null) return;

            var direction = _target.position - _transform.position;
            direction.y = 0;

            if (direction.magnitude <= _reachDistance)
            {
                OnTargetReached?.Invoke();
                return;
            }

            _transform.rotation = Quaternion.Slerp(
                _transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );

            _transform.position += _transform.forward * _speed * Time.deltaTime;
        }

        public void OnExit() { }
    }
}