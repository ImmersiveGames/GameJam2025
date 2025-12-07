using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Strategy
{
    [CreateAssetMenu(
        menuName = "ImmersiveGames/PlanetSystems/Defense/Chase Strategies/Direct",
        fileName = "DirectChaseStrategy")]
    public sealed class DirectChaseStrategySo : MinionChaseStrategySo
    {
        [Header("Configuração do movimento direto")]
        [SerializeField]
        private Ease ease = Ease.Linear;

        [Tooltip("Fator de duração extra (1 = apenas distância/velocidade).")]
        [SerializeField, Min(0f)]
        private float extraDurationFactor;

        public override Tween CreateChaseTween(
            Transform minion,
            Transform target,
            float baseSpeed,
            string targetLabel)
        {
            if (minion == null || target == null)
            {
                return null;
            }

            if (baseSpeed <= 0f)
            {
                return null;
            }

            float distance = Vector3.Distance(minion.position, target.position);
            float durationFromSpeed = distance / Mathf.Max(0.01f, baseSpeed);
            float duration = durationFromSpeed * (1f + extraDurationFactor);

            return minion.DOMove(target.position, duration)
                .SetEase(ease);
        }
    }
}