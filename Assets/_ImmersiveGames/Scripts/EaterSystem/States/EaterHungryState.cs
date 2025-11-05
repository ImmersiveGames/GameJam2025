using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Com Fome" – prioriza aproximação de jogadores enquanto mantém distância segura.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterHungryState : EaterMovementState
    {
        public EaterHungryState(EaterBehavior behavior) : base(behavior)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Behavior?.SetHungry(true);
            DebugUtility.LogVerbose<EaterHungryState>("Entrando no estado Com Fome.");
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterHungryState>("Saindo do estado Com Fome.");
            base.OnExit();
        }

        protected override float GetDirectionInterval()
        {
            return Mathf.Max(Config.DirectionChangeInterval * 0.5f, 0.1f);
        }

        protected override float ResolveSpeed()
        {
            return Mathf.Max(Config.MaxSpeed, Config.MinSpeed);
        }

        protected override void AdjustMovement(ref Vector3 direction, ref float speed)
        {
            if (!TryGetPlayerAnchor(out Vector3 anchor))
            {
                return;
            }

            Vector3 toAnchor = anchor - Transform.position;
            toAnchor.y = 0f;
            if (toAnchor.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
            float minDistance = Mathf.Max(Config.HungryMinDistanceFromPlayer, 0f);
            float distance = toAnchor.magnitude;

            if (minDistance > 0f && distance < minDistance)
            {
                Vector3 awayFromAnchor = -toAnchor.normalized;
                direction = Vector3.Slerp(direction, awayFromAnchor, 1f);
                return;
            }

            if (maxDistance > 0f && distance >= maxDistance)
            {
                direction = toAnchor;
                return;
            }

            float attraction = Mathf.Clamp01(Config.HungryPlayerAttraction);
            direction = Vector3.Slerp(direction, toAnchor.normalized, attraction);

            if (direction.sqrMagnitude > Mathf.Epsilon && Behavior != null)
            {
                Behavior.ReportHungryMetrics(distance, Vector3.Dot(direction.normalized, toAnchor.normalized));
            }
        }

        protected override Vector3 AdjustPosition(Vector3 proposedPosition, Vector3 direction, float speed)
        {
            if (!TryGetPlayerAnchor(out Vector3 anchor))
            {
                return proposedPosition;
            }

            float minDistance = Mathf.Max(Config.HungryMinDistanceFromPlayer, 0f);
            if (minDistance <= 0f)
            {
                return proposedPosition;
            }

            Vector3 offset = proposedPosition - anchor;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance >= minDistance || distance <= Mathf.Epsilon)
            {
                if (distance <= Mathf.Epsilon)
                {
                    Vector3 displacement = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.right;
                    displacement *= minDistance;
                    return new Vector3(anchor.x + displacement.x, proposedPosition.y, anchor.z + displacement.z);
                }

                return proposedPosition;
            }

            Vector3 correction = offset.normalized * (minDistance - distance);
            return new Vector3(
                proposedPosition.x + correction.x,
                proposedPosition.y,
                proposedPosition.z + correction.z);
        }
    }
}
