using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de defesa por planeta:
    /// - PoolData usado para as defesas
    /// - Perfil de onda (wave profile)
    /// - Estratégia defensiva (próxima etapa)
    ///
    /// A ideia é que cada planeta possua um pacote completo de defesa
    /// exclusivamente via dados, sem depender de variáveis de prefab ou
    /// configurações globais compartilhadas. Esta é a "fonte única" por
    /// planeta, evitando campos duplicados em controllers.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseLoadout",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Loadout")]
    public sealed class PlanetDefenseLoadoutSo : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Wave profile usado como base para o planeta.")]
        [FormerlySerializedAs("baseWaveProfile")]
        [SerializeField]
        private DefenseWaveProfileSo defaultWaveProfile;

        [Tooltip("Modo de alvo preferido para multiplayer local (Player vs Eater).")]
        [FormerlySerializedAs("targetMode")]
        [SerializeField]
        private DefenseTargetMode preferredTargetRole = DefenseTargetMode.PreferPlayer;

        [Tooltip("PoolData usado para spawnar minions defensivos deste planeta.")]
        [FormerlySerializedAs("defensePoolData")]
        [SerializeField]
        private PoolData defensePoolData;

        [Tooltip("Dados do minion usados por este planeta (pool/prefab + comportamento padrão).")]
        [SerializeField]
        private DefensesMinionData minionData;

        [Header("Optional Overrides")]
        [Tooltip("Permite substituir o wave profile base por um customizado.")]
        [FormerlySerializedAs("useCustomWaveProfile")]
        [SerializeField]
        private bool useCustomWaveProfileOverride;

        [Tooltip("Wave profile customizado quando o override estiver habilitado.")]
        [FormerlySerializedAs("customWaveProfile")]
        [SerializeField]
        private DefenseWaveProfileSo customWaveProfileOverride;

        [Space]
        [Tooltip("Habilita override de padrão de spawn sem mutar o profile fonte.")]
        [SerializeField]
        private bool useWaveSpawnPatternOverride;

        [Tooltip("Padrão de spawn aplicado em runtime quando o override estiver ligado.")]
        [FormerlySerializedAs("spawnPatternOverride")]
        [SerializeField]
        private DefenseSpawnPatternSo waveSpawnPatternOverride;

        [Space]
        [Tooltip("Define uma estratégia específica para este planeta.")]
        [FormerlySerializedAs("useCustomStrategy")]
        [SerializeField]
        private bool useCustomDefenseStrategyOverride;

        [Tooltip("Estratégia customizada aplicada quando o override estiver habilitado.")]
        [FormerlySerializedAs("customStrategy")]
        [SerializeField]
        private DefenseStrategySo customDefenseStrategyOverride;

        [Header("Legacy (Hidden)")]
        [Tooltip("Overrides legados mantidos apenas para compatibilidade durante a migração.")]
        [FormerlySerializedAs("waveProfileOverride")]
        [SerializeField, HideInInspector]
        private DefenseWaveProfileSo planetWaveProfileOverride;

        [FormerlySerializedAs("defenseStrategy")]
        [SerializeField, HideInInspector]
        private DefenseStrategySo planetDefenseStrategyOverride;

        private DefenseWaveProfileSo runtimeWaveProfileCache;
        private DefenseWaveProfileSo cachedWaveProfileSource;
        private DefenseSpawnPatternSo cachedSpawnPattern;

        /// <summary>
        /// PoolData que o planeta quer usar como fonte única.
        /// </summary>
        public PoolData DefensePoolData => defensePoolData;

        /// <summary>
        /// Dados do minion escolhidos para este planeta.
        /// </summary>
        public DefensesMinionData MinionData => minionData;

        /// <summary>
        /// Target mode escolhido para o planeta, útil para gerar estratégia simples.
        /// </summary>
        public DefenseTargetMode PreferredTargetRole => preferredTargetRole;

        /// <summary>
        /// Estratégia customizada quando o override está ativo; caso contrário null.
        /// </summary>
        public DefenseStrategySo CustomStrategy => useCustomDefenseStrategyOverride ? customDefenseStrategyOverride : null;

        /// <summary>
        /// Wave profile resolvido considerando overrides e cache de runtime
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
                    return planetWaveProfileOverride;
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
        /// Retorna a estratégia defensiva efetiva, considerando overrides e legados.
        /// </summary>
        /// <param name="fallbackStrategy">Estratégia padrão definida pelo serviço.</param>
        public IDefenseStrategy ResolveStrategy(IDefenseStrategy fallbackStrategy)
        {
            if (CustomStrategy != null)
            {
                return CustomStrategy;
            }

            if (planetDefenseStrategyOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} ainda usa estratégia legada. Migre para os campos principais para manter SRP.");
                return planetDefenseStrategyOverride;
            }

            return fallbackStrategy;
        }

        private void OnValidate()
        {
            if (defaultWaveProfile == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} está sem DefenseWaveProfile base. Configure para evitar fallback silencioso.");
            }

            if (defensePoolData == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} está sem PoolData. Configure para evitar setup incompleto.");
            }

            if (minionData == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} está sem DefensesMinionData. Configure para manter a orquestração consistente.");
            }

            if (useCustomWaveProfileOverride && customWaveProfileOverride == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} habilitou custom wave profile mas não atribuiu um ScriptableObject. Desative o override ou forneça um profile válido.");
                useCustomWaveProfileOverride = false;
            }

            if (!useCustomWaveProfileOverride && customWaveProfileOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} possui custom wave profile preenchido mas o override está desabilitado. Limpe o campo para evitar confusão.");
            }

            if (useWaveSpawnPatternOverride && waveSpawnPatternOverride == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} habilitou spawn pattern override mas não forneceu um DefenseSpawnPatternSo. Desative o override ou atribua um padrão válido.");
                useWaveSpawnPatternOverride = false;
            }

            if (waveSpawnPatternOverride != null && defaultWaveProfile == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} define spawn pattern override mas não possui base wave profile. Configure o core antes de aplicar overrides.");
            }

            if (useCustomDefenseStrategyOverride && customDefenseStrategyOverride == null)
            {
                DebugUtility.LogError<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} habilitou custom strategy mas não atribuiu uma StrategySo. Desative o override ou forneça uma estratégia válida.");
                useCustomDefenseStrategyOverride = false;
            }

            if (!useCustomDefenseStrategyOverride && customDefenseStrategyOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} possui custom strategy preenchida mas o override está desabilitado. Limpe o campo para manter a inspeção clara.");
            }

            if (planetWaveProfileOverride != null)
            {
                DebugUtility.LogWarning<PlanetDefenseLoadoutSo>($"[PlanetDefenseLoadoutSo] {name} ainda depende de wave profile legado. Migre para os campos principais para evitar fallbacks.");
            }

            runtimeWaveProfileCache = null;
            cachedWaveProfileSource = null;
            cachedSpawnPattern = null;
        }
    }
}