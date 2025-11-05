using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Vagando" – o Eater percorre o cenário de forma aleatória enquanto está saciado.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterWanderingState : EaterMovementState
    {
        public EaterWanderingState(EaterBehavior behavior) : base(behavior)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Behavior?.SetHungry(false);
            Behavior?.RestartWanderingTimer();
            DebugUtility.LogVerbose<EaterWanderingState>("Entrando no estado Vagando.");
        }

        public override void OnExit()
        {
            Behavior?.StopWanderingTimer();
            DebugUtility.LogVerbose<EaterWanderingState>("Saindo do estado Vagando.");
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            if (Behavior != null && !Behavior.IsHungry && Behavior.HasWanderingTimerElapsed())
            {
                bool changed = Behavior.SetHungry(true);
                if (changed)
                {
                    DebugUtility.LogVerbose<EaterWanderingState>("Tempo de vagar finalizado. Eater ficou com fome.");
                }
            }
        }

        protected override void AdjustMovement(ref Vector3 direction, ref float speed)
        {
            if (!TryGetPlayerAnchor(out Vector3 anchor))
            {
                return;
            }

            float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
            if (maxDistance <= 0f)
            {
                return;
            }

            Vector3 toAnchor = anchor - Transform.position;
            toAnchor.y = 0f;
            float distance = toAnchor.magnitude;
            if (distance > maxDistance)
            {
                direction = toAnchor;
                return;
            }

            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            float bias = Mathf.Clamp01(distance / maxDistance) * Mathf.Clamp01(Config.WanderingReturnBias);
            if (bias <= 0f)
            {
                return;
            }

            direction = Vector3.Slerp(direction, toAnchor.normalized, bias);
        }

        protected override Vector3 AdjustPosition(Vector3 proposedPosition, Vector3 direction, float speed)
        {
            if (!TryGetPlayerAnchor(out Vector3 anchor))
            {
                return proposedPosition;
            }

            float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
            if (maxDistance <= 0f)
            {
                return proposedPosition;
            }

            Vector3 offset = proposedPosition - anchor;
            offset.y = 0f;
            float maxDistanceSqr = maxDistance * maxDistance;
            if (offset.sqrMagnitude <= maxDistanceSqr)
            {
                return proposedPosition;
            }

            Vector3 normalizedOffset = offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : direction;
            Vector3 clampedOffset = normalizedOffset * maxDistance;
            return new Vector3(anchor.x + clampedOffset.x, proposedPosition.y, anchor.z + clampedOffset.z);
        }
    }
}
