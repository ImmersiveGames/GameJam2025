using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado base para deslocamentos do Eater.
    /// Centraliza lógica de direção, rotação e ajustes posicionais reaproveitados por estados derivados.
    /// </summary>
    internal abstract class EaterMovementState : EaterBehaviorState
    {
        private Vector3 _baseDirection = Vector3.forward;
        private float _baseSpeed;
        private float _directionTimer;

        protected EaterMovementState(EaterBehavior behavior) : base(behavior)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            SelectNewMovementSample();
        }

        public override void OnExit()
        {
            base.OnExit();
            Behavior?.ClearMovementSample();
        }

        public override void Update()
        {
            base.Update();

            _directionTimer += Time.deltaTime;
            if (_directionTimer >= GetDirectionInterval())
            {
                SelectNewMovementSample();
            }

            Vector3 direction = _baseDirection;
            float speed = _baseSpeed;
            AdjustMovement(ref direction, ref speed);

            if (direction.sqrMagnitude <= Mathf.Epsilon || speed <= Mathf.Epsilon)
            {
                return;
            }

            direction = direction.normalized;
            Vector3 nextPosition = Transform.position + direction * (speed * Time.deltaTime);
            nextPosition = ClampToGameArea(nextPosition);
            nextPosition = AdjustPosition(nextPosition, direction, speed);

            Transform.position = nextPosition;
            RotateTowards(direction);
        }

        protected virtual float GetDirectionInterval()
        {
            return Mathf.Max(Config.DirectionChangeInterval, 0.1f);
        }

        protected virtual float ResolveSpeed()
        {
            return Mathf.Max(Random.Range(Config.MinSpeed, Config.MaxSpeed), 0f);
        }

        protected virtual Vector3 ResolveDirection()
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            return randomDirection.sqrMagnitude > Mathf.Epsilon ? randomDirection.normalized : Transform.forward;
        }

        protected virtual void AdjustMovement(ref Vector3 direction, ref float speed)
        {
        }

        protected virtual Vector3 AdjustPosition(Vector3 proposedPosition, Vector3 direction, float speed)
        {
            return proposedPosition;
        }

        protected bool TryGetPlayerAnchor(out Vector3 anchor)
        {
            if (Behavior != null)
            {
                return Behavior.TryGetPlayerAnchor(out anchor);
            }

            anchor = Vector3.zero;
            return false;
        }

        protected Vector3 ClampToGameArea(Vector3 position)
        {
            Rect area = Behavior != null ? Behavior.GameArea : new Rect(position.x, position.z, 0f, 0f);
            position.x = Mathf.Clamp(position.x, area.xMin, area.xMax);
            position.z = Mathf.Clamp(position.z, area.yMin, area.yMax);
            return position;
        }

        private void SelectNewMovementSample()
        {
            _directionTimer = 0f;
            _baseDirection = ResolveDirection();
            _baseSpeed = ResolveSpeed();
            Behavior?.ReportMovementSample(_baseDirection, _baseSpeed);
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, targetRotation, Time.deltaTime * Config.RotationSpeed);
        }
    }
}
