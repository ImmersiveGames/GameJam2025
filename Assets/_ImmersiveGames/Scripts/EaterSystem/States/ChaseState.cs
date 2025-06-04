using System;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.GameManagerSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ChaseState : IState
    {
        private readonly Transform _transform;
        private readonly Func<Transform> _getTarget;
        private readonly Func<float> _getSpeed;

        public ChaseState(Transform transform, Func<Transform> getTarget, Func<float> getSpeed)
        {
            _transform = transform;
            _getTarget = getTarget;
            _getSpeed = getSpeed;
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
            if (!GameManager.Instance.ShouldPlayingGame()) return;

            var target = _getTarget();
            if (target == null)
            {
                DebugUtility.LogVerbose<ChaseState>("Alvo nulo. Transitando para outro estado.");
                return;
            }

            // Direção no plano XZ
            Vector3 direction = (target.position - _transform.position);
            direction.y = 0f;
            direction.Normalize();

            // Rotaciona visualmente para olhar para o alvo
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 10f);

                // Bloqueia rotação X e Z (opcional, para evitar inclinação)
                Vector3 fixedEuler = _transform.eulerAngles;
                fixedEuler.x = 0f;
                fixedEuler.z = 0f;
                _transform.eulerAngles = fixedEuler;
            }

            // Move diretamente em direção ao alvo
            float moveAmount = _getSpeed() * Time.deltaTime;
            _transform.Translate(direction * moveAmount, Space.World);
        }



        public void FixedUpdate() { }
    }
}