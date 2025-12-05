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
    /// Resolve target role para escolher a entrada (Entry) correta e o preset
    /// de wave associado, mantendo cache por planeta e delegando logs ao
    /// DebugUtility para acompanhamento no Editor. Não define comportamento
    /// de minions — apenas como e onde eles entram.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseOrchestrationService : IPlanetDefenseSetupOrchestrator
    {
        private readonly Dictionary<PlanetsMaster, DefenseEntryConfiguration> _configuredDefenseEntriesV1 = new();
        private readonly Dictionary<PlanetsMaster, DefenseEntryConfigV2> _configuredDefenseEntriesV2 = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<(DetectionType detectionType, DefenseRole role), PlanetDefenseSetupContext>> _resolvedContexts = new();
        private readonly Dictionary<PlanetsMaster, int> _sequentialIndices = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<int, PlanetDefenseEntrySo>> _sequentialEntryCache = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<int, DefenseEntryConfigSO>> _sequentialEntryCacheV2 = new();
        private readonly Dictionary<PlanetsMaster, float> _cachedApproxRadii = new();
        private const bool WarmUpPools = true;

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
            _configuredDefenseEntriesV1[planet] = new DefenseEntryConfiguration(entries, defenseChoiceMode);
            ClearCachedContext(planet);
            PreloadDefensePools(entries, planet);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[DefenseEntries] Planeta {planet.ActorName} configurado com {entries.Count} entradas (modo: {defenseChoiceMode}).");
        }

        public void ConfigureDefenseEntriesV2(
            PlanetsMaster planet,
            IReadOnlyList<DefenseEntryConfigSO> defenseEntries,
            DefenseChoiceMode defenseChoiceMode)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Planeta nulo ao configurar entradas v2.");
                return;
            }

            var entries = defenseEntries ?? Array.Empty<DefenseEntryConfigSO>();
            _configuredDefenseEntriesV2[planet] = new DefenseEntryConfigV2(entries, defenseChoiceMode);
            ClearCachedContext(planet);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[DefenseEntriesV2] Planeta {planet.ActorName} configurado com {entries.Count} entradas (modo: {defenseChoiceMode}).");
        }

        public PlanetDefenseSetupContext ResolveEffectiveConfig(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole targetRole)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Planeta nulo ao resolver configuração de defesa.");
                return null;
            }

            if (TryReuseCachedContext(planet, detectionType, targetRole, out var cached))
            {
                return cached;
            }

            var resource = planet.HasAssignedResource ? planet.AssignedResource : null;
            PlanetDefenseSetupContext context;

            if (TryResolveFromV2(planet, detectionType, targetRole, resource, out var contextV2))
            {
                context = contextV2;
            }
            else
            {
                context = ResolveEntryContextV1(planet, detectionType, targetRole, resource);
            }

            CacheContext(planet, detectionType, targetRole, context);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[Context] {planet.ActorName} resolvido com Pool='{context.WavePreset?.PoolData?.name ?? "null"}', WavePreset='{context.WavePreset?.name ?? "null"}'.");

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
            return WarmUpPools && context?.WavePreset?.PoolData != null;
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
            DefenseRole targetRole,
            out PlanetDefenseSetupContext cached)
        {
            cached = null;

            var cacheKey = (detectionType, targetRole);

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
            DefenseRole targetRole,
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

            contextsByDetection[(detectionType, targetRole)] = context;
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
            _sequentialEntryCacheV2.Remove(planet);
            _cachedApproxRadii.Remove(planet);
        }

        private bool TryResolveFromV2(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole targetRole,
            PlanetResourcesSo resource,
            out PlanetDefenseSetupContext context)
        {
            context = null;

            if (!_configuredDefenseEntriesV2.TryGetValue(planet, out var configuration) ||
                configuration.Entries == null ||
                configuration.Entries.Count == 0)
            {
                return false;
            }

            var selectedEntry = SelectEntry(configuration, planet);

            if (selectedEntry == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"Nenhuma DefenseEntryConfigSO válida para {planet.ActorName}; caindo para fluxo v1.");
                return false;
            }

            var roleConfig = ResolveRoleConfig(selectedEntry, targetRole);
            var wavePreset = roleConfig.WavePreset;
            var poolData = wavePreset?.PoolData;
            var spawnRadius = CalculatePlanetRadius(planet, roleConfig.SpawnOffset);
            var spawnOffset = Vector3.zero;
            var entryConfig = selectedEntry;
            var minionConfig = roleConfig.MinionConfig;

            ValidateWavePresetRuntime(planet, wavePreset);

            if (wavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"WavePreset não resolvido para {planet.ActorName} no fluxo v2; configure binds ou default.");
                return false;
            }

            if (poolData == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"PoolData ausente ao resolver defesa de {planet.ActorName} no fluxo v2; configure PoolData no WavePreset.");
            }

            context = new PlanetDefenseSetupContext(
                planet,
                detectionType,
                targetRole,
                resource,
                null,
                entryConfig,
                minionConfig,
                wavePreset,
                spawnOffset,
                spawnRadius);

            return true;
        }

        private PlanetDefenseSetupContext ResolveEntryContextV1(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole targetRole,
            PlanetResourcesSo resource)
        {
            if (!_configuredDefenseEntriesV1.TryGetValue(planet, out var configuration))
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"Nenhuma configuração de defesa encontrada para {planet?.ActorName ?? "planeta nulo"}.");
                return new PlanetDefenseSetupContext(planet, detectionType, targetRole, resource);
            }

            var selectedEntry = SelectEntry(configuration, planet);
            var wavePreset = ResolveWavePreset(selectedEntry, targetRole);
            var poolData = wavePreset?.PoolData;
            var spawnRadius = CalculatePlanetRadius(planet, selectedEntry?.SpawnOffset ?? 0f);

            DefenseEntryConfigSO entryConfig = null;
            DefenseMinionConfigSO minionConfig = null;

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
                targetRole,
                resource,
                null,
                entryConfig,
                minionConfig,
                wavePreset,
                Vector3.zero,
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

        private DefenseEntryConfigSO SelectEntry(DefenseEntryConfigV2 configuration, PlanetsMaster planet)
        {
            if (configuration.Entries == null || configuration.Entries.Count == 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>($"Lista de entradas v2 vazia para {planet.ActorName}; configure ao menos uma entrada.");
                return null;
            }

            return configuration.ChoiceMode switch
            {
                DefenseChoiceMode.Random => SelectRandomEntry(configuration.Entries),
                DefenseChoiceMode.Sequential => SelectSequentialEntry(configuration.Entries, planet),
                _ => SelectSequentialEntry(configuration.Entries, planet)
            };
        }

        private DefenseEntryConfigSO SelectRandomEntry(IReadOnlyList<DefenseEntryConfigSO> entries)
        {
            if (entries.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, entries.Count);
            return entries[index];
        }

        private DefenseEntryConfigSO SelectSequentialEntry(IReadOnlyList<DefenseEntryConfigSO> entries, PlanetsMaster planet)
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

            var cachedEntry = ResolveCachedSequentialEntryV2(planet, currentIndex);
            var entry = cachedEntry ?? entries[currentIndex];

            CacheSequentialEntryV2(planet, currentIndex, entry);
            _sequentialIndices[planet] = (currentIndex + 1) % entries.Count;
            return entry;
        }

        private DefenseEntryConfigSO ResolveCachedSequentialEntryV2(PlanetsMaster planet, int index)
        {
            if (!_sequentialEntryCacheV2.TryGetValue(planet, out var cache) || cache == null)
            {
                return null;
            }

            return cache.TryGetValue(index, out var cached) ? cached : null;
        }

        private void CacheSequentialEntryV2(PlanetsMaster planet, int index, DefenseEntryConfigSO entry)
        {
            if (planet == null || entry == null)
            {
                return;
            }

            if (!_sequentialEntryCacheV2.TryGetValue(planet, out var cache) || cache == null)
            {
                cache = new Dictionary<int, DefenseEntryConfigSO>();
                _sequentialEntryCacheV2[planet] = cache;
            }

            cache[index] = entry;
        }

        private DefenseEntryConfigSO.RoleDefenseConfig ResolveRoleConfig(DefenseEntryConfigSO entry, DefenseRole targetRole)
        {
            if (entry == null)
            {
                return default;
            }

            if (entry.Bindings != null && entry.Bindings.TryGetValue(targetRole, out var roleConfig))
            {
                return roleConfig;
            }

            return entry.DefaultConfig;
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

        private float CalculatePlanetRadius(PlanetsMaster planet, float spawnOffsetMagnitude)
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

            return Mathf.Max(0f, approxRadius + spawnOffsetMagnitude);
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

        private readonly struct DefenseEntryConfigV2
        {
            public readonly IReadOnlyList<DefenseEntryConfigSO> Entries;
            public readonly DefenseChoiceMode ChoiceMode;

            public DefenseEntryConfigV2(IReadOnlyList<DefenseEntryConfigSO> entries, DefenseChoiceMode choiceMode)
            {
                Entries = entries;
                ChoiceMode = choiceMode;
            }
        }
    }
}
