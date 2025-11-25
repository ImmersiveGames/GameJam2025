using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IPlanetDefenseActivationListener
    {
        void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent);
        void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent);
        void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent);
    }

    public sealed class PlanetDefenseSpawnConfig
    {
        public bool WarmUpPools { get; set; } = true;
        public bool StopWavesOnDisable { get; set; } = true;
        public float DebugLoopIntervalSeconds { get; set; } = 3f;
        public float DebugWaveDurationSeconds { get; set; } = 12f;
        public int DebugWaveSpawnCount { get; set; } = 6;
    }

    /// <summary>
    /// Serviço simples de log para acompanhar a ativação e desativação das
    /// defesas planetárias. Ele mantém um flag por planeta para evitar
    /// múltiplos loops de debug e utiliza o ciclo de Update do Unity para
    /// emitir mensagens apenas enquanto o jogo está rodando.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetDefenseSpawnService : MonoBehaviour, IPlanetDefenseActivationListener, IInjectableComponent
    {
        private readonly Dictionary<PlanetsMaster, ActiveDefenseState> _activeDefenses = new();

        [Inject] [SerializeField] private PlanetDefenseSpawnConfig _config = new();

        private EventBinding<PlanetDefenseEngagedEvent> _engagedBinding;
        private EventBinding<PlanetDefenseDisengagedEvent> _disengagedBinding;
        private EventBinding<PlanetDefenseDisabledEvent> _disabledBinding;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(PlanetDefenseSpawnService);

        private void Awake()
        {
            _config ??= new PlanetDefenseSpawnConfig();
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
            _activeDefenses.Clear();
        }

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            _config ??= new PlanetDefenseSpawnConfig();
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            if (_activeDefenses.ContainsKey(engagedEvent.Planet))
            {
                return;
            }

            var state = new ActiveDefenseState(
                engagedEvent.Planet,
                engagedEvent.DetectionType,
                FormatDetector(engagedEvent.Detector),
                Time.time + _config.DebugLoopIntervalSeconds);

            _activeDefenses.Add(engagedEvent.Planet, state);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"{state.DetectorName} ativou as defesas do planeta {state.Planet.ActorName} (sensor: {state.DetectionType?.TypeName ?? "Unknown"}).",
                DebugUtility.Colors.CrucialInfo);
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return;
            }

            if (_activeDefenses.Remove(disengagedEvent.Planet, out var state))
            {
                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"Defesas do planeta {state.Planet.ActorName} encerradas; {FormatDetector(disengagedEvent.Detector)} saiu do sensor {disengagedEvent.DetectionType?.TypeName ?? "Unknown"}.");
            }
        }

        public void OnDefenseDisabled(PlanetDefenseDisabledEvent disabledEvent)
        {
            if (disabledEvent.Planet == null)
            {
                return;
            }

            _activeDefenses.Remove(disabledEvent.Planet);
        }

        private void Update()
        {
            if (!Application.isPlaying || Time.timeScale <= 0f)
            {
                return;
            }

            if (_activeDefenses.Count == 0)
            {
                return;
            }

            float now = Time.time;
            foreach (var state in _activeDefenses.Values)
            {
                if (now < state.NextLogTime)
                {
                    continue;
                }

                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_config.DebugWaveDurationSeconds:0.##}s | Spawns previstos: {_config.DebugWaveSpawnCount}.");

                state.NextLogTime = now + _config.DebugLoopIntervalSeconds;
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

        private sealed class ActiveDefenseState
        {
            public ActiveDefenseState(PlanetsMaster planet, DetectionType detectionType, string detectorName, float nextLogTime)
            {
                Planet = planet;
                DetectionType = detectionType;
                DetectorName = detectorName;
                NextLogTime = nextLogTime;
            }

            public PlanetsMaster Planet { get; }
            public DetectionType DetectionType { get; }
            public string DetectorName { get; }
            public float NextLogTime { get; set; }
        }
    }
}
