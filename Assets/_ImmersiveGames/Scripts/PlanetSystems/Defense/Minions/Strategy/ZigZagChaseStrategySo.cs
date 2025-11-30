using _ImmersiveGames.Scripts.Utils.DebugSystems;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Estratégia de perseguição em zigzag usando DOTween.
    ///
    /// A ideia:
    /// - Move o minion até o alvo em linha reta (DOMove)
    /// - Ao mesmo tempo aplica um deslocamento lateral oscilando (DOBlendableMoveBy com LoopType.Yoyo)
    /// - O resultado é um caminho "serpenteando" até o alvo.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ZigZagChaseStrategy",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Chase Strategies/ZigZag")]
    public class ZigZagChaseStrategySo : MinionChaseStrategySo
    {
        [Header("Amplitude lateral")]
        [Tooltip("Intensidade do deslocamento lateral em cada oscilação (unidades de mundo).")]
        [SerializeField, Min(0.1f)]
        private float lateralAmplitude = 2f;

        [Header("Quantidade de ZigZags")]
        [Tooltip("Quantas idas/voltas laterais o minion faz até chegar no alvo.")]
        [SerializeField, Min(1)]
        private int zigZagCount = 3;

        [Header("Suavização da rotação")]
        [Tooltip("O controller já faz um Lerp no forward, mas podemos ajustar quanto 'distorce' o caminho.")]
        [SerializeField, Range(0f, 1f)]
        private float lateralBlendFactor = 1f;

        public override Tween CreateChaseTween(Transform minion, Transform target, float speed, string targetLabel)
        {
            if (minion == null || target == null)
            {
                DebugUtility.LogWarning<ZigZagChaseStrategySo>($"Minion ou Target nulo. Retornando null.");
                return null;
            }

            // Direção principal (reta) até o alvo
            Vector3 startPos = minion.position;
            Vector3 endPos   = target.position;

            Vector3 forwardDir = (endPos - startPos);
            float distance     = forwardDir.magnitude;

            if (distance <= 0.001f || speed <= 0.001f)
            {
                // Nada a fazer
                return null;
            }

            forwardDir.Normalize();

            // Direção lateral: perpendicular ao vetor até o alvo
            Vector3 lateralDir = Vector3.Cross(forwardDir, Vector3.up);
            if (lateralDir.sqrMagnitude < 0.0001f)
            {
                // Caso raro: alvo esteja exatamente acima/abaixo
                lateralDir = Vector3.Cross(forwardDir, Vector3.right);
            }
            lateralDir.Normalize();

            // Aplica o blend factor na direção lateral (permite "quase reto" se for baixo)
            lateralDir *= lateralAmplitude * Mathf.Max(0f, lateralBlendFactor);

            // Tempo total baseado na distância / velocidade
            float duration = distance / Mathf.Max(0.01f, speed);

            // Sequência principal
            var seq = DOTween.Sequence();

            // Movimento principal até o alvo
            var moveForward = minion.DOMove(endPos, duration)
                                    .SetEase(Ease.Linear);

            seq.Join(moveForward);

            // Movimento lateral oscilando (zigzag) usando DOBlendableMoveBy
            // Vamos fazer 2 * zigZagCount loops (vai e volta).
            int loops = Mathf.Max(1, zigZagCount * 2);

            var lateralTween = minion.DOBlendableMoveBy(lateralDir, duration / loops)
                                     .SetEase(Ease.InOutSine)
                                     .SetLoops(loops, LoopType.Yoyo);

            seq.Join(lateralTween);

            return seq;
        }
    }
}
