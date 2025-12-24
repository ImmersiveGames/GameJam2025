using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop
{
    /// <summary>
    /// Bridge de eventos do GameLoop (EventBus -> IGameLoopService).
    ///
    /// Regras:
    /// - Só consome COMMAND (GameStartEvent). REQUEST é responsabilidade do coordinator.
    /// - O coordinator decide quando emitir COMMAND; o bridge apenas executa.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopEventInputBridge : IDisposable
    {
        private readonly EventBinding<GameStartEvent> _onStartCommand;
        private readonly EventBinding<GamePauseEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;
        private readonly EventBinding<GameResetRequestedEvent> _onResetRequested;

        private bool _disposed;

        public GameLoopEventInputBridge()
        {
            _onStartCommand = new EventBinding<GameStartEvent>(OnGameStartCommand);
            _onPause = new EventBinding<GamePauseEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
            _onResetRequested = new EventBinding<GameResetRequestedEvent>(OnGameResetRequested);

            EventBus<GameStartEvent>.Register(_onStartCommand);
            EventBus<GamePauseEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);
            EventBus<GameResetRequestedEvent>.Register(_onResetRequested);

            DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (consome apenas COMMAND).");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            EventBus<GameStartEvent>.Unregister(_onStartCommand);
            EventBus<GamePauseEvent>.Unregister(_onPause);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResume);
            EventBus<GameResetRequestedEvent>.Unregister(_onResetRequested);
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            return DependencyManager.Provider.TryGetGlobal(out loop) && loop != null;
        }

        private void OnGameStartCommand(GameStartEvent evt)
        {
            if (!TryResolveLoop(out var loop))
            {
                DebugUtility.LogError<GameLoopEventInputBridge>(
                    "[GameLoop] IGameLoopService não encontrado no DI global ao processar GameStartEvent (COMMAND).");
                return;
            }

            DebugUtility.Log<GameLoopEventInputBridge>(
                "[GameLoop] GameStartEvent (COMMAND) recebido. Liberando GameLoop.RequestStart().",
                DebugUtility.Colors.Info);

            // Initialize é idempotente; mantido como proteção contra regressões.
            loop.Initialize();
            loop.RequestStart();
        }

        private void OnGamePause(GamePauseEvent evt)
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
