using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop;
namespace _ImmersiveGames.NewScripts.Runtime.GameLoop.Bridges
{
    /// <summary>
    /// Bridge de eventos definitivos do GameLoop (EventBus -> IGameLoopService).
    ///
    /// Regras (alinhadas ao GameLoop.md):
    /// - NÃO consome eventos de intenção de start (GameStartCommandEvent). Start é coordenado via SceneFlow.
    /// - Consome apenas eventos definitivos: pause/resume/reset.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopEventInputBridge : IDisposable
    {
        private readonly EventBinding<GamePauseCommandEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;
        private readonly EventBinding<GameExitToMenuRequestedEvent> _onExitToMenu;
        private readonly EventBinding<GameResetRequestedEvent> _onResetRequested;

        private bool _disposed;

        public GameLoopEventInputBridge()
        {
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
            _onResetRequested = new EventBinding<GameResetRequestedEvent>(OnGameResetRequested);

            EventBus<GamePauseCommandEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);
            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
            EventBus<GameResetRequestedEvent>.Register(_onResetRequested);

            DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (pause/resume/reset).");
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
            EventBus<GameResetRequestedEvent>.Unregister(_onResetRequested);
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

            DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                $"[GameLoop] ExitToMenu recebido -> RequestReady (não voltar para Playing). reason='{evt?.Reason ?? "<null>"}'.");
            loop.RequestReady();
        }

        private void OnGameResetRequested(GameResetRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            DebugUtility.Log<GameLoopEventInputBridge>(
                $"[GameLoop] RestartRequested -> RequestReset (expect Boot cycle). reason='{evt?.Reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            loop.RequestReset();
        }
    }
}
