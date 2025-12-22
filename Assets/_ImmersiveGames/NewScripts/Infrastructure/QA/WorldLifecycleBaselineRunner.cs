/*
 * ChangeLog
 * - Gate Validation agora força spawn do player via ResetWorldAsync se necessário e aguarda warmup determinístico.
 * - Adicionado validador automatizado do Gate (ações) com ContextMenu/Hotkey e logs determinísticos para QA.
 */
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.NewScripts.Gameplay.Player.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Runner minimalista para acionar o baseline do WorldLifecycle sem depender do fluxo de produção.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleBaselineRunner : MonoBehaviour
    {
        private const string LogPrefix = "[Baseline]";
        private const float GateVelocityEpsilon = 0.01f;
        private const float GatePositionEpsilon = 0.01f;

        private static int _runCounter;
        private static int _gateRunCounter;

        private bool _isRunning;
        private bool _isGateValidationRunning;
        private bool _autoInitDisabled;
        private bool _savedRepeatedVerbose;
        private bool _hasSavedRepeatedVerbose;
        private bool _pausedByQaToggle;
        private bool _lastPublishedPauseState;
        private bool _loggedEventBusUnavailable;

        [SerializeField] private bool disableControllerAutoInitializeOnStart = true;
        [SerializeField] private bool suppressRepeatedCallWarningsDuringBaseline = true;
        [SerializeField] private bool restoreDebugSettingsAfterBaseline = true;

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BaselineDebugBootstrap.SetRunnerActive(true);
#endif
        }

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BaselineDebugBootstrap.SetRunnerActive(true);
#endif

            if (!disableControllerAutoInitializeOnStart)
            {
                return;
            }

            var controller = FindFirstObjectByType<WorldLifecycleController>();
            if (controller == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleBaselineRunner),
                    $"{LogPrefix} AutoInitializeOnStart não pôde ser desabilitado no Awake — WorldLifecycleController não encontrado. ({BuildSceneTimeScaleInfo()})");
                return;
            }

            controller.AutoInitializeOnStart = false;
            _autoInitDisabled = true;
            DebugUtility.Log(typeof(WorldLifecycleBaselineRunner),
                $"{LogPrefix} AutoInitializeOnStart desabilitado no Awake (pre-Start) ({BuildSceneTimeScaleInfo()})");
        }

        private void OnDisable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BaselineDebugBootstrap.SetRunnerActive(false);
#endif
            RestoreRepeatedWarningSuppressionIfNeeded();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BaselineDebugBootstrap.SetRunnerActive(false);
#endif
            RestoreRepeatedWarningSuppressionIfNeeded();
        }

        [ContextMenu("QA/Baseline/Run Hard Reset")]
        public async void RunHardResetContextMenu()
        {
            await RunHardResetAsync("ContextMenu/HardReset");
        }

        [ContextMenu("QA/Baseline/Run Soft Reset Players")]
        public async void RunSoftResetPlayersContextMenu()
        {
            await RunSoftResetPlayersAsync("ContextMenu/SoftResetPlayers");
        }

        [ContextMenu("QA/Baseline/Run Full Baseline (Hard then Players)")]
        public async void RunFullBaselineContextMenu()
        {
            await RunFullBaselineAsync("ContextMenu/FullBaseline");
        }

        [ContextMenu("QA/Gate/Toggle Pause (EventBus)")]
        public void TogglePauseEventBus()
        {
            _pausedByQaToggle = !_pausedByQaToggle;
            ApplyPauseState(_pausedByQaToggle);
        }

        [ContextMenu("QA/Gate/Force Pause (EventBus)")]
        public void ForcePauseEventBus()
        {
            _pausedByQaToggle = true;
            ApplyPauseState(true);
        }

        [ContextMenu("QA/Gate/Force Resume (EventBus)")]
        public void ForceResumeEventBus()
        {
            _pausedByQaToggle = false;
            ApplyPauseState(false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("QA/Gate/Run Gate Validation (Actions)")]
        public async void RunGateValidationActionsContextMenu()
        {
            await RunGateValidationAsync("ContextMenu/GateValidation");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                _ = RunHardResetAsync("Hotkey/F7");
            }
            else if (Input.GetKeyDown(KeyCode.F8))
            {
                _ = RunSoftResetPlayersAsync("Hotkey/F8");
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                _ = RunFullBaselineAsync("Hotkey/F9");
            }
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                _ = RunGateValidationAsync("Hotkey/F10");
            }
        }
