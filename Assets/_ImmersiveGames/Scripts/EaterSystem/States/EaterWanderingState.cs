using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado responsável por manter o eater vagando de maneira aleatória,
    /// respeitando limites mínimos e máximos de distância em relação aos jogadores.
    /// </summary>
    internal sealed class EaterWanderingState : EaterMoveState
    {
        public EaterWanderingState() : base("Wandering")
        {
        }

        protected override float EvaluateSpeed()
        {
            return Behavior.GetRandomRoamingSpeed();
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

            float maxDistance = Config?.WanderingMaxDistanceFromPlayer ?? 0f;
            float minDistance = Config?.WanderingMinDistanceFromPlayer ?? 0f;

            if (maxDistance > 0f && distance > maxDistance)
            {
                return toAnchor.normalized;
            }

            if (minDistance > 0f && distance < minDistance)
            {
                return (-toAnchor).normalized;
            }

            float bias = Mathf.Clamp01(Config?.WanderingReturnBias ?? 0f);
            if (bias <= 0f)
            {
                return direction;
            }

            Vector3 normalized = toAnchor.normalized;
            Vector3 blended = Vector3.Slerp(direction, normalized, bias);
            return blended.sqrMagnitude > Mathf.Epsilon ? blended.normalized : normalized;
        }

        protected override void OnDirectionChosen(Vector3 direction, float speed, bool force)
        {
            base.OnDirectionChosen(direction, speed, force);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Nova direção de passeio: {direction} | velocidade={speed:F2}",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }
    }
}
