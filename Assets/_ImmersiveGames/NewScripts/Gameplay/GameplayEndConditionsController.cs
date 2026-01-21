using UnityEngine;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay
{
    /// <summary>
    /// Gatilhos mínimos (production-friendly) para encerrar uma run de gameplay.
    ///
    /// Importante:
    /// - Este componente tenta receber IGameRunEndRequestService via [Inject],
    ///   mas também faz fallback para resolver no DI global caso o injector não injete este MonoBehaviour.
    /// - Como a GameplayScene pode NÃO ser recarregada em Restart, este controller deve rearmar por GameRunStartedEvent.
    /// - A deduplicação/consumo é responsabilidade do consumidor (ex.: GameRunOutcomeService).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayEndConditionsController : MonoBehaviour
    {
        [Header("Timeout (mínimo para validar produção)")]
        [SerializeField] private bool enableTimeout = true;

        [Min(0.1f)]
        [SerializeField] private float timeoutSeconds = 30f;

        [SerializeField] private GameRunOutcome timeoutOutcome = GameRunOutcome.Defeat;

        [SerializeField] private string timeoutReason = "Gameplay/Timeout";

        [Header("Clock")]
        [Tooltip("Se true, usa Time.unscaledTime para o timeout (não é afetado por pause/TimeScale).")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Dev-only manual triggers (opcional)")]
        [Tooltip("Permite disparo manual em Editor/Development Build.")]
        [SerializeField] private bool enableDevManualTriggers = false;

        [SerializeField] private KeyCode devVictoryKey = KeyCode.F9;
        [SerializeField] private KeyCode devDefeatKey = KeyCode.F10;

        [Header("Dev-only reasons (opcional)")]
        [SerializeField] private string devVictoryReason = "Gameplay/DevManualVictory";
        [SerializeField] private string devDefeatReason = "Gameplay/DevManualDefeat";

        [Header("Rearm")]
        [Tooltip("Se true, reseta estado interno quando GameRunStartedEvent for observado.")]
        [SerializeField] private bool rearmOnGameRunStarted = true;

        [Inject] private IGameRunEndRequestService _endRequest;

        private float _startTime;
        private bool _requested;
        private bool _loggedMissingService;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private bool _subscribed;

        private void Awake()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
        }

        private void OnEnable()
        {
            Subscribe();

            // Arm inicial (caso este objeto habilite no meio do fluxo).
            ResetLocalState("OnEnable");

            // Best-effort: tenta resolver já no enable (caso não tenha sido injetado).
            TryResolveEndRequestService();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_requested)
                return;

            // Se não foi injetado, tenta resolver via DI global.
            if (!TryResolveEndRequestService())
                return;

            var now = useUnscaledTime ? Time.unscaledTime : Time.time;

            if (enableTimeout && (now - _startTime) >= timeoutSeconds)
            {
                _requested = true;
                _endRequest.RequestEnd(timeoutOutcome, timeoutReason);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enableDevManualTriggers)
            {
                if (Input.GetKeyDown(devVictoryKey))
                {
                    _requested = true;
                    _endRequest.RequestEnd(GameRunOutcome.Victory, devVictoryReason);
                    return;
                }

                if (Input.GetKeyDown(devDefeatKey))
                {
                    _requested = true;
                    _endRequest.RequestEnd(GameRunOutcome.Defeat, devDefeatReason);
                    return;
                }
            }
#endif
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (!rearmOnGameRunStarted)
                return;

            // Este é o ponto crítico: a GameplayScene pode não ser recarregada em Restart,
            // então este controller precisa rearmar aqui.
            ResetLocalState("GameRunStartedEvent");

            // Tenta resolver novamente (ordem de inicialização pode variar).
            TryResolveEndRequestService();

            DebugUtility.LogVerbose<GameplayEndConditionsController>(
                "[GameplayEndConditionsController] Rearmed on GameRunStartedEvent.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            // Mantém coerência: se a run acabou por qualquer motivo, não disparar de novo até rearm.
            _requested = true;
        }

        private void ResetLocalState(string reason)
        {
            _requested = false;
            _loggedMissingService = false;

            _startTime = useUnscaledTime ? Time.unscaledTime : Time.time;

            DebugUtility.LogVerbose<GameplayEndConditionsController>(
                $"[GameplayEndConditionsController] State reset. reason='{reason}', startTime={_startTime:0.000}.",
                DebugUtility.Colors.Info);
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            _subscribed = false;
        }

        private bool TryResolveEndRequestService()
        {
            if (_endRequest != null)
                return true;

            // Fallback: resolve do DI global.
            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var svc) &&
                svc != null)
            {
                _endRequest = svc;
                return true;
            }

            // Loga uma vez e continua tentando nos próximos frames (não “morre”).
            if (!_loggedMissingService)
            {
                _loggedMissingService = true;
                DebugUtility.LogWarning<GameplayEndConditionsController>(
                    "[GameplayEndConditionsController] IGameRunEndRequestService não disponível (ainda). " +
                    "Aguardando DI global. Se persistir, verifique se o serviço está registrado e se o DependencyManager inicializou.",
                    this);
            }

            return false;
        }
    }
}
