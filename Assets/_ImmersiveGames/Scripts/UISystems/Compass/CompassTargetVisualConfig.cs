using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.Compass
{
    /// <summary>
    /// ConfiguraÃ§Ã£o visual para um tipo especÃ­fico de alvo rastreÃ¡vel pela bÃºssola.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassTargetVisualConfig", menuName = "ImmersiveGames/Legacy/UI/Compass/Target Visual Config")]
    public class CompassTargetVisualConfig : ScriptableObject
    {
        [Header("IdentificaÃ§Ã£o")]
        [Tooltip("Tipo de alvo que utilizarÃ¡ esta configuraÃ§Ã£o visual.")]
        public CompassTargetType targetType = CompassTargetType.Objective;

        [Header("AparÃªncia")]
        [Tooltip("Ãcone a ser exibido na bÃºssola.")]
        public Sprite iconSprite;

        [Tooltip("Cor base utilizada para o Ã­cone do alvo.")]
        public Color baseColor = Color.white;

        [Tooltip("Tamanho base do Ã­cone na bÃºssola.")]
        public float baseSize = 24f;

        [Header("Modo DinÃ¢mico")]
        [Tooltip("Define se o Ã­cone Ã© estÃ¡tico ou derivado dinamicamente (ex.: recurso de planeta).")]
        public CompassIconDynamicMode dynamicMode = CompassIconDynamicMode.Static;

        [Tooltip("Se true, exibe o Ã­cone genÃ©rico de planeta atÃ© a descoberta do recurso; caso contrÃ¡rio, usa o Ã­cone padrÃ£o.")]
        public bool hideUntilDiscovered = true;

        [Tooltip("Ãcone opcional a ser usado para planetas antes do recurso ser descoberto (Ã­cone genÃ©rico).")]
        public Sprite undiscoveredPlanetIcon;

        [Header("Planet Resource Styles")]
        [Tooltip("Database opcional para aplicar cor especÃ­fica de acordo com o tipo de recurso do planeta (tamanho continua definido pelo tipo de alvo).")]
        public PlanetResourceCompassStyleDatabase planetResourceStyleDatabase;

        // Recomenda-se configurar planetas criando uma config com:
        // targetType = Planet, dynamicMode = PlanetResourceIcon, hideUntilDiscovered = true.
        // Nesse modo, iconSprite pode ficar nulo e undiscoveredPlanetIcon define o Ã­cone genÃ©rico exibido antes
        // da descoberta do recurso; apÃ³s revelado, o Ã­cone muda para o ResourceIcon do planeta e pode aplicar
        // estilos adicionais via PlanetResourceCompassStyleDatabase (apenas cor por tipo de recurso). O tamanho
        // permanece definido pelo baseSize desta configuraÃ§Ã£o de alvo type.
    }
}
