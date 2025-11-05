using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Vagando" – o Eater navega aleatoriamente pelo mapa enquanto está satisfeito.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterWanderingState : EaterMoveState
    {
        public EaterWanderingState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Context.SetHungry(false);
            Context.RestartWanderingTimer();
            DebugUtility.LogVerbose<EaterWanderingState>("Entrando no estado Vagando.");
        }

        public override void OnExit()
        {
            Context.StopWanderingTimer();
            DebugUtility.LogVerbose<EaterWanderingState>("Saindo do estado Vagando.");
        }

        public override void Update()
        {
            base.Update();

            if (!Context.IsHungry && Context.HasWanderingTimerElapsed())
            {
                bool changed = Context.SetHungry(true);
                if (changed)
                {
                    DebugUtility.LogVerbose<EaterWanderingState>("Tempo de vagar finalizado. Eater ficou com fome.");
                }
            }

            if (Context.TryGetPlayerAnchor(out Vector3 anchor))
            {
                float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
                MaintainMaxDistanceFromPlayer(anchor, maxDistance);
            }
        }

        protected override float EvaluateSpeed()
        {
            return UnityEngine.Random.Range(Config.MinSpeed, Config.MaxSpeed);
        }

        protected override Vector3 EvaluateDirection()
        {
            Vector3 randomDirection = base.EvaluateDirection();
            if (!Context.TryGetPlayerAnchor(out Vector3 anchor))
            {
                return randomDirection;
            }

            float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
            if (maxDistance <= 0f)
            {
                return randomDirection;
            }

            Vector3 toAnchor = anchor - Transform.position;
            toAnchor.y = 0f;
            float distance = toAnchor.magnitude;
            if (distance > maxDistance)
            {
                return toAnchor.sqrMagnitude > 0f ? toAnchor.normalized : randomDirection;
            }

            if (distance <= Mathf.Epsilon)
            {
                return randomDirection;
            }

            float bias = Mathf.Clamp01(distance / maxDistance) * Mathf.Clamp01(Config.WanderingReturnBias);
            if (bias <= 0f)
            {
                return randomDirection;
            }

            Vector3 blendedDirection = Vector3.Slerp(randomDirection, toAnchor.normalized, bias);
            return blendedDirection.normalized;
        }

        private void MaintainMaxDistanceFromPlayer(Vector3 anchor, float maxDistance)
        {
            if (maxDistance <= 0f)
            {
                return;
            }

            Vector3 currentPosition = Transform.position;
            Vector3 offset = currentPosition - anchor;
            offset.y = 0f;

            float maxDistanceSqr = maxDistance * maxDistance;
            if (offset.sqrMagnitude <= maxDistanceSqr)
            {
                return;
            }

            Vector3 clampedOffset = offset.sqrMagnitude > 0f ? offset.normalized * maxDistance : Vector3.forward * maxDistance;
            Vector3 clampedPosition = new Vector3(anchor.x + clampedOffset.x, currentPosition.y, anchor.z + clampedOffset.z);
            Transform.position = clampedPosition;
        }
    }
}
