using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.Navigation;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Runtime
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
                "[Navigation] MacroRestartCoordinator registered (GameResetRequestedEvent -> RestartMacroAsync).",
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
                $"[OBS][Navigation] MacroRestartRequested reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (!TryResolveNavigationService(out var navigation))
            {
                return;
            }

            NavigationTaskRunner.FireAndForget(
                navigation.RestartMacroAsync(reason),
                typeof(MacroRestartCoordinator),
                "MacroRestartCoordinator/RestartMacroAsync");
        }

        private static bool TryResolveNavigationService(out IGameNavigationService navigation)
        {
            navigation = null;

            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    "[FATAL][H1][Navigation] MacroRestart missing DependencyManager.Provider.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out navigation) || navigation == null)
            {
                HardFailFastH1.Trigger(typeof(MacroRestartCoordinator),
                    "[FATAL][H1][Navigation] MacroRestart missing IGameNavigationService.");
            }

            return true;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Restart/Unspecified" : reason.Trim();
        }
    }
}
