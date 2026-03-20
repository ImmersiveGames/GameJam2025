using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Representa um preset de wave, definindo quantas vezes uma entrada Ã©
    /// disparada e quantos minions surgem em cada disparo (lote). MantÃ©m o
    /// SRP ao separar a configuraÃ§Ã£o de wave do mapeamento por alvo role
    /// feito pelo DefenseEntryConfigSO.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WavePreset",
        menuName = "ImmersiveGames/Legacy/PlanetSystems/Defense/Planets/Wave Preset")]
    public sealed class WavePresetSo : ScriptableObject
    {
        [Header("Pool (ObrigatÃ³rio)")]
        [Tooltip("Pool de minions, obrigatÃ³rio.")]
        [SerializeField]
        private PoolData poolData;

        [Header("ConfiguraÃ§Ã£o de Onda")]
        [Tooltip("Quantidade de minions por disparo de entrada (lote), obrigatÃ³rio.")]
        [FormerlySerializedAs("numberOfEnemiesPerWave")]
        [SerializeField]
        private int numberOfMinionsPerWave = 1;

        [Tooltip("Tempo entre waves (disparos da mesma entrada), obrigatÃ³rio.")]
        [SerializeField]
        private float intervalBetweenWaves = 1f;

        [Tooltip("PadrÃ£o de posicionamento, opcional.")]
        [SerializeField]
        private DefenseSpawnPatternSo spawnPattern;

        [Header("Comportamento da Wave (opcional)")]
        [Tooltip("Perfil de comportamento especÃ­fico desta wave, opcional.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSo waveBehaviorProfile;

        /// <summary>
        /// Pool obrigatÃ³rio para instanciar minions desta onda.
        /// </summary>
        public PoolData PoolData => poolData;

        /// <summary>
        /// Quantidade de minions gerados em cada disparo da entrada (uma wave).
        /// </summary>
        public int NumberOfMinionsPerWave => numberOfMinionsPerWave;

        /// <summary>
        /// Intervalo, em segundos, entre cada wave disparada pela mesma entrada.
        /// </summary>
        public float IntervalBetweenWaves => intervalBetweenWaves;

        /// <summary>
        /// PadrÃ£o opcional de posicionamento dos minions.
        /// </summary>
        public DefenseSpawnPatternSo SpawnPattern => spawnPattern;

        /// <summary>
        /// Perfil opcional que ajusta o comportamento dos minions desta wave.
        /// </summary>
        public DefenseMinionBehaviorProfileSo WaveBehaviorProfile => waveBehaviorProfile;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (poolData == null)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "PoolData obrigatÃ³rio â€” configure ou waves falharÃ£o.",
                    this);
            }

            if (numberOfMinionsPerWave <= 0)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "NumberOfMinionsPerWave deve ser maior que 0.",
                    this);
            }

            if (intervalBetweenWaves <= 0f)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "IntervalBetweenWaves deve ser maior que 0.",
                    this);
            }

            if (spawnPattern != null && numberOfMinionsPerWave <= 0)
            {
                DebugUtility.LogError<WavePresetSo>(
                    "SpawnPattern usado, mas nÃºmero de minions zero â€” configure corretamente.",
                    this);
            }
        }
#endif
    }
}

