using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado "Com Fome" – o Eater procura por alvos enquanto sente fome.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    internal sealed class EaterHungryState : EaterMoveState
    {
        public EaterHungryState(EaterBehaviorContext context) : base(context)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Context.SetHungry(true);
            DebugUtility.LogVerbose<EaterHungryState>("Entrando no estado Com Fome.");
        }

        public override void OnExit()
        {
            DebugUtility.LogVerbose<EaterHungryState>("Saindo do estado Com Fome.");
        }

        protected override float EvaluateSpeed()
        {
            float baseSpeed = Mathf.Max(Config.MaxSpeed, Config.MinSpeed);
            return baseSpeed * 0.75f;
        }

        protected override float GetDirectionInterval()
        {
            return Mathf.Max(Config.DirectionChangeInterval * 0.5f, 0.1f);
        }

        protected override Vector3 EvaluateDirection()
        {
            Vector3 randomDirection = base.EvaluateDirection();
            if (!Context.TryGetPlayerAnchor(out Vector3 anchor))
            {
                return randomDirection;
            }

            Vector3 toAnchor = anchor - Transform.position;
            if (toAnchor.sqrMagnitude <= Mathf.Epsilon)
            {
                return randomDirection;
            }

            float maxDistance = Mathf.Max(Config.WanderingMaxDistanceFromPlayer, 0f);
            float distance = toAnchor.magnitude;
            if (maxDistance > 0f && distance >= maxDistance)
            {
                return toAnchor.normalized;
            }

            float attraction = Mathf.Clamp01(Config.HungryPlayerAttraction);
            if (maxDistance > 0f && distance > 0f)
            {
                float proximityFactor = 1f - Mathf.Clamp01(distance / maxDistance);
                attraction = Mathf.Clamp01(attraction + (1f - attraction) * proximityFactor);
            }

            Vector3 directionToAnchor = toAnchor.normalized;
            Vector3 blendedDirection = Vector3.Slerp(randomDirection, directionToAnchor, attraction);
            return blendedDirection.sqrMagnitude > 0f ? blendedDirection.normalized : directionToAnchor;
        }

        protected override void OnDirectionChosen(Vector3 direction, float speed)
        {
            base.OnDirectionChosen(direction, speed);

            if (!Context.TryGetPlayerAnchor(out Vector3 anchor))
            {
                DebugUtility.LogVerbose<EaterHungryState>(
                    $"Direção escolhida em fome sem âncora de players | velocidade={speed:F2}");
                return;
            }

            Vector3 toAnchor = anchor - Transform.position;
            float distance = toAnchor.magnitude;
            float alignment = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon && toAnchor.sqrMagnitude > Mathf.Epsilon)
            {
                alignment = Vector3.Dot(direction.normalized, toAnchor.normalized);
            }

            DebugUtility.LogVerbose<EaterHungryState>(
                $"Nova direção em fome | velocidade={speed:F2} | distânciaJogador={distance:F2} | alinhamento={alignment:F2}");
            Context.ReportHungryMetrics(distance, alignment);
        }
    }
}
