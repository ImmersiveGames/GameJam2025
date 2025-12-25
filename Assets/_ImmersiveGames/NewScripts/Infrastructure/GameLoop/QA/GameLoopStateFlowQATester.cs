using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStateFlowQaTester : MonoBehaviour
    {
        private const string DefaultStartProfile = "startup";
        private const float DefaultTimeoutSeconds = 6f;

        private const string StateBoot = "Boot";
        private const string StateMenu = "Ready";
        private const string StatePlaying = "Playing";
        private const string StatePaused = "Paused";

        [Header("Runner")]
        [SerializeField] private string label = "GameLoopStateFlowQATester";
        [SerializeField] private bool runOnStart;
        [SerializeField] private int warmupFrames = 2;
        [SerializeField] private float timeoutSeconds = DefaultTimeoutSeconds;

        [Header("Expected Flow")]
        [SerializeField] private string expectedInitialState = StateMenu;
        [SerializeField] private string expectedPostResetState = StateMenu;

        [Header("Scene Flow")]
        [SerializeField] private string expectedStartProfile = DefaultStartProfile;

        private int _passes;
        private int _fails;

        private IGameLoopService _originalService;
        private CountingGameLoopService _countingService;
        private IStateDependentService _stateDependentService;
        private ISimulationGateService _gateService;

        private EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;
        private bool _seenScenesReady;
        private string _scenesReadyProfile = string.Empty;

        private void Awake()
        {

            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);
        }

        private void OnDestroy()
        {
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
            RestoreServiceOverride();
        }

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        [ContextMenu("QA/GameLoop/State Flow/Run")]
        public void Run()
        {
            if (_running)
            {
                return;
            }
            StartCoroutine(RunFlow());
        }

        private bool _running;

        private IEnumerator RunFlow()
        {
            _running = true;
            _passes = _fails = 0;

            try
            {
                // Não precisa mais de manualTick (driver sempre existe via Bootstrap)
                if (!TryResolveDependencies())
                {
                    Fail("Dependências críticas indisponíveis.");
                    yield break;
                }

                // Warmup
                yield return WaitFrames(warmupFrames);

                // Estado inicial
                yield return WaitForStateName(expectedInitialState, "InitialState", StateBoot);
                ValidateMovePermission(false, $"InitialState/{expectedInitialState}");

                ResetScenesReadyTracking();

                // Dispara COMMAND diretamente (QA não usa REQUEST → Coordinator)
                EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());

                yield return WaitForScenesReady();
                yield return WaitFrames(3);

                if (_countingService.RequestStartCount != 1)
                {
                    Fail($"RequestStart count={_countingService.RequestStartCount} (esperado 1).");
                }
                else
                {
                    Pass("RequestStart chamado exatamente 1x.");
                }

                yield return WaitForStateName(StatePlaying, "ToPlaying");
                ValidateMovePermission(true, "ToPlaying/Playing");

                yield return ValidatePauseGate();

                // Pause
                EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
                yield return WaitForStateName(StatePaused, "ToPaused");
                ValidateMovePermission(false, "ToPaused/Paused");

                // Resume
                EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(false));
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
                yield return WaitForStateName(StatePlaying, "BackToPlaying");
                ValidateMovePermission(true, "BackToPlaying/Playing");

                // Reset
                EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
                yield return WaitForStateName(expectedPostResetState, "PostReset", StateBoot);
                ValidateMovePermission(false, $"PostReset/{expectedPostResetState}");
            }
            finally
            {
                RestoreServiceOverride();

                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: Completo. Passes={_passes} Fails={_fails}.",
                    _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

                _running = false;
            }
        }

        private bool TryResolveDependencies()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<IGameLoopService>(out var loop) || loop == null)
            {
                return LogFail("IGameLoopService indisponível.");
            }
            _originalService = loop;
            _countingService = new CountingGameLoopService(loop);
            provider.RegisterGlobal<IGameLoopService>(_countingService, allowOverride: true);

            if (!provider.TryGetGlobal(out _stateDependentService))
            {
                return LogFail("IStateDependentService indisponível.");
            }
            if (!provider.TryGetGlobal(out _gateService))
            {
                return LogFail("ISimulationGateService indisponível.");
            }

            return true;

            bool LogFail(string msg)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQaTester), $"[QA] {label}: {msg}");
                return false;
            }
        }

        private void RestoreServiceOverride()
        {
            if (_originalService != null)
            {
                DependencyManager.Provider.RegisterGlobal(_originalService, allowOverride: true);
                _originalService = _countingService = null;
            }
        }

        private IEnumerator WaitForStateName(string expected, string step, string alternate = null)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                string current = _countingService.CurrentStateIdName;
                if (current == expected || current == alternate)
                {
                    Pass($"{step}: estado '{current}'.");
                    yield break;
                }
                yield return null;
            }
            Fail($"{step}: timeout. atual='{_countingService.CurrentStateIdName}' esperado='{expected}'.");
        }

        private IEnumerator WaitForScenesReady()
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (_seenScenesReady)
                {
                    Pass($"ScenesReady (profile='{_scenesReadyProfile}').");
                    yield break;
                }
                yield return null;
            }
            Fail("Timeout ScenesReady.");
        }

        private void ResetScenesReadyTracking() => _seenScenesReady = false;

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (_seenScenesReady)
            {
                return;
            }
            string profile = evt.Context.TransitionProfileName ?? string.Empty;
            if (profile != expectedStartProfile)
            {
                return;
            }

            _seenScenesReady = true;
            _scenesReadyProfile = profile;
        }

        private IEnumerator ValidatePauseGate()
        {
            bool before = _stateDependentService.CanExecuteAction(ActionType.Move);

            using (_gateService.Acquire(SimulationGateTokens.Pause))
            {
                yield return null;
                if (_stateDependentService.CanExecuteAction(ActionType.Move))
                {
                    Fail("Gate Pause não bloqueou Move.");
                }
                else
                {
                    Pass("Gate Pause bloqueou Move.");
                }
            }

            yield return null;

            bool after = _stateDependentService.CanExecuteAction(ActionType.Move);
            if (before)
            {
                Pass("Move liberado antes/após gate.");
            }
            else
            {
                Fail("Move não estava liberado antes do gate.");
            }
            if (after)
            {
                Pass("Move liberado após release.");
            }
            else
            {
                Fail("Move ficou bloqueado após release.");
            }
        }

        private void ValidateMovePermission(bool expected, string contextInfo)
        {
            bool actual = _stateDependentService.CanExecuteAction(ActionType.Move);
            if (actual == expected)
            {
                Pass($"Move {(expected ? "liberado" : "bloqueado")} em {contextInfo}.");
            }
            else
            {
                Fail($"Move {(expected ? "não liberado" : "liberado")} em {contextInfo}.");
            }
        }

        private IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private void Pass(string msg) { _passes++; DebugUtility.Log(typeof(GameLoopStateFlowQaTester), $"[QA] PASS - {msg}", DebugUtility.Colors.Success); }
        private void Fail(string msg) { _fails++; DebugUtility.LogError(typeof(GameLoopStateFlowQaTester), $"[QA] FAIL - {msg}"); }

        private sealed class CountingGameLoopService : IGameLoopService
        {
            private readonly IGameLoopService _inner;
            public int RequestStartCount { get; private set; }
            public string CurrentStateIdName => _inner.CurrentStateIdName;

            public CountingGameLoopService(IGameLoopService inner) => _inner = inner;

            public void Initialize() => _inner.Initialize();
            public void Tick(float dt) => _inner.Tick(dt);
            public void RequestStart() { RequestStartCount++; _inner.RequestStart(); }
            public void RequestPause() => _inner.RequestPause();
            public void RequestResume() => _inner.RequestResume();
            public void RequestReset() => _inner.RequestReset();
            public void Dispose() => _inner.Dispose();
        }
    }
}
