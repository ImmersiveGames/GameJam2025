using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Encapsula parâmetros de configuração para uma defesa planetária.
    /// Permite que runners recebam dados de recurso, pool e estratégia
    /// sem depender diretamente de ScriptableObjects específicos adicionais.
    /// </summary>
    public sealed class PlanetDefenseSetupContext
    {
        public PlanetDefenseSetupContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetResourcesSo planetResource = null,
            IDefenseStrategy strategy = null,
            PoolData poolData = null,
            DefenseWaveProfileSo waveProfile = null,
            PlanetDefenseLoadoutSo loadout = null)
        {
            Planet = planet;
            DetectionType = detectionType;
            PlanetResource = planetResource;
            Strategy = strategy;
            PoolData = poolData;
            WaveProfile = waveProfile;
            Loadout = loadout;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public PlanetResourcesSo PlanetResource { get; }
        public IDefenseStrategy Strategy { get; }
        public PoolData PoolData { get; }
        public DefenseWaveProfileSo WaveProfile { get; }
        public PlanetDefenseLoadoutSo Loadout { get; }

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

        DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole role,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile);
    }
}
