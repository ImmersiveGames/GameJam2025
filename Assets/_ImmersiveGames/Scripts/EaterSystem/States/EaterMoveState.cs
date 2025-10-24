using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado base para movimentos vagos do Eater com direção e limites.
    /// </summary>
    internal abstract class EaterMoveState : EaterBehaviorState
    {
        private Vector3 _direction;
        private float _currentSpeed;

        protected EaterMoveState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            ChooseNewDirection();
        }

        public override void Update()
        {
            base.Update();

            if (Context.StateTimer >= GetDirectionInterval())
            {
                ChooseNewDirection();
                Context.ResetStateTimer();
            }

            RotateTowardsDirection();
            MoveForward();
            KeepWithinBounds();
        }

        protected virtual float GetDirectionInterval()
        {
            return Mathf.Max(Config.DirectionChangeInterval, 0.1f);
        }

        protected abstract float EvaluateSpeed();

        protected virtual Vector3 EvaluateDirection()
        {
            Vector3 newDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            return newDirection.sqrMagnitude > 0f ? newDirection.normalized : Transform.forward;
        }

        private void ChooseNewDirection()
        {
            _direction = EvaluateDirection();
            _currentSpeed = Mathf.Max(EvaluateSpeed(), 0f);
            Context.ReportMovementSample(_direction, _currentSpeed);
            OnDirectionChosen(_direction, _currentSpeed);
        }

        protected virtual void OnDirectionChosen(Vector3 direction, float speed)
        {
        }

        private void RotateTowardsDirection()
        {
            if (_direction == Vector3.zero)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(_direction, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, targetRotation, Time.deltaTime * Config.RotationSpeed);
        }

        private void MoveForward()
        {
            float distance = _currentSpeed * Time.deltaTime;
            Transform.Translate(Vector3.forward * distance, Space.Self);
        }

        private void KeepWithinBounds()
        {
            Vector3 position = Transform.position;
            position.x = Mathf.Clamp(position.x, Context.GameArea.xMin, Context.GameArea.xMax);
            position.z = Mathf.Clamp(position.z, Context.GameArea.yMin, Context.GameArea.yMax);
            Transform.position = position;
        }
    }
}
