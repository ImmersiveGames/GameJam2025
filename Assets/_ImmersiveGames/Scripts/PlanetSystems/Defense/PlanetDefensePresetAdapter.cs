using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Adaptador estático para converter <see cref="PlanetDefensePresetSo"/> em <see cref="PlanetDefenseSetupContext"/>,
    /// garantindo compatibilidade com o sistema existente e preservando fallbacks para SOs antigos.
    /// Comentários em português brasileiro, código em inglês conforme convenção.
    /// </summary>
    public static class PlanetDefensePresetAdapter
    {
        private static readonly Dictionary<PlanetsMaster, CachedContext> CachedContexts = new();
        private static readonly Dictionary<DefenseTargetMode, SimplePlanetDefenseStrategy> StrategyCache = new();
        private static readonly object SyncRoot = new();

        /// <summary>
        /// Resolve um contexto de defesa a partir de um preset, aplicando fallbacks legados
        /// (pool/wave/estratégia) quando necessário e cacheando o resultado para o planeta.
        /// </summary>
        public static PlanetDefenseSetupContext Resolve(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetDefensePresetSo preset,
            PoolData defaultPoolData = null,
            DefenseWaveProfileSo legacyWaveProfile = null,
            IDefenseStrategy legacyStrategy = null,
            PlanetDefenseLoadoutSo legacyLoadout = null)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefensePresetAdapter>("Tentativa de resolver preset com planeta nulo.");
                return null;
            }

            lock (SyncRoot)
            {
                if (TryReuseCachedContext(planet, detectionType, preset, legacyWaveProfile, legacyStrategy, legacyLoadout, out var cached))
                {
                    return cached.Context;
                }

                var poolData = legacyLoadout?.DefensePoolData ?? defaultPoolData;
                var waveProfile = ResolveWaveProfile(preset, legacyLoadout, legacyWaveProfile);
                var strategy = ResolveStrategy(preset, legacyLoadout, legacyStrategy);
                var resource = planet.HasAssignedResource ? planet.AssignedResource : null;

                var context = new PlanetDefenseSetupContext(
                    planet,
                    detectionType,
                    resource,
                    strategy,
                    poolData,
                    waveProfile,
                    legacyLoadout);

                strategy?.ConfigureContext(context);

                CacheContext(planet, detectionType, preset, legacyWaveProfile, legacyStrategy, legacyLoadout, context);
                LogResolvedContext(context, preset);

                return context;
            }
        }

        /// <summary>
        /// Limpa o cache para um planeta específico, permitindo reconfiguração manual.
        /// </summary>
        public static void ClearCache(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                CachedContexts.Remove(planet);
            }
        }

        /// <summary>
        /// Limpa todo o cache para cenários de multiplayer local onde presets mudam entre partidas.
        /// </summary>
        public static void ClearAll()
        {
            lock (SyncRoot)
            {
                CachedContexts.Clear();
            }
        }

        private static bool TryReuseCachedContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetDefensePresetSo preset,
            DefenseWaveProfileSo legacyWaveProfile,
            IDefenseStrategy legacyStrategy,
            PlanetDefenseLoadoutSo legacyLoadout,
            out CachedContext cached)
        {
            if (CachedContexts.TryGetValue(planet, out cached))
            {
                bool sameDetection = cached.Context?.DetectionType == detectionType;
                bool samePreset = cached.Preset == preset;
                bool sameLegacyWave = cached.LegacyWaveProfile == legacyWaveProfile;
                bool sameLegacyStrategy = cached.LegacyStrategy == legacyStrategy;
                bool sameLoadout = cached.LegacyLoadout == legacyLoadout;

                if (sameDetection && samePreset && sameLegacyWave && sameLegacyStrategy && sameLoadout)
                {
                    DebugUtility.LogVerbose<PlanetDefensePresetAdapter>(
                        $"[Cache] Reutilizando contexto para {planet.ActorName} (Preset='{preset?.name ?? "null"}').");
                    return true;
                }
            }

            cached = default;
            return false;
        }

        private static DefenseWaveProfileSo ResolveWaveProfile(
            PlanetDefensePresetSo preset,
            PlanetDefenseLoadoutSo legacyLoadout,
            DefenseWaveProfileSo legacyWaveProfile)
        {
            if (preset?.ResolvedWaveProfile != null)
            {
                return preset.ResolvedWaveProfile;
            }

            if (legacyLoadout?.WaveProfileOverride != null)
            {
                return legacyLoadout.WaveProfileOverride;
            }

            return legacyWaveProfile;
        }

        private static IDefenseStrategy ResolveStrategy(
            PlanetDefensePresetSo preset,
            PlanetDefenseLoadoutSo legacyLoadout,
            IDefenseStrategy legacyStrategy)
        {
            if (preset?.CustomStrategy != null)
            {
                return preset.CustomStrategy;
            }

            if (preset != null)
            {
                return GetOrCreateSimpleStrategy(preset.TargetMode);
            }

            if (legacyLoadout?.DefenseStrategy != null)
            {
                return legacyLoadout.DefenseStrategy;
            }

            return legacyStrategy ?? GetOrCreateSimpleStrategy(DefenseTargetMode.PreferPlayer);
        }

        private static SimplePlanetDefenseStrategy GetOrCreateSimpleStrategy(DefenseTargetMode targetMode)
        {
            if (StrategyCache.TryGetValue(targetMode, out var cached))
            {
                return cached;
            }

            var strategy = new SimplePlanetDefenseStrategy(targetMode);
            StrategyCache[targetMode] = strategy;
            return strategy;
        }

        private static void CacheContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            PlanetDefensePresetSo preset,
            DefenseWaveProfileSo legacyWaveProfile,
            IDefenseStrategy legacyStrategy,
            PlanetDefenseLoadoutSo legacyLoadout,
            PlanetDefenseSetupContext context)
        {
            CachedContexts[planet] = new CachedContext
            {
                Context = context,
                DetectionType = detectionType,
                Preset = preset,
                LegacyWaveProfile = legacyWaveProfile,
                LegacyStrategy = legacyStrategy,
                LegacyLoadout = legacyLoadout
            };
        }

        private static void LogResolvedContext(PlanetDefenseSetupContext context, PlanetDefensePresetSo preset)
        {
            var planetName = context.Planet?.ActorName ?? "Unknown";
            var poolName = context.PoolData?.name ?? "null";
            var waveName = context.WaveProfile?.name ?? "null";
            var strategyId = context.Strategy?.StrategyId ?? "null";
            var presetName = preset?.name ?? "null";

            DebugUtility.LogVerbose<PlanetDefensePresetAdapter>(
                $"[PresetContext] Planeta={planetName}; Preset={presetName}; Pool={poolName}; WaveProfile={waveName}; Strategy={strategyId}.");
        }

        private struct CachedContext
        {
            public PlanetDefenseSetupContext Context { get; set; }
            public DetectionType DetectionType { get; set; }
            public PlanetDefensePresetSo Preset { get; set; }
            public DefenseWaveProfileSo LegacyWaveProfile { get; set; }
            public IDefenseStrategy LegacyStrategy { get; set; }
            public PlanetDefenseLoadoutSo LegacyLoadout { get; set; }
        }
    }
}
