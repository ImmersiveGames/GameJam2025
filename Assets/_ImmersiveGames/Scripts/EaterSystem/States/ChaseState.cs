using System;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    public class ChaseState : IState
    {
        private readonly Transform _transform;
        private readonly Func<Transform> _getTarget;
        private readonly Func<float> _getSpeed;
        private readonly float _reachDistance;

        public event Action OnTargetReached;

        public ChaseState(
            Transform transform,
            Func<Transform> getTarget,
            Func<float> getSpeed,
            float reachDistance)
        {
            _transform = transform;
            _getTarget = getTarget;
            _getSpeed = getSpeed;
            _reachDistance = reachDistance;
        }

        public void OnEnter() { }

        public void OnExit() { }

        public void Update()
        {
            Transform target = _getTarget();
            if (target == null) return;

            Vector3 direction = target.position - _transform.position;
            direction.y = 0;

            float distance = direction.magnitude;

            if (distance <= _reachDistance)
            {
                OnTargetReached?.Invoke();
                return;
            }

            // Rotaciona suavemente para o alvo
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            // Move para frente
            _transform.position += _transform.forward * (_getSpeed() * Time.deltaTime);
        }

        public void FixedUpdate() { }
    }
}