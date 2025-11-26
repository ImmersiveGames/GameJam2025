using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Resultado da estratégia, centralizando parâmetros de spawn para
    /// que runners possam operar sem depender de configuração concreta
    /// do serviço.
    /// </summary>
    public readonly struct DefenseStrategyResult
    {
        public DefenseStrategyResult(
            DetectionType detectionType,
            PoolData poolData,
            DefensesMinionData minionData,
            int minionsPerWave,
            float waveIntervalSeconds,
            float spawnRadius)
        {
            DetectionType = detectionType;
            PoolData = poolData;
            MinionData = minionData;
            MinionsPerWave = minionsPerWave;
            WaveIntervalSeconds = waveIntervalSeconds;
            SpawnRadius = spawnRadius;
        }

        public DetectionType DetectionType { get; }
        public PoolData PoolData { get; }
        public DefensesMinionData MinionData { get; }
        public int MinionsPerWave { get; }
        public float WaveIntervalSeconds { get; }
        public float SpawnRadius { get; }
    }

    public interface IDefenseStrategy
    {
        DefenseStrategyResult BuildStrategy(PlanetsMaster planet, IDetector detector, DetectionType detectionType, PlanetDefenseSpawnConfig config);
    }

    /// <summary>
    /// Estratégia padrão que deriva parâmetros diretamente da configuração
    /// e mantém compatibilidade com o fluxo atual (apenas telemetria).
    /// </summary>
    public sealed class DefaultDefenseStrategy : IDefenseStrategy
    {
        public DefenseStrategyResult BuildStrategy(PlanetsMaster planet, IDetector detector, DetectionType detectionType, PlanetDefenseSpawnConfig config)
        {
            int minionsPerWave = config.MinionsPerWave > 0 ? config.MinionsPerWave : config.DebugWaveSpawnCount;
            float interval = config.WaveIntervalSeconds > 0f ? config.WaveIntervalSeconds : config.DebugWaveDurationSeconds;
            return new DefenseStrategyResult(
                detectionType,
                config.DefaultPoolData,
                config.DefaultMinionData,
                minionsPerWave,
                interval,
                config.SpawnRadius);
        }
    }
}
