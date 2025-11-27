using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
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

    public sealed class PlanetDefenseSpawnConfig
    {
        public bool WarmUpPools { get; set; } = true;
        public bool StopWavesOnDisable { get; set; } = true;

        // Intervalo de log; por padrão 5s para simular ciclos de spawn mais curtos.
        public float DebugLoopIntervalSeconds { get; set; } = 5f;

        // Duração "esperada" de uma onda de spawn para fins de telemetria/debug.
        public float DebugWaveDurationSeconds { get; set; } = 5f;

        // Quantidade estimada de spawns por onda (apenas para log).
        public int DebugWaveSpawnCount { get; set; } = 6;
    }

    /// <summary>
    /// Serviço de defesa planetária com orquestração de pooling e waves via DI,
    /// delegando estado e logs para colaboradores especializados.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class PlanetDefenseSpawnService : MonoBehaviour, IPlanetDefenseActivationListener, IInjectableComponent
    {
        [SerializeField] private PoolData defaultPoolData;

        [Inject] private PlanetDefenseSpawnConfig _config = new();
        [Inject] private IPlanetDefensePoolRunner _poolRunner;
        [Inject] private IPlanetDefenseWaveRunner _waveRunner;
        [Inject] private DefenseStateManager _stateManager;

        private DefenseDebugLogger _debugLogger;

        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        private void Awake()
        {
            ResolveDependenciesFromProvider();
            _config ??= new PlanetDefenseSpawnConfig();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(_config);
            RegisterAsGlobalListener();
            EnsureDebugInterval();
            WarnIfPoolDataMissing();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            ResolveDependenciesFromProvider();
            _config ??= new PlanetDefenseSpawnConfig();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(_config);
            _debugLogger.Configure(_config);
            EnsureDebugInterval();
            WarnIfPoolDataMissing();
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
            _debugLogger?.StopAll();
            _stateManager?.ClearAll();
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
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

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
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

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
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

        private void RegisterAsGlobalListener()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPlanetDefenseActivationListener>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPlanetDefenseActivationListener>(this);
                DependencyManager.Provider.RegisterGlobal<IDefenseEngagedListener>(this);
                DependencyManager.Provider.RegisterGlobal<IDefenseDisengagedListener>(this);
                DependencyManager.Provider.RegisterGlobal<IDefenseDisabledListener>(this);
            }
            else if (!ReferenceEquals(existing, this))
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>("Global PlanetDefenseSpawnService already registered; skipping self-registration.");
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

        private void ResolveDependenciesFromProvider()
        {
            var provider = DependencyManager.Provider;

            if (_config == null && provider.TryGetGlobal(out PlanetDefenseSpawnConfig resolvedConfig))
            {
                _config = resolvedConfig;
            }

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

        private void RegisterBindings()
        {
            _engagedBinding ??= new EventBinding<PlanetDefenseEngagedEvent>(OnDefenseEngaged);
            _disengagedBinding ??= new EventBinding<PlanetDefenseDisengagedEvent>(OnDefenseDisengaged);
            _disabledBinding ??= new EventBinding<PlanetDefenseDisabledEvent>(OnDefenseDisabled);

            EventBus<PlanetDefenseEngagedEvent>.Register(_engagedBinding);
            EventBus<PlanetDefenseDisengagedEvent>.Register(_disengagedBinding);
            EventBus<PlanetDefenseDisabledEvent>.Register(_disabledBinding);
        }

        private void UnregisterBindings()
        {
            if (_engagedBinding != null)
            {
                EventBus<PlanetDefenseEngagedEvent>.Unregister(_engagedBinding);
            }

            if (_disengagedBinding != null)
            {
                EventBus<PlanetDefenseDisengagedEvent>.Unregister(_disengagedBinding);
            }

            if (_disabledBinding != null)
            {
                EventBus<PlanetDefenseDisabledEvent>.Unregister(_disabledBinding);
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