#endif

        private async Task RunHardResetAsync(string trigger)
        {
            if (!CanStartBaseline())
            {
                return;
            }

            var runId = NextRunId();
            ApplyRepeatedWarningSuppressionIfNeeded();

            var controller = FindController();
            try
            {
                if (controller == null)
                {
                    return;
                }

                DisableControllerAutoInitializeOnStartIfNeeded(runId, controller);

                LogInfo(runId, $"START Hard Reset (trigger='{trigger}', {BuildSceneTimeScaleInfo()})");
                try
                {
                    await controller.ResetWorldAsync($"Baseline/HardReset/{runId}");
                    LogInfo(runId, $"END Hard Reset ({BuildSceneTimeScaleInfo()})");
                }
                catch (Exception ex)
                {
                    LogError(runId, $"Exception during Hard Reset: {ex}");
                }
            }
            finally
            {
                _isRunning = false;
                RestoreRepeatedWarningSuppressionIfNeeded();
            }
        }

        private async Task RunSoftResetPlayersAsync(string trigger)
        {
            if (!CanStartBaseline())
            {
                return;
            }

            var runId = NextRunId();
            ApplyRepeatedWarningSuppressionIfNeeded();

            var controller = FindController();
            try
            {
                if (controller == null)
                {
                    return;
                }

                DisableControllerAutoInitializeOnStartIfNeeded(runId, controller);

                LogInfo(runId, $"START Soft Reset Players (trigger='{trigger}', {BuildSceneTimeScaleInfo()})");
                try
                {
                    await controller.ResetPlayersAsync($"Baseline/SoftResetPlayers/{runId}");
                    LogInfo(runId, $"END Soft Reset Players ({BuildSceneTimeScaleInfo()})");
                }
                catch (Exception ex)
                {
                    LogError(runId, $"Exception during Soft Reset Players: {ex}");
                }
            }
            finally
            {
                _isRunning = false;
                RestoreRepeatedWarningSuppressionIfNeeded();
            }
        }

        private async Task RunFullBaselineAsync(string trigger)
        {
            if (!CanStartBaseline())
            {
                return;
            }

            var runId = NextRunId();
            var stopwatch = Stopwatch.StartNew();
            var hardResetSucceeded = false;
            var softResetSucceeded = false;
            ApplyRepeatedWarningSuppressionIfNeeded();

            var controller = FindController();
            try
            {
                if (controller == null)
                {
                    return;
                }

                DisableControllerAutoInitializeOnStartIfNeeded(runId, controller);

                LogInfo(runId, $"START Full Baseline (trigger='{trigger}', {BuildSceneTimeScaleInfo()})");
                try
                {
                    LogInfo(runId, "Hard Reset - BEGIN");
                    await controller.ResetWorldAsync($"Baseline/HardReset/{runId}");
                    hardResetSucceeded = true;
                    LogInfo(runId, "Hard Reset - END");

                    LogInfo(runId, "Soft Reset Players - BEGIN");
                    await controller.ResetPlayersAsync($"Baseline/SoftResetPlayers/{runId}");
                    softResetSucceeded = true;
                    LogInfo(runId, "Soft Reset Players - END");

                    LogInfo(runId, $"END Full Baseline ({BuildSceneTimeScaleInfo()})");
                }
                catch (Exception ex)
                {
                    LogError(runId, $"Exception during Full Baseline: {ex}");
                }
            }
            finally
            {
                stopwatch.Stop();
                LogInfo(runId,
                    $"Baseline Summary — activeScene='{SceneManager.GetActiveScene().name}', runId='{runId}', Hard Reset={(hardResetSucceeded ? "SUCCESS" : "FAILED")}, Soft Reset Players={(softResetSucceeded ? "SUCCESS" : "FAILED")}, totalTimeMs={stopwatch.ElapsedMilliseconds}");
                _isRunning = false;
                RestoreRepeatedWarningSuppressionIfNeeded();
            }
        }

        private bool CanStartBaseline()
        {
            if (_isRunning)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleBaselineRunner),
                    $"{LogPrefix} baseline ignorado — já existe uma execução em andamento.");
                return false;
            }

            _isRunning = true;
            return true;
        }

        private WorldLifecycleController FindController()
        {
            var controller = FindFirstObjectByType<WorldLifecycleController>();
            if (controller != null)
            {
                return controller;
            }

            DebugUtility.LogError(typeof(WorldLifecycleBaselineRunner),
                $"{LogPrefix} não encontrou WorldLifecycleController na cena. Abortando baseline. " +
                $"Detalhes de cena: activeScene='{SceneManager.GetActiveScene().name}', runnerScene='{gameObject.scene.name}'.");
            return null;
        }

        private static string NextRunId()
        {
            var next = ++_runCounter;
            return $"Run-{next:0000}";
        }

        private static void LogInfo(string runId, string message)
        {
            DebugUtility.Log(typeof(WorldLifecycleBaselineRunner),
                $"{LogPrefix} [{runId}] {message}");
        }

        private static void LogError(string runId, string message)
        {
            DebugUtility.LogError(typeof(WorldLifecycleBaselineRunner),
                $"{LogPrefix} [{runId}] {message}");
        }

        private static string BuildSceneTimeScaleInfo()
        {
            return $"scene='{SceneManager.GetActiveScene().name}', timeScale={Time.timeScale}";
        }

        private void DisableControllerAutoInitializeOnStartIfNeeded(string runId, WorldLifecycleController controller)
        {
            if (!disableControllerAutoInitializeOnStart)
            {
                return;
            }

            controller.AutoInitializeOnStart = false;

            if (_autoInitDisabled)
            {
                return;
            }

            _autoInitDisabled = true;
            LogInfo(runId, $"AutoInitializeOnStart desabilitado pelo baseline runner ({BuildSceneTimeScaleInfo()})");
        }

        private void ApplyRepeatedWarningSuppressionIfNeeded()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SaveRepeatedWarningStateIfNeeded();

            if (!suppressRepeatedCallWarningsDuringBaseline)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(false);
