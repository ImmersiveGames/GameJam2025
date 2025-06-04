using System;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Verbose)]
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

        public void OnEnter()
        {
            DebugUtility.LogVerbose<ChaseState>("Entrou no estado de perseguição.");
        }

        public void OnExit()
        {
            DebugUtility.LogVerbose<ChaseState>("Saiu do estado de perseguição.");
        }

        public void Update()
        {
            var target = _getTarget();

            var direction = target.position - _transform.position;
            direction.y = 0;

            float distance = direction.magnitude;

            if (distance <= _reachDistance)
            {
                OnTargetReached?.Invoke();
                return;
            }

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 5f);
 
            }

            float moveAmount = _getSpeed() * Time.deltaTime;
            _transform.position += _transform.forward * moveAmount;
        }

        public void FixedUpdate() { }
    }
}
