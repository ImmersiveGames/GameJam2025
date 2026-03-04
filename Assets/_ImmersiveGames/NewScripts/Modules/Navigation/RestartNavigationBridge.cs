using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Bridge global: ao receber GameResetRequestedEvent, aciona restart canônico de navegação.
    /// </summary>
    public sealed class RestartNavigationBridge : IDisposable
    {
        private const string RestartReason = "PostGame/Restart";

        private readonly IGameNavigationService _navigationService;
        private readonly EventBinding<GameResetRequestedEvent> _resetBinding;
        private bool _disposed;
        private bool _restartInProgress;

        public RestartNavigationBridge(IGameNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _resetBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

            DebugUtility.LogVerbose<RestartNavigationBridge>(
                "[Navigation] RestartNavigationBridge registrado (GameResetRequestedEvent -> RestartAsync).",
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
            string reason = evt?.Reason ?? RestartReason;

            if (_restartInProgress)
            {
                DebugUtility.LogWarning<RestartNavigationBridge>(
                    $"[WARN][OBS][Navigation] RestartRequested ignored reason='restart_in_progress' requestReason='{reason}'.");
                return;
            }

            _restartInProgress = true;

            DebugUtility.Log<RestartNavigationBridge>(
                $"[OBS][Navigation] RestartRequested -> IGameNavigationService.RestartAsync(reason='{reason}').",
                DebugUtility.Colors.Info);

            NavigationTaskRunner.FireAndForget(
                RestartAsync(reason),
                typeof(RestartNavigationBridge),
                "Restart -> IGameNavigationService.RestartAsync");
        }

        private async System.Threading.Tasks.Task RestartAsync(string reason)
        {
            try
            {
                await _navigationService.RestartAsync(reason);
            }
            finally
            {
                _restartInProgress = false;
            }
        }
    }
}
