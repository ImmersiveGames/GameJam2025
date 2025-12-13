using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Configurações gerais de comportamento da bússola, desacopladas da cena.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassSettings", menuName = "ImmersiveGames/UI/Compass/Settings")]
    public class CompassSettings : ScriptableObject
    {
        [Header("Campo angular da bússola")]
        [Tooltip("Metade do campo de visão horizontal em graus. O valor representa o ângulo máximo para esquerda e direita.")]
        public float compassHalfAngleDegrees = 180f;

        [Header("Limites de distância")]
        [Tooltip("Distância máxima para exibir alvos na bússola.")]
        public float maxDistance = 250f;

        [Tooltip("Distância mínima para exibir alvos na bússola.")]
        public float minDistance;

        [Header("Comportamento visual")]
        [Tooltip("Quando verdadeiro, ícones fora do campo angular são fixados na borda da bússola.")]
        public bool clampIconsAtEdges = true;
    }
}
