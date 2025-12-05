using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.SkinSystems.Runtime;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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
        private readonly Dictionary<PlanetsMaster, DefenseEntryConfiguration> _configuredDefenseEntries = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<(DetectionType detectionType, DefenseRole role), PlanetDefenseSetupContext>> _resolvedContexts = new();
        private readonly Dictionary<PlanetsMaster, int> _sequentialIndices = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<int, DefenseEntryConfigSO>> _sequentialEntryCache = new();
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
            IReadOnlyList<DefenseEntryConfigSO> defenseEntries,
            DefenseChoiceMode defenseChoiceMode)
        {
            if (planet == null)
            {
                return;
            }

            var entries = defenseEntries ?? Array.Empty<DefenseEntryConfigSO>();
            _configuredDefenseEntries[planet] = new DefenseEntryConfiguration(entries, defenseChoiceMode);
            ClearCachedContext(planet);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[DefenseEntries] Planeta {planet.ActorName} configurado com {entries.Count} entradas (modo: {defenseChoiceMode}).");
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
            var context = ResolveEntryContext(planet, detectionType, targetRole, resource);

            CacheContext(planet, detectionType, targetRole, context);

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[Context] {planet.ActorName} resolvido com WavePreset='{context.WavePreset?.name ?? "null"}'.");

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
            return WarmUpPools && context?.WavePreset != null;
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
            _cachedApproxRadii.Remove(planet);
        }

        private PlanetDefenseSetupContext ResolveEntryContext(
            PlanetsMaster planet,
            DetectionType detectionType,
            DefenseRole targetRole,
            PlanetResourcesSo resource)
        {
            if (!_configuredDefenseEntries.TryGetValue(planet, out var configuration))
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"Nenhuma configuração de defesa encontrada para {planet?.ActorName ?? "planeta nulo"}.");
                return new PlanetDefenseSetupContext(planet, detectionType, targetRole, resource);
            }

            var selectedEntry = SelectEntry(configuration, planet);
            var roleConfig = ResolveRoleConfig(selectedEntry, targetRole);
            var spawnOffset = selectedEntry != null ? new Vector3(selectedEntry.SpawnOffset, 0f, 0f) : Vector3.zero;
            var wavePreset = roleConfig.WavePreset;
            var spawnRadius = CalculatePlanetRadius(planet, spawnOffset.magnitude);

            DefenseEntryConfigSO entryConfig = null;
            DefenseMinionConfigSO minionConfig = null;

            ValidateWavePresetRuntime(planet, wavePreset);

            if (wavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>($"WavePreset não resolvido para {planet.ActorName}; configure bind default para evitar falhas.");
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

        private DefenseEntryConfigSO SelectEntry(DefenseEntryConfiguration configuration, PlanetsMaster planet)
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

            var cachedEntry = ResolveCachedSequentialEntry(planet, currentIndex);
            var entry = cachedEntry ?? entries[currentIndex];

            CacheSequentialEntry(planet, currentIndex, entry);
            _sequentialIndices[planet] = (currentIndex + 1) % entries.Count;
            return entry;
        }

        private DefenseEntryConfigSO ResolveCachedSequentialEntry(PlanetsMaster planet, int index)
        {
            if (!_sequentialEntryCache.TryGetValue(planet, out var cache) || cache == null)
            {
                return null;
            }

            return cache.TryGetValue(index, out var cached) ? cached : null;
        }

        private void CacheSequentialEntry(PlanetsMaster planet, int index, DefenseEntryConfigSO entry)
        {
            if (planet == null || entry == null)
            {
                return;
            }

            if (!_sequentialEntryCache.TryGetValue(planet, out var cache) || cache == null)
            {
                cache = new Dictionary<int, DefenseEntryConfigSO>();
                _sequentialEntryCache[planet] = cache;
            }

            cache[index] = entry;
        }

        private DefenseEntryConfigSO.RoleDefenseConfig ResolveRoleConfig(DefenseEntryConfigSO entry, DefenseRole role)
        {
            if (entry == null)
            {
                return default;
            }

            if (entry.Bindings != null && entry.Bindings.TryGetValue(role, out var config))
            {
                return config;
            }

            return entry.DefaultConfig;
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

        private readonly struct DefenseEntryConfiguration
        {
            public readonly IReadOnlyList<DefenseEntryConfigSO> Entries;
            public readonly DefenseChoiceMode ChoiceMode;

            public DefenseEntryConfiguration(IReadOnlyList<DefenseEntryConfigSO> entries, DefenseChoiceMode choiceMode)
            {
                Entries = entries;
                ChoiceMode = choiceMode;
            }
        }
    }
}
