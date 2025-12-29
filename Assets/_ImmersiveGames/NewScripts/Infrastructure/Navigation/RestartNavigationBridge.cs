using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Navigation
{
    /// <summary>
    /// Bridge global: ao receber GameResetRequestedEvent, aciona o IGameNavigationService.
    /// Mantém o reset oficial via SceneFlow + WorldLifecycle (profile gameplay).
    /// </summary>
    public sealed class RestartNavigationBridge : IDisposable
    {
        private const string RestartReason = "PostGame/Restart";

        private readonly EventBinding<GameResetRequestedEvent> _resetBinding;
        private bool _disposed;

        public RestartNavigationBridge()
        {
            _resetBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

            DebugUtility.LogVerbose<RestartNavigationBridge>(
                "[Navigation] RestartNavigationBridge registrado (GameResetRequestedEvent -> RequestToGameplay).",
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
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigation) || navigation == null)
            {
                DebugUtility.LogWarning<RestartNavigationBridge>(
                    "[Navigation] IGameNavigationService indisponível; Restart ignorado.");
                return;
            }

            DebugUtility.Log<RestartNavigationBridge>(
                "[Navigation] GameResetRequestedEvent recebido -> RequestToGameplay.",
                DebugUtility.Colors.Info);

            _ = navigation.RequestToGameplay(RestartReason);
        }
    }
}
