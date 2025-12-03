using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.SkinSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Orquestrador focado em preparar contexto, pools e runner de waves.
    /// Mantém cache por planeta e delega logs ao DebugUtility para acompanhamento no Editor.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseOrchestrationService : IPlanetDefenseSetupOrchestrator
    {
        private readonly Dictionary<PlanetsMaster, DefenseEntryConfiguration> _configuredDefenseEntries = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<(DetectionType detectionType, DefenseRole role), PlanetDefenseSetupContext>> _resolvedContexts = new();
        private readonly Dictionary<PlanetsMaster, int> _sequentialIndices = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<int, PlanetDefenseEntrySo>> _sequentialEntryCache = new();
        private readonly Dictionary<PlanetsMaster, float> _cachedApproxRadii = new();
        private const bool WarmUpPools = true;
        private const bool ReleasePoolsOnDisable = true;

        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseOrchestrationService);

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveDependenciesFromProvider();
        }

        public void ConfigureDefenseEntries(
            PlanetsMaster planet,
            IReadOnlyList<PlanetDefenseEntrySo> defenseEntries,
            DefenseChoiceMode defenseChoiceMode)
        {
            if (planet == null)
            {
                return;
            }

            var entries = defenseEntries ?? Array.Empty<PlanetDefenseEntrySo>();
            _configuredDefenseEntries[planet] = new DefenseEntryConfiguration(entries, defenseChoiceMode);
            ClearCachedContext(planet);
            PreloadDefensePools(entries, planet);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[DefenseEntries] Planeta {planet.ActorName} configurado com {entries.Count} entradas (modo: {defenseChoiceMode}).");
        }

        public PlanetDefenseSetupContext ResolveEffectiveConfig(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole detectionRole)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Planeta nulo ao resolver configuração de defesa.");
                return null;
            }

            if (TryReuseCachedContext(planet, detectionType, detectionRole, out var cached))
            {
                return cached;
            }

            var resource = planet.HasAssignedResource ? planet.AssignedResource : null;
            var context = ResolveEntryContext(planet, detectionType, detectionRole, resource);

            CacheContext(planet, detectionType, detectionRole, context);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[Context] {planet.ActorName} resolvido com Pool='{context.PoolData?.name ?? "null"}', WavePreset='{context.WavePreset?.name ?? "null"}'.");

            return context;
        }

        public void PrepareRunners(PlanetDefenseSetupContext context)
        {
            if (context == null)
            {
                return;
            }

            _poolRunner?.ConfigureForPlanet(context);

            if (ShouldWarmUpPools(context))
            {
                _poolRunner?.WarmUp(context);
            }

            if (context.Strategy != null)
            {
                _waveRunner?.ConfigureStrategy(context.Planet, context.Strategy);
            }
        }

        public void ConfigurePrimaryTarget(PlanetsMaster planet, Transform target, string targetLabel, DefenseRole targetRole)
        {
            _waveRunner?.ConfigurePrimaryTarget(planet, target, targetLabel, targetRole);
        }

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            _waveRunner?.StartWaves(planet, detectionType, strategy);
        }

        public void StopWaves(PlanetsMaster planet)
        {
            _waveRunner?.StopWaves(planet);
        }

        public void ReleasePools(PlanetsMaster planet)
        {
            _poolRunner?.Release(planet);
        }

        public void ClearContext(PlanetsMaster planet)
        {
            ClearCachedContext(planet);
        }

        private static bool ShouldWarmUpPools(PlanetDefenseSetupContext context)
        {
            return WarmUpPools && context?.PoolData != null;
        }

        private void ResolveDependenciesFromProvider()
        {
            var provider = DependencyManager.Provider;

            if (_poolRunner == null && provider.TryGetGlobal(out IPlanetDefensePoolRunner resolvedPoolRunner))
            {
                _poolRunner = resolvedPoolRunner;
            }

            if (_waveRunner == null && provider.TryGetGlobal(out IPlanetDefenseWaveRunner resolvedWaveRunner))
            {
                _waveRunner = resolvedWaveRunner;
            }

        }

        private bool TryReuseCachedContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole detectionRole,
            out PlanetDefenseSetupContext cached)
        {
            cached = null;

            var cacheKey = (detectionType, detectionRole);

            if (_resolvedContexts.TryGetValue(planet, out var contextsByDetection) &&
                contextsByDetection != null &&
                contextsByDetection.TryGetValue(cacheKey, out var context) &&
                context != null)
            {
                cached = context;
                return true;
            }

            return false;
        }

        private void CacheContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole detectionRole,
            PlanetDefenseSetupContext context)
        {
            if (planet == null || context == null)
            {
                return;
            }

            if (!_resolvedContexts.TryGetValue(planet, out var contextsByDetection) || contextsByDetection == null)
            {
                contextsByDetection = new Dictionary<(DetectionType detectionType, DefenseRole role), PlanetDefenseSetupContext>();
                _resolvedContexts[planet] = contextsByDetection;
            }

            contextsByDetection[(detectionType, detectionRole)] = context;
        }

        private void ClearCachedContext(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            _resolvedContexts.Remove(planet);
            _sequentialIndices.Remove(planet);
            _sequentialEntryCache.Remove(planet);
            _cachedApproxRadii.Remove(planet);
        }

        private PlanetDefenseSetupContext ResolveEntryContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole detectionRole,
            PlanetResourcesSo resource)
        {
            if (!_configuredDefenseEntries.TryGetValue(planet, out var configuration))
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"Nenhuma configuração de defesa encontrada para {planet?.ActorName ?? "planeta nulo"}.");
                return new PlanetDefenseSetupContext(planet, detectionType, resource);
            }

            var selectedEntry = SelectEntry(configuration, planet);
            var wavePreset = ResolveWavePreset(selectedEntry, detectionRole);
            var poolData = wavePreset?.PoolData;
            var spawnRadius = CalculatePlanetRadius(planet, selectedEntry?.SpawnOffset ?? 0f);

            ValidateWavePresetRuntime(planet, wavePreset);

            if (wavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>($"WavePreset não resolvido para {planet.ActorName}; configure bind default para evitar falhas.");
            }

            if (poolData == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"PoolData ausente ao resolver defesa de {planet.ActorName}; configure PoolData no WavePreset.");
            }

            var context = new PlanetDefenseSetupContext(
                planet,
                detectionType,
                resource,
                null,
                poolData,
                null,
                null,
                wavePreset,
                selectedEntry?.SpawnOffset ?? 0f,
                spawnRadius);

            return context;
        }

        private PlanetDefenseEntrySo SelectEntry(DefenseEntryConfiguration configuration, PlanetsMaster planet)
        {
            if (configuration.Entries == null || configuration.Entries.Count == 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>($"Lista de entradas vazia para {planet.ActorName}; configure ao menos uma entrada.");
                return null;
            }

            return configuration.ChoiceMode switch
            {
                DefenseChoiceMode.Random => SelectRandomEntry(configuration.Entries),
                DefenseChoiceMode.Sequential => SelectSequentialEntry(configuration.Entries, planet),
                _ => SelectSequentialEntry(configuration.Entries, planet)
            };
        }

        private PlanetDefenseEntrySo SelectRandomEntry(IReadOnlyList<PlanetDefenseEntrySo> entries)
        {
            if (entries.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, entries.Count);
            return entries[index];
        }

        private PlanetDefenseEntrySo SelectSequentialEntry(IReadOnlyList<PlanetDefenseEntrySo> entries, PlanetsMaster planet)
        {
            if (!_sequentialIndices.TryGetValue(planet, out var currentIndex))
            {
                currentIndex = 0;
            }

            if (entries.Count == 0)
            {
                return null;
            }

            if (currentIndex >= entries.Count)
            {
                currentIndex = 0;
            }

            var cachedEntry = ResolveCachedSequentialEntry(planet, currentIndex);
            var entry = cachedEntry ?? entries[currentIndex];

            CacheSequentialEntry(planet, currentIndex, entry);
            _sequentialIndices[planet] = (currentIndex + 1) % entries.Count;
            return entry;
        }

        private PlanetDefenseEntrySo ResolveCachedSequentialEntry(PlanetsMaster planet, int index)
        {
            if (!_sequentialEntryCache.TryGetValue(planet, out var cache) || cache == null)
            {
                return null;
            }

            return cache.TryGetValue(index, out var cached) ? cached : null;
        }

        private void CacheSequentialEntry(PlanetsMaster planet, int index, PlanetDefenseEntrySo entry)
        {
            if (planet == null || entry == null)
            {
                return;
            }

            if (!_sequentialEntryCache.TryGetValue(planet, out var cache) || cache == null)
            {
                cache = new Dictionary<int, PlanetDefenseEntrySo>();
                _sequentialEntryCache[planet] = cache;
            }

            cache[index] = entry;
        }

        private WavePresetSo ResolveWavePreset(PlanetDefenseEntrySo entry, DefenseRole role)
        {
            if (entry == null)
            {
                return null;
            }

            if (entry.EntryBindByRole != null && entry.EntryBindByRole.TryGetValue(role, out var mappedPreset) && mappedPreset != null)
            {
                return mappedPreset;
            }

            return entry.EntryDefaultWavePreset;
        }

        private float CalculatePlanetRadius(PlanetsMaster planet, float spawnOffset)
        {
            if (planet == null)
            {
                return 0f;
            }

            if (!_cachedApproxRadii.TryGetValue(planet, out var approxRadius))
            {
                if (!DependencyManager.Provider.TryGetForObject(planet.ActorId, out SkinRuntimeStateTracker tracker))
                {
                    DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"SkinRuntimeStateTracker não encontrado para {planet.ActorName}; usando offset apenas.");
                    approxRadius = 0f;
                }
                else
                {
                    var state = tracker.GetStateOrEmpty(ModelType.ModelRoot);
                    approxRadius = Mathf.Max(0f, state.ApproxRadius);
                }

                _cachedApproxRadii[planet] = approxRadius;
            }

            return Mathf.Max(0f, approxRadius + spawnOffset);
        }

        private void ValidateWavePresetRuntime(PlanetsMaster planet, WavePresetSo wavePreset)
        {
            if (wavePreset == null)
            {
                return;
            }

            if (wavePreset.NumberOfEnemiesPerWave <= 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"NumberOfEnemiesPerWave inválido em '{wavePreset.name}' para planeta {planet?.ActorName ?? "Unknown"}.");
            }

            if (wavePreset.IntervalBetweenWaves <= 0f)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"IntervalBetweenWaves inválido em '{wavePreset.name}' para planeta {planet?.ActorName ?? "Unknown"}.");
            }

            if (wavePreset.SpawnPattern != null && wavePreset.NumberOfEnemiesPerWave <= 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"SpawnPattern configurado em '{wavePreset.name}' mas NumberOfEnemiesPerWave está inválido para {planet?.ActorName ?? "Unknown"}.");
            }
        }

        private void PreloadDefensePools(IReadOnlyList<PlanetDefenseEntrySo> entries, PlanetsMaster planet)
        {
            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("PoolManager indisponível; preload de pools não executado.");
                return;
            }

            var seenPools = new HashSet<PoolData>();

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                RegisterPool(poolManager, entry.EntryDefaultWavePreset, seenPools, planet);

                if (entry.EntryBindByRole == null)
                {
                    continue;
                }

                foreach (var bind in entry.EntryBindByRole)
                {
                    RegisterPool(poolManager, bind.Value, seenPools, planet, bind.Key);
                }
            }
        }

        private void RegisterPool(PoolManager poolManager, WavePresetSo preset, HashSet<PoolData> seenPools, PlanetsMaster planet, DefenseRole? role = null)
        {
            if (preset == null)
            {
                return;
            }

            var poolData = preset.PoolData;
            if (poolData == null)
            {
                var roleSuffix = role.HasValue ? $" para role {role.Value}" : string.Empty;
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"PoolData obrigatório não configurado no WavePreset '{preset.name}'{roleSuffix} para planeta {planet.ActorName}.");
                return;
            }

            if (!seenPools.Add(poolData))
            {
                return;
            }

            poolManager.RegisterPool(poolData);
        }

        private readonly struct DefenseEntryConfiguration
        {
            public readonly IReadOnlyList<PlanetDefenseEntrySo> Entries;
            public readonly DefenseChoiceMode ChoiceMode;

            public DefenseEntryConfiguration(IReadOnlyList<PlanetDefenseEntrySo> entries, DefenseChoiceMode choiceMode)
            {
                Entries = entries;
                ChoiceMode = choiceMode;
            }
        }
    }
}
