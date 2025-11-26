using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
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

    public interface IPlanetDefenseActivationListener : IDefenseEngagedListener, IDefenseDisengagedListener, IDefenseDisabledListener
    {
    }

    public sealed class PlanetDefenseSpawnConfig
    {
        public bool WarmUpPools { get; set; } = true;
        public bool StopWavesOnDisable { get; set; } = true;

        // Configurações reais de spawn
        public int MinionsPerWave { get; set; } = 6;
        public float WaveIntervalSeconds { get; set; } = 5f;
        public float SpawnRadius { get; set; } = 12f;
        public _ImmersiveGames.Scripts.Utils.PoolSystems.PoolData DefaultPoolData { get; set; }
#if UNITY_EDITOR
            = null
#endif
        ;
        public DefensesMinionData DefaultMinionData { get; set; }
#if UNITY_EDITOR
            = null
#endif
        ;

        // Intervalo de log; por padrão 5s para simular ciclos de spawn mais curtos.
        public float DebugLoopIntervalSeconds { get; set; } = 5f;

        // Duração "esperada" de uma onda de spawn para fins de telemetria/debug.
        public float DebugWaveDurationSeconds { get; set; } = 5f;

        // Quantidade estimada de spawns por onda (apenas para log).
        public int DebugWaveSpawnCount { get; set; } = 6;
    }

    /// <summary>
    /// Serviço orquestrador das defesas planetárias. Mantém-se reativo a
    /// eventos, delegando estado, log e execução para classes específicas
    /// (StateManager, DebugLogger e runners), promovendo segregação de
    /// responsabilidades.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    [DisallowMultipleComponent]
    public class PlanetDefenseSpawnService : MonoBehaviour, IPlanetDefenseActivationListener, IInjectableComponent
    {
        private readonly DefenseStateManager _stateManager = new();
        private readonly DefenseDebugLogger _logger = new();
        private readonly Dictionary<PlanetsMaster, Coroutine> _debugCoroutines = new();

        [Inject] private PlanetDefenseSpawnConfig _config = new();
        [Inject] private IPlanetDefensePoolRunner _poolRunner = new NullPlanetDefensePoolRunner();
        [Inject] private IPlanetDefenseWaveRunner _waveRunner = new NullPlanetDefenseWaveRunner();
        [Inject] private IDefenseStrategy _defenseStrategy = new DefaultDefenseStrategy();

        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        private void Awake()
        {
            EnsureConfig();
            ValidateDependencies();
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
            StopAllDebugCoroutines();
            _stateManager.ClearAll();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            EnsureConfig();
            ValidateDependencies();
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Detector == null)
            {
                DefenseUtils.LogIgnoredNullDetector(this);
                return;
            }

            if (!_stateManager.TryEngage(engagedEvent, out var state))
            {
                return;
            }

            DefenseRole role = DefenseUtils.ResolveDefenseRole(engagedEvent.Detector);
            string detectorName = DefenseUtils.FormatDetector(engagedEvent.Detector, role);
            _logger.LogEngaged(engagedEvent, detectorName);

            var strategy = BuildStrategy(engagedEvent.Planet, engagedEvent.Detector, engagedEvent.DetectionType);

            _poolRunner.ConfigureForPlanet(engagedEvent.Planet, strategy);
            _waveRunner.ConfigureForPlanet(engagedEvent.Planet, strategy);

            if (engagedEvent.IsFirstEngagement && _config.WarmUpPools)
            {
                _logger.LogPoolWarmUp(engagedEvent.Planet, strategy);
                _poolRunner.WarmUp(engagedEvent.Planet, engagedEvent.DetectionType, strategy);
            }

            if (!_waveRunner.IsRunning(engagedEvent.Planet))
            {
                _waveRunner.StartWaves(engagedEvent.Planet, engagedEvent.DetectionType, strategy);
                StartDebugLoop(state);
            }
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Detector == null)
            {
                DefenseUtils.LogIgnoredNullDetector(this);
                return;
            }

            if (!_stateManager.TryDisengage(disengagedEvent, out var state))
            {
                return;
            }

            DefenseRole role = DefenseUtils.ResolveDefenseRole(disengagedEvent.Detector);
            string detectorName = DefenseUtils.FormatDetector(disengagedEvent.Detector, role);
            _logger.LogDisengaged(disengagedEvent, detectorName);

            if (state.ActiveDetectors <= 0 || disengagedEvent.IsLastDisengagement)
            {
                StopDebugLoop(disengagedEvent.Planet);
                _waveRunner.StopWaves(disengagedEvent.Planet);
            }
        }

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            int activeDetectors = _stateManager.ClearPlanet(disabledEvent.Planet);
            StopDebugLoop(disabledEvent.Planet);

            if (_config.StopWavesOnDisable)
            {
                _waveRunner.StopWaves(disabledEvent.Planet);
            }

            if (disabledEvent.Planet != null)
            {
                _poolRunner.Release(disabledEvent.Planet);
            }

            if (activeDetectors > 0)
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"Defesa desabilitada para {disabledEvent.Planet?.ActorName ?? "Unknown"} com {activeDetectors} detectores ainda ativos.");
            }
        }

        private void StartDebugLoop(PlanetDefenseState state)
        {
            if (state == null || _config.DebugLoopIntervalSeconds <= 0f)
            {
                return;
            }

            if (_debugCoroutines.ContainsKey(state.Planet))
            {
                return;
            }

            Coroutine routine = StartCoroutine(DebugRoutine(state));
            if (routine != null)
            {
                _debugCoroutines[state.Planet] = routine;
            }
        }

        private void StopDebugLoop(PlanetsMaster planet)
        {
            if (planet == null || !_debugCoroutines.TryGetValue(planet, out var routine))
            {
                return;
            }

            StopCoroutine(routine);
            _debugCoroutines.Remove(planet);
        }

        private IEnumerator DebugRoutine(PlanetDefenseState state)
        {
            var wait = new WaitForSeconds(_config.DebugLoopIntervalSeconds);
            while (true)
            {
                if (state.ActiveDetectors <= 0)
                {
                    yield break;
                }

                _logger.LogWaveTelemetry(state, _config, Time.time);
                yield return wait;
            }
        }

        private DefenseStrategyResult BuildStrategy(PlanetsMaster planet, IDetector detector, DetectionType detectionType)
        {
            var strategy = _defenseStrategy ?? new DefaultDefenseStrategy();
            return strategy.BuildStrategy(planet, detector, detectionType, _config);
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

        private void EnsureConfig()
        {
            _config ??= new PlanetDefenseSpawnConfig();
            if (_config.DebugLoopIntervalSeconds <= 0f)
            {
                _config.DebugLoopIntervalSeconds = _config.DebugWaveDurationSeconds;
            }
        }

        private void ValidateDependencies()
        {
            _poolRunner ??= new NullPlanetDefensePoolRunner();
            _waveRunner ??= new NullPlanetDefenseWaveRunner();
            _defenseStrategy ??= new DefaultDefenseStrategy();
        }

        private void StopAllDebugCoroutines()
        {
            foreach (var routine in _debugCoroutines.Values)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            _debugCoroutines.Clear();
        }
    }
}
