using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

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
            PlanetDefenseLoadoutSo loadout = null,
            DefenseEntryConfigSo entryConfig = null,
            DefenseEntryConfigSo.RoleDefenseConfig roleConfig = null,
            DefenseMinionConfigSo minionConfig = null,
            int minionsPerWave = 0,
            float secondsBetweenWaves = 0f,
            float spawnRadius = 0f,
            float spawnHeightOffset = 0f,
            DefenseSpawnPatternSo spawnPattern = null,
            Vector3? spawnOffset = null)
        {
            Planet = planet;
            DetectionType = detectionType;
            PlanetResource = planetResource;
            Strategy = strategy;
            PoolData = poolData;
            WaveProfile = waveProfile;
            Loadout = loadout;
            EntryConfig = entryConfig;
            RoleConfig = roleConfig;
            MinionConfig = minionConfig;
            MinionsPerWave = minionsPerWave;
            SecondsBetweenWaves = secondsBetweenWaves;
            SpawnRadius = spawnRadius;
            SpawnHeightOffset = spawnHeightOffset;
            SpawnPattern = spawnPattern;
            SpawnOffset = spawnOffset ?? Vector3.zero;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public PlanetResourcesSo PlanetResource { get; }
        public IDefenseStrategy Strategy { get; }
        public PoolData PoolData { get; }
        public DefenseWaveProfileSo WaveProfile { get; }
        public PlanetDefenseLoadoutSo Loadout { get; }
        public DefenseEntryConfigSo EntryConfig { get; }
        public DefenseEntryConfigSo.RoleDefenseConfig RoleConfig { get; }
        public DefenseMinionConfigSo MinionConfig { get; }
        public int MinionsPerWave { get; }
        public float SecondsBetweenWaves { get; }
        public float SpawnRadius { get; }
        public float SpawnHeightOffset { get; }
        public DefenseSpawnPatternSo SpawnPattern { get; }
        public Vector3 SpawnOffset { get; }

    }

    /// <summary>
    /// Define o comportamento de defesa para um planeta específico, permitindo
    /// estratégias customizadas (ex.: agressiva para Eater, defensiva para Player)
    /// baseadas no role do alvo detectado.
    /// </summary>
    public interface IDefenseStrategy
    {
        string StrategyId { get; }
        DefenseRole TargetRole { get; }

        void ConfigureContext(PlanetDefenseSetupContext context);
        void OnEngaged(PlanetsMaster planet, DetectionType detectionType);
        void OnDisengaged(PlanetsMaster planet, DetectionType detectionType);

        DefenseMinionBehaviorProfileSO SelectMinionProfile(
            DefenseRole targetRole,
            DefenseMinionBehaviorProfileSO waveProfile,
            DefenseMinionBehaviorProfileSO minionProfile);

        /// <summary>
        /// Resolve dinamicamente o <see cref="DefenseRole"/> desejado para o alvo atual,
        /// permitindo que cada estratégia aplique mapeamentos ou fallbacks sem que os
        /// chamadores precisem conhecer configurações extras (ex.: DefenseRoleConfig).
        /// </summary>
        /// <param name="targetIdentifier">Identificador textual do alvo (ex.: ActorName do detector).</param>
        /// <param name="requestedRole">Role do alvo detectado informado pelo evento que disparou a defesa.</param>
        /// <returns>Role do alvo escolhido pela estratégia, considerando mapeamentos internos e fallbacks.</returns>
        DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole);
    }
}
