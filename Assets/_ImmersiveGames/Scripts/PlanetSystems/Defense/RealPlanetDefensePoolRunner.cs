using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que registra e aquece pools de minions utilizando o PoolSystem real.
    /// </summary>
    public sealed class RealPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _configured = new();
        private readonly Dictionary<PlanetsMaster, ObjectPool> _planetPools = new();
        private readonly HashSet<PlanetsMaster> _warmedPlanets = new();

        public void ConfigureForPlanet(PlanetDefenseSetupContext context)
        {
            if (context?.Planet == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("Contexto ou planeta inválido ao configurar defesa.");
                return;
            }

            var planet = context.Planet;
            _configured[planet] = context;

            var poolData = context.WavePreset?.PoolData;
            if (context.WavePreset == null || poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>(
                    $"PoolData ausente ou WavePreset nulo para {planet.ActorName}; defesa poderá falhar por falta de pool.");
                return;
            }

            if (!PoolData.Validate(poolData, planet))
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"PoolData inválido para {planet.ActorName}; registro de pool cancelado.");
                return;
            }

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("PoolManager indisponível; não foi possível registrar pool de defesa.");
                return;
            }

            if (_planetPools.TryGetValue(planet, out var existingPool) && existingPool != null)
            {
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolData.ObjectName}' reutilizada para planeta {planet.ActorName}.");
                return;
            }

            var pool = poolManager.RegisterPool(poolData);
            if (pool == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"Falha ao registrar pool '{poolData.ObjectName}' para planeta {planet.ActorName}.");
                return;
            }

            _planetPools[planet] = pool;
            DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolData.ObjectName}' registrada para planeta {planet.ActorName}.");
        }

        public bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context)
        {
            return _configured.TryGetValue(planet, out context);
        }

        public void WarmUp(PlanetsMaster planet, DetectionType detectionType)
        {
            var context = _configured.TryGetValue(planet, out var existing)
                ? existing
                : new PlanetDefenseSetupContext(planet, detectionType, DefenseRole.Unknown);

            WarmUp(context);
        }

        public void WarmUp(PlanetDefenseSetupContext context)
        {
            if (context?.Planet == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("Contexto ou planeta inválido ao aquecer defesa.");
                return;
            }

            var planet = context.Planet;
            _configured[planet] = context;

            var poolData = context.WavePreset?.PoolData;
            if (context.WavePreset == null || poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>(
                    $"PoolData ausente ou WavePreset nulo para {planet.ActorName}; warm-up cancelado.");
                return;
            }

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("PoolManager indisponível; warm-up não pode prosseguir.");
                return;
            }

            if (!_planetPools.ContainsKey(planet))
            {
                ConfigureForPlanet(context);
            }

            if (!_planetPools.TryGetValue(planet, out var pool) || pool == null)
            {
                pool = poolManager.RegisterPool(poolData);
                if (pool == null)
                {
                    DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"Pool não configurada para planeta {planet.ActorName}; warm-up cancelado.");
                    return;
                }
                _planetPools[planet] = pool;
            }

            string poolName = poolData.ObjectName ?? pool.name;
            if (!_warmedPlanets.Add(planet))
            {
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' já aquecida para planeta {planet.ActorName}.");
                return;
            }

            poolManager.RegisterPool(poolData);
            DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' warmed for planet {planet.ActorName}.");
        }

        public void Release(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_planetPools.TryGetValue(planet, out var pool) && pool != null)
            {
                pool.ClearPool();
                _configured.TryGetValue(planet, out var context);
                var poolData = context?.WavePreset?.PoolData;
                string poolName = poolData != null ? poolData.ObjectName : pool.name;
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' cleared for planet {planet.ActorName}.");
            }

            _configured.Remove(planet);
            _planetPools.Remove(planet);
            _warmedPlanets.Remove(planet);
        }
    }
}

