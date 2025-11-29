using System;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [CreateAssetMenu(
        menuName = "ImmersiveGames/PlanetSystems/Defense/Entry Strategies/Arc",
        fileName = "ArcEntryStrategy")]
    public sealed class ArcEntryStrategySo : MinionEntryStrategySo
    {
        [Header("Tweens")]
        [SerializeField]
        private Ease moveEase = Ease.OutQuad;

        [SerializeField]
        private Ease scaleEase = Ease.OutQuad;

        [Header("Arco")]
        [Tooltip("Quão forte o arco desvia lateralmente entre planeta e órbita (0.0 = quase reto).")]
        [SerializeField, Range(0f, 1.5f)]
        private float arcStrength = 0.75f;

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
            Vector3 tinyScale = finalScale * initialScaleFactor;
            minion.position = planetCenter;
            minion.localScale = tinyScale;

            Vector3 dir = orbitPosition - planetCenter;
            float distance = dir.magnitude;
            Vector3 dirNorm = distance > 0.0001f ? dir / distance : Vector3.forward;

            // Vetor perpendicular para "curvar" a trajetória
            Vector3 up = Vector3.up;
            if (Vector3.Dot(dirNorm, up) > 0.9f) // quase paralelo ao up
            {
                up = Vector3.right;
            }

            Vector3 side = Vector3.Cross(dirNorm, up).normalized;
            float arcOffsetMagnitude = distance * 0.5f * arcStrength;
            Vector3 midPoint = planetCenter + dir * 0.5f + side * arcOffsetMagnitude;

            // Vamos dividir a duração: metade até o ponto intermediário, metade até a órbita
            float halfDuration = entryDurationSeconds * 0.5f;

            var sequence = DOTween.Sequence();

            // 1) centro -> ponto intermediário
            sequence.Append(
                minion.DOMove(midPoint, halfDuration)
                      .SetEase(moveEase));

            // 2) intermediário -> órbita
            sequence.Append(
                minion.DOMove(orbitPosition, halfDuration)
                      .SetEase(moveEase));

            // Escala pequena -> final durante TODO o percurso
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
