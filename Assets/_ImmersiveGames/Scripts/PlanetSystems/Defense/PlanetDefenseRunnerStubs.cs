using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Implementação de fallback para pools de defesa planetária.
    /// Serve apenas para remover erros de DI enquanto os sistemas reais
    /// de spawn/pool não são implementados, mantendo a arquitetura estável.
    /// </summary>
    public sealed class NullPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        public void WarmUp(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<NullPlanetDefensePoolRunner>(
                $"[Stub] WarmUp ignorado para {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public void Release(PlanetsMaster planet)
        {
            DebugUtility.LogVerbose<NullPlanetDefensePoolRunner>(
                $"[Stub] Release ignorado para {planet?.ActorName ?? "Unknown"}.");
        }
    }

    /// <summary>
    /// Implementação de fallback para o runner de ondas de defesa planetária.
    /// Mantém apenas um estado mínimo para que os eventos de defesa consigam
    /// verificar se já existe uma execução ativa, sem realizar spawns reais.
    /// </summary>
    public sealed class NullPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner
    {
        private readonly HashSet<PlanetsMaster> _running = new();

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.Add(planet))
            {
                DebugUtility.LogVerbose<NullPlanetDefenseWaveRunner>(
                    $"[Stub] StartWaves marcado para {planet.ActorName} ({detectionType?.TypeName ?? "Unknown"}).");
            }
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.Remove(planet))
            {
                DebugUtility.LogVerbose<NullPlanetDefenseWaveRunner>(
                    $"[Stub] StopWaves marcado para {planet.ActorName}.");
            }
        }

        public bool IsRunning(PlanetsMaster planet)
        {
            return planet != null && _running.Contains(planet);
        }
    }
}
