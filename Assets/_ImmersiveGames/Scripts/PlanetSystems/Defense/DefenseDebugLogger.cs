using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável por logs verbosos do fluxo de defesa planetária,
    /// mantendo a formatação em um único lugar para facilitar ajustes
    /// e reutilização em testes manuais.
    /// </summary>
    public sealed class DefenseDebugLogger
    {
        public void LogEngaged(PlanetDefenseEngagedEvent engagedEvent, string detectorName)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Planeta {engagedEvent.Planet?.ActorName ?? "Unknown"} iniciou defesas contra {detectorName}.",
                DebugUtility.Colors.CrucialInfo);
        }

        public void LogDisengaged(PlanetDefenseDisengagedEvent disengagedEvent, string detectorName)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Planeta {disengagedEvent.Planet?.ActorName ?? "Unknown"} encerrou defesas contra {detectorName}.");
        }

        public void LogWaveTelemetry(PlanetDefenseState state, PlanetDefenseSpawnConfig config, float timestamp)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.PrimaryDetectionType?.TypeName ?? "Unknown"} | " +
                $"Onda: {config.DebugWaveDurationSeconds:0.##}s | Spawns previstos: {config.DebugWaveSpawnCount}. (@ {timestamp:0.00}s)");
        }

        public void LogWaveStart(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Iniciando ondas de defesa em {planet.ActorName} | Intervalo: {strategy.WaveIntervalSeconds:0.##}s | Minions/onda: {strategy.MinionsPerWave}");
        }

        public void LogWaveStop(PlanetsMaster planet)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Ondas de defesa interrompidas em {planet.ActorName}.");
        }

        public void LogPoolWarmUp(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Pool] Warming up para {planet.ActorName} com pool {strategy.PoolData?.ObjectName ?? "desconhecido"}.");
        }

        public void LogPoolRelease(PlanetsMaster planet)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Pool] Liberando recursos de defesa para {planet.ActorName}.");
        }
    }
}
