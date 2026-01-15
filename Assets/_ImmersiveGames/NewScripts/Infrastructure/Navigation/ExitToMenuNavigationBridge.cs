using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Navigation
{
    /// <summary>
    /// Bridge global: ao receber GameExitToMenuRequestedEvent, aciona o IGameNavigationService.
    /// Caminho único para retornar ao menu (via overlays: pause/victory/defeat).
    /// </summary>
    public sealed class ExitToMenuNavigationBridge : IDisposable
    {
        private readonly EventBinding<GameExitToMenuRequestedEvent> _exitToMenuBinding;
        private bool _disposed;

        public ExitToMenuNavigationBridge()
        {
            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);

            DebugUtility.LogVerbose<ExitToMenuNavigationBridge>(
                "[Navigation] ExitToMenuNavigationBridge registrado (GameExitToMenuRequestedEvent -> RequestMenuAsync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_exitToMenuBinding);
        }

        private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigation) || navigation == null)
            {
                DebugUtility.LogWarning<ExitToMenuNavigationBridge>(
                    "[Navigation] IGameNavigationService indisponível; ExitToMenu ignorado.");
                return;
            }

            var reason = evt?.Reason ?? "ExitToMenu/Unspecified";

            DebugUtility.Log<ExitToMenuNavigationBridge>(
                $"[Navigation] ExitToMenu recebido -> RequestMenuAsync. routeId='{GameNavigationCatalog.Routes.ToMenu}', reason='{reason}'.",
                DebugUtility.Colors.Info);

            _ = navigation.RequestMenuAsync(reason);
        }
    }
}
