using System;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.StateMachine;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class AIController : MonoBehaviour
    {
        [Header("Wander")]
        public float minWanderSpeed = 1f;
        public float maxWanderSpeed = 3f;
        public float directionChangeInterval = 2f;

        [Header("Chase")]
        public Transform target;
        public float chaseSpeed = 6f;
        public float reachDistance = 1.5f;

        [Header("Eating")]
        public float eatingDuration = 3f;
        public float healAmount = 30f;

        private StateMachine.StateMachine _stateMachine;
        private Transform _currentTarget;
        private bool _reachedTarget;
        private bool _isEating;
        private EaterHealth _health;

        void Start()
        {
            _health = GetComponent<EaterHealth>();

            var builder = new StateMachineBuilder();

            var wander = new WanderState(transform, minWanderSpeed, maxWanderSpeed, directionChangeInterval);
            var chase = new ChaseState(transform, _currentTarget, GetCurrentChaseSpeed(), reachDistance, new UnityEngine.Events.UnityEvent());

            var eating = new EatingState(eatingDuration, healAmount, _health, () => {
                _isEating = false;
                _currentTarget = null;
            });

            // evento que ativa a transição para o estado Eating
            chase.OnTargetReached.AddListener(() =>
            {
                _reachedTarget = true;
            });

            builder
                .AddState(wander, out var wanderRef)
                .AddState(chase, out var chaseRef)
                .AddState(eating, out var eatingRef)
                .StateInitial(wanderRef)

                .Any(chaseRef, new HasTargetPredicate(() => _currentTarget))
                .At(chaseRef, eatingRef, new BoolPredicate(() => _reachedTarget))
                .At(eatingRef, wanderRef, new BoolPredicate(() => !_isEating && _currentTarget == null));

            _stateMachine = builder.Build();
        }

        void Update()
        {
            _stateMachine?.Update();
        }
        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        public void SetTarget(Transform target)
        {
            _currentTarget = target;
            _reachedTarget = false;
        }

        float GetCurrentChaseSpeed()
        {
            return _health.GetCurrentHealth() <= _health.GetMaxHealth() * 0.25f
                ? chaseSpeed * 1.5f // aumenta a velocidade se vida estiver baixa
                : chaseSpeed;
        }

        public void StartChase()
        {
            if (_currentTarget == null)
            {
                Debug.LogWarning("Sem alvo para perseguir.");
                return;
            }
        }
    }
}
