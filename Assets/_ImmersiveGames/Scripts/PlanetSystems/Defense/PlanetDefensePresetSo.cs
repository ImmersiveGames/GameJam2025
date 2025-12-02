using System;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Preset único e direto para configurar a defesa de um planeta no Editor.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefensePreset",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Preset")]
    public sealed class PlanetDefensePresetSo : ScriptableObject
    {
        [Header("Wave Settings")]
        [SerializeField, Min(1)] private int enemiesPerWave = 6;
        [SerializeField, Min(1)] private int secondsBetweenWaves = 5;
        [SerializeField] private float spawnRadius = 4f;
        [SerializeField] private float spawnHeightOffset = 0.5f;
        [SerializeField, Tooltip("Padrão opcional de spawn em órbita.")]
        private DefenseSpawnPatternSo spawnPattern;

        [Header("Minion Type")]
        [SerializeField, Tooltip("Tipo de minion defensivo (define comportamento via BehaviorProfile).")]
        private DefensesMinionData minionData;
        [SerializeField, Tooltip("PoolData que será usado para spawnar o minion.")]
        private PoolData defensePoolData;

        [Header("Targeting")]
        [SerializeField, Tooltip("Quem o planeta deve priorizar ao defender.")]
        private DefenseTargetMode targetMode = DefenseTargetMode.PreferPlayer;

        [Header("Advanced Overrides")]
        [SerializeField, Tooltip("Usar uma estratégia customizada em vez do alvo padrão do preset.")]
        private bool useCustomStrategy;
        [SerializeField, Tooltip("Estratégia customizada opcional.")]
        private DefenseStrategySo customStrategy;
        [SerializeField, Tooltip("Usar um WaveProfile já existente ao invés de gerar a partir do preset.")]
        private bool useCustomWaveProfile;
        [SerializeField, Tooltip("Wave profile customizado (opcional).")]
        private DefenseWaveProfileSo customWaveProfile;

        [NonSerialized] private DefenseWaveProfileSo _runtimeWaveProfile;
        [NonSerialized] private PresetDefenseStrategy _runtimeStrategy;

        public PoolData DefensePoolData => defensePoolData;
        public DefensesMinionData MinionData => minionData;
        public DefenseTargetMode TargetMode => targetMode;
        public bool UseCustomStrategy => useCustomStrategy;
        public bool UseCustomWaveProfile => useCustomWaveProfile;
        public DefenseStrategySo CustomStrategy => customStrategy;
        public DefenseWaveProfileSo CustomWaveProfile => customWaveProfile;

        /// <summary>
        /// Gera (ou reutiliza) um WaveProfile simples a partir dos campos do preset.
        /// Nunca grava o profile em disco; ele só vive em memória.
        /// </summary>
        public DefenseWaveProfileSo GetWaveProfile()
        {
            if (useCustomWaveProfile && customWaveProfile != null)
            {
                return customWaveProfile;
            }

            if (_runtimeWaveProfile == null)
            {
                _runtimeWaveProfile = CreateInstance<DefenseWaveProfileSo>();
            }

            _runtimeWaveProfile.secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            _runtimeWaveProfile.enemiesPerWave = Mathf.Max(1, enemiesPerWave);
            _runtimeWaveProfile.spawnRadius = Mathf.Max(0f, spawnRadius);
            _runtimeWaveProfile.spawnHeightOffset = spawnHeightOffset;
            _runtimeWaveProfile.spawnPattern = spawnPattern;
            _runtimeWaveProfile.defaultMinionProfile = minionData != null
                ? minionData.BehaviorProfileV2
                : null;

            return _runtimeWaveProfile;
        }

        /// <summary>
        /// Resolve a estratégia final considerando o modo de alvo e overrides.
        /// </summary>
        public IDefenseStrategy ResolveStrategy(IDefenseStrategy fallbackStrategy)
        {
            if (useCustomStrategy && customStrategy != null)
            {
                return customStrategy;
            }

            _ = fallbackStrategy; // Mantém compatibilidade sem perder warnings de parâmetro não usado.
            _runtimeStrategy ??= new PresetDefenseStrategy(this);
            return _runtimeStrategy;
        }

        public PoolData ResolvePoolData(PoolData fallbackPool)
        {
            return defensePoolData != null ? defensePoolData : fallbackPool;
        }

        private void OnValidate()
        {
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
            secondsBetweenWaves = Mathf.Max(1, secondsBetweenWaves);
            spawnRadius = Mathf.Max(0f, spawnRadius);
        }

        private sealed class PresetDefenseStrategy : IDefenseStrategy
        {
            private readonly PlanetDefensePresetSo _preset;

            public PresetDefenseStrategy(PlanetDefensePresetSo preset)
            {
                _preset = preset;
            }

            public string StrategyId => $"Preset::{_preset.name}";

            public DefenseRole TargetRole => _preset.targetMode switch
            {
                DefenseTargetMode.PlayerOnly => DefenseRole.Player,
                DefenseTargetMode.EaterOnly => DefenseRole.Eater,
                DefenseTargetMode.PreferPlayer => DefenseRole.Player,
                DefenseTargetMode.PreferEater => DefenseRole.Eater,
                _ => DefenseRole.Unknown
            };

            public void ConfigureContext(PlanetDefenseSetupContext context)
            {
                // Context já vem pronto do preset; nada extra a configurar.
            }

            public void OnEngaged(PlanetsMaster planet, _ImmersiveGames.Scripts.DetectionsSystems.Core.DetectionType detectionType)
            {
                // Uso leve de logs para manter neutralidade de runtime.
                _ImmersiveGames.Scripts.Utils.DebugSystems.DebugUtility.LogVerbose<PresetDefenseStrategy>(
                    $"[PresetStrategy] Engaged for {planet?.ActorName ?? "Unknown"} with TargetMode={_preset.targetMode}.");
            }

            public void OnDisengaged(PlanetsMaster planet, _ImmersiveGames.Scripts.DetectionsSystems.Core.DetectionType detectionType)
            {
                _ImmersiveGames.Scripts.Utils.DebugSystems.DebugUtility.LogVerbose<PresetDefenseStrategy>(
                    $"[PresetStrategy] Disengaged for {planet?.ActorName ?? "Unknown"}.");
            }

            public DefenseMinionBehaviorProfileSO SelectMinionProfile(
                DefenseRole role,
                DefenseMinionBehaviorProfileSO waveProfile,
                DefenseMinionBehaviorProfileSO minionProfile)
            {
                return waveProfile != null ? waveProfile : minionProfile;
            }

            public DefenseRole ResolveTargetRole(string targetIdentifier, DefenseRole requestedRole)
            {
                if (requestedRole != DefenseRole.Unknown)
                {
                    return requestedRole;
                }

                return _preset.targetMode switch
                {
                    DefenseTargetMode.PlayerOnly => DefenseRole.Player,
                    DefenseTargetMode.EaterOnly => DefenseRole.Eater,
                    DefenseTargetMode.PreferPlayer => DefenseRole.Player,
                    DefenseTargetMode.PreferEater => DefenseRole.Eater,
                    DefenseTargetMode.PlayerOrEater => DefenseRole.Unknown,
                    _ => TargetRole
                };
            }
        }
    }
}
