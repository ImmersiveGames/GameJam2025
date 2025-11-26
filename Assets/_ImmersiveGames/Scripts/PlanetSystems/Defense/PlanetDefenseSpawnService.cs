using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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
    public class PlanetDefenseSpawnService : MonoBehaviour, IPlanetDefenseActivationListener, IInjectableComponent
    {
        [SerializeField] private DefensesMinionData defaultMinionData;

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
            _config ??= new PlanetDefenseSpawnConfig();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(this, _config);
            EnsureDebugInterval();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            _config ??= new PlanetDefenseSpawnConfig();
            _stateManager ??= new DefenseStateManager();
            _debugLogger ??= new DefenseDebugLogger(this, _config);
            _debugLogger.Configure(_config);
            EnsureDebugInterval();
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
                $"{state.LastDetectorName} ativou as defesas do planeta {state.Planet.ActorName} (sensor: {state.DetectionType?.TypeName ?? "Unknown"}).",
                DebugUtility.Colors.CrucialInfo);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {state.Planet.ActorName}: {state.ActiveDetectors} (último detector: {state.LastDetectorName}).");

            if (!engagedEvent.IsFirstEngagement)
            {
                return;
            }

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
                defaultMinionData,
                null);
        }

        private void EnsureDebugInterval()
        {
            if (_config.DebugLoopIntervalSeconds <= 0f)
            {
                _config.DebugLoopIntervalSeconds = _config.DebugWaveDurationSeconds;
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
