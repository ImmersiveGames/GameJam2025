using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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

        private static int _runCounter;

        private bool _isRunning;
        private bool _autoInitDisabled;
        private bool _savedRepeatedVerbose;
        private bool _hasSavedRepeatedVerbose;

        [SerializeField] private bool disableControllerAutoInitializeOnStart = true;
        [SerializeField] private bool suppressRepeatedCallWarningsDuringBaseline = true;
        [SerializeField] private bool restoreDebugSettingsAfterBaseline = true;

        private void Awake()
        {
            BaselineDebugBootstrap.IsBaselineRunning = true;
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
            BaselineDebugBootstrap.IsBaselineRunning = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BaselineDebugBootstrap.SetRunnerActive(false);
#endif
            RestoreRepeatedWarningSuppressionIfNeeded();
        }

        private void OnDestroy()
        {
            BaselineDebugBootstrap.IsBaselineRunning = false;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
                BaselineDebugBootstrap.IsBaselineRunning = false;
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
                BaselineDebugBootstrap.IsBaselineRunning = false;
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
                    LogInfo(runId, "Hard Reset - END");

                    LogInfo(runId, "Soft Reset Players - BEGIN");
                    await controller.ResetPlayersAsync($"Baseline/SoftResetPlayers/{runId}");
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
                _isRunning = false;
                BaselineDebugBootstrap.IsBaselineRunning = false;
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
            BaselineDebugBootstrap.IsBaselineRunning = true;
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
            SaveRepeatedWarningStateIfNeeded();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
            if (_hasSavedRepeatedVerbose)
            {
                return;
            }

            _savedRepeatedVerbose = DebugUtility.GetRepeatedCallVerbose();
            _hasSavedRepeatedVerbose = true;
        }

        private void RestoreRepeatedWarningState()
        {
            if (!_hasSavedRepeatedVerbose || !restoreDebugSettingsAfterBaseline)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(_savedRepeatedVerbose);
            _hasSavedRepeatedVerbose = false;
        }
    }
}
