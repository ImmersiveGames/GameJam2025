using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MacroRestartCoordinator : IDisposable
    {
        private const string DefaultReason = "Restart/Unspecified";
        private const double DebounceWindowSeconds = 0.35d;

        private readonly EventBinding<GameResetRequestedEvent> _resetBinding;
        private readonly SemaphoreSlim _executionGate = new SemaphoreSlim(1, 1);
        private readonly object _stateSync = new object();

        private bool _inFlight;
        private bool _pending;
        private string _pendingReason = string.Empty;
        private double _lastAcceptedRealtime;
        private int _runId;
        private bool _disposed;

        public MacroRestartCoordinator()
        {
            _resetBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

            DebugUtility.LogVerbose<MacroRestartCoordinator>(
                "[Navigation] MacroRestartCoordinator registered (GameResetRequestedEvent -> canonical macro restart).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameResetRequestedEvent>.Unregister(_resetBinding);
            _executionGate.Dispose();
        }

        private void OnResetRequested(GameResetRequestedEvent evt)
        {
            string reason = NormalizeReason(evt?.Reason);
            DebugUtility.Log<MacroRestartCoordinator>(
                $"[OBS][Navigation] MacroRestartRequested reason='{reason}'.",
                DebugUtility.Colors.Info);

            lock (_stateSync)
            {
                if (_inFlight)
                {
                    _pending = true;
                    _pendingReason = reason;

                    DebugUtility.Log<MacroRestartCoordinator>(
                        $"[OBS][Navigation] MacroRestartQueued reason='{reason}' queuedBecause='in_flight'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                double now = Time.realtimeSinceStartupAsDouble;
                if (_lastAcceptedRealtime > 0d)
                {
                    double elapsed = now - _lastAcceptedRealtime;
                    if (elapsed >= 0d && elapsed < DebounceWindowSeconds)
                    {
                        DebugUtility.Log<MacroRestartCoordinator>(
                            $"[OBS][Navigation] MacroRestartDebounced reason='{reason}' windowMs='{Math.Round(elapsed * 1000d):0}'.",
                            DebugUtility.Colors.Info);
                        return;
                    }
                }

                _inFlight = true;
                _lastAcceptedRealtime = now;
            }

            NavigationTaskRunner.FireAndForget(
                RunCoalescedRestartLoopAsync(reason),
                typeof(MacroRestartCoordinator),
                "MacroRestartCoordinator/RunCoalescedRestartLoop");
        }

        private async Task RunCoalescedRestartLoopAsync(string firstReason)
        {
            await _executionGate.WaitAsync();
            try
            {
                string nextReason = firstReason;

                while (true)
                {
                    int runId;
                    lock (_stateSync)
                    {
                        runId = ++_runId;
                    }

                    string effectiveReason = $"{NormalizeReason(nextReason)}#r{runId}";
                    DebugUtility.Log<MacroRestartCoordinator>(
                        $"[OBS][Navigation] MacroRestartStart runId='{runId}' effectiveReason='{effectiveReason}'.",
                        DebugUtility.Colors.Info);

                    try
                    {
                        ExecuteRestartPipelineOrFail(effectiveReason, out var gameLoopService, out var levelFlowRuntimeService, out var restartContextService);

                        // Comentario: ordem canônica e estável para evitar overlap de estados.
                        restartContextService.Clear(effectiveReason);
                        gameLoopService.RequestReset();
                        await levelFlowRuntimeService.StartGameplayDefaultAsync(effectiveReason, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        DebugUtility.LogError<MacroRestartCoordinator>(
                            $"[ERROR][Navigation] MacroRestart failed runId='{runId}' effectiveReason='{effectiveReason}' ex='{ex}'.");
                        HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                            $"[FATAL][H1][Navigation] MacroRestart pipeline failed. runId='{runId}' effectiveReason='{effectiveReason}' ex='{ex.GetType().Name}: {ex.Message}'.");
                    }

                    bool shouldRunPending;
                    string queuedReason;
                    lock (_stateSync)
                    {
                        shouldRunPending = _pending;
                        queuedReason = _pendingReason;
                        _pending = false;
                        _pendingReason = string.Empty;
                        _lastAcceptedRealtime = Time.realtimeSinceStartupAsDouble;
                    }

                    DebugUtility.Log<MacroRestartCoordinator>(
                        $"[OBS][Navigation] MacroRestartCompleted runId='{runId}' effectiveReason='{effectiveReason}' pending='{shouldRunPending.ToString().ToLowerInvariant()}'.",
                        DebugUtility.Colors.Info);

                    if (!shouldRunPending)
                    {
                        break;
                    }

                    nextReason = NormalizeReason(queuedReason);
                }
            }
            finally
            {
                lock (_stateSync)
                {
                    _inFlight = false;
                    _pending = false;
                    _pendingReason = string.Empty;
                }

                _executionGate.Release();
            }
        }

        private static void ExecuteRestartPipelineOrFail(
            string reason,
            out IGameLoopService gameLoopService,
            out ILevelFlowRuntimeService levelFlowRuntimeService,
            out IRestartContextService restartContextService)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][Navigation] MacroRestart missing DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out gameLoopService) || gameLoopService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][Navigation] MacroRestart missing IGameLoopService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out levelFlowRuntimeService) || levelFlowRuntimeService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][Navigation] MacroRestart missing ILevelFlowRuntimeService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out restartContextService) || restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][Navigation] MacroRestart missing IRestartContextService. reason='{reason}'.");
            }
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
        }
    }
}
