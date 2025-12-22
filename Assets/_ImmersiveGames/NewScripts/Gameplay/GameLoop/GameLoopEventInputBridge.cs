using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Bridge de entrada que escuta eventos globais e sinaliza o GameLoopService.
    /// Não publica eventos de volta para evitar loops.
    /// </summary>
    public sealed class GameLoopEventInputBridge : IDisposable
    {
        private readonly IGameLoopService _gameLoopService;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeBinding;
        private EventBinding<GameResetRequestedEvent> _resetBinding;
        private bool _bindingsRegistered;
        private bool _disposed;

        public GameLoopEventInputBridge()
        {
            if (!DependencyManager.Provider.TryGetGlobal(out _gameLoopService) || _gameLoopService == null)
            {
                DebugUtility.LogError<GameLoopEventInputBridge>(
                    "[GameLoop] IGameLoopService não encontrado; GameLoopEventInputBridge não pode sinalizar o loop.");
                return;
            }

            TryRegisterBindings();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (!_bindingsRegistered)
            {
                return;
            }

            EventBus<GameStartEvent>.Unregister(_startBinding);
            EventBus<GamePauseEvent>.Unregister(_pauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_resumeBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_resetBinding);
            _bindingsRegistered = false;
        }

        private void TryRegisterBindings()
        {
            try
            {
                _startBinding = new EventBinding<GameStartEvent>(_ => _gameLoopService.RequestStart());
                _pauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
                _resumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => _gameLoopService.RequestResume());
                _resetBinding = new EventBinding<GameResetRequestedEvent>(_ => _gameLoopService.RequestReset());

                EventBus<GameStartEvent>.Register(_startBinding);
                EventBus<GamePauseEvent>.Register(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
                EventBus<GameResetRequestedEvent>.Register(_resetBinding);

                _bindingsRegistered = true;
                DebugUtility.LogVerbose<GameLoopEventInputBridge>("[GameLoop] Bridge de entrada registrado no EventBus.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GameLoopEventInputBridge>(
                    $"[GameLoop] Falha ao registrar bridge no EventBus (seguirá sem eventos): {ex}");
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            if (evt is { IsPaused: true })
            {
                _gameLoopService.RequestPause();
                return;
            }

            _gameLoopService.RequestResume();
        }
    }
}
