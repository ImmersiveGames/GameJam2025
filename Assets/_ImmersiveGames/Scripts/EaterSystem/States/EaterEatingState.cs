using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Comendo" – o Eater consome o alvo atual e aplica mordidas periódicas.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        private const float BiteInterval = 1.2f;
        private float _biteTimer;

        public EaterEatingState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _biteTimer = 0f;
            Context.SetEating(true);
            Context.ReportMovementSample(Vector3.zero, 0f);
            FaceTargetImmediately();
            DebugUtility.LogVerbose<EaterEatingState>("Entrando no estado Comendo.");
        }

        public override void Update()
        {
            base.Update();

            MaintainFacingTarget();

            _biteTimer += Time.deltaTime;
            if (_biteTimer < BiteInterval)
            {
                return;
            }

            _biteTimer = 0f;
            PlanetsMaster target = Context.Target;
            if (target != null)
            {
                Context.Master.OnEventEaterBite(target);
            }
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterEatingState>("Saindo do estado Comendo.");
            bool changed = Context.SetEating(false);
            PlanetsMaster target = Context.Target;
            if (changed && target != null)
            {
                Context.Master.OnEventEndEatPlanet(target);
            }
        }

        private void MaintainFacingTarget()
        {
            if (!Context.TryGetTargetPosition(out Vector3 targetPosition))
            {
                return;
            }

            Vector3 direction = targetPosition - Transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Transform.rotation = Quaternion.Slerp(Transform.rotation, targetRotation, Time.deltaTime * Config.RotationSpeed);
        }

        private void FaceTargetImmediately()
        {
            if (!Context.TryGetTargetPosition(out Vector3 targetPosition))
            {
                return;
            }

            Vector3 direction = targetPosition - Transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
