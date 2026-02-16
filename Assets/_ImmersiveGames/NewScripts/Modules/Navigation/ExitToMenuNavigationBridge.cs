using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Bridge global: ao receber GameExitToMenuRequestedEvent, aciona o IGameNavigationService.
    /// Caminho único para retornar ao menu (via overlays: pause/victory/defeat).
    /// </summary>
    public sealed class ExitToMenuNavigationBridge : IDisposable
    {
        // Comentário: reason padrão alinhado com o fluxo de pós-game.
        private const string ExitToMenuReason = "PostGame/ExitToMenu";

        private readonly EventBinding<GameExitToMenuRequestedEvent> _exitToMenuBinding;
        private bool _disposed;

        public ExitToMenuNavigationBridge()
        {
            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);

            DebugUtility.LogVerbose<ExitToMenuNavigationBridge>(
                "[Navigation] ExitToMenuNavigationBridge registrado (GameExitToMenuRequestedEvent -> NavigateAsync(core=Menu)).",
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

            string reason = evt?.Reason ?? ExitToMenuReason;

            DebugUtility.Log<ExitToMenuNavigationBridge>(
                $"[Navigation] ExitToMenu recebido -> NavigateAsync(core=Menu). reason='{reason}'.",
                DebugUtility.Colors.Info);

            NavigationTaskRunner.FireAndForget(
                navigation.NavigateAsync(GameNavigationIntentKind.Menu, reason),
                typeof(ExitToMenuNavigationBridge),
                $"ExitToMenu -> coreIntent=Menu");
        }
    }
}
