using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Serviço de defesa planetária com orquestração de pooling e waves via DI,
    /// delegando estado e logs para colaboradores especializados.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class PlanetDefenseSpawnService : MonoBehaviour, IInjectableComponent
    {
        [SerializeField] private PoolData defaultPoolData;

        [Inject] private PlanetDefenseSpawnConfig _config = new();
        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private DefenseStateManager _stateManager;

        private DefenseDebugLogger _debugLogger;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);
            InitializeCollaborators();
            RegisterAsGlobalService();
            EnsureDebugInterval();
            WarnIfPoolDataMissing();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            InitializeCollaborators();
            EnsureDebugInterval();
            WarnIfPoolDataMissing();
        }

        private void OnDisable()
        {
            _debugLogger?.StopAll();
            _stateManager?.ClearAll();
        }

        /// <summary>
        /// Manipula engajamento disparado pelo primeiro detector ativo.
        /// </summary>
        public void HandleDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
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

            if (_config.WarmUpPools)
            {
                _poolRunner?.WarmUp(context);
            }

            if (context.Strategy != null)
            {
                _waveRunner?.ConfigureStrategy(state.Planet, context.Strategy);
            }

            _waveRunner?.StartWaves(state.Planet, state.DetectionType, context.Strategy);
            _debugLogger?.StartLogging(state);
        }

        /// <summary>
        /// Manipula desengajamento disparado quando um detector sai do planeta.
        /// </summary>
        public void HandleDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
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

        /// <summary>
        /// Manipula desativação total do planeta.
        /// </summary>
        public void HandleDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (disabledEvent.Planet == null)
            {
                return;
            }

            _waveRunner?.StopWaves(disabledEvent.Planet);
            if (_config.StopWavesOnDisable)
            {
                _poolRunner?.Release(disabledEvent.Planet);
            }

            _debugLogger?.StopLogging(disabledEvent.Planet);
            _stateManager?.ClearPlanet(disabledEvent.Planet);
        }

        private void InitializeCollaborators()
        {
            _config ??= new PlanetDefenseSpawnConfig();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(_config);
        }

        private void RegisterAsGlobalService()
        {
            var provider = DependencyManager.Provider;
            if (!provider.TryGetGlobal(out PlanetDefenseSpawnService existing) || existing == null)
            {
                provider.RegisterGlobal(this);
            }
            else if (!ReferenceEquals(existing, this))
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    "Outro PlanetDefenseSpawnService já está registrado; mantendo referência existente.");
            }
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
                defaultPoolData);
        }

        private void EnsureDebugInterval()
        {
            if (_config.DebugLoopIntervalSeconds <= 0f)
            {
                _config.DebugLoopIntervalSeconds = _config.DebugWaveDurationSeconds;
            }
        }

        private void WarnIfPoolDataMissing()
        {
            if (defaultPoolData == null)
            {
                DebugUtility.LogWarning<PlanetDefenseSpawnService>("Default PoolData not configured; defense waves will not warm up pools.");
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
