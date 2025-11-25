using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Perfil configurável via Inspector para reutilizar parâmetros de spawn
    /// das defesas planetárias em vários controladores.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlanetDefenseSpawnProfile",
        menuName = "Immersive Games/Planet Defense/Spawn Profile")]
    public sealed class PlanetDefenseSpawnProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Conjunto de parâmetros usados pelo serviço de spawn de defesas.")]
        private PlanetDefenseSpawnConfig config = new();

        public PlanetDefenseSpawnConfig Config => config;

        public PlanetDefenseSpawnConfig CreateRuntimeConfig()
        {
            return new PlanetDefenseSpawnConfig
            {
                WarmUpPools = config.WarmUpPools,
                StopWavesOnDisable = config.StopWavesOnDisable,
                DefensePoolData = config.DefensePoolData,
                SpawnIntervalSeconds = config.SpawnIntervalSeconds,
                WaveDurationSeconds = config.WaveDurationSeconds,
                WaveSpawnCount = config.WaveSpawnCount
            };
        }
    }
}
