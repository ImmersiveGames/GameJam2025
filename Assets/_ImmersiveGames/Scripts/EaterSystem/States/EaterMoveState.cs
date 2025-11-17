using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado base responsável por tratar movimento genérico do Eater.
    /// Mantém direção, velocidade e aplica ajustes comuns durante a rotação/locomoção.
    /// </summary>
    internal abstract class EaterMoveState : EaterBehaviorState
    {
        private Vector3 _currentDirection;
        private float _currentSpeed;
        private CountdownTimer _directionTimer;

        protected EaterMoveState(string stateName) : base(stateName)
        {
        }

        protected virtual float DirectionInterval => Mathf.Max(Config?.DirectionChangeInterval ?? 1f, 0.1f);

        protected virtual bool ShouldRespectPlayerBounds => true;

        public override void OnEnter()
        {
            base.OnEnter();
            RestartMovement();
        }

        public override void OnExit()
        {
            StopDirectionTimer();
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            TickDirectionTimer();

            Move(Time.deltaTime);
        }

        protected abstract float EvaluateSpeed();

        protected virtual Vector3 EvaluateDirection()
        {
            Vector2 random = Random.insideUnitCircle;
            Vector3 direction = new(random.x, 0f, random.y);
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Transform.forward;
        }

        protected virtual Vector3 AdjustDirection(Vector3 direction)
        {
            return direction;
        }

        protected virtual void OnDirectionChosen(Vector3 direction, float speed, bool force)
        {
            if (!force || !Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Direção inicial configurada: {direction} com velocidade {speed:F2}",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }

        protected virtual void Move(float deltaTime)
        {
            if (_currentSpeed <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 adjustedDirection = AdjustDirection(_currentDirection);
            if (adjustedDirection.sqrMagnitude > Mathf.Epsilon)
            {
                _currentDirection = adjustedDirection.normalized;
            }

            if (_currentDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Behavior.RotateTowards(_currentDirection, deltaTime);
            Behavior.Move(_currentDirection, _currentSpeed, deltaTime, ShouldRespectPlayerBounds);
        }

        protected void RestartMovement()
        {
            RestartDirectionTimer();
            ChooseNewDirection(force: true);
        }

        private void ChooseNewDirection(bool force = false)
        {
            Vector3 direction = EvaluateDirection();
            Vector3 adjusted = AdjustDirection(direction);
            if (adjusted.sqrMagnitude > Mathf.Epsilon)
            {
                _currentDirection = adjusted.normalized;
            }
            else if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                _currentDirection = direction.normalized;
            }
            else
            {
                _currentDirection = Transform.forward;
            }

            _currentSpeed = Mathf.Max(EvaluateSpeed(), 0f);
            OnDirectionChosen(_currentDirection, _currentSpeed, force);
        }

        private void TickDirectionTimer()
        {
            if (_directionTimer == null)
            {
                RestartDirectionTimer();
                return;
            }

            _directionTimer.Update(Time.deltaTime);

            if (!_directionTimer.IsFinished)
            {
                return;
            }

            ChooseNewDirection();
            RestartDirectionTimer();
        }

        private void RestartDirectionTimer()
        {
            float safeInterval = Mathf.Max(DirectionInterval, 0.05f);

            if (_directionTimer == null)
            {
                _directionTimer = new CountdownTimer(safeInterval);
            }
            else
            {
                _directionTimer.Stop();
                _directionTimer.Reset(safeInterval);
            }

            _directionTimer.Start();
        }

        private void StopDirectionTimer()
        {
            if (_directionTimer == null)
            {
                return;
            }

            _directionTimer.Stop();
        }
    }
}