#endif
        }

        private void RestoreRepeatedWarningSuppressionIfNeeded()
        {
            if (!restoreDebugSettingsAfterBaseline)
            {
                return;
            }

            if (!_hasSavedRepeatedVerbose)
            {
                return;
            }

            RestoreRepeatedWarningState();
        }

        private void SaveRepeatedWarningStateIfNeeded()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_hasSavedRepeatedVerbose)
            {
                return;
            }

            _savedRepeatedVerbose = DebugUtility.GetRepeatedCallVerbose();
            _hasSavedRepeatedVerbose = true;
#endif
        }

        private void RestoreRepeatedWarningState()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_hasSavedRepeatedVerbose || !restoreDebugSettingsAfterBaseline)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(_savedRepeatedVerbose);
            _hasSavedRepeatedVerbose = false;
#endif
        }

        // Gate toggle de QA: publica eventos para bloquear/liberar ações.
        // Importante: o gate não congela física; gravidade/rigidbodies continuam como parte do loop normal ou FSM.
        private void ApplyPauseState(bool paused)
        {
            _ = TryApplyPauseState(paused, null);
        }

        private bool TryApplyPauseState(bool paused, string runId)
        {
            if (_lastPublishedPauseState == paused)
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleBaselineRunner),
                    paused
                        ? "[QA Gate Toggle] Ignorado: ações já estavam bloqueadas; física segue não congelada."
                        : "[QA Gate Toggle] Ignorado: ações já estavam liberadas; física continua normal.");
                return true;
            }

            try
            {
                EventBus<GamePauseEvent>.Raise(new GamePauseEvent(paused));
                var prefix = runId != null ? $"[{runId}] " : string.Empty;
                DebugUtility.LogVerbose(typeof(WorldLifecycleBaselineRunner),
                    paused
                        ? $"{prefix}[QA Gate Toggle] Ações bloqueadas; física NÃO congelada (GamePauseEvent)."
                        : $"{prefix}[QA Gate Toggle] Ações liberadas; física continua normal (GamePauseEvent).");

                if (!paused)
                {
                    EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
                    DebugUtility.LogVerbose(typeof(WorldLifecycleBaselineRunner),
                        $"{prefix}[QA Gate Toggle] GameResumeRequestedEvent publicado (gate libera ações; física não é congelada pelo gate).");
                }

                _lastPublishedPauseState = paused;
                return true;
            }
            catch (Exception ex)
            {
                if (_loggedEventBusUnavailable)
                {
                    return false;
                }

                DebugUtility.LogWarning(typeof(WorldLifecycleBaselineRunner),
                    $"[QA Gate Toggle] EventBus indisponível; não foi possível publicar pause/resume ({ex.GetType().Name})");
                _loggedEventBusUnavailable = true;
                return false;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private async Task RunGateValidationAsync(string trigger)
        {
            if (_isRunning)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleBaselineRunner),
                    $"{LogPrefix} Gate validation ignorada — baseline em andamento.");
                return;
            }

            if (_isGateValidationRunning)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleBaselineRunner),
                    $"{LogPrefix} Gate validation ignorada — já existe uma execução em andamento.");
                return;
            }

            _isGateValidationRunning = true;
            var runId = NextGateRunId();

            LogInfo(runId, $"START Gate Validation (trigger='{trigger}', scene='{SceneManager.GetActiveScene().name}')");

            try
            {
                var controller = await EnsurePlayerSpawnedForGateValidationAsync(runId);
                if (controller == null)
                {
                    return;
                }

                var rb = controller.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    LogError(runId, "FAIL Gate Validation — Rigidbody não encontrado no player.");
                    return;
                }

                if (!DependencyManager.Provider.TryGetGlobal(out IStateDependentService stateService) || stateService == null)
                {
                    LogError(runId, "FAIL Gate Validation — IStateDependentService não foi resolvido.");
                    return;
                }

                var initialPos = rb.position;
                var initialRot = rb.rotation;
                var initialLinear = rb.linearVelocity;
                var initialAngular = rb.angularVelocity;

                ResetHorizontalMotion(rb);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                controller.QA_ClearInputs();
