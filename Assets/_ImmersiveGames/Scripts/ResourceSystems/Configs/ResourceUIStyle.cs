using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/Resource UI Style")]
    public class ResourceUIStyle : ScriptableObject
    {
        [Header("Fill Colors")]
        public Gradient fillGradient;

        [Header("Pending Bar")]
        public Color pendingColor = new(1f, 1f, 1f, 0.6f);

        /// <summary>
        /// Calcula a cor do preenchimento baseado no valor normalizado informado.
        /// </summary>
        public Color EvaluateFillColor(float normalizedValue)
        {
            if (!HasFillGradient())
                return Color.white;

            return fillGradient.Evaluate(Mathf.Clamp01(normalizedValue));
        }

        /// <summary>
        /// Indica se o gradiente está configurado corretamente para avaliação.
        /// </summary>
        public bool HasFillGradient()
        {
            return fillGradient != null &&
                   fillGradient.colorKeys != null &&
                   fillGradient.colorKeys.Length > 0;
        }
    }
}
