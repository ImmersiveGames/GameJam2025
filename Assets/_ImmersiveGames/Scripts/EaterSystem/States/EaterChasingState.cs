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
        public EaterChasingState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Context.SetProximitySensorActive(true);
            DebugUtility.LogVerbose<EaterChasingState>("Entrando no estado Perseguindo.");
        }

        public override void Update()
        {
            base.Update();

            if (!Context.TryGetTargetPosition(out Vector3 targetPosition))
            {
                return;
            }

            Vector3 currentPosition = Transform.position;
            Vector3 direction = targetPosition - currentPosition;
            float sqrDistance = direction.sqrMagnitude;
            if (sqrDistance <= Mathf.Epsilon)
            {
                MaintainFacing(targetPosition);
                return;
            }

            Vector3 normalizedDirection = direction.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, targetRotation, Time.deltaTime * Config.RotationSpeed);

            if (Context.IsTargetInProximity)
            {
                if (Context.TryGetProximityHoldPosition(out Vector3 holdPosition))
                {
                    Transform.position = holdPosition;
                }

                Context.ReportMovementSample(Vector3.zero, 0f);
                MaintainFacing(targetPosition);
                return;
            }

            float chaseSpeed = Mathf.Max(Config.MaxSpeed * Config.MultiplierChase, Config.MinSpeed);
            Context.ReportMovementSample(normalizedDirection, chaseSpeed);

            Transform.position = Vector3.MoveTowards(currentPosition, targetPosition, chaseSpeed * Time.deltaTime);

        }

        private void MaintainFacing(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - Transform.position;
            if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, desiredRotation, Time.deltaTime * Config.RotationSpeed);
        }

        public override void OnExit()
        {
            base.OnExit();
            Context.SetProximitySensorActive(false);
            DebugUtility.LogVerbose<EaterChasingState>("Saindo do estado Perseguindo.");
        }
    }
}
