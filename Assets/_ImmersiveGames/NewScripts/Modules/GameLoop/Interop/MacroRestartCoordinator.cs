using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Interop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MacroRestartCoordinator : IDisposable
    {
        private readonly EventBinding<GameResetRequestedEvent> _resetBinding;
        private bool _disposed;

        public MacroRestartCoordinator()
        {
            _resetBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

            DebugUtility.LogVerbose<MacroRestartCoordinator>(
                "[GameLoop] MacroRestartCoordinator registered (GameResetRequestedEvent -> restart macro).",
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
        }

        private void OnResetRequested(GameResetRequestedEvent evt)
        {
            string reason = NormalizeReason(evt?.Reason);
            DebugUtility.Log<MacroRestartCoordinator>(
                $"[OBS][GameLoop] MacroRestartRequested reason='{reason}'.",
                DebugUtility.Colors.Info);

            ResolveDependenciesOrFail(reason, out var gameLoopService, out var levelFlowRuntimeService, out var restartContextService);

            _ = ObserveAsync(RestartAsync(reason, gameLoopService, levelFlowRuntimeService, restartContextService), reason);
        }

        private static async Task RestartAsync(
            string reason,
            IGameLoopService gameLoopService,
            ILevelFlowRuntimeService levelFlowRuntimeService,
            IRestartContextService restartContextService)
        {
            restartContextService.Clear(reason);
            gameLoopService.RequestReset();
            await levelFlowRuntimeService.StartGameplayDefaultAsync(reason, CancellationToken.None);
        }

        private static async Task ObserveAsync(Task task, string reason)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(MacroRestartCoordinator),
                    $"[GameLoop] MacroRestart failed reason='{reason}'. ex={ex}");
            }
        }

        private static void ResolveDependenciesOrFail(
            string reason,
            out IGameLoopService gameLoopService,
            out ILevelFlowRuntimeService levelFlowRuntimeService,
            out IRestartContextService restartContextService)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][GameLoop] MacroRestart missing DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out gameLoopService) || gameLoopService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][GameLoop] MacroRestart missing IGameLoopService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out levelFlowRuntimeService) || levelFlowRuntimeService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][GameLoop] MacroRestart missing ILevelFlowRuntimeService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out restartContextService) || restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    $"[FATAL][H1][GameLoop] MacroRestart missing IRestartContextService. reason='{reason}'.");
            }
        }

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "Restart/Unspecified" : reason.Trim();
    }
}
