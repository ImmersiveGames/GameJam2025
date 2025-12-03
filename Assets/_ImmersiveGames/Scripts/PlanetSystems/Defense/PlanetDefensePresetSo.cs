using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

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
        [Header("Waves")]
        [Tooltip("Quantidade de inimigos por wave.")]
        [SerializeField]
        private int waveEnemies = 6;

        [Tooltip("Intervalo, em segundos, entre waves.")]
        [SerializeField]
        private int waveIntervalSeconds = 5;

        [Tooltip("Raio de spawn ao redor do planeta.")]
        [SerializeField]
        private float waveSpawnRadius = 4f;

        [Tooltip("Offset vertical do spawn.")]
        [SerializeField]
        private float waveSpawnHeight = 0.5f;

        [Tooltip("Padrão de distribuição (opcional).")]
        [SerializeField]
        private DefenseSpawnPatternSo waveSpawnPattern;

        [Header("Minions")]
        [Tooltip("Profile padrão dos minions da wave.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO waveMinionProfile;

        [Tooltip("Dados do minion (prefab/pool).")]
        [SerializeField]
        private DefensesMinionData minionData;

        [Header("Alvo & Estratégia")]
        [Tooltip("Modo de seleção de alvo em multiplayer local.")]
        [SerializeField]
        private DefenseTargetMode targetMode = DefenseTargetMode.PreferPlayer;

        [Tooltip("Estratégia defensiva opcional específica.")]
        [SerializeField]
        private DefenseStrategySo strategy;

        private DefenseWaveProfileSo cachedWaveProfile;

        /// <summary>
        /// Quantidade de inimigos por wave, exposta com nomenclatura padronizada.
        /// </summary>
        public int PlanetDefenseWaveEnemiesCount => waveEnemies;

        /// <summary>
        /// Tempo entre waves em segundos, exposto de forma explícita.
        /// </summary>
        public int PlanetDefenseWaveSecondsBetweenWaves => waveIntervalSeconds;

        /// <summary>
        /// Raio de spawn utilizado para orbitas de wave.
        /// </summary>
        public float PlanetDefenseWaveSpawnRadius => waveSpawnRadius;

        /// <summary>
        /// Offset vertical para spawn dos inimigos.
        /// </summary>
        public float PlanetDefenseWaveSpawnHeightOffset => waveSpawnHeight;

        /// <summary>
        /// Padrão de spawn configurado no preset.
        /// </summary>
        public DefenseSpawnPatternSo PlanetDefenseWaveSpawnPattern => waveSpawnPattern;

        /// <summary>
        /// Profile padrão de comportamento de minion para esta configuração.
        /// </summary>
        public DefenseMinionBehaviorProfileSO PlanetDefenseWaveMinionProfile => waveMinionProfile;

        /// <summary>
        /// Dados de minion definidos pelo planeta (prefab/pool), respeitando SRP.
        /// </summary>
        public DefensesMinionData MinionData => minionData;

        /// <summary>
        /// Estratégia defensiva específica, caso atribuída.
        /// </summary>
        public DefenseStrategySo CustomStrategy => strategy;

        /// <summary>
        /// Modo de alvo usado para gerar estratégias simples.
        /// </summary>
        public DefenseTargetMode TargetMode => targetMode;

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

                cachedWaveProfile.enemiesPerWave = waveEnemies;
                cachedWaveProfile.secondsBetweenWaves = waveIntervalSeconds;
                cachedWaveProfile.spawnRadius = waveSpawnRadius;
                cachedWaveProfile.spawnHeightOffset = waveSpawnHeight;
                cachedWaveProfile.defaultMinionProfile = waveMinionProfile;
                cachedWaveProfile.spawnPattern = waveSpawnPattern;

                return cachedWaveProfile;
            }
        }

        private void OnValidate()
        {
            if (waveEnemies <= 0)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"{nameof(PlanetDefensePresetSo)} exige WaveEnemies > 0.", this);
                waveEnemies = 1;
            }

            if (waveIntervalSeconds <= 0)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"{nameof(PlanetDefensePresetSo)} exige WaveIntervalSeconds > 0.", this);
                waveIntervalSeconds = 1;
            }

            if (minionData == null)
            {
                DebugUtility.LogError<PlanetDefensePresetSo>($"{nameof(PlanetDefensePresetSo)} exige MinionData atribuído.", this);
            }

            waveSpawnRadius = Mathf.Max(0f, waveSpawnRadius);
        }
    }
}
