using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Representa um preset de onda isolado contendo dados mínimos para
    /// spawn consistente dos minions. Mantém o SRP ao separar a definição
    /// da onda do mapeamento por role configurado em PlanetDefenseEntrySo.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WavePreset",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Planets/Wave Preset")]
    public sealed class WavePresetSo : ScriptableObject
    {
        [Header("Pool (Obrigatório)")]
        [Tooltip("Pool de minions, obrigatório.")]
        [SerializeField]
        private PoolData poolData;

        [Header("Configuração de Onda")]
        [Tooltip("Número de inimigos por entrada, obrigatório.")]
        [SerializeField]
        private int numberOfEnemiesPerWave = 1;

        [Tooltip("Tempo entre entradas, obrigatório.")]
        [SerializeField]
        private float intervalBetweenWaves = 1f;

        [Tooltip("Padrão de posicionamento, opcional.")]
        [SerializeField]
        private DefenseSpawnPatternSo spawnPattern;

        /// <summary>
        /// Pool obrigatório para instanciar minions desta onda.
        /// </summary>
        public PoolData PoolData => poolData;

        /// <summary>
        /// Quantidade de inimigos a serem gerados em cada entrada.
        /// </summary>
        public int NumberOfEnemiesPerWave => numberOfEnemiesPerWave;

        /// <summary>
        /// Intervalo, em segundos, entre cada entrada de wave.
        /// </summary>
        public float IntervalBetweenWaves => intervalBetweenWaves;

        /// <summary>
        /// Padrão opcional de posicionamento dos minions.
        /// </summary>
        public DefenseSpawnPatternSo SpawnPattern => spawnPattern;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (poolData == null)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "PoolData obrigatório — configure ou waves falharão.",
                    this);
            }

            if (numberOfEnemiesPerWave <= 0)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "NumberOfEnemiesPerWave deve ser maior que 0.",
                    this);
            }

            if (intervalBetweenWaves <= 0f)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "IntervalBetweenWaves deve ser maior que 0.",
                    this);
            }

            if (spawnPattern != null && numberOfEnemiesPerWave <= 0)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "SpawnPattern usado, mas número de inimigos zero — configure corretamente.",
                    this);
            }
        }
#endif
    }
}