#endif

                var unpausedBefore = await RunGatePhaseAsync(controller, rb, stateService, 10, new Vector2(0f, 1f), runId, pausedExpected: false, phase: "Phase-A/PrePause");
                var pausePublished = TryApplyPauseState(true, runId);
                if (!pausePublished)
                {
                    LogError(runId, "FAIL Gate Validation — EventBus indisponível ao publicar pause. Abortando validação.");
                    RestoreInitialState(controller, rb, initialPos, initialRot, initialLinear, initialAngular);
                    return;
                }

                ResetHorizontalMotion(rb);
                var paused = await RunGatePhaseAsync(controller, rb, stateService, 10, new Vector2(0f, 1f), runId, pausedExpected: true, phase: "Phase-B/Paused");

                var resumePublished = TryApplyPauseState(false, runId);
                if (!resumePublished)
                {
                    LogError(runId, "FAIL Gate Validation — EventBus indisponível ao publicar resume. Abortando validação.");
                    RestoreInitialState(controller, rb, initialPos, initialRot, initialLinear, initialAngular);
                    return;
                }

                ResetHorizontalMotion(rb);
                var unpausedAfter = await RunGatePhaseAsync(controller, rb, stateService, 10, new Vector2(0f, 1f), runId, pausedExpected: false, phase: "Phase-C/PostResume");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                controller.QA_ClearInputs();
