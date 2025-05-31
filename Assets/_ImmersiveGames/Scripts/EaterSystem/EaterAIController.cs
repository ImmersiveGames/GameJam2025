using System;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterHealth))]
    [RequireComponent(typeof(EaterDetectable))]
    public class EaterAIController : MonoBehaviour
    {
        [Header("Wander Settings")]
        [SerializeField] private float minWanderSpeed = 1f;
        [SerializeField] private float maxWanderSpeed = 3f;
        [SerializeField] private float directionChangeInterval = 2f;

        [Header("Chase Settings")]
        [SerializeField] private float baseChaseSpeed = 6f;
        [SerializeField] private float reachDistance = 1.5f;

        [Header("Eating Settings")]
        [SerializeField] private float eatingDuration = 3f;
        [SerializeField] private float healAmount = 30f;

        private StateMachine.StateMachine _stateMachine;
        private EaterHealth _health;
        private EaterDetectable _detector;

        private Transform _currentTarget;
        private bool _isEating;
        private bool _targetReached;

        private void Awake()
        {
            _health = GetComponent<EaterHealth>();
            _detector = GetComponent<EaterDetectable>();

            _detector.OnTargetUpdated += HandleTargetUpdated;
        }

        private void OnDestroy()
        {
            _detector.OnTargetUpdated -= HandleTargetUpdated;
        }

        private void Start()
        {
            // Estados
            var wanderState = new WanderState(transform, minWanderSpeed, maxWanderSpeed, directionChangeInterval);
            var chaseState = new ChaseState(
                transform,
                () => _currentTarget,
                () => GetChaseSpeed(),
                reachDistance
            );

            var eatingState = new EatingState(
                eatingDuration,
                healAmount,
                _health,
                OnFinishEating
            );

            // Quando o alvo for alcançado, mudar para "comendo"
            chaseState.OnTargetReached += () =>
            {
                _targetReached = true;
                _isEating = true;
            };

            // Construção da máquina de estados
            _stateMachine = new StateMachineBuilder()
                .AddState(wanderState, out var wanderRef)
                .AddState(chaseState, out var chaseRef)
                .AddState(eatingState, out var eatingRef)

                .At(wanderRef, chaseRef, new BoolPredicate(() => _currentTarget != null))
                .At(chaseRef, eatingRef, new BoolPredicate(() => _targetReached))
                .At(eatingRef, wanderRef, new BoolPredicate(() => !_isEating && _currentTarget == null))

                .StateInitial(wanderRef)
                .Build();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        private void HandleTargetUpdated(Transform target)
        {
            _currentTarget = target;
            _targetReached = false;
            _isEating = false;
        }

        private void OnFinishEating()
        {
            _isEating = false;
            _currentTarget = null;
        }

        private float GetChaseSpeed()
        {
            float healthRatio = _health.GetCurrentHealth() / _health.GetMaxHealth();
            return healthRatio <= 0.25f ? baseChaseSpeed * 1.5f : baseChaseSpeed;
        }
    }
}
