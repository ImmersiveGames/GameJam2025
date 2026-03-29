using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate.Interop;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.PostGame;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Interop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ExitToMenuCoordinator : IDisposable
    {
        private readonly EventBinding<GameExitToMenuRequestedEvent> _exitBinding;
        private bool _disposed;

        public ExitToMenuCoordinator()
        {
            _exitBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_exitBinding);

            DebugUtility.LogVerbose<ExitToMenuCoordinator>(
                "[GameLoop] ExitToMenuCoordinator registered as bridge temporária (GameExitToMenuRequestedEvent -> Navigation).",
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
        }

        private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
        {
            string reason = NormalizeReason(evt?.Reason);
            DebugUtility.Log<ExitToMenuCoordinator>(
                $"[OBS][ExitToMenu] ExitToMenuRequested reason='{reason}' bridge='ExitToMenuCoordinator'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<ExitToMenuCoordinator>(
                $"[OBS][Navigation] ExitToMenuPrimaryDispatch reason='{reason}' dispatch='GoToMenuAsync'.",
                DebugUtility.Colors.Info);

            ResolveDependenciesOrFail(reason, out var loop, out var navigation);
            ReleasePauseGateIfPresent(reason);
            MarkExitResultIfInPostGame(loop, reason);

            loop.RequestReady();
            _ = ObserveAsync(navigation.GoToMenuAsync(reason), reason);
        }

        private static async Task ObserveAsync(Task task, string reason)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(ExitToMenuCoordinator),
                    $"[GameLoop] ExitToMenu failed reason='{reason}'. ex={ex}");
            }
        }

        private static void ResolveDependenciesOrFail(string reason, out IGameLoopService loop, out IGameNavigationService navigation)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(ExitToMenuCoordinator),
                    $"[FATAL][H1][GameLoop] ExitToMenu missing DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out loop) || loop == null)
            {
                HardFailFastH1.Trigger(typeof(ExitToMenuCoordinator),
                    $"[FATAL][H1][GameLoop] ExitToMenu missing IGameLoopService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out navigation) || navigation == null)
            {
                HardFailFastH1.Trigger(typeof(ExitToMenuCoordinator),
                    $"[FATAL][H1][GameLoop] ExitToMenu missing IGameNavigationService. reason='{reason}'.");
            }
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

        private static void MarkExitResultIfInPostGame(IGameLoopService loop, string reason)
        {
            if (loop == null || !string.Equals(loop.CurrentStateIdName, nameof(GameLoopStateId.PostPlay), StringComparison.Ordinal))
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var resultService) && resultService != null)
            {
                resultService.TrySetExit(reason);
            }
        }

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "ExitToMenu/Unspecified" : reason.Trim();
    }
}
