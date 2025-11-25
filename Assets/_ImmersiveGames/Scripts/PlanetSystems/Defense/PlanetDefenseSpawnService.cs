using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        // Intervalo de log; se não for definido, usa a duração da onda para manter paridade com o ciclo de spawn.
        public float DebugLoopIntervalSeconds { get; set; } = 0f;

        // Duração "esperada" de uma onda de spawn para fins de telemetria/debug.
        public float DebugWaveDurationSeconds { get; set; } = 12f;

        // Quantidade estimada de spawns por onda (apenas para log).
        public int DebugWaveSpawnCount { get; set; } = 6;
    }

    /// <summary>
    /// Serviço simples de log para acompanhar a ativação e desativação das
    /// defesas planetárias. Ele mantém um flag por planeta para evitar
    /// múltiplos loops de debug e utiliza o ciclo de Update do Unity para
    /// emitir mensagens apenas enquanto o jogo está rodando.
    ///
    /// A contagem de detectores ativos vem do controlador de defesa; se um
    /// planeta já estiver ativo, novos detectores apenas atualizam a contagem
    /// sem disparar uma nova ativação. O desligamento só ocorre quando o
    /// último detector sair.
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

            // Se o intervalo não foi configurado explicitamente, seguir a duração da onda
            // para alinhar o log periódico ao ritmo de spawn esperado.
            if (_config.DebugLoopIntervalSeconds <= 0f)
            {
                _config.DebugLoopIntervalSeconds = _config.DebugWaveDurationSeconds;
            }
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

            if (_config.DebugLoopIntervalSeconds <= 0f)
            {
                _config.DebugLoopIntervalSeconds = _config.DebugWaveDurationSeconds;
            }
        }

        public void OnDefenseEngaged(PlanetDefenseEngagedEvent engagedEvent)
        {
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return;
            }

            if (_activeDefenses.TryGetValue(engagedEvent.Planet, out var existing))
            {
                int updatedCount = Mathf.Max(existing.ActiveDetectors, engagedEvent.ActiveDetectors);
                if (updatedCount != existing.ActiveDetectors)
                {
                    existing.ActiveDetectors = updatedCount;

                    DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                        $"[Debug] Detectores ativos em {existing.Planet.ActorName}: {existing.ActiveDetectors} (último detector: {FormatDetector(engagedEvent.Detector)}).");
                }
                return;
            }

            var state = new ActiveDefenseState(
                engagedEvent.Planet,
                engagedEvent.DetectionType,
                FormatDetector(engagedEvent.Detector),
                Time.time + _config.DebugLoopIntervalSeconds,
                Mathf.Max(1, engagedEvent.ActiveDetectors));

            _activeDefenses.Add(engagedEvent.Planet, state);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"{state.DetectorName} ativou as defesas do planeta {state.Planet.ActorName} (sensor: {state.DetectionType?.TypeName ?? "Unknown"}).",
                DebugUtility.Colors.CrucialInfo);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {state.Planet.ActorName}: {state.ActiveDetectors} (último detector: {state.DetectorName}).");
        }

        public void OnDefenseDisengaged(PlanetDefenseDisengagedEvent disengagedEvent)
        {
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return;
            }

            if (_activeDefenses.TryGetValue(disengagedEvent.Planet, out var state))
            {
                int remainingDetectors = Mathf.Max(disengagedEvent.ActiveDetectors, state.ActiveDetectors - 1);

                DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                    $"[Debug] Detectores ativos em {state.Planet.ActorName}: {remainingDetectors} após saída de {FormatDetector(disengagedEvent.Detector)}.");

                if (remainingDetectors <= 0 || disengagedEvent.IsLastDisengagement)
                {
                    _activeDefenses.Remove(disengagedEvent.Planet);

                    DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                        $"Defesas do planeta {state.Planet.ActorName} encerradas; {FormatDetector(disengagedEvent.Detector)} saiu do sensor {disengagedEvent.DetectionType?.TypeName ?? "Unknown"}.");
                }
                else
                {
                    state.ActiveDetectors = remainingDetectors;
                }
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

#if UNITY_EDITOR
            if (EditorApplication.isPaused)
            {
                return;
            }
#endif

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
            public ActiveDefenseState(PlanetsMaster planet, DetectionType detectionType, string detectorName, float nextLogTime, int activeDetectors)
            {
                Planet = planet;
                DetectionType = detectionType;
                DetectorName = detectorName;
                NextLogTime = nextLogTime;
                ActiveDetectors = activeDetectors;
            }

            public PlanetsMaster Planet { get; }
            public DetectionType DetectionType { get; }
            public string DetectorName { get; }
            public float NextLogTime { get; set; }
            public int ActiveDetectors { get; set; }
        }
    }
}
