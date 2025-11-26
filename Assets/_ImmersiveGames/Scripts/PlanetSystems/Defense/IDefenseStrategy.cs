using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Encapsula parâmetros de configuração para uma defesa planetária.
    /// Permite que runners recebam dados de minion, recurso e estratégia
    /// sem depender diretamente de ScriptableObjects específicos.
    /// </summary>
    public sealed class PlanetDefenseSetupContext
    {
        public PlanetDefenseSetupContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetResourcesSo planetResource = null,
            DefensesMinionData preferredMinion = null,
            IDefenseStrategy strategy = null)
        {
            Planet = planet;
            DetectionType = detectionType;
            PlanetResource = planetResource;
            PreferredMinion = preferredMinion;
            Strategy = strategy;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public PlanetResourcesSo PlanetResource { get; }
        public DefensesMinionData PreferredMinion { get; }
        public IDefenseStrategy Strategy { get; }

        public bool HasResource => PlanetResource != null;
        public bool HasPreferredMinion => PreferredMinion != null;
        public bool HasStrategy => Strategy != null;
    }

    /// <summary>
    /// Define o comportamento de defesa para um planeta específico, permitindo
    /// estratégias customizadas (ex.: agressiva para Eater, defensiva para Player).
    /// </summary>
    public interface IDefenseStrategy
    {
        string StrategyId { get; }
        DefenseRole TargetRole { get; }

        void ConfigureContext(PlanetDefenseSetupContext context);
        void OnEngaged(PlanetsMaster planet, DetectionType detectionType);
        void OnDisengaged(PlanetsMaster planet, DetectionType detectionType);
    }
}
