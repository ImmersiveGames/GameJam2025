using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado em que o eater busca manter-se mais próximo dos jogadores, mas ainda com movimento aleatório.
    /// </summary>
    internal sealed class EaterHungryState : EaterMoveState
    {
        public EaterHungryState() : base("Hungry")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Behavior?.ResumeAutoFlow("HungryState.OnEnter");
        }

        public override void OnExit()
        {
            Behavior?.PauseAutoFlow("HungryState.OnExit");
            base.OnExit();
        }

        protected override float DirectionInterval => Mathf.Max(base.DirectionInterval * 0.5f, 0.1f);

        protected override float EvaluateSpeed()
        {
            float min = Config != null ? Mathf.Max(0f, Config.MinSpeed) : 0f;
            float max = Config != null ? Mathf.Max(min, Config.MaxSpeed) : min;
            float baseSpeed = Mathf.Max(min, max);
            return baseSpeed * 0.75f;
        }

        protected override Vector3 AdjustDirection(Vector3 direction)
        {
            if (!Behavior.TryGetClosestPlayerAnchor(out Vector3 anchor, out float distance))
            {
                return direction;
            }

            Vector3 toAnchor = anchor - Transform.position;
            if (toAnchor.sqrMagnitude <= Mathf.Epsilon)
            {
                return direction;
            }

            float maxDistance = Behavior.MaxPlayerDistance;
            float minDistance = Behavior.MinPlayerDistance;

            if (minDistance > 0f && distance < minDistance)
            {
                return (-toAnchor).normalized;
            }

            if (maxDistance > 0f && distance > maxDistance)
            {
                return toAnchor.normalized;
            }

            float attraction = Mathf.Clamp01(Config?.HungryPlayerAttraction ?? 0.75f);
            if (maxDistance > 0f && distance > 0f)
            {
                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
                attraction = Mathf.Clamp01(attraction + (1f - attraction) * (1f - normalizedDistance));
            }

            Vector3 normalized = toAnchor.normalized;
            Vector3 blended = Vector3.Slerp(direction, normalized, attraction);
            return blended.sqrMagnitude > Mathf.Epsilon ? blended.normalized : normalized;
        }

        protected override void OnDirectionChosen(Vector3 direction, float speed, bool force)
        {
            base.OnDirectionChosen(direction, speed, force);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log<EaterHungryState>(
                $"Nova direção faminta: {direction} | velocidade={speed:F2}",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }
    }
}
