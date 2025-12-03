using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Fonte única de configuração defensiva por planeta.
    /// É arrastada diretamente no <see cref="PlanetsMaster"/>,
    /// encapsulando ondas, alvo, minion e estratégia sem depender
    /// de ScriptableObjects legados.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefensePreset",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Defense Preset")]
    public sealed class PlanetDefensePresetSo : ScriptableObject
    {
        [Header("Wave Settings (Single Source)")]
        [Tooltip("Quantidade de inimigos que serão spawnados por wave.")]
        [SerializeField]
        private int planetDefenseWaveEnemiesCount = 6;

        [Tooltip("Intervalo, em segundos, entre waves consecutivas.")]
        [SerializeField]
        private int planetDefenseWaveSecondsBetweenWaves = 5;

        [Tooltip("Raio base utilizado para spawn orbital dos inimigos.")]
        [SerializeField]
        private float planetDefenseWaveSpawnRadius = 4f;

        [Tooltip("Offset vertical aplicado ao spawn, relativo ao planeta.")]
        [SerializeField]
        private float planetDefenseWaveSpawnHeightOffset = 0.5f;

        [Tooltip("Padrão de distribuição espacial por wave (opcional).")]
        [SerializeField]
        private DefenseSpawnPatternSo planetDefenseWaveSpawnPattern;

        [Header("Minion Defaults")]
        [Tooltip("Profile padrão aplicado aos minions spawnados pelas waves.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO planetDefenseWaveMinionProfile;

        [Tooltip("Dados de minion usados pelo planeta (prefab/pool).")]
        [SerializeField]
        private DefensesMinionData planetDefenseMinionData;

        [Header("Strategy & Targeting")]
        [Tooltip("Modo de seleção de alvo em multiplayer local.")]
        [SerializeField]
        private DefenseTargetMode planetDefenseTargetMode = DefenseTargetMode.PreferPlayer;

        [Tooltip("Estratégia defensiva opcional específica deste planeta.")]
        [SerializeField]
        private DefenseStrategySo planetDefenseStrategy;

        private DefenseWaveProfileSo cachedWaveProfile;

        /// <summary>
        /// Quantidade de inimigos por wave, exposta com nomenclatura padronizada.
        /// </summary>
        public int PlanetDefenseWaveEnemiesCount => planetDefenseWaveEnemiesCount;

        /// <summary>
        /// Tempo entre waves em segundos, exposto de forma explícita.
        /// </summary>
        public int PlanetDefenseWaveSecondsBetweenWaves => planetDefenseWaveSecondsBetweenWaves;

        /// <summary>
        /// Raio de spawn utilizado para orbitas de wave.
        /// </summary>
        public float PlanetDefenseWaveSpawnRadius => planetDefenseWaveSpawnRadius;

        /// <summary>
        /// Offset vertical para spawn dos inimigos.
        /// </summary>
        public float PlanetDefenseWaveSpawnHeightOffset => planetDefenseWaveSpawnHeightOffset;

        /// <summary>
        /// Padrão de spawn configurado no preset.
        /// </summary>
        public DefenseSpawnPatternSo PlanetDefenseWaveSpawnPattern => planetDefenseWaveSpawnPattern;

        /// <summary>
        /// Profile padrão de comportamento de minion para esta configuração.
        /// </summary>
        public DefenseMinionBehaviorProfileSO PlanetDefenseWaveMinionProfile => planetDefenseWaveMinionProfile;

        /// <summary>
        /// Dados de minion definidos pelo planeta (prefab/pool), respeitando SRP.
        /// </summary>
        public DefensesMinionData MinionData => planetDefenseMinionData;

        /// <summary>
        /// Estratégia defensiva específica, caso atribuída.
        /// </summary>
        public DefenseStrategySo CustomStrategy => planetDefenseStrategy;

        /// <summary>
        /// Modo de alvo usado para gerar estratégias simples.
        /// </summary>
        public DefenseTargetMode TargetMode => planetDefenseTargetMode;

        /// <summary>
        /// Perfil de wave resolvido a partir dos dados internos, sem SO legados.
        /// Mantém cache para evitar alocações extras em multiplayer local.
        /// </summary>
        public DefenseWaveProfileSo ResolvedWaveProfile
        {
            get
            {
                if (cachedWaveProfile == null)
                {
                    cachedWaveProfile = ScriptableObject.CreateInstance<DefenseWaveProfileSo>();
                    cachedWaveProfile.name = $"{name}_WaveProfile";
                }

                cachedWaveProfile.enemiesPerWave = planetDefenseWaveEnemiesCount;
                cachedWaveProfile.secondsBetweenWaves = planetDefenseWaveSecondsBetweenWaves;
                cachedWaveProfile.spawnRadius = planetDefenseWaveSpawnRadius;
                cachedWaveProfile.spawnHeightOffset = planetDefenseWaveSpawnHeightOffset;
                cachedWaveProfile.defaultMinionProfile = planetDefenseWaveMinionProfile;
                cachedWaveProfile.spawnPattern = planetDefenseWaveSpawnPattern;

                return cachedWaveProfile;
            }
        }

        private void OnValidate()
        {
            if (planetDefenseWaveEnemiesCount <= 0)
            {
                Debug.LogError($"{nameof(PlanetDefensePresetSo)} exige PlanetDefenseWaveEnemiesCount > 0.", this);
                planetDefenseWaveEnemiesCount = 1;
            }

            if (planetDefenseWaveSecondsBetweenWaves <= 0)
            {
                Debug.LogError($"{nameof(PlanetDefensePresetSo)} exige PlanetDefenseWaveSecondsBetweenWaves > 0.", this);
                planetDefenseWaveSecondsBetweenWaves = 1;
            }

            if (planetDefenseMinionData == null)
            {
                Debug.LogError($"{nameof(PlanetDefensePresetSo)} exige PlanetDefenseMinionData atribuído.", this);
            }

            planetDefenseWaveSpawnRadius = Mathf.Max(0f, planetDefenseWaveSpawnRadius);
        }
    }
}
