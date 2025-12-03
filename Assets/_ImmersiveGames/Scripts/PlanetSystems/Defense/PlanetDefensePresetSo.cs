using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Preset único de defesa para configuração no Editor, mantendo a composição
    /// com perfis existentes (waves, minion, estratégia) e evitando duplicação de dados.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefensePreset",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Preset")]
    public sealed class PlanetDefensePresetSo : ScriptableObject
    {
        [Header("Wave Profile (source of counts, timing, radius, height)")]
        [Tooltip("Wave profile used by default for this preset; carries enemies per wave, interval, radius and height.")]
        [SerializeField]
        private DefenseWaveProfileSo baseWaveProfile;

        [Header("Optional Wave Overrides")]
        [Tooltip("Allow replacing the base wave profile with a custom one for this planet.")]
        [SerializeField]
        private bool useCustomWaveProfile;

        [Tooltip("Custom wave profile when the override is enabled.")]
        [SerializeField]
        private DefenseWaveProfileSo customWaveProfile;

        [Tooltip("Optional spawn pattern override applied at runtime without mutating the source profile.")]
        [SerializeField]
        private DefenseSpawnPatternSo spawnPatternOverride;

        [Header("Targeting")]
        [Tooltip("How minions should select targets in local multiplayer (player vs eater).")]
        [SerializeField]
        private DefenseTargetMode targetMode = DefenseTargetMode.PreferPlayer;

        [Header("Minion Data")]
        [Tooltip("Minion data (pool/prefab + default behavior) used by this preset.")]
        [SerializeField]
        private DefensesMinionData minionData;

        [Header("Advanced Overrides")]
        [Tooltip("Allow setting a specific strategy for this planet instead of relying on defaults.")]
        [SerializeField]
        private bool useCustomStrategy;

        [Tooltip("Custom strategy applied when the override flag is enabled.")]
        [SerializeField]
        private DefenseStrategySo customStrategy;

        private DefenseWaveProfileSo runtimeWaveProfileCache;
        private DefenseWaveProfileSo cachedWaveProfileSource;
        private DefenseSpawnPatternSo cachedSpawnPattern;

        /// <summary>
        /// Wave profile resolvido considerando override e cache de runtime
        /// para não alocar instâncias a cada chamada.
        /// </summary>
        public DefenseWaveProfileSo ResolvedWaveProfile
        {
            get
            {
                var sourceProfile = useCustomWaveProfile && customWaveProfile != null
                    ? customWaveProfile
                    : baseWaveProfile;

                if (sourceProfile == null)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = null;
                    cachedSpawnPattern = null;
                    return null;
                }

                if (spawnPatternOverride == null)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = null;
                    return sourceProfile;
                }

                if (runtimeWaveProfileCache == null || cachedWaveProfileSource != sourceProfile || cachedSpawnPattern != spawnPatternOverride)
                {
                    runtimeWaveProfileCache = ScriptableObject.CreateInstance<DefenseWaveProfileSo>();
                    runtimeWaveProfileCache.name = $"{name}_RuntimeWaveProfile";
                    runtimeWaveProfileCache.secondsBetweenWaves = sourceProfile.secondsBetweenWaves;
                    runtimeWaveProfileCache.enemiesPerWave = sourceProfile.enemiesPerWave;
                    runtimeWaveProfileCache.spawnRadius = sourceProfile.spawnRadius;
                    runtimeWaveProfileCache.spawnHeightOffset = sourceProfile.spawnHeightOffset;
                    runtimeWaveProfileCache.defaultMinionProfile = sourceProfile.defaultMinionProfile;
                    runtimeWaveProfileCache.spawnPattern = spawnPatternOverride;

                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = spawnPatternOverride;
                }

                return runtimeWaveProfileCache;
            }
        }

        /// <summary>
        /// Target mode escolhido para o preset (Player/Eater), evitando SOs extras.
        /// </summary>
        public DefenseTargetMode TargetMode => targetMode;

        /// <summary>
        /// Minion data associado ao preset, mantendo SRP: o planeta escolhe o tipo
        /// de minion, não o comportamento.
        /// </summary>
        public DefensesMinionData MinionData => minionData;

        /// <summary>
        /// Estratégia ativa considerando overrides avançados.
        /// </summary>
        public DefenseStrategySo CustomStrategy => useCustomStrategy ? customStrategy : null;
    }
}
