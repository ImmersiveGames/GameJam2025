using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de perseguição: o eater avança na direção do planeta marcado via PlanetMarkingManager.
    /// </summary>
    internal sealed class EaterChasingState : EaterBehaviorState
    {
        private bool _reportedMissingTarget;

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                if (!_reportedMissingTarget && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogVerbose<EaterChasingState>("Nenhum planeta marcado para perseguir.", Behavior);
                    _reportedMissingTarget = true;
                }
                return;
            }

            _reportedMissingTarget = false;

            Vector3 toTarget = target.position - Transform.position;
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            float stopDistance = Mathf.Max(Config?.MinimumChaseDistance ?? 0f, 0f);
            if (stopDistance > 0f && distance <= stopDistance)
            {
                Behavior.LookAt(target.position);
                return;
            }

            Vector3 direction = toTarget.normalized;
            float speed = Behavior.GetChaseSpeed();
            float travelDistance = speed * Time.deltaTime;

            if (stopDistance > 0f)
            {
                float remaining = Mathf.Max(distance - stopDistance, 0f);
                travelDistance = Mathf.Min(travelDistance, remaining);
            }

            if (travelDistance <= 0f)
            {
                return;
            }

            Behavior.RotateTowards(direction, Time.deltaTime);
            Behavior.Translate(direction * travelDistance, respectPlayerBounds: false);
        }
    }
}
