using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IDefenseEngagedListener
    {
        void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent);
    }

    public interface IDefenseDisengagedListener
    {
        void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent);
    }

    public interface IDefenseDisabledListener
    {
        void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent);
    }

    /// <summary>
    /// Interface agregadora mantida para compatibilidade; listeners podem implementar
    /// apenas os eventos necessários através das interfaces segmentadas.
    /// </summary>
    public interface IPlanetDefenseActivationListener : IDefenseEngagedListener, IDefenseDisengagedListener, IDefenseDisabledListener
    {
    }

    /// <summary>
    /// Serviço de defesa planetária com orquestração de pooling e waves via DI,
    /// delegando estado e logs para colaboradores especializados. Mantém-se puro
    /// (sem bindings de EventBus); o escutador MonoBehaviour apenas encaminha os
    /// eventos para os métodos Handle*.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseSpawnService : IInjectableComponent
    {
        private PoolData defaultPoolData;
        private DefenseWaveProfileSO _waveProfile;
        private bool _warmUpPools = true;
        private bool _stopWavesOnDisable = true;

        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private DefenseStateManager _stateManager;

        private DefenseDebugLogger _debugLogger;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        public PlanetDefenseSpawnService()
        {
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(_waveProfile);
        }

        public void SetDefaultPoolData(PoolData poolData)
        {
            defaultPoolData = poolData;
            LogDefaultPoolData();
        }

        /// <summary>
        /// SO não injetado via DI – passado via método do Controller.
        /// </summary>
        public void SetWaveProfile(DefenseWaveProfileSO waveProfile)
        {
            _waveProfile = waveProfile;
            _debugLogger?.Configure(_waveProfile);
            WarnIfProfileMissing();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveDependenciesFromProvider();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(_waveProfile);
            _debugLogger.Configure(_waveProfile);
            WarnIfProfileMissing();
            LogDefaultPoolData();
            LogWaveProfile();
            WarnIfPoolDataMissing();
        }

        public void HandleEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            var state = _stateManager.RegisterEngagement(
                engagedEvent.Planet,
                engagedEvent.DetectionType,
                FormatDetector(engagedEvent.Detector),
                engagedEvent.ActiveDetectors);

            if (state == null)
            {
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {engagedEvent.Planet.ActorName}: {state.ActiveDetectors} após entrada de {FormatDetector(engagedEvent.Detector)}. Primeiro? {engagedEvent.IsFirstEngagement}.");

            var context = BuildContext(state);
            _poolRunner?.ConfigureForPlanet(context);

            if (ShouldWarmUpPools())
            {
                _poolRunner?.WarmUp(context);
            }

            if (context.Strategy != null)
            {
                _waveRunner?.ConfigureStrategy(state.Planet, context.Strategy);
            }

            if (engagedEvent.IsFirstEngagement)
            {
                _waveRunner?.StartWaves(state.Planet, state.DetectionType, context.Strategy);
                _debugLogger?.StartLogging(state);
            }
        }

        public void HandleDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return;
            }

            var state = _stateManager.RegisterDisengagement(
                disengagedEvent.Planet,
                disengagedEvent.DetectionType,
                FormatDetector(disengagedEvent.Detector),
                Mathf.Max(disengagedEvent.ActiveDetectors, 0),
                out var removed);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {disengagedEvent.Planet.ActorName}: {state?.ActiveDetectors ?? 0} após saída de {FormatDetector(disengagedEvent.Detector)}.");

            if (removed || disengagedEvent.IsLastDisengagement)
            {
                _waveRunner?.StopWaves(disengagedEvent.Planet);
                _debugLogger?.StopLogging(disengagedEvent.Planet);
            }
        }

        public void HandleDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (disabledEvent.Planet == null)
            {
                return;
            }

            _waveRunner?.StopWaves(disabledEvent.Planet);
            if (_stopWavesOnDisable)
            {
                _poolRunner?.Release(disabledEvent.Planet);
            }

            _debugLogger?.StopLogging(disabledEvent.Planet);
            _stateManager?.ClearPlanet(disabledEvent.Planet);
        }

        private PlanetDefenseSetupContext BuildContext(DefenseState state)
        {
            PlanetResourcesSo resource = null;
            if (state.Planet != null && state.Planet.HasAssignedResource)
            {
                resource = state.Planet.AssignedResource;
            }

            return new PlanetDefenseSetupContext(
                state.Planet,
                state.DetectionType,
                resource,
                null,
                defaultPoolData,
                _waveProfile);
        }

        private void LogDefaultPoolData()
        {
            string poolName = defaultPoolData != null ? defaultPoolData.name : "null";
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>($"[PoolDebug] Default PoolData configurado: {poolName}; WarmUpPools: {_warmUpPools}.");
        }

        private void LogWaveProfile()
        {
            var profile = _waveProfile;

            if (profile == null)
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>("[WaveDebug] Nenhum DefenseWaveProfileSO atribuído; serão usados valores padrão para debug de ondas.");
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[WaveDebug] WaveProfile configurado: {profile.name}; Intervalo: {profile.waveIntervalSeconds}s; Minions/Onda: {profile.minionsPerWave}; Raio: {profile.spawnRadius}; Altura: {profile.spawnHeightOffset}.");
        }

        private void WarnIfProfileMissing()
        {
            if (_waveProfile == null)
            {
                DebugUtility.LogWarning<PlanetDefenseSpawnService>("DefenseWaveProfileSO não atribuído; usando valores padrão para depuração.");
            }
        }

        private void WarnIfPoolDataMissing()
        {
            if (defaultPoolData == null)
            {
                DebugUtility.LogWarning<PlanetDefenseSpawnService>("Default PoolData not configured; defense waves will not warm up pools.");
            }
        }

        private bool ShouldWarmUpPools()
        {
            return _warmUpPools && defaultPoolData != null;
        }

        private void ResolveDependenciesFromProvider()
        {
            var provider = DependencyManager.Provider;

            if (_stateManager == null && provider.TryGetGlobal(out DefenseStateManager resolvedStateManager))
            {
                _stateManager = resolvedStateManager;
            }

            if (_poolRunner == null && provider.TryGetGlobal(out IPlanetDefensePoolRunner resolvedPoolRunner))
            {
                _poolRunner = resolvedPoolRunner;
            }

            if (_waveRunner == null && provider.TryGetGlobal(out IPlanetDefenseWaveRunner resolvedWaveRunner))
            {
                _waveRunner = resolvedWaveRunner;
            }
        }


        private static string FormatDetector(IDetector detector)
        {
            if (detector == null)
            {
                return "Um detector desconhecido";
            }

            string actorName = detector.Owner?.ActorName ?? detector.ToString();
            return actorName.Contains("Eater")
                ? $"O Eater ({actorName})"
                : actorName.Contains("Player")
                    ? $"O Player ({actorName})"
                    : actorName;
        }
    }
}
