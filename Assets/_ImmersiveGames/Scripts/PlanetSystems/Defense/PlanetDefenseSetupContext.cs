using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Encapsula parâmetros de configuração para uma defesa planetária.
    /// Permite que runners recebam dados de recurso, configurações de entrada,
    /// perfil de comportamento de minion e wave sem depender diretamente de
    /// ScriptableObjects adicionais, mantendo o contexto livre de PoolData
    /// (responsabilidade do PoolSystem).
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
            DefenseMinionBehaviorProfileSO minionBehaviorProfile = null,
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
            MinionBehaviorProfile = minionBehaviorProfile;
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
        public DefenseMinionBehaviorProfileSO MinionBehaviorProfile { get; }
        public WavePresetSo WavePreset { get; }
        public Vector3 SpawnOffset { get; }
        public float SpawnRadius { get; }
    }
}
