using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Runner minimalista para acionar o baseline do WorldLifecycle sem depender do fluxo de produção.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleBaselineRunner : MonoBehaviour
    {
        private const string LogPrefix = "[Baseline]";

        private static int _runCounter;

        private bool _isRunning;

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
            var controller = FindController();
            if (controller == null)
            {
                _isRunning = false;
                return;
            }

            LogInfo(runId, $"START Hard Reset (trigger='{trigger}')");
            try
            {
                await controller.ResetWorldAsync($"Baseline/HardReset/{runId}");
                LogInfo(runId, "END Hard Reset");
            }
            catch (Exception ex)
            {
                LogError(runId, $"Exception during Hard Reset: {ex}");
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task RunSoftResetPlayersAsync(string trigger)
        {
            if (!CanStartBaseline())
            {
                return;
            }

            var runId = NextRunId();
            var controller = FindController();
            if (controller == null)
            {
                _isRunning = false;
                return;
            }

            LogInfo(runId, $"START Soft Reset Players (trigger='{trigger}')");
            try
            {
                await controller.ResetPlayersAsync($"Baseline/SoftResetPlayers/{runId}");
                LogInfo(runId, "END Soft Reset Players");
            }
            catch (Exception ex)
            {
                LogError(runId, $"Exception during Soft Reset Players: {ex}");
            }
            finally
            {
                _isRunning = false;
            }
        }

        private async Task RunFullBaselineAsync(string trigger)
        {
            if (!CanStartBaseline())
            {
                return;
            }

            var runId = NextRunId();
            var controller = FindController();
            if (controller == null)
            {
                _isRunning = false;
                return;
            }

            LogInfo(runId, $"START Full Baseline (trigger='{trigger}')");
            try
            {
                LogInfo(runId, "Hard Reset - BEGIN");
                await controller.ResetWorldAsync($"Baseline/HardReset/{runId}");
                LogInfo(runId, "Hard Reset - END");

                LogInfo(runId, "Soft Reset Players - BEGIN");
                await controller.ResetPlayersAsync($"Baseline/SoftResetPlayers/{runId}");
                LogInfo(runId, "Soft Reset Players - END");

                LogInfo(runId, "END Full Baseline");
            }
            catch (Exception ex)
            {
                LogError(runId, $"Exception during Full Baseline: {ex}");
            }
            finally
            {
                _isRunning = false;
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
                $"{LogPrefix} não encontrou WorldLifecycleController na cena. Abortando baseline.");
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
    }
}
