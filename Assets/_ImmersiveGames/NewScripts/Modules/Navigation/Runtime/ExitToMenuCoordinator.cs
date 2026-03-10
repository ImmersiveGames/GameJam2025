using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gates.Interop;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ExitToMenuCoordinator : IDisposable
    {
        private const string DefaultReason = "ExitToMenu/Unspecified";

        private readonly EventBinding<GameExitToMenuRequestedEvent> _exitBinding;
        private readonly SemaphoreSlim _executionGate = new SemaphoreSlim(1, 1);
        private readonly object _stateSync = new object();

        private bool _inFlight;
        private bool _pending;
        private string _pendingReason = string.Empty;
        private int _runId;
        private bool _disposed;
        private int _lastExitFrame = -1;
        private string _lastExitKey = string.Empty;
        private bool _loggedMissingProvider;
        private bool _loggedMissingGameLoop;
        private bool _loggedMissingNavigation;

        public ExitToMenuCoordinator()
        {
            _exitBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);

            DebugUtility.LogVerbose<ExitToMenuCoordinator>(
                "[Navigation] ExitToMenuCoordinator registered (GameExitToMenuRequestedEvent -> RequestReady + ExitToMenuAsync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_exitBinding);
            _executionGate.Dispose();
        }

        private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
        {
            string reason = NormalizeReason(evt?.Reason);
            string key = BuildExitKey(reason);
            int frame = Time.frameCount;

            DebugUtility.Log<ExitToMenuCoordinator>(
                $"[OBS][Navigation] ExitToMenuRequested reason='{reason}'.",
                DebugUtility.Colors.Info);

            lock (_stateSync)
            {
                if (_lastExitFrame == frame && string.Equals(_lastExitKey, key, StringComparison.Ordinal))
                {
                    DebugUtility.Log<ExitToMenuCoordinator>(
                        $"[OBS][Navigation] ExitToMenuDeduped reason='{reason}' reason2='dedupe_same_frame' frame='{frame}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _lastExitFrame = frame;
                _lastExitKey = key;

                if (_inFlight)
                {
                    _pending = true;
                    _pendingReason = reason;

                    DebugUtility.Log<ExitToMenuCoordinator>(
                        $"[OBS][Navigation] ExitToMenuQueued reason='{reason}' queuedBecause='in_flight'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                _inFlight = true;
            }

            NavigationTaskRunner.FireAndForget(
                RunCoalescedExitLoopAsync(reason),
                typeof(ExitToMenuCoordinator),
                "ExitToMenuCoordinator/RunCoalescedExitLoop");
        }

        private async Task RunCoalescedExitLoopAsync(string firstReason)
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

                    string reason = NormalizeReason(nextReason);
                    DebugUtility.Log<ExitToMenuCoordinator>(
                        $"[OBS][Navigation] ExitToMenuStart runId='{runId}' reason='{reason}'.",
                        DebugUtility.Colors.Info);

                    bool completed = await TryRunExitAsync(reason);

                    bool shouldRunPending;
                    string queuedReason;
                    lock (_stateSync)
                    {
                        shouldRunPending = completed && _pending;
                        queuedReason = _pendingReason;
                        _pending = false;
                        _pendingReason = string.Empty;
                    }

                    DebugUtility.Log<ExitToMenuCoordinator>(
                        $"[OBS][Navigation] ExitToMenuCompleted runId='{runId}' pending='{shouldRunPending.ToString().ToLowerInvariant()}'.",
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

        private async Task<bool> TryRunExitAsync(string reason)
        {
            if (!TryResolveServices(out var loop, out var navigation))
            {
                return false;
            }

            ReleasePauseGateIfPresent(reason);

            // Coment?rio: ordem can?nica e est?vel. Primeiro retira o loop de Playing/Paused, depois navega.
            loop.RequestReady();
            await navigation.ExitToMenuAsync(reason);
            return true;
        }

        private bool TryResolveServices(out IGameLoopService loop, out IGameNavigationService navigation)
        {
            loop = null;
            navigation = null;

            if (DependencyManager.Provider == null)
            {
                LogMissingProviderOnce();
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out loop) || loop == null)
            {
                LogMissingGameLoopOnce();
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out navigation) || navigation == null)
            {
                LogMissingNavigationOnce();
                return false;
            }

            return true;
        }

        private static void ReleasePauseGateIfPresent(string reason)
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var pauseBridge) &&
                pauseBridge != null)
            {
                pauseBridge.ReleaseForExitToMenu(reason);
            }
        }

        private void LogMissingProviderOnce()
        {
            if (_loggedMissingProvider)
            {
                return;
            }

            _loggedMissingProvider = true;
            DebugUtility.LogWarning<ExitToMenuCoordinator>(
                "[WARN][Navigation] ExitToMenu request ignored; DependencyManager.Provider missing.");
        }

        private void LogMissingGameLoopOnce()
        {
            if (_loggedMissingGameLoop)
            {
                return;
            }

            _loggedMissingGameLoop = true;
            DebugUtility.LogWarning<ExitToMenuCoordinator>(
                "[WARN][Navigation] ExitToMenu request ignored; IGameLoopService missing.");
        }

        private void LogMissingNavigationOnce()
        {
            if (_loggedMissingNavigation)
            {
                return;
            }

            _loggedMissingNavigation = true;
            DebugUtility.LogWarning<ExitToMenuCoordinator>(
                "[WARN][Navigation] ExitToMenu request ignored; IGameNavigationService missing.");
        }

        private static string BuildExitKey(string reason)
            => $"exit|reason={reason}";

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
    }
}
