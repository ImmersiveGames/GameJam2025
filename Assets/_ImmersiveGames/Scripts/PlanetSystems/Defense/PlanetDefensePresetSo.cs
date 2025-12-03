using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField]
        private DefenseWaveProfileSo baseWaveProfile;

        [Tooltip("How minions should select targets in local multiplayer (player vs eater).")]
        [SerializeField]
        private DefenseTargetMode targetMode = DefenseTargetMode.PreferPlayer;

        [Tooltip("Pool data that defines prefab, lifetime e comportamento padrão para minions deste preset.")]
        [SerializeField]
        private DefensesMinionData defensePoolData;

        [Header("Optional Overrides")]
        [Tooltip("Allow replacing the base wave profile with a custom one for this planet.")]
        [SerializeField]
        private bool useCustomWaveProfile;

        [Tooltip("Custom wave profile when the override is enabled.")]
        [SerializeField, HideInInspector]
        private DefenseWaveProfileSo customWaveProfile;

        [Tooltip("Optional spawn pattern override applied at runtime without mutating the source profile.")]
        [SerializeField, HideInInspector]
        private DefenseSpawnPatternSo spawnPatternOverride;

        [Tooltip("Allow setting a specific strategy for this planet instead of relying on defaults.")]
        [SerializeField]
        private bool useCustomStrategy;

        [Tooltip("Custom strategy applied when the override flag is enabled.")]
        [SerializeField, HideInInspector]
        private PlanetDefenseStrategySo customStrategy;

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
        /// Pool data associado ao preset, mantendo SRP: o planeta escolhe o tipo
        /// de minion, não o comportamento.
        /// </summary>
        public DefensesMinionData DefensePoolData => defensePoolData;

        /// <summary>
        /// Estratégia ativa considerando overrides avançados.
        /// </summary>
        public PlanetDefenseStrategySo CustomStrategy => useCustomStrategy ? customStrategy : null;

        private void OnValidate()
        {
            if (baseWaveProfile == null)
            {
                Debug.LogError($"[{nameof(PlanetDefensePresetSo)}] Base wave profile é obrigatório em {name}.");
            }

            if (defensePoolData == null)
            {
                Debug.LogError($"[{nameof(PlanetDefensePresetSo)}] Defense pool data é obrigatório em {name}.");
            }

            if (useCustomWaveProfile && customWaveProfile == null)
            {
                Debug.LogError($"[{nameof(PlanetDefensePresetSo)}] Custom wave profile habilitado mas não atribuído em {name}.");
            }

            if (useCustomStrategy && customStrategy == null)
            {
                Debug.LogError($"[{nameof(PlanetDefensePresetSo)}] Custom strategy habilitado mas não atribuído em {name}.");
            }

            if (spawnPatternOverride != null && baseWaveProfile == null)
            {
                Debug.LogError($"[{nameof(PlanetDefensePresetSo)}] Spawn pattern override requer um base wave profile em {name}.");
            }

            if (!useCustomWaveProfile && customWaveProfile != null)
            {
                Debug.LogWarning($"[{nameof(PlanetDefensePresetSo)}] customWaveProfile está definido mas o override está desabilitado em {name}.");
            }

            if (!useCustomStrategy && customStrategy != null)
            {
                Debug.LogWarning($"[{nameof(PlanetDefensePresetSo)}] customStrategy está definido mas o override está desabilitado em {name}.");
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlanetDefensePresetSo))]
        private sealed class PlanetDefensePresetSoEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseWaveProfile"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defensePoolData"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Optional Overrides", EditorStyles.boldLabel);

                var useCustomWaveProfile = serializedObject.FindProperty("useCustomWaveProfile");
                EditorGUILayout.PropertyField(useCustomWaveProfile);
                if (useCustomWaveProfile.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customWaveProfile"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPatternOverride"));
                }

                EditorGUILayout.Space();
                var useCustomStrategy = serializedObject.FindProperty("useCustomStrategy");
                EditorGUILayout.PropertyField(useCustomStrategy);
                if (useCustomStrategy.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customStrategy"));
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
