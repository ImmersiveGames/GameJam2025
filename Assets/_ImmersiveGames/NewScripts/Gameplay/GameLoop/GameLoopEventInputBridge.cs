using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Bridge de entrada que escuta eventos globais e sinaliza o GameLoopService.
    /// Importante (Opção B):
    /// - NÃO consome GameStartEvent. O start é coordenado por GameLoopSceneFlowCoordinator
    ///   (GameStartEvent -> SceneFlow -> ScenesReady -> GameLoop.RequestStart()).
    /// </summary>
    public sealed class GameLoopEventInputBridge : IDisposable
    {
        private readonly IGameLoopService _gameLoopService;

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

            EventBus<GamePauseEvent>.Unregister(_pauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_resumeBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_resetBinding);
            _bindingsRegistered = false;
        }

        private void TryRegisterBindings()
        {
            try
            {
                _pauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
                _resumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => _gameLoopService.RequestResume());
                _resetBinding = new EventBinding<GameResetRequestedEvent>(_ => _gameLoopService.RequestReset());

                EventBus<GamePauseEvent>.Register(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
                EventBus<GameResetRequestedEvent>.Register(_resetBinding);

                _bindingsRegistered = true;

                DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                    "[GameLoop] Bridge de entrada registrado (Pause/Resume/Reset). Start é coordenado por GameLoopSceneFlowCoordinator.");
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
