using UnityEngine;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

namespace _ImmersiveGames.NewScripts.Gameplay
{
    /// <summary>
    /// Gatilhos mínimos (production-friendly) para encerrar uma run de gameplay.
    ///
    /// Importante:
    /// - Este componente tenta receber IGameRunEndRequestService via [Inject],
    ///   mas também faz fallback para resolver no DI global caso o injector não injete este MonoBehaviour.
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

        [Header("Dev-only manual triggers (opcional)")]
        [Tooltip("Permite disparo manual em Editor/Development Build.")]
        [SerializeField] private bool enableDevManualTriggers = false;

        [SerializeField] private KeyCode devVictoryKey = KeyCode.F9;
        [SerializeField] private KeyCode devDefeatKey = KeyCode.F10;

        [Header("Dev-only reasons (opcional)")]
        [SerializeField] private string devVictoryReason = "Gameplay/DevManualVictory";
        [SerializeField] private string devDefeatReason = "Gameplay/DevManualDefeat";

        [Inject] private IGameRunEndRequestService _endRequest;

        private float _startTime;
        private bool _requested;
        private bool _loggedMissingService;

        private void OnEnable()
        {
            _startTime = Time.time;
            _requested = false;
            _loggedMissingService = false;

            // Best-effort: tenta resolver já no enable (caso não tenha sido injetado).
            TryResolveEndRequestService();
        }

        private void Update()
        {
            if (_requested)
                return;

            // Se não foi injetado, tenta resolver via DI global.
            if (!TryResolveEndRequestService())
                return;

            if (enableTimeout && (Time.time - _startTime) >= timeoutSeconds)
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
                Debug.LogWarning(
                    "[GameplayEndConditionsController] IGameRunEndRequestService não disponível (ainda). " +
                    "Aguardando DI global. Se persistir, verifique se o serviço está registrado e se o DependencyManager inicializou.");
            }

            return false;
        }
    }
}
