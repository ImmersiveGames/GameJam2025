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
    public interface IPlanetDefenseActivationListener : IDefenseEngagedListener, IDefenseDisengagedListener, IDefenseDisabledListener { }

    /// <summary>
    /// Serviço de defesa planetária com orquestração de pooling e waves via DI,
    /// delegando estado e logs para colaboradores especializados. Mantém-se puro
    /// (sem bindings de EventBus); o escutador MonoBehaviour apenas encaminha os
    /// eventos para os métodos Handle*.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public class PlanetDefenseSpawnService : IInjectableComponent
    {
        private PoolData _defaultPoolData;
        private DefenseWaveProfileSo _waveProfile;
        private IDefenseStrategy _defaultStrategy;
        private const bool WarmUpPools = true;
        private const bool StopWavesOnDisable = true;

        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private DefenseStateManager _stateManager = new();

        private DefenseDebugLogger _debugLogger = new(null);

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

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
            _debugLogger?.Configure(_waveProfile);
            WarnIfProfileMissing();
        }

        public void SetDefenseStrategy(IDefenseStrategy defenseStrategy)
        {
            _defaultStrategy = defenseStrategy;
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
            LogStrategy();
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

            // 🔵 NOVO: extrai Transform + label do alvo para o runner
            Transform targetTransform = null;
            string targetLabel = engagedEvent.Detector.Owner?.ActorName ?? engagedEvent.Detector.ToString();
            DefenseRole targetRole = engagedEvent.Role;

            if (engagedEvent.Detector.Owner is Component ownerComponent)
            {
                targetTransform = ownerComponent.transform;
            }
            else if (engagedEvent.Detector is Component detectorComponent)
            {
                targetTransform = detectorComponent.transform;
            }

            // ALTERAÇÃO (Passo 1.2): usar a interface em vez de cast para RealPlanetDefenseWaveRunner
            _waveRunner?.ConfigurePrimaryTarget(
                state.Planet,
                targetTransform,
                targetLabel,
                targetRole);

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
                out _);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {disengagedEvent.Planet.ActorName}: {state?.ActiveDetectors ?? 0} após saída de {FormatDetector(disengagedEvent.Detector)}.");

            var noDetectorsRemaining = disengagedEvent.IsLastDisengagement || state?.ActiveDetectors <= 0;

            if (noDetectorsRemaining)
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"[Debug] Nenhum detector restante em {disengagedEvent.Planet.ActorName}. Encerrando waves e logging.");

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
            if (StopWavesOnDisable)
            {
                _poolRunner?.Release(disabledEvent.Planet);
            }

            _debugLogger?.StopLogging(disabledEvent.Planet);
            _stateManager?.ClearPlanet(disabledEvent.Planet);
        }

        private PlanetDefenseSetupContext BuildContext(DefenseState state)
        {
            PlanetResourcesSo resource = state.Planet.HasAssignedResource ? state.Planet.AssignedResource : null;

            return new PlanetDefenseSetupContext(
                state.Planet,
                state.DetectionType,
                resource,
                _defaultStrategy,
                _defaultPoolData,
                _waveProfile); // ←- única fonte de configuração
        }

        private void LogDefaultPoolData()
        {
            string poolName = _defaultPoolData != null ? _defaultPoolData.name : "null";
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>($"[PoolDebug] Default PoolData configurado: {poolName}; WarmUpPools: {WarmUpPools}.");
        }

        private void LogWaveProfile()
        {
            var profile = _waveProfile;

            if (profile == null)
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>("[WaveDebug] Nenhum DefenseWaveProfileSO atribuído; serão usados valores padrão para debug de ondas.");
                return;
            }

            string profileName = profile.defaultMinionProfile != null
                ? profile.defaultMinionProfile.name
                : "null";

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[WaveDebug] WaveProfile configurado: {profile.name}; Intervalo: {profile.secondsBetweenWaves}s; Minions/Onda: {profile.enemiesPerWave}; Raio: {profile.spawnRadius}; Altura: {profile.spawnHeightOffset}; MinionProfile: {profileName}.");
        }

        private void LogStrategy()
        {
            if (_defaultStrategy == null)
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    "[StrategyDebug] Nenhuma DefenseStrategy atribuída; serão usadas preferências padrão ou do WaveRunner.");
                return;
            }

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[StrategyDebug] DefenseStrategy configurada: {_defaultStrategy.StrategyId}; TargetRole preferido: {_defaultStrategy.TargetRole}.");
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
            if (_defaultPoolData == null)
            {
                DebugUtility.LogWarning<PlanetDefenseSpawnService>("Default PoolData not configured; defense waves will not warm up pools.");
            }
        }

        private bool ShouldWarmUpPools()
        {
            return WarmUpPools && _defaultPoolData != null;
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
