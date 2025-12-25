using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop
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
        private readonly EventBinding<GameResetRequestedEvent> _onResetRequested;

        private bool _disposed;

        public GameLoopEventInputBridge()
        {
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
            _onResetRequested = new EventBinding<GameResetRequestedEvent>(OnGameResetRequested);

            EventBus<GamePauseCommandEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);
            EventBus<GameResetRequestedEvent>.Register(_onResetRequested);

            DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (pause/resume/reset).");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            EventBus<GamePauseCommandEvent>.Unregister(_onPause);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResume);
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
                return;

            if (evt != null && evt.IsPaused)
                loop.RequestPause();
            else
                loop.RequestResume();
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
                return;

            loop.RequestResume();
        }

        private void OnGameResetRequested(GameResetRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
                return;

            loop.RequestReset();
        }
    }
}
