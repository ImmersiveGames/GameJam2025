using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges
{
    /// <summary>
    /// Bridge de eventos definitivos do GameLoop (EventBus -> IGameLoopService).
    ///
    /// Regras (alinhadas ao GameLoop.md):
    /// - NÃO consome eventos de intenção de start (GameStartCommandEvent). Start é coordenado via SceneFlow.
    /// - Consome apenas eventos definitivos: pause/resume/exit-to-menu.
    /// - MacroRestart canônico é coordenado por MacroRestartCoordinator (sem listener de reset aqui).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopCommandEventBridge : IDisposable
    {
        private readonly EventBinding<GamePauseCommandEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;
        private readonly EventBinding<GameExitToMenuRequestedEvent> _onExitToMenu;

        private bool _disposed;

        public GameLoopCommandEventBridge()
        {
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);

            EventBus<GamePauseCommandEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);
            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);

            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (pause/resume/exit).",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                "[OBS][LEGACY] GameResetRequestedEvent listener disabled in GameLoopCommandEventBridge; MacroRestartCoordinator owns canonical restart.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            EventBus<GamePauseCommandEvent>.Unregister(_onPause);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResume);
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_onExitToMenu);
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            return DependencyManager.Provider.TryGetGlobal(out loop) && loop != null;
        }

        private void OnGamePause(GamePauseCommandEvent evt)
        {
            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            if (evt != null && evt.IsPaused)
            {
                loop.RequestPause();
            }
            else
            {
                loop.RequestResume();
            }
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            loop.RequestResume();
        }

        private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                $"[GameLoop] ExitToMenu recebido -> RequestReady (não voltar para Playing). reason='{evt?.Reason ?? "<null>"}'.");
            loop.RequestReady();
        }
    }
}
