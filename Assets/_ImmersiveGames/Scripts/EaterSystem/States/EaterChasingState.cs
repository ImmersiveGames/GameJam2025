using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Perseguindo" – o Eater corre em direção ao alvo marcado.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterChasingState : EaterBehaviorState
    {
        public EaterChasingState(EaterBehavior behavior) : base(behavior)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            DebugUtility.LogVerbose<EaterChasingState>("Entrando no estado Perseguindo.");
        }

        public override void Update()
        {
            base.Update();

            if (Behavior != null && Behavior.HasProximityContactForTarget)
            {
                if (Behavior.TryGetProximityHoldPosition(out Vector3 holdPosition))
                {
                    Transform.position = holdPosition;
                }

                Behavior.ClearMovementSample();
                return;
            }

            if (Behavior != null && Behavior.ShouldEat)
            {
                Behavior.ClearMovementSample();
                return;
            }

            if (Behavior == null || !Behavior.TryGetTargetPosition(out Vector3 targetPosition))
            {
                return;
            }

            Vector3 currentPosition = Transform.position;
            Vector3 direction = (targetPosition - currentPosition);
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();

            float chaseSpeed = Mathf.Max(Config.MaxSpeed * Config.MultiplierChase, Config.MinSpeed);
            Behavior.ReportMovementSample(direction, chaseSpeed);

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, targetRotation, Time.deltaTime * Config.RotationSpeed);

            Transform.position = Vector3.MoveTowards(currentPosition, targetPosition, chaseSpeed * Time.deltaTime);

        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterChasingState>("Saindo do estado Perseguindo.");
        }
    }
}
