using System;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        menuName = "ImmersiveGames/PlanetSystems/Defense/Entry Strategies/Straight",
        fileName = "StraightEntryStrategy")]
    public sealed class StraightEntryStrategySo : MinionEntryStrategySo
    {
        [Header("Tweens")]
        [SerializeField]
        private Ease moveEase = Ease.OutQuad;

        [SerializeField]
        private Ease scaleEase = Ease.OutQuad;

        public override Sequence BuildEntrySequence(
            Transform minion,
            Vector3 planetCenter,
            Vector3 orbitPosition,
            Vector3 finalScale,
            float entryDurationSeconds,
            float initialScaleFactor,
            float orbitIdleDelaySeconds,
            Action onCompleted)
        {
            if (minion == null)
            {
                return null;
            }

            // Ponto e escala iniciais
            var tinyScale = finalScale * initialScaleFactor;
            minion.position = planetCenter;
            minion.localScale = tinyScale;

            var sequence = DOTween.Sequence();

            // Movimento centro -> órbita
            sequence.Append(
                minion.DOMove(orbitPosition, entryDurationSeconds)
                    .SetEase(moveEase));

            // Escala pequena -> final
            sequence.Join(
                minion.DOScale(finalScale, entryDurationSeconds)
                    .SetEase(scaleEase));

            // Idle em órbita
            if (orbitIdleDelaySeconds > 0f)
            {
                sequence.AppendInterval(orbitIdleDelaySeconds);
            }

            sequence.OnComplete(() => { onCompleted?.Invoke(); });

            return sequence;
        }
    }
}