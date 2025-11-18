using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;

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
        public Sprite iconSprite;

        [Tooltip("Cor base utilizada para o ícone do alvo.")]
        public Color baseColor = Color.white;

        [Tooltip("Tamanho base do ícone na bússola.")]
        public float baseSize = 24f;

        [Header("Modo Dinâmico")]
        [Tooltip("Define se o ícone é estático ou derivado dinamicamente (ex.: recurso de planeta).")]
        public CompassIconDynamicMode dynamicMode = CompassIconDynamicMode.Static;

        [Tooltip("Se true, exibe o ícone genérico de planeta até a descoberta do recurso; caso contrário, usa o ícone padrão.")]
        public bool hideUntilDiscovered = true;

        [Tooltip("Ícone opcional a ser usado para planetas antes do recurso ser descoberto (ícone genérico)." )]
        public Sprite undiscoveredPlanetIcon;

        [Header("Planet Resource Styles")]
        [Tooltip("Database opcional para aplicar cor e tamanho específicos de acordo com o tipo de recurso do planeta.")]
        public PlanetResourceCompassStyleDatabase planetResourceStyleDatabase;

        // Recomenda-se configurar planetas criando uma config com:
        // targetType = Planet, dynamicMode = PlanetResourceIcon, hideUntilDiscovered = true.
        // Nesse modo, iconSprite pode ficar nulo e undiscoveredPlanetIcon define o ícone genérico exibido antes
        // da descoberta do recurso; após revelado, o ícone muda para o ResourceIcon do planeta e pode aplicar
        // estilos adicionais via PlanetResourceCompassStyleDatabase (cor/tamanho por tipo de recurso).
    }
}
