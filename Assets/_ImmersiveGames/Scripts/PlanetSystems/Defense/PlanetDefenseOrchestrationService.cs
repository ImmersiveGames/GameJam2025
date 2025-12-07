using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
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
    /// Resolve alvo role para escolher a entrada (Entry) correta e o preset
    /// de wave associado, mantendo cache por planeta e delegando logs ao
    /// DebugUtility para acompanhamento no Editor. Não define comportamento
    /// de minions — apenas como e onde eles entram.
    /// </summary>
    public class PlanetDefenseOrchestrationService : IPlanetDefenseSetupOrchestrator
    {
        private readonly Dictionary<PlanetsMaster, DefenseEntryConfigV2> _configuredDefenseEntriesV2 = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<(DetectionType detectionType, DefenseRole role), PlanetDefenseSetupContext>> _resolvedContexts = new();
        private readonly Dictionary<PlanetsMaster, int> _sequentialIndices = new();
        private readonly Dictionary<PlanetsMaster, Dictionary<int, DefenseEntryConfigSo>> _sequentialEntryCacheV2 = new();
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

        public void ConfigureDefenseEntriesV2(
            PlanetsMaster planet,
            IReadOnlyList<DefenseEntryConfigSo> defenseEntries,
            DefenseChoiceMode defenseChoiceMode)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Planeta nulo ao configurar entradas v2.");
                return;
            }

            var entries = defenseEntries ?? Array.Empty<DefenseEntryConfigSo>();
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
            if (!TryResolveFromV2(planet, detectionType, targetRole, resource, out var context))
            {
                context = new PlanetDefenseSetupContext(planet, detectionType, targetRole, resource);
            }

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

            if (!_configuredDefenseEntriesV2.TryGetValue(planet, out var configuration))
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"Nenhuma configuração v2 registrada para {planet.ActorName ?? "planeta nulo"}.");
                return false;
            }

            if (configuration.entries == null || configuration.entries.Count == 0)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>($"Entradas v2 vazias para {planet.ActorName}; configure DefenseEntryConfigSO.");
                return false;
            }

            var selectedEntry = SelectEntry(configuration, planet);

            if (selectedEntry == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"Nenhuma DefenseEntryConfigSO válida para {planet.ActorName}; retorno de contexto vazio.");
                return false;
            }

            var roleConfig = ResolveRoleConfig(selectedEntry, targetRole);
            var wavePreset = roleConfig.WavePreset;
            var minionBehaviorProfile = roleConfig.MinionBehaviorProfile;
            var spawnRadius = CalculatePlanetRadius(planet, roleConfig.SpawnOffset);
            var spawnOffset = Vector3.zero;
            var entryConfig = selectedEntry;

            ValidateWavePresetRuntime(planet, wavePreset);

            if (wavePreset == null)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"WavePreset não resolvido para {planet.ActorName} no fluxo v2; configure binds ou default.");
                return false;
            }
            context = new PlanetDefenseSetupContext(
                planet,
                detectionType,
                targetRole,
                resource,
                null,
                entryConfig,
                wavePreset,
                minionBehaviorProfile,
                spawnOffset,
                spawnRadius);

            return true;
        }

        private DefenseEntryConfigSo SelectEntry(DefenseEntryConfigV2 configuration, PlanetsMaster planet)
        {
            if (configuration.entries == null || configuration.entries.Count == 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>($"Lista de entradas v2 vazia para {planet.ActorName}; configure ao menos uma entrada.");
                return null;
            }

            return configuration.choiceMode switch
            {
                DefenseChoiceMode.Random => SelectRandomEntry(configuration.entries),
                _ => SelectSequentialEntry(configuration.entries, planet)
            };
        }

        private DefenseEntryConfigSo SelectRandomEntry(IReadOnlyList<DefenseEntryConfigSo> entries)
        {
            if (entries.Count == 0)
            {
                return null;
            }

            var index = UnityEngine.Random.Range(0, entries.Count);
            return entries[index];
        }

        private DefenseEntryConfigSo SelectSequentialEntry(IReadOnlyList<DefenseEntryConfigSo> entries, PlanetsMaster planet)
        {
            var currentIndex = _sequentialIndices.GetValueOrDefault(planet, 0);

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

        private DefenseEntryConfigSo ResolveCachedSequentialEntryV2(PlanetsMaster planet, int index)
        {
            if (!_sequentialEntryCacheV2.TryGetValue(planet, out var cache) || cache == null)
            {
                return null;
            }

            return cache.GetValueOrDefault(index);
        }

        private void CacheSequentialEntryV2(PlanetsMaster planet, int index, DefenseEntryConfigSo entry)
        {
            if (planet == null || entry == null)
            {
                return;
            }

            if (!_sequentialEntryCacheV2.TryGetValue(planet, out var cache) || cache == null)
            {
                cache = new Dictionary<int, DefenseEntryConfigSo>();
                _sequentialEntryCacheV2[planet] = cache;
            }

            cache[index] = entry;
        }

        private DefenseEntryConfigSo.RoleDefenseConfig ResolveRoleConfig(DefenseEntryConfigSo entry, DefenseRole targetRole)
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

            if (wavePreset.NumberOfMinionsPerWave <= 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"NumberOfMinionsPerWave inválido em '{wavePreset.name}' para planeta {planet?.ActorName ?? "Unknown"}.");
            }

            if (wavePreset.IntervalBetweenWaves <= 0f)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"IntervalBetweenWaves inválido em '{wavePreset.name}' para planeta {planet?.ActorName ?? "Unknown"}.");
            }

            if (wavePreset.SpawnPattern != null && wavePreset.NumberOfMinionsPerWave <= 0)
            {
                DebugUtility.LogError<PlanetDefenseOrchestrationService>(
                    $"SpawnPattern configurado em '{wavePreset.name}' mas NumberOfMinionsPerWave está inválido para {planet?.ActorName ?? "Unknown"}.");
            }
        }

        private readonly struct DefenseEntryConfigV2
        {
            public readonly IReadOnlyList<DefenseEntryConfigSo> entries;
            public readonly DefenseChoiceMode choiceMode;

            public DefenseEntryConfigV2(IReadOnlyList<DefenseEntryConfigSo> entries, DefenseChoiceMode choiceMode)
            {
                this.entries = entries;
                this.choiceMode = choiceMode;
            }
        }
    }
}
