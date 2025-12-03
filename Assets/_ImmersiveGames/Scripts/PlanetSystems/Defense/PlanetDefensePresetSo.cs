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
        [Header("Wave Profile (source of counts, timing, radius, height)")]
        [Tooltip("Wave profile used by default for this preset; carries enemies per wave, interval, radius and height.")]
        [SerializeField, FormerlySerializedAs("baseWaveProfile")]
        private DefenseWaveProfileSo primaryWaveProfile;

        [Header("Optional Wave Overrides")]
        [Tooltip("Allow replacing the base wave profile with a custom one for this planet.")]
        [SerializeField, FormerlySerializedAs("useCustomWaveProfile")]
        private bool useOverrideWaveProfile;

        [Tooltip("Custom wave profile when the override is enabled.")]
        [SerializeField, FormerlySerializedAs("customWaveProfile")]
        private DefenseWaveProfileSo overrideWaveProfile;

        [Tooltip("Optional spawn pattern override applied at runtime without mutating the source profile.")]
        [SerializeField, FormerlySerializedAs("spawnPatternOverride")]
        private DefenseSpawnPatternSo runtimeSpawnPatternOverride;

        [Header("Targeting")]
        [Tooltip("How minions should select targets in local multiplayer (player vs eater).")]
        [SerializeField, FormerlySerializedAs("targetMode")]
        private DefenseTargetMode preferredTargetMode = DefenseTargetMode.PreferPlayer;

        [Header("Minion Data")]
        [Tooltip("Minion data (pool/prefab + default behavior) used by this preset.")]
        [SerializeField, FormerlySerializedAs("minionData")]
        private DefensesMinionData presetMinionData;

        [Header("Advanced Overrides")]
        [Tooltip("Allow setting a specific strategy for this planet instead of relying on defaults.")]
        [SerializeField, FormerlySerializedAs("useCustomStrategy")]
        private bool useCustomDefenseStrategy;

        [Tooltip("Custom strategy applied when the override flag is enabled.")]
        [SerializeField, FormerlySerializedAs("customStrategy")]
        private DefenseStrategySo customDefenseStrategy;

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
                var sourceProfile = useOverrideWaveProfile && overrideWaveProfile != null
                    ? overrideWaveProfile
                    : primaryWaveProfile;

                if (sourceProfile == null)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = null;
                    cachedSpawnPattern = null;
                    return null;
                }

                if (runtimeSpawnPatternOverride == null)
                {
                    runtimeWaveProfileCache = null;
                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = null;
                    return sourceProfile;
                }

                if (runtimeWaveProfileCache == null || cachedWaveProfileSource != sourceProfile || cachedSpawnPattern != runtimeSpawnPatternOverride)
                {
                    runtimeWaveProfileCache = ScriptableObject.CreateInstance<DefenseWaveProfileSo>();
                    runtimeWaveProfileCache.name = $"{name}_RuntimeWaveProfile";
                    runtimeWaveProfileCache.secondsBetweenWaves = sourceProfile.secondsBetweenWaves;
                    runtimeWaveProfileCache.enemiesPerWave = sourceProfile.enemiesPerWave;
                    runtimeWaveProfileCache.spawnRadius = sourceProfile.spawnRadius;
                    runtimeWaveProfileCache.spawnHeightOffset = sourceProfile.spawnHeightOffset;
                    runtimeWaveProfileCache.defaultMinionProfile = sourceProfile.defaultMinionProfile;
                    runtimeWaveProfileCache.spawnPattern = runtimeSpawnPatternOverride;

                    cachedWaveProfileSource = sourceProfile;
                    cachedSpawnPattern = runtimeSpawnPatternOverride;
                }

                return runtimeWaveProfileCache;
            }
        }

        /// <summary>
        /// Target mode escolhido para o preset (Player/Eater), evitando SOs extras.
        /// </summary>
        public DefenseTargetMode TargetMode => preferredTargetMode;

        /// <summary>
        /// Minion data associado ao preset, mantendo SRP: o planeta escolhe o tipo
        /// de minion, não o comportamento.
        /// </summary>
        public DefensesMinionData MinionData => presetMinionData;

        /// <summary>
        /// Estratégia ativa considerando overrides avançados.
        /// </summary>
        public DefenseStrategySo CustomStrategy => useCustomDefenseStrategy ? customDefenseStrategy : null;

        private void OnValidate()
        {
            runtimeWaveProfileCache = null;
            cachedWaveProfileSource = null;
            cachedSpawnPattern = null;

            if (primaryWaveProfile == null)
            {
                Debug.LogError(
                    $"[{nameof(PlanetDefensePresetSo)}] {name} requer um {nameof(DefenseWaveProfileSo)} primário. Sem ele, o planeta não consegue orquestrar waves.",
                    this);
            }

            if (presetMinionData == null)
            {
                Debug.LogError(
                    $"[{nameof(PlanetDefensePresetSo)}] {name} requer um {nameof(DefensesMinionData)} para evitar fallback oculto.",
                    this);
            }

            if (useOverrideWaveProfile && overrideWaveProfile == null)
            {
                Debug.LogError(
                    $"[{nameof(PlanetDefensePresetSo)}] {name} está configurado para override de wave profile, mas nenhum asset foi atribuído.",
                    this);
            }

            if (useCustomDefenseStrategy && customDefenseStrategy == null)
            {
                Debug.LogError(
                    $"[{nameof(PlanetDefensePresetSo)}] {name} habilitou estratégia personalizada sem apontar um {nameof(DefenseStrategySo)}.",
                    this);
            }
        }
    }
}
