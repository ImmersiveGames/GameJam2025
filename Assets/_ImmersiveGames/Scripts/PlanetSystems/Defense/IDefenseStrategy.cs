using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Encapsula parâmetros de configuração para uma defesa planetária.
    /// Permite que runners recebam dados de recurso, configurações de entrada,
    /// minion e wave sem depender diretamente de ScriptableObjects adicionais.
    /// </summary>
    public sealed class PlanetDefenseSetupContext
    {
        public PlanetDefenseSetupContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole defenseRole,
            PlanetResourcesSo planetResource = null,
            IDefenseStrategy strategy = null,
            DefenseEntryConfigSO entryConfig = null,
            DefenseMinionConfigSO minionConfig = null,
            WavePresetSo wavePreset = null,
            Vector3 spawnOffset = default,
            float spawnRadius = 0f)
        {
            Planet = planet;
            DetectionType = detectionType;
            DefenseRole = defenseRole;
            PlanetResource = planetResource;
            Strategy = strategy;
            EntryConfig = entryConfig;
            MinionConfig = minionConfig;
            WavePreset = wavePreset;
            SpawnOffset = spawnOffset;
            SpawnRadius = spawnRadius;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; }
        public DefenseRole DefenseRole { get; }
        public PlanetResourcesSo PlanetResource { get; }
        public IDefenseStrategy Strategy { get; }
        public DefenseEntryConfigSO EntryConfig { get; }
        public DefenseMinionConfigSO MinionConfig { get; }
        public WavePresetSo WavePreset { get; }
        public Vector3 SpawnOffset { get; }
        public float SpawnRadius { get; }

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
