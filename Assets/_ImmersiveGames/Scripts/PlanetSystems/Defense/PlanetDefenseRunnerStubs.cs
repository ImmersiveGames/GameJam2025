using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IPlanetDefensePoolRunner
    {
        void WarmUp(PlanetsMaster planet, DetectionType detectionType);
        void Release(PlanetsMaster planet);

        /// <summary>
        /// Configuração opcional por planeta, permitindo customizar recurso e minion
        /// antes do aquecimento da pool.
        /// </summary>
        void ConfigureForPlanet(PlanetDefenseSetupContext context);

        /// <summary>
        /// Consulta de configuração para que serviços externos possam recuperar
        /// as preferências antes de iniciar ondas reais.
        /// </summary>
        bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context);

        /// <summary>
        /// Atalho opcional que combina configuração e aquecimento em uma única chamada.
        /// </summary>
        void WarmUp(PlanetDefenseSetupContext context);
    }

    public interface IPlanetDefenseWaveRunner
    {
        void StartWaves(PlanetsMaster planet, DetectionType detectionType);
        void StopWaves(PlanetsMaster planet);
        bool IsRunning(PlanetsMaster planet);

        /// <summary>
        /// Permite iniciar ondas com uma estratégia explícita para o planeta.
        /// </summary>
        void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy);

        /// <summary>
        /// Registra uma estratégia para ser reutilizada em futuras ativações de defesa.
        /// </summary>
        void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy);

        /// <summary>
        /// Recupera a estratégia atual configurada para o planeta.
        /// </summary>
        bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy);
    }

    /// <summary>
    /// Implementação de fallback para pools de defesa planetária.
    /// Serve apenas para remover erros de DI enquanto os sistemas reais
    /// de spawn/pool não são implementados, mantendo a arquitetura estável.
    /// </summary>
    public sealed class NullPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _configuredPlanets = new();

        public void WarmUp(PlanetsMaster planet, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<NullPlanetDefensePoolRunner>(
                $"[Stub] WarmUp ignorado para {planet?.ActorName ?? "Unknown"} ({detectionType?.TypeName ?? "Unknown"}).");
        }

        public void Release(PlanetsMaster planet)
        {
            _configuredPlanets.Remove(planet);
            DebugUtility.LogVerbose<NullPlanetDefensePoolRunner>(
                $"[Stub] Release ignorado para {planet?.ActorName ?? "Unknown"}.");
        }

        public void ConfigureForPlanet(PlanetDefenseSetupContext context)
        {
            if (context?.Planet == null)
            {
                return;
            }

            _configuredPlanets[context.Planet] = context;
            DebugUtility.LogVerbose<NullPlanetDefensePoolRunner>(
                $"[Stub] Configuração registrada para {context.Planet.ActorName} | Minion: {context.PreferredMinion?.name ?? "Nenhum"} | Recurso: {context.PlanetResource?.ResourceType.ToString() ?? "N/D"} | Estratégia: {context.Strategy?.StrategyId ?? "N/A"}.");
        }

        public bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context)
        {
            return _configuredPlanets.TryGetValue(planet, out context);
        }

        public void WarmUp(PlanetDefenseSetupContext context)
        {
            if (context == null)
            {
                return;
            }

            ConfigureForPlanet(context);
            WarmUp(context.Planet, context.DetectionType);
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
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();

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

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            ConfigureStrategy(planet, strategy);
            StartWaves(planet, detectionType);

            if (planet != null && strategy != null)
            {
                DebugUtility.LogVerbose<NullPlanetDefenseWaveRunner>(
                    $"[Stub] Estratégia '{strategy.StrategyId}' aplicada para {planet.ActorName} antes de iniciar ondas.");
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

            _strategies.Remove(planet);
        }

        public bool IsRunning(PlanetsMaster planet)
        {
            return planet != null && _running.Contains(planet);
        }

        public void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy)
        {
            if (planet == null || strategy == null)
            {
                return;
            }

            _strategies[planet] = strategy;
        }

        public bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy)
        {
            return _strategies.TryGetValue(planet, out strategy);
        }
    }
}
