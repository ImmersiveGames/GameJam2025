using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Header("Core Settings")]
        [Tooltip("Wave profile used by default for this preset; carries enemies per wave, interval, radius and height.")]
        [FormerlySerializedAs("baseWaveProfile")]
        [SerializeField]
        private DefenseWaveProfileSo defaultWaveProfile;

        [Tooltip("How minions should select targets in local multiplayer (player vs eater).")]
        [FormerlySerializedAs("targetMode")]
        [SerializeField]
        private DefenseTargetMode preferredTargetRole = DefenseTargetMode.PreferPlayer;

        [Tooltip("Minion data (pool/prefab + default behavior) used by this preset.")]
        [SerializeField]
        private DefensesMinionData minionData;

        [Header("Optional Overrides")]
        [Tooltip("Allow replacing the base wave profile with a custom one for this planet.")]
        [FormerlySerializedAs("useCustomWaveProfile")]
        [SerializeField]
        private bool useCustomWaveProfileOverride;

        [Tooltip("Custom wave profile when the override is enabled.")]
        [FormerlySerializedAs("customWaveProfile")]
        [SerializeField]
        private DefenseWaveProfileSo customWaveProfileOverride;

        [Space]
        [Tooltip("Enable a spawn pattern override without mutating the source wave profile.")]
        [SerializeField]
        private bool useWaveSpawnPatternOverride;

        [Tooltip("Optional spawn pattern override applied at runtime without mutating the source profile.")]
        [FormerlySerializedAs("spawnPatternOverride")]
        [SerializeField]
        private DefenseSpawnPatternSo waveSpawnPatternOverride;

        [Space]
        [Tooltip("Allow setting a specific strategy for this planet instead of relying on defaults.")]
        [FormerlySerializedAs("useCustomStrategy")]
        [SerializeField]
        private bool useCustomDefenseStrategyOverride;

        [Tooltip("Custom strategy applied when the override flag is enabled.")]
        [FormerlySerializedAs("customStrategy")]
        [SerializeField]
        private DefenseStrategySo customDefenseStrategyOverride;

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
                var sourceProfile = useCustomWaveProfileOverride && customWaveProfileOverride != null
                    ? customWaveProfileOverride
                    : defaultWaveProfile;

                if (sourceProfile == null)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = null;
                    cachedSpawnPattern = null;
                    return null;
                }

                var applySpawnPatternOverride = useWaveSpawnPatternOverride && waveSpawnPatternOverride != null;

                if (!applySpawnPatternOverride)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = null;
                    return sourceProfile;
                }

                if (runtimeWaveProfileCache == null || cachedWaveProfileSource != sourceProfile || cachedSpawnPattern != waveSpawnPatternOverride)
                {
                    runtimeWaveProfileCache = ScriptableObject.CreateInstance<DefenseWaveProfileSo>();
                    runtimeWaveProfileCache.name = $"{name}_RuntimeWaveProfile";
                    runtimeWaveProfileCache.secondsBetweenWaves = sourceProfile.secondsBetweenWaves;
                    runtimeWaveProfileCache.enemiesPerWave = sourceProfile.enemiesPerWave;
                    runtimeWaveProfileCache.spawnRadius = sourceProfile.spawnRadius;
                    runtimeWaveProfileCache.spawnHeightOffset = sourceProfile.spawnHeightOffset;
                    runtimeWaveProfileCache.defaultMinionProfile = sourceProfile.defaultMinionProfile;
                    runtimeWaveProfileCache.spawnPattern = waveSpawnPatternOverride;

                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = waveSpawnPatternOverride;
                }

                return runtimeWaveProfileCache;
            }
        }

        /// <summary>
        /// Target mode escolhido para o preset (Player/Eater), evitando SOs extras.
        /// </summary>
        public DefenseTargetMode PreferredTargetRole => preferredTargetRole;

        [System.Obsolete("Use PreferredTargetRole instead for clarity.")]
        public DefenseTargetMode TargetMode => preferredTargetRole;

        /// <summary>
        /// Minion data associado ao preset, mantendo SRP: o planeta escolhe o tipo
        /// de minion, não o comportamento.
        /// </summary>
        public DefensesMinionData MinionData => minionData;

        /// <summary>
        /// Estratégia ativa considerando overrides avançados.
        /// </summary>
        public DefenseStrategySo CustomStrategy => useCustomDefenseStrategyOverride ? customDefenseStrategyOverride : null;

        private void OnValidate()
        {
            if (defaultWaveProfile == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} está sem DefenseWaveProfile base. Configure para evitar fallback silencioso.");
            }

            if (minionData == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} está sem DefensesMinionData. Configure para manter a orquestração consistente.");
            }

            if (useCustomWaveProfileOverride && customWaveProfileOverride == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} habilitou custom wave profile mas não atribuiu um ScriptableObject. Desative o override ou forneça um profile válido.");
                useCustomWaveProfileOverride = false;
            }

            if (!useCustomWaveProfileOverride && customWaveProfileOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} possui custom wave profile preenchido mas o override está desabilitado. Limpe o campo para evitar confusão.");
            }

            if (useWaveSpawnPatternOverride && waveSpawnPatternOverride == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} habilitou spawn pattern override mas não forneceu um DefenseSpawnPatternSo. Desative o override ou atribua um padrão válido.");
                useWaveSpawnPatternOverride = false;
            }

            if (waveSpawnPatternOverride != null && defaultWaveProfile == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} define spawn pattern override mas não possui base wave profile. Configure o core antes de aplicar overrides.");
            }

            if (useCustomDefenseStrategyOverride && customDefenseStrategyOverride == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} habilitou custom strategy mas não atribuiu uma StrategySo. Desative o override ou forneça uma estratégia válida.");
                useCustomDefenseStrategyOverride = false;
            }

            if (!useCustomDefenseStrategyOverride && customDefenseStrategyOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefensePresetSo>($"[PlanetDefensePresetSo] {name} possui custom strategy preenchida mas o override está desabilitado. Limpe o campo para manter a inspeção clara.");
            }

            runtimeWaveProfileCache = null;
            cachedWaveProfileSource = null;
            cachedSpawnPattern = null;
        }
    }

}