#endif
                RestoreInitialState(controller, rb, initialPos, initialRot, initialLinear, initialAngular);

                var allPassed = unpausedBefore.Passed && paused.Passed && unpausedAfter.Passed;
                LogInfo(runId, allPassed
                    ? $"PASS Gate Validation — movimento antes/depois OK, bloqueio durante pause OK. (trigger='{trigger}')"
                    : $"FAIL Gate Validation — verifique fases: before={unpausedBefore.Passed}, paused={paused.Passed}, after={unpausedAfter.Passed}. (trigger='{trigger}')");
            }
            catch (Exception ex)
            {
                LogError(runId, $"FAIL Gate Validation — exceção inesperada: {ex}");
            }
            finally
            {
                _isGateValidationRunning = false;
            }
        }

        private async Task<NewPlayerMovementController> EnsurePlayerSpawnedForGateValidationAsync(string runId)
        {
            var controller = FindFirstObjectByType<NewPlayerMovementController>();
            if (controller != null)
            {
                return controller;
            }

            var lifecycleController = FindFirstObjectByType<WorldLifecycleController>();
            if (lifecycleController == null)
            {
                LogError(runId,
                    $"FAIL Gate Validation — Player não encontrado e WorldLifecycleController ausente. activeScene='{SceneManager.GetActiveScene().name}', runnerScene='{gameObject.scene.name}'. Sugestão: execute Hard Reset/Baseline antes.");
                return null;
            }

            LogInfo(runId, "Player não encontrado; executando ResetWorldAsync para forçar spawn antes da validação do Gate.");
            try
            {
                await lifecycleController.ResetWorldAsync($"Baseline/GateValidation/Spawn/{runId}");
            }
            catch (Exception ex)
            {
                LogError(runId, $"FAIL Gate Validation — exceção ao forçar spawn via ResetWorldAsync: {ex}");
                return null;
            }

            await AwaitFrames(3);

            controller = FindFirstObjectByType<NewPlayerMovementController>();
            if (controller == null)
            {
                LogError(runId,
                    $"FAIL Gate Validation — Player ainda não encontrado após reset. activeScene='{SceneManager.GetActiveScene().name}', runnerScene='{gameObject.scene.name}'. Sugestão: execute Hard Reset/Baseline primeiro.");
            }

            return controller;
        }

        private static async Task AwaitFrames(int frames)
        {
            var target = Mathf.Max(1, frames);
            for (var i = 0; i < target; i++)
            {
                await Task.Yield();
            }
        }

        private static void ResetHorizontalMotion(Rigidbody rb)
        {
            var current = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, current.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        private static void RestoreInitialState(NewPlayerMovementController controller, Rigidbody rb, Vector3 pos, Quaternion rot, Vector3 linear, Vector3 angular)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            controller.QA_ClearInputs();
#endif
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = linear;
            rb.angularVelocity = angular;
        }

        private async Task<GatePhaseResult> RunGatePhaseAsync(NewPlayerMovementController controller, Rigidbody rb, IStateDependentService stateService, int fixedFrames, Vector2 moveInput, string runId, bool pausedExpected, string phase)
        {
            var startFrame = CurrentFixedFrameIndex();
            var startPos = rb.position;
            float maxSpeed = 0f;

            controller.QA_SetMoveInput(moveInput);
            controller.QA_SetLookInput(Vector2.zero);

            while (CurrentFixedFrameIndex() - startFrame < fixedFrames)
            {
                await Task.Yield();

                var horizontalSpeed = HorizontalMagnitude(rb.linearVelocity);
                if (horizontalSpeed > maxSpeed)
                {
                    maxSpeed = horizontalSpeed;
                }
            }

            var delta = rb.position - startPos;
            var horizontalDelta = HorizontalMagnitude(delta);
            var moveAllowed = stateService.CanExecuteAction(ActionType.Move);
            var navigateAllowed = stateService.CanExecuteAction(ActionType.Navigate);
            var passed = pausedExpected
                ? maxSpeed <= GateVelocityEpsilon && horizontalDelta <= GatePositionEpsilon && !moveAllowed
                : (maxSpeed > GateVelocityEpsilon || horizontalDelta > GatePositionEpsilon) && moveAllowed;

            var stateLabel = pausedExpected ? "PAUSED" : "UNPAUSED";
            var message = passed
                ? $"PASS {phase} — state={stateLabel}, maxSpeedXZ={maxSpeed:F4}, deltaPosXZ={horizontalDelta:F4}, canMove={moveAllowed}, canNavigate={navigateAllowed}."
                : $"FAIL {phase} — state={stateLabel}, maxSpeedXZ={maxSpeed:F4}, deltaPosXZ={horizontalDelta:F4}, canMove={moveAllowed}, canNavigate={navigateAllowed}. Esperado {(pausedExpected ? "sem movimento" : "movimento detectável") } (Gate não congela gravidade).";

            if (passed)
            {
                LogInfo(runId, message);
            }
            else
            {
                LogError(runId, message);
            }

            return new GatePhaseResult
            {
                Passed = passed,
                MaxHorizontalSpeed = maxSpeed,
                HorizontalDelta = horizontalDelta
            };
        }

        private static int CurrentFixedFrameIndex()
        {
            return Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);
        }

        private static float HorizontalMagnitude(Vector3 value)
        {
            return new Vector2(value.x, value.z).magnitude;
        }

        private static string NextGateRunId()
        {
            var next = ++_gateRunCounter;
            return $"GateRun-{next:0000}";
        }

        private struct GatePhaseResult
        {
            public bool Passed { get; set; }
            public float MaxHorizontalSpeed { get; set; }
            public float HorizontalDelta { get; set; }
        }
#endif
    }
}
