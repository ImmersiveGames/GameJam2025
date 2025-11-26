using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que registra e aquece pools de minions utilizando o PoolSystem real.
    /// </summary>
    public sealed class RealPlanetDefensePoolRunner : IPlanetDefensePoolRunner
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _configured = new();
        private readonly Dictionary<PlanetsMaster, string> _poolNames = new();

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

            var poolName = DefensePoolNaming.GetPoolName(context.Planet, context);
            if (poolManager.GetPool(poolName) != null)
            {
                _poolNames[context.Planet] = poolName;
                return;
            }

            var poolData = BuildPoolData(poolName, context.PreferredMinion);
            if (poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefensePoolRunner>($"Cannot warm up pool for {context.Planet.ActorName}: missing minion data.");
                return;
            }

            var pool = poolManager.RegisterPool(poolData);
            if (pool != null)
            {
                _poolNames[context.Planet] = poolName;
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' warmed for planet {context.Planet.ActorName}.");
            }
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

            if (_poolNames.TryGetValue(planet, out var poolName))
            {
                var pool = poolManager.GetPool(poolName);
                pool?.ClearPool();
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool '{poolName}' cleared for planet {planet.ActorName}.");
            }

            _poolNames.Remove(planet);
            _configured.Remove(planet);
        }

        private PoolData BuildPoolData(string poolName, DefensesMinionData minionData)
        {
            if (minionData == null)
            {
                return null;
            }

            var data = ScriptableObject.CreateInstance<PoolData>();
            SetPrivateField(data, "objectName", poolName);
            SetPrivateField(data, "initialPoolSize", 5);
            SetPrivateField(data, "canExpand", true);
            SetPrivateField(data, "objectConfigs", new PoolableObjectData[] { minionData });
            SetPrivateField(data, "reconfigureOnReturn", true);
            return data;
        }

        private void SetPrivateField<TValue>(PoolData data, string fieldName, TValue value)
        {
            var field = typeof(PoolData).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(data, value);
        }
    }

    public static class DefensePoolNaming
    {
        public static string GetPoolName(PlanetsMaster planet, PlanetDefenseSetupContext context)
        {
            var minionId = context?.PreferredMinion != null ? context.PreferredMinion.name : "Minion";
            var detection = context?.DetectionType?.TypeName ?? "Unknown";
            var planetName = planet != null ? planet.ActorName : "UnknownPlanet";
            return $"Defense_{planetName}_{detection}_{minionId}";
        }
    }
}
