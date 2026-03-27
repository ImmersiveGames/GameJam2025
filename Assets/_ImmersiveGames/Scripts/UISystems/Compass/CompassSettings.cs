using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.Compass
{
    /// <summary>
    /// ConfiguraÃ§Ãµes gerais de comportamento da bÃºssola, desacopladas da cena.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassSettings", menuName = "ImmersiveGames/Legacy/UI/Compass/Settings")]
    public class CompassSettings : ScriptableObject
    {
        [Header("Campo angular da bÃºssola")]
        [Tooltip("Metade do campo de visÃ£o horizontal em graus. O valor representa o Ã¢ngulo mÃ¡ximo para esquerda e direita.")]
        public float compassHalfAngleDegrees = 180f;

        [Header("Limites de distÃ¢ncia")]
        [Tooltip("DistÃ¢ncia mÃ¡xima para exibir alvos na bÃºssola.")]
        public float maxDistance = 250f;

        [Tooltip("DistÃ¢ncia mÃ­nima para exibir alvos na bÃºssola.")]
        public float minDistance;

        [Header("Comportamento visual")]
        [Tooltip("Quando verdadeiro, Ã­cones fora do campo angular sÃ£o fixados na borda da bÃºssola.")]
        public bool clampIconsAtEdges = true;
    }
}
