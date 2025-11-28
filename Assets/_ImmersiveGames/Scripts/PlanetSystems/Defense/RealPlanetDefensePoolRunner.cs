using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que registra e aquece pools de minions utilizando o PoolSystem real.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _configured = new();
        private readonly Dictionary<PlanetsMaster, ObjectPool> _planetPools = new();

        public void ConfigureForPlanet(PlanetDefenseSetupContext context)
        {
            if (context?.Planet == null)
            {
                return;
            }

            _configured[context.Planet] = context;
        }

        public bool TryGetConfiguration(PlanetsMaster planet, out PlanetDefenseSetupContext context)
        {
            return _configured.TryGetValue(planet, out context);
        }

        public void WarmUp(PlanetsMaster planet, DetectionType detectionType)
        {
            var context = _configured.TryGetValue(planet, out var existing)
                ? existing
                : new PlanetDefenseSetupContext(planet, detectionType);

            WarmUp(context);
        }

        public void WarmUp(PlanetDefenseSetupContext context)
        {
            if (context?.Planet == null)
            {
                return;
            }

            _configured[context.Planet] = context;

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>("PoolManager not available; cannot warm up defense pool.");
                return;
            }

            var poolData = context.PoolData;
            if (poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"PoolData not configured for planet {context.Planet.ActorName}; skipping warm up.");
                return;
            }

            if (!PoolData.Validate(poolData, context.Planet))
            {
                return;
            }

            var poolName = poolData.ObjectName;
            var pool = poolManager.GetPool(poolName) ?? poolManager.RegisterPool(poolData);
            if (pool == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"Failed to register pool '{poolName}' for planet {context.Planet.ActorName}.");
                return;
            }

            _planetPools[context.Planet] = pool;
            DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' warmed for planet {context.Planet.ActorName}.");
        }

        public void Release(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                return;
            }

            if (_planetPools.TryGetValue(planet, out var pool))
            {
                pool?.ClearPool();
                _configured.TryGetValue(planet, out var context);
                var poolName = context?.PoolData != null ? context.PoolData.ObjectName : pool.name;
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' cleared for planet {planet.ActorName}.");
            }

            _configured.Remove(planet);
            _planetPools.Remove(planet);
        }
    }
}
