using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.Navigation;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Runtime
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
                "[Navigation] ExitToMenuCoordinator registered (GameExitToMenuRequestedEvent -> ExitToMenuMacroAsync).",
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
                $"[OBS][Navigation] ExitToMenuRequested reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (!TryResolveNavigationService(out var navigation))
            {
                return;
            }

            NavigationTaskRunner.FireAndForget(
                navigation.ExitToMenuMacroAsync(reason),
                typeof(ExitToMenuCoordinator),
                "ExitToMenuCoordinator/ExitToMenuMacroAsync");
        }

        private static bool TryResolveNavigationService(out IGameNavigationService navigation)
        {
            navigation = null;

            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(ExitToMenuCoordinator),
                    "[FATAL][H1][Navigation] ExitToMenu missing DependencyManager.Provider.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out navigation) || navigation == null)
            {
                HardFailFastH1.Trigger(typeof(ExitToMenuCoordinator),
                    "[FATAL][H1][Navigation] ExitToMenu missing IGameNavigationService.");
            }

            return true;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "ExitToMenu/Unspecified" : reason.Trim();
        }
    }
}
