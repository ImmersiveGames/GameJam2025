using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Orquestrador focado em preparar contexto, pools e runner de waves.
    /// Mantém cache por planeta e delega logs ao IDefenseLogger injetado via DI.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseOrchestrationService : IPlanetDefenseSetupOrchestrator
    {
        private PoolData _defaultPoolData;
        private DefenseWaveProfileSo _waveProfile;
        private IDefenseStrategy _defaultStrategy;
        private readonly Dictionary<PlanetsMaster, PlanetDefenseLoadoutSo> _configuredLoadouts = new();
        private readonly Dictionary<PlanetsMaster, PlanetDefenseSetupContext> _resolvedContexts = new();
        private const bool WarmUpPools = true;
        private const bool ReleasePoolsOnDisable = true;

        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private IDefenseLogger _defenseLogger;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseOrchestrationService);

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveDependenciesFromProvider();
            _defenseLogger?.Configure(_waveProfile);
            WarnIfProfileMissing();
            LogDefaultPoolData();
            LogWaveProfile();
            WarnIfPoolDataMissing();
            LogStrategy();
        }

        public void SetDefaultPoolData(PoolData poolData)
        {
            _defaultPoolData = poolData;
            LogDefaultPoolData();
        }

        /// <summary>
        /// SO não injetado via DI – passado via método do Controller.
        /// </summary>
        public void SetWaveProfile(DefenseWaveProfileSo waveProfile)
        {
            _waveProfile = waveProfile;
            _defenseLogger?.Configure(_waveProfile);
            WarnIfProfileMissing();
        }

        public void SetDefenseStrategy(IDefenseStrategy defenseStrategy)
        {
            _defaultStrategy = defenseStrategy;
            LogStrategy();
        }

        public void ConfigureLoadout(PlanetsMaster planet, PlanetDefenseLoadoutSo loadout)
        {
            if (planet == null)
            {
                return;
            }

            _configuredLoadouts[planet] = loadout;
            _resolvedContexts.Remove(planet);
            string loadoutName = loadout != null ? loadout.name : "null";
            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[Loadout] Planeta {planet.ActorName} usando PlanetDefenseLoadout='{loadoutName}'.");
        }

        public PlanetDefenseSetupContext ResolveEffectiveConfig(PlanetsMaster planet, DetectionType detectionType)
        {
            if (planet == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Planeta nulo ao resolver configuração de defesa.");
                return null;
            }

            if (_resolvedContexts.TryGetValue(planet, out var cached) && cached != null && cached.DetectionType == detectionType)
            {
                return cached;
            }

            var resource = planet.HasAssignedResource ? planet.AssignedResource : null;
            _configuredLoadouts.TryGetValue(planet, out var loadout);

            var preset = loadout?.DefensePreset;

            PlanetDefenseSetupContext context;

            if (preset != null)
            {
                context = PlanetDefensePresetAdapter.BuildContext(
                    planet,
                    detectionType,
                    resource,
                    loadout,
                    preset,
                    _defaultPoolData,
                    _waveProfile,
                    _defaultStrategy);
            }
            else
            {
                var poolData = loadout?.DefensePoolData ?? _defaultPoolData;
                var waveProfile = loadout?.WaveProfileOverride ?? _waveProfile;
                var strategy = loadout?.DefenseStrategy ?? _defaultStrategy;

                context = new PlanetDefenseSetupContext(
                    planet,
                    detectionType,
                    resource,
                    strategy,
                    poolData,
                    waveProfile,
                    loadout);

                strategy?.ConfigureContext(context);
            }

            _resolvedContexts[planet] = context;

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[Context] {planet.ActorName} resolvido com Pool='{context.PoolData?.name ?? "null"}', WaveProfile='{context.WaveProfile?.name ?? "null"}', Strategy='{context.Strategy?.StrategyId ?? "null"}'.");

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
            _resolvedContexts.Remove(planet);
        }

        private void LogDefaultPoolData()
        {
            string poolName = _defaultPoolData != null ? _defaultPoolData.name : "null";
            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>($"[PoolDebug] Default PoolData configurado: {poolName}; WarmUpPools: {WarmUpPools}.");
        }

        private void LogWaveProfile()
        {
            var profile = _waveProfile;

            if (profile == null)
            {
                DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>("[WaveDebug] Nenhum DefenseWaveProfileSO atribuído; serão usados valores padrão para debug de ondas.");
                return;
            }

            string profileName = profile.defaultMinionProfile != null
                ? profile.defaultMinionProfile.name
                : "null";

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[WaveDebug] WaveProfile configurado: {profile.name}; Intervalo: {profile.secondsBetweenWaves}s; Minions/Onda:{profile.enemiesPerWave}; Raio: {profile.spawnRadius}; Altura: {profile.spawnHeightOffset}; MinionProfile: {profileName}.");
        }

        private void LogStrategy()
        {
            if (_defaultStrategy == null)
            {
                DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                    "[StrategyDebug] Nenhuma DefenseStrategy atribuída; serão usadas preferências padrão ou do WaveRunner.");
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseOrchestrationService>(
                $"[StrategyDebug] DefenseStrategy configurada: {_defaultStrategy.StrategyId}; TargetRole preferido: {_defaultStrategy.TargetRole}.");
        }

        private void WarnIfProfileMissing()
        {
            if (_waveProfile == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("DefenseWaveProfileSO não atribuído; usando valores padrão para depuração.");
            }
        }

        private void WarnIfPoolDataMissing()
        {
            if (_defaultPoolData == null)
            {
                DebugUtility.LogWarning<PlanetDefenseOrchestrationService>("Default PoolData not configured; defense waves will not warm up pools.");
            }
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

            if (_defenseLogger == null && provider.TryGetGlobal(out IDefenseLogger resolvedLogger))
            {
                _defenseLogger = resolvedLogger;
            }
        }
    }
}
