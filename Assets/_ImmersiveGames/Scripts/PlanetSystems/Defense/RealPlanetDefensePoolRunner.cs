using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que registra e aquece pools de minions utilizando o PoolSystem real.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _configured = new();
        private readonly Dictionary<PlanetsMaster, List<ObjectPool>> _planetPools = new();
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

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("PoolManager indisponível; não foi possível registrar pool de defesa.");
                return;
            }

            var poolsForPlanet = _planetPools.TryGetValue(planet, out var existingList) && existingList != null
                ? existingList
                : new List<ObjectPool>();

            foreach (var poolData in EnumeratePoolData(context))
            {
                if (poolData == null)
                {
                    continue;
                }

                if (!PoolData.Validate(poolData, planet))
                {
                    DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"PoolData inválido para {planet.ActorName}; registro de pool cancelado.");
                    continue;
                }

                if (poolsForPlanet.Exists(p => p != null && p.name == poolData.ObjectName))
                {
                    continue;
                }

                var pool = poolManager.RegisterPool(poolData);
                if (pool != null)
                {
                    poolsForPlanet.Add(pool);
                    DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolData.ObjectName}' registrada para planeta {planet.ActorName}.");
                }
            }

            _planetPools[planet] = poolsForPlanet;
        }

        public bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context)
        {
            return _configured.TryGetValue(planet, out context);
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

            if (!_planetPools.ContainsKey(planet))
            {
                ConfigureForPlanet(context);
            }

            if (!_planetPools.TryGetValue(planet, out var pools) || pools == null || pools.Count == 0)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"Pool não configurada para planeta {planet.ActorName}; warm-up cancelado.");
                return;
            }

            string poolName = context.PoolData?.ObjectName ?? pools[0].name;
            if (!_warmedPlanets.Add(planet))
            {
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' já aquecida para planeta {planet.ActorName}.");
                return;
            }

            DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' warmed for planet {planet.ActorName}.");
        }

        public void Release(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_planetPools.TryGetValue(planet, out var pools) && pools != null)
            {
                foreach (var pool in pools)
                {
                    pool?.ClearPool();
                }

                _configured.TryGetValue(planet, out var context);
                string poolName = context?.PoolData != null ? context.PoolData.ObjectName : pools[0].name;
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' cleared for planet {planet.ActorName}.");
            }

            _configured.Remove(planet);
            _planetPools.Remove(planet);
            _warmedPlanets.Remove(planet);
        }

        private IEnumerable<PoolData> EnumeratePoolData(PlanetDefenseSetupContext context)
        {
            if (context?.PoolData != null)
            {
                yield return context.PoolData;
            }

            var entry = context?.EntryConfig;
            if (entry?.defaultConfig?.minionConfig?.PoolData != null)
            {
                yield return entry.defaultConfig.minionConfig.PoolData;
            }

            if (entry?.roleConfigs != null)
            {
                foreach (var roleConfig in entry.roleConfigs)
                {
                    if (roleConfig?.minionConfig?.PoolData != null)
                    {
                        yield return roleConfig.minionConfig.PoolData;
                    }
                }
            }
        }
    }
}
