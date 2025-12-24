using System;
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
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStateFlowQaTester : MonoBehaviour
    {
        private const string DefaultStartProfile = "startup";
        private const float DefaultTimeoutSeconds = 6f;

        private const string StateBoot = "Boot";
        private const string StateMenu = "Menu";
        private const string StatePlaying = "Playing";
        private const string StatePaused = "Paused";

        [Header("Runner")]
        [SerializeField] private string label = "GameLoopStateFlowQATester";
        [SerializeField] private bool runOnStart;
        [SerializeField] private int warmupFrames = 2;
        [SerializeField] private float timeoutSeconds = DefaultTimeoutSeconds;

        [Header("Expected Flow")]
        [Tooltip("Estado inicial esperado quando o QA começa a observar. Em geral será 'Menu' (Boot já ocorreu).")]
        [SerializeField] private string expectedInitialState = StateMenu;

        [Tooltip("Estado esperado após um Reset solicitado. Se você considera Boot 'apenas uma vez', use 'Menu'.")]
        [SerializeField] private string expectedPostResetState = StateMenu;

        [Header("Scene Flow")]
        [SerializeField] private string expectedStartProfile = DefaultStartProfile;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private int _passes;
        private int _fails;

        private IGameLoopService _originalService;
        private CountingGameLoopService _countingService;
        private IStateDependentService _stateDependentService;
        private ISimulationGateService _gateService;

        private EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;
        private bool _manualTick;
        private bool _running;
        private bool _seenScenesReady;
        private string _scenesReadyProfile = string.Empty;
        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
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
                if (verboseLogs)
                {
                    DebugUtility.LogWarning(typeof(GameLoopStateFlowQaTester),
                        $"[QA] {label}: execução ignorada (já em andamento). scene='{_sceneName}'.");
                }
                return;
            }

            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            _running = true;
            _passes = 0;
            _fails = 0;

            try
            {
                _manualTick = FindFirstObjectByType<GameLoopDriver>(FindObjectsInactive.Include) == null;

                if (!TryResolveDependencies())
                {
                    Fail("Dependências críticas indisponíveis (DI/GameLoop/State/Gate).");
                    yield break;
                }

                // Warmup
                for (int i = 0; i < Mathf.Max(0, warmupFrames); i++)
                {
                    yield return TickFrame();
                }

                // Initial state (normalmente Menu; Boot pode acontecer antes do QA começar a observar)
                yield return WaitForStateName(expectedInitialState, "InitialState", allowAlternate: StateBoot);

                ValidateMovePermission(false, $"InitialState/{expectedInitialState}");

                ResetScenesReadyTracking();

                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: Step StartRequest via GameStartEvent (scene='{_sceneName}').");

                EventBus<GameStartEvent>.Raise(new GameStartEvent());

                yield return WaitForScenesReady();
                yield return WaitFrames(3);

                if (_countingService.RequestStartCount != 1)
                {
                    Fail($"RequestStart deveria ocorrer 1x após ScenesReady. count={_countingService.RequestStartCount}.");
                }
                else
                {
                    Pass("RequestStart liberado exatamente 1x após ScenesReady.");
                }

                yield return WaitForStateName(StatePlaying, "ToPlaying");
                ValidateMovePermission(true, "ToPlaying/Playing");

                yield return ValidatePauseGate();

                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: Step Pause/Resume (scene='{_sceneName}').");

                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
                yield return WaitForStateName(StatePaused, "ToPaused");
                ValidateMovePermission(false, "ToPaused/Paused");

                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
                yield return WaitForStateName(StatePlaying, "BackToPlaying");
                ValidateMovePermission(true, "BackToPlaying/Playing");

                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: Step Reset (scene='{_sceneName}').");

                EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());

                // Se a sua definição de Reset não deve voltar pra Boot, configure expectedPostResetState = "Menu".
                yield return WaitForStateName(expectedPostResetState, "PostReset", allowAlternate: StateBoot);
                ValidateMovePermission(false, $"PostReset/{expectedPostResetState}");
            }
            finally
            {
                RestoreServiceOverride();

                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: QA complete. Passes={_passes} Fails={_fails}.",
                    _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

                _running = false;
            }
        }

        private bool TryResolveDependencies()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<IGameLoopService>(out var loop) || loop == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: IGameLoopService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            _originalService = loop;
            _countingService = new CountingGameLoopService(loop);
            provider.RegisterGlobal<IGameLoopService>(_countingService, allowOverride: true);

            if (!provider.TryGetGlobal(out _stateDependentService) || _stateDependentService == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: IStateDependentService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            if (!provider.TryGetGlobal(out _gateService) || _gateService == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: ISimulationGateService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: dependências resolvidas. manualTick={_manualTick}, scene='{_sceneName}'.");
            }

            return true;
        }

        private void RestoreServiceOverride()
        {
            if (_originalService == null)
            {
                return;
            }

            DependencyManager.Provider.RegisterGlobal(_originalService, allowOverride: true);
            _originalService = null;
            _countingService = null;
        }

        private IEnumerator WaitForStateName(string expected, string stepLabel, string allowAlternate = null)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);

            while (Time.realtimeSinceStartup <= deadline)
            {
                string current = _countingService != null ? _countingService.CurrentStateName : "<null>";

                if (string.Equals(current, expected, StringComparison.Ordinal) ||
                    (!string.IsNullOrEmpty(allowAlternate) && string.Equals(current, allowAlternate, StringComparison.Ordinal)))
                {
                    Pass($"{stepLabel} confirmado (state='{current}').");
                    yield break;
                }

                yield return TickFrame();
            }

            string finalState = _countingService != null ? _countingService.CurrentStateName : "<null>";
            Fail($"Timeout aguardando estado '{expected}' no step '{stepLabel}'. atual='{finalState}'.");
        }

        private IEnumerator WaitForScenesReady()
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);

            while (Time.realtimeSinceStartup <= deadline)
            {
                if (_seenScenesReady)
                {
                    Pass($"ScenesReady observado (profile='{_scenesReadyProfile}').");
                    yield break;
                }

                yield return TickFrame();
            }

            Fail($"Timeout aguardando SceneTransitionScenesReadyEvent do profile '{expectedStartProfile}'.");
        }

        private void ResetScenesReadyTracking()
        {
            _seenScenesReady = false;
            _scenesReadyProfile = string.Empty;
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            // evt/context podem ser struct em alguns códigos; não usar "== null".
            string profile = evt.Context.TransitionProfileName;

            if (_seenScenesReady)
            {
                return;
            }

            if (!string.Equals(profile, expectedStartProfile, StringComparison.Ordinal))
            {
                if (verboseLogs)
                {
                    DebugUtility.LogVerbose(typeof(GameLoopStateFlowQaTester),
                        $"[QA] {label}: ScenesReady ignorado (profile='{profile}'). Esperado='{expectedStartProfile}'.");
                }
                return;
            }

            _seenScenesReady = true;
            _scenesReadyProfile = profile;

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: ScenesReady recebido (profile='{_scenesReadyProfile}').");
            }
        }

        private IEnumerator ValidatePauseGate()
        {
            if (_gateService == null)
            {
                Fail("ISimulationGateService indisponível para validar gate Pause.");
                yield break;
            }

            if (_stateDependentService == null)
            {
                Fail("IStateDependentService indisponível para validar gate Pause.");
                yield break;
            }

            bool allowedBefore = _stateDependentService.CanExecuteAction(ActionType.Move);

            using (IDisposable gateHandle = _gateService.Acquire(SimulationGateTokens.Pause))
            {
                yield return TickFrame();

                bool allowedDuring = _stateDependentService.CanExecuteAction(ActionType.Move);

                if (allowedDuring)
                {
                    Fail("Gate Pause não bloqueou ActionType.Move (esperado bloqueado).");
                }
                else
                {
                    Pass("Gate Pause bloqueou ActionType.Move (OK).");
                }
            }

            yield return TickFrame();

            bool allowedAfter = _stateDependentService.CanExecuteAction(ActionType.Move);

            if (!allowedBefore)
            {
                Fail("Move não estava liberado antes de aplicar o gate Pause (esperado liberado).");
            }
            else
            {
                Pass("Move liberado antes de aplicar o gate Pause (OK).");
            }

            if (allowedAfter)
            {
                Pass("Move liberado novamente após liberar o gate Pause (OK).");
            }
            else
            {
                Fail("Move permaneceu bloqueado após liberar o gate Pause (erro).");
            }
        }

        private void ValidateMovePermission(bool expectedAllowed, string stateLabel)
        {
            if (_stateDependentService == null)
            {
                Fail("IStateDependentService indisponível para validar Move.");
                return;
            }

            bool allowed = _stateDependentService.CanExecuteAction(ActionType.Move);
            string expectation = expectedAllowed ? "liberado" : "bloqueado";

            if (allowed == expectedAllowed)
            {
                Pass($"Move {expectation} em {stateLabel}.");
            }
            else
            {
                Fail($"Move não está {expectation} em {stateLabel}.");
            }
        }

        private IEnumerator TickFrame()
        {
            if (_manualTick && _countingService != null)
            {
                _countingService.Tick(Time.deltaTime);
            }

            yield return null;
        }

        private IEnumerator WaitFrames(int frames)
        {
            int total = Mathf.Max(0, frames);
            for (int i = 0; i < total; i++)
            {
                yield return TickFrame();
            }
        }

        private void Pass(string message)
        {
            _passes++;
            DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                $"[QA][GameLoopStateFlow] PASS - {message}", DebugUtility.Colors.Success);
        }

        private void Fail(string message)
        {
            _fails++;
            DebugUtility.LogError(typeof(GameLoopStateFlowQaTester),
                $"[QA][GameLoopStateFlow] FAIL - {message}");
        }

        private sealed class CountingGameLoopService : IGameLoopService
        {
            private readonly IGameLoopService _inner;

            public int RequestStartCount { get; private set; }
            public string CurrentStateName => _inner.CurrentStateName;

            public CountingGameLoopService(IGameLoopService inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public void Initialize() => _inner.Initialize();
            public void Tick(float dt) => _inner.Tick(dt);

            public void RequestStart()
            {
                RequestStartCount++;
                _inner.RequestStart();
            }

            public void RequestPause() => _inner.RequestPause();
            public void RequestResume() => _inner.RequestResume();
            public void RequestReset() => _inner.RequestReset();
            public void Dispose() => _inner.Dispose();
        }
    }
}
