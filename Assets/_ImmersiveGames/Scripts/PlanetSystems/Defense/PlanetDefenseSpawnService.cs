using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
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

    [System.Serializable]
    public sealed class PlanetDefenseSpawnConfig
    {
        [SerializeField]
        [Tooltip("Pré-aquecer pools antes de spawnar (futuro). Mantido para compatibilidade.")]
        private bool warmUpPools = true;

        [SerializeField]
        [Tooltip("Parar ondas ao desabilitar o planeta (futuro). Mantido para compatibilidade.")]
        private bool stopWavesOnDisable = true;

        [SerializeField]
        [Tooltip("Pool utilizada para spawnar as defesas do planeta.")]
        private PoolData defensePoolData;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Intervalo entre cada onda de defesa.")]
        private float spawnIntervalSeconds = 5f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Duração estimada de uma onda (para logs).")]
        private float waveDurationSeconds = 5f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Quantidade de objetos spawnados por onda.")]
        private int waveSpawnCount = 6;

        public bool WarmUpPools
        {
            get => warmUpPools;
            set => warmUpPools = value;
        }

        public bool StopWavesOnDisable
        {
            get => stopWavesOnDisable;
            set => stopWavesOnDisable = value;
        }

        public PoolData DefensePoolData
        {
            get => defensePoolData;
            set => defensePoolData = value;
        }

        public float SpawnIntervalSeconds
        {
            get => spawnIntervalSeconds;
            set => spawnIntervalSeconds = value;
        }

        public float WaveDurationSeconds
        {
            get => waveDurationSeconds;
            set => waveDurationSeconds = value;
        }

        public int WaveSpawnCount
        {
            get => waveSpawnCount;
            set => waveSpawnCount = value;
        }
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
        private ObjectPool _defensePool;
        private bool _poolErrorLogged;

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
            if (_config.SpawnIntervalSeconds <= 0f)
            {
                _config.SpawnIntervalSeconds = _config.WaveDurationSeconds;
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

            if (_config.SpawnIntervalSeconds <= 0f)
            {
                _config.SpawnIntervalSeconds = _config.WaveDurationSeconds;
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
                Time.time,
                Mathf.Max(1, engagedEvent.ActiveDetectors));

            _activeDefenses.Add(engagedEvent.Planet, state);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"{state.DetectorName} ativou as defesas do planeta {state.Planet.ActorName} (sensor: {state.DetectionType?.TypeName ?? "Unknown"}).",
                DebugUtility.Colors.CrucialInfo);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Detectores ativos em {state.Planet.ActorName}: {state.ActiveDetectors} (último detector: {state.DetectorName}).");

            // Primeira onda registrada imediatamente após a ativação; o próximo ciclo respeita o intervalo configurado.
            SpawnWaveAndLog(state, Time.time);
            state.NextSpawnTime = Time.time + _config.SpawnIntervalSeconds;
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
                if (now < state.NextSpawnTime)
                {
                    continue;
                }

                SpawnWaveAndLog(state, now);
                state.NextSpawnTime = now + _config.SpawnIntervalSeconds;
            }
        }

        private void SpawnWaveAndLog(ActiveDefenseState state, float timestamp)
        {
            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_config.WaveDurationSeconds:0.##}s | Spawns previstos: {_config.WaveSpawnCount}. (@ {timestamp:0.00}s)");

            if (!TryEnsurePool())
            {
                return;
            }

            int spawnCount = Mathf.Max(1, _config.WaveSpawnCount);
            Vector3 spawnPosition = state.Planet.transform.position;
            var spawned = _defensePool.GetMultipleObjects(spawnCount, spawnPosition, state.Planet);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"[Spawn] {spawned.Count}/{spawnCount} objetos da pool '{_config.DefensePoolData.ObjectName}' instanciados para defender {state.Planet.ActorName}. (@ {timestamp:0.00}s)");
        }

        private bool TryEnsurePool()
        {
            var manager = PoolManager.Instance;
            if (manager == null)
            {
                LogPoolErrorOnce("PoolManager não encontrado — spawns de defesa não serão executados.");
                return false;
            }

            if (_config?.DefensePoolData == null)
            {
                LogPoolErrorOnce("PoolData de defesa não configurado no PlanetDefenseSpawnService.");
                return false;
            }

            _defensePool = manager.RegisterPool(_config.DefensePoolData);
            if (_defensePool == null || !_defensePool.IsInitialized)
            {
                LogPoolErrorOnce($"Falha ao inicializar a pool '{_config.DefensePoolData.ObjectName}' para defesas planetárias.");
                return false;
            }

            _poolErrorLogged = false;
            return true;
        }

        private void LogPoolErrorOnce(string message)
        {
            if (_poolErrorLogged)
            {
                return;
            }

            DebugUtility.LogError<PlanetDefenseSpawnService>(message);
            _poolErrorLogged = true;
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
            if (detector?.Owner == null)
            {
                return "Um detector desconhecido";
            }

            var owner = detector.Owner;
            return owner switch
            {
                EaterSystem.EaterMaster eater => $"O Eater ({eater.ActorName})",
                ActorSystems.ActorMaster actorMaster => $"O Ator ({actorMaster.ActorName})",
                _ => !string.IsNullOrWhiteSpace(owner.ActorName)
                    ? owner.ActorName
                    : detector.ToString()
            };
        }

        private sealed class ActiveDefenseState
        {
            public ActiveDefenseState(PlanetsMaster planet, DetectionType detectionType, string detectorName, float nextSpawnTime, int activeDetectors)
            {
                Planet = planet;
                DetectionType = detectionType;
                DetectorName = detectorName;
                NextSpawnTime = nextSpawnTime;
                ActiveDetectors = activeDetectors;
            }

            public PlanetsMaster Planet { get; }
            public DetectionType DetectionType { get; }
            public string DetectorName { get; }
            public float NextSpawnTime { get; set; }
            public int ActiveDetectors { get; set; }
        }
    }
}
