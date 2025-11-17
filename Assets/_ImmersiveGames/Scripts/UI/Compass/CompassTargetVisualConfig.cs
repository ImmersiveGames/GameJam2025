using UnityEngine;
namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Configuração visual para um tipo específico de alvo rastreável pela bússola.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassTargetVisualConfig", menuName = "ImmersiveGames/UI/Compass/Target Visual Config")]
    public class CompassTargetVisualConfig : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Tipo de alvo que utilizará esta configuração visual.")]
        public CompassTargetType targetType = CompassTargetType.Objective;

        [Header("Aparência")]
        [Tooltip("Ícone a ser exibido na bússola.")]
        public Sprite icon;

        [Tooltip("Cor base utilizada para o ícone do alvo.")]
        public Color baseColor = Color.white;

        [Tooltip("Tamanho base do ícone na bússola.")]
        public float baseSize = 24f;
    }
}
