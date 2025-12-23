using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStateFlowQATester : MonoBehaviour
    {
        private const string DefaultStartProfile = "startup";
        private const float DefaultTimeoutSeconds = 6f;

        [Header("Runner")]
        [SerializeField] private string label = "GameLoopStateFlowQATester";
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private int warmupFrames = 2;
        [SerializeField] private float timeoutSeconds = DefaultTimeoutSeconds;

        [Header("Scene Flow")]
        [SerializeField] private string expectedStartProfile = DefaultStartProfile;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

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
                    DebugUtility.LogWarning(typeof(GameLoopStateFlowQATester),
                        $"[QA] {label}: execução ignorada (já em andamento). scene='{_sceneName}'.");
                }
                return;
            }

            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            _running = true;

            int passes = 0;
            int fails = 0;

            try
            {
                _manualTick = FindFirstObjectByType<GameLoopDriver>(FindObjectsInactive.Include) == null;

                if (!TryResolveDependencies())
                {
                    RegisterFail("Dependências críticas indisponíveis (DI/GameLoop/State/Gate).", ref passes, ref fails);
                    yield break;
                }

                if (!GameLoopSceneFlowCoordinator.IsInstalled)
                {
                    RegisterFail("GameLoopSceneFlowCoordinator não está instalado. Fluxo Opção B indisponível.", ref passes, ref fails);
                    yield break;
                }

                _countingService.Initialize();

                for (int i = 0; i < Mathf.Max(0, warmupFrames); i++)
                {
                    yield return TickFrame();
                }

                yield return WaitForState(GameLoopStateId.Menu, "Boot → Menu", ref passes, ref fails);
                ValidateMovePermission(false, "Menu", ref passes, ref fails);

                ResetScenesReadyTracking();

                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: Step StartRequest via GameStartEvent (scene='{_sceneName}').");

                EventBus<GameStartEvent>.Raise(new GameStartEvent());

                yield return WaitForScenesReady(ref passes, ref fails);
                yield return WaitFrames(3);

                if (_countingService.RequestStartCount != 1)
                {
                    RegisterFail(
                        $"RequestStart deveria ocorrer 1x após ScenesReady. count={_countingService.RequestStartCount}.",
                        ref passes,
                        ref fails);
                }
                else
                {
                    RegisterPass("RequestStart liberado exatamente 1x após ScenesReady.", ref passes, ref fails);
                }

                yield return WaitForState(GameLoopStateId.Playing, "Menu → Playing", ref passes, ref fails);
                ValidateMovePermission(true, "Playing", ref passes, ref fails);

                yield return ValidatePauseGate(ref passes, ref fails);

                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: Step Pause/Resume (scene='{_sceneName}').");

                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
                yield return WaitForState(GameLoopStateId.Paused, "Playing → Paused", ref passes, ref fails);
                ValidateMovePermission(false, "Paused", ref passes, ref fails);

                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
                yield return WaitForState(GameLoopStateId.Playing, "Paused → Playing", ref passes, ref fails);
                ValidateMovePermission(true, "Playing (resume)", ref passes, ref fails);

                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: Step Reset (scene='{_sceneName}').");

                EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
                yield return WaitForState(GameLoopStateId.Boot, "Reset → Boot", ref passes, ref fails);
                yield return WaitForState(GameLoopStateId.Menu, "Boot → Menu (post-reset)", ref passes, ref fails);
                ValidateMovePermission(false, "Menu (post-reset)", ref passes, ref fails);
            }
            finally
            {
                RestoreServiceOverride();

                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: QA complete. Passes={passes} Fails={fails}.",
                    fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

                _running = false;
            }
        }

        private bool TryResolveDependencies()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<IGameLoopService>(out var loop) || loop == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: IGameLoopService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            _originalService = loop;
            _countingService = new CountingGameLoopService(loop);
            provider.RegisterGlobal<IGameLoopService>(_countingService, allowOverride: true);

            if (!provider.TryGetGlobal<IStateDependentService>(out _stateDependentService) || _stateDependentService == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: IStateDependentService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            if (!provider.TryGetGlobal<ISimulationGateService>(out _gateService) || _gateService == null)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: ISimulationGateService indisponível no DI global (scene='{_sceneName}').");
                return false;
            }

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
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

        private IEnumerator WaitForState(GameLoopStateId targetState, string stepLabel, ref int passes, ref int fails)
        {
            string targetName = targetState.ToString();
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);

            while (Time.realtimeSinceStartup <= deadline)
            {
                if (string.Equals(_countingService.CurrentStateName, targetName, StringComparison.Ordinal))
                {
                    RegisterPass($"{stepLabel} confirmado (state='{targetName}').", ref passes, ref fails);
                    yield break;
                }

                yield return TickFrame();
            }

            RegisterFail($"Timeout aguardando estado '{targetName}' no step '{stepLabel}'.", ref passes, ref fails);
        }

        private IEnumerator WaitForScenesReady(ref int passes, ref int fails)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);

            while (Time.realtimeSinceStartup <= deadline)
            {
                if (_seenScenesReady)
                {
                    RegisterPass($"ScenesReady observado (profile='{_scenesReadyProfile}').", ref passes, ref fails);
                    yield break;
                }

                yield return TickFrame();
            }

            RegisterFail(
                $"Timeout aguardando SceneTransitionScenesReadyEvent do profile '{expectedStartProfile}'.",
                ref passes,
                ref fails);
        }

        private void ResetScenesReadyTracking()
        {
            _seenScenesReady = false;
            _scenesReadyProfile = string.Empty;
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (_seenScenesReady || evt == null)
            {
                return;
            }

            string profile = evt.Context.TransitionProfileName;

            if (!string.Equals(profile, expectedStartProfile, StringComparison.Ordinal))
            {
                if (verboseLogs)
                {
                    DebugUtility.LogVerbose(typeof(GameLoopStateFlowQATester),
                        $"[QA] {label}: ScenesReady ignorado (profile='{profile}'). Esperado='{expectedStartProfile}'.");
                }
                return;
            }

            _seenScenesReady = true;
            _scenesReadyProfile = profile;

            if (verboseLogs)
            {
                DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                    $"[QA] {label}: ScenesReady recebido (profile='{_scenesReadyProfile}').");
            }
        }

        private IEnumerator ValidatePauseGate(ref int passes, ref int fails)
        {
            if (_gateService == null)
            {
                RegisterFail("ISimulationGateService indisponível para validar gate Pause.", ref passes, ref fails);
                yield break;
            }

            if (_stateDependentService == null)
            {
                RegisterFail("IStateDependentService indisponível para validar gate Pause.", ref passes, ref fails);
                yield break;
            }

            bool allowedBefore = _stateDependentService.CanExecuteAction(ActionType.Move);

            using (IDisposable gateHandle = _gateService.Acquire(SimulationGateTokens.Pause))
            {
                yield return TickFrame();

                bool allowedDuring = _stateDependentService.CanExecuteAction(ActionType.Move);

                if (allowedDuring)
                {
                    RegisterFail("Gate Pause não bloqueou ActionType.Move em Playing.", ref passes, ref fails);
                }
                else
                {
                    RegisterPass("Gate Pause bloqueou ActionType.Move em Playing.", ref passes, ref fails);
                }
            }

            yield return TickFrame();

            bool allowedAfter = _stateDependentService.CanExecuteAction(ActionType.Move);

            if (!allowedBefore)
            {
                RegisterFail("Move não estava liberado antes de aplicar o gate Pause (esperado Playing).", ref passes, ref fails);
            }
            else
            {
                RegisterPass("Move liberado antes de aplicar o gate Pause (Playing).", ref passes, ref fails);
            }

            if (allowedAfter)
            {
                RegisterPass("Move liberado novamente após liberar o gate Pause.", ref passes, ref fails);
            }
            else
            {
                RegisterFail("Move permaneceu bloqueado após liberar o gate Pause.", ref passes, ref fails);
            }
        }

        private void ValidateMovePermission(bool expectedAllowed, string stateLabel, ref int passes, ref int fails)
        {
            if (_stateDependentService == null)
            {
                RegisterFail("IStateDependentService indisponível para validar Move.", ref passes, ref fails);
                return;
            }

            bool allowed = _stateDependentService.CanExecuteAction(ActionType.Move);
            string expectation = expectedAllowed ? "liberado" : "bloqueado";

            if (allowed == expectedAllowed)
            {
                RegisterPass($"Move {expectation} em {stateLabel}.", ref passes, ref fails);
            }
            else
            {
                RegisterFail($"Move não está {expectation} em {stateLabel}.", ref passes, ref fails);
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

        private static void RegisterPass(string message, ref int passes, ref int fails)
        {
            passes++;
            DebugUtility.Log(typeof(GameLoopStateFlowQATester),
                $"[QA][GameLoopStateFlow] PASS - {message}", DebugUtility.Colors.Success);
        }

        private static void RegisterFail(string message, ref int passes, ref int fails)
        {
            fails++;
            DebugUtility.LogError(typeof(GameLoopStateFlowQATester),
                $"[QA][GameLoopStateFlow] FAIL - {message}");
        }

        private sealed class CountingGameLoopService : IGameLoopService
        {
            private readonly IGameLoopService _inner;

            public int RequestStartCount { get; private set; }

            public CountingGameLoopService(IGameLoopService inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public string CurrentStateName => _inner.CurrentStateName;

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
