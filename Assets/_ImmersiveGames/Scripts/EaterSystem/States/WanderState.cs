using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    [DebugLevel(DebugLevel.Verbose)]
    public class WanderState : IState
    {
        private readonly Transform _transform;
        private readonly EaterDesire _eaterDesire;
        private readonly float _minSpeed, _maxSpeed, _changeInterval;
        private float _moveTimer;
        private float _desireTimer;
        private Vector3 _direction;
        private float _currentSpeed;
        private EventBinding<DesireUnlockedEvent> _desireUnlockedBinding;

        public WanderState(Transform transform, EaterDesire eaterDesire, float minSpeed, float maxSpeed, float changeInterval)
        {
            _transform = transform;
            _eaterDesire = eaterDesire;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
            _changeInterval = changeInterval;
        }

        public void OnEnter()
        {
            PickNewDirection();
            _desireUnlockedBinding = new EventBinding<DesireUnlockedEvent>(ResetDesireTimer);
            EventBus<DesireUnlockedEvent>.Register(_desireUnlockedBinding);
            DebugUtility.Log<WanderState>($"Entrou no estado de vagar.");
        }

        public void Update()
        {
            UpdateMovement();
            UpdateDesire();
            Move();
        }

        public void FixedUpdate() { }

        public void OnExit()
        {
            _moveTimer = 0f;
            _desireTimer = 0f;
            EventBus<DesireUnlockedEvent>.Unregister(_desireUnlockedBinding);
            DebugUtility.Log<WanderState>($"Saiu do estado de vagar.");
        }

        private void UpdateMovement()
        {
            _moveTimer += Time.deltaTime;
            if (_moveTimer >= _changeInterval)
            {
                PickNewDirection();
            }
        }

        private void UpdateDesire()
        {
            if (_eaterDesire.GetDesiredResource() == null)
            {
                return;
            }

            _desireTimer += Time.deltaTime;
            float desireChangeInterval = _eaterDesire.DesireConfig?.DesireChangeInterval ?? 10f;
            if (_desireTimer >= desireChangeInterval)
            {
                _eaterDesire.ChooseNewDesire("WanderState.Update");
                _desireTimer = 0f;
                DebugUtility.Log<WanderState>($"Desejo trocado após {desireChangeInterval}s.");
            }
        }

        private void Move()
        {
            _transform.Translate(_direction * (_currentSpeed * Time.deltaTime), Space.World);
        }

        private void PickNewDirection()
        {
            _moveTimer = 0f;
            _direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            _currentSpeed = Random.Range(_minSpeed, _maxSpeed);
        }

        private void ResetDesireTimer()
        {
            _desireTimer = 0f;
            DebugUtility.Log<WanderState>($"Desire timer resetado após destravamento.");
        }
    }
}