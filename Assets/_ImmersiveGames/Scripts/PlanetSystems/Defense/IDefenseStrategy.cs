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
            WavePresetSo wavePreset = null,
            float spawnOffset = 0f,
            float spawnRadius = 0f)
        {
            Planet = planet;
            DetectionType = detectionType;
            PlanetResource = planetResource;
            Strategy = strategy;
            PoolData = poolData;
            WavePreset = wavePreset;
            SpawnOffset = spawnOffset;
            SpawnRadius = spawnRadius;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public PlanetResourcesSo PlanetResource { get; }
        public IDefenseStrategy Strategy { get; }
        public PoolData PoolData { get; }
        public WavePresetSo WavePreset { get; }
        public float SpawnOffset { get; }
        public float SpawnRadius { get; }

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

        /// <summary>
        /// Resolve dinamicamente o <see cref="DefenseRole"/> desejado para o alvo atual,
        /// permitindo que cada estratégia aplique mapeamentos ou fallbacks sem que os
        /// chamadores precisem conhecer configurações extras (ex.: DefenseRoleConfig).
        /// </summary>
        /// <param name="targetIdentifier">Identificador textual do alvo (ex.: ActorName do detector).</param>
        /// <param name="requestedRole">Role solicitado explicitamente pelo evento que disparou a defesa.</param>
        /// <returns>Role decidido pela estratégia, considerando mapeamentos internos e fallbacks.</returns>
        DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole);
    }
}
