using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.State
{
    internal sealed class GameplayStateGateBindings : IDisposable
    {
        private readonly Action _onGameStartRequested;
        private readonly Action _onGameRunStarted;
        private readonly Action _onGameRunEnded;
        private readonly Action<GamePauseCommandEvent> _onGamePause;
        private readonly Action<GameResumeRequestedEvent> _onGameResumeRequested;
        private readonly Action<GameResetRequestedEvent> _onGameResetRequested;
        private readonly Action<ReadinessChangedEvent> _onReadinessChanged;

        private EventBinding<GameStartRequestedEvent> _gameStartRequestedBinding;
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private EventBinding<GameRunEndedEvent> _gameRunEndedBinding;
        private EventBinding<GamePauseCommandEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private EventBinding<GameResetRequestedEvent> _gameResetBinding;
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        public GameplayStateGateBindings(
            Action onGameStartRequested,
            Action onGameRunStarted,
            Action onGameRunEnded,
            Action<GamePauseCommandEvent> onGamePause,
            Action<GameResumeRequestedEvent> onGameResumeRequested,
            Action<GameResetRequestedEvent> onGameResetRequested,
            Action<ReadinessChangedEvent> onReadinessChanged)
        {
            _onGameStartRequested = onGameStartRequested;
            _onGameRunStarted = onGameRunStarted;
            _onGameRunEnded = onGameRunEnded;
            _onGamePause = onGamePause;
            _onGameResumeRequested = onGameResumeRequested;
            _onGameResetRequested = onGameResetRequested;
            _onReadinessChanged = onReadinessChanged;
        }

        public bool TryRegister()
        {
            try
            {
                _gameStartRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ => _onGameStartRequested());
                _gameRunStartedBinding = new EventBinding<GameRunStartedEvent>(_ => _onGameRunStarted());
                _gameRunEndedBinding = new EventBinding<GameRunEndedEvent>(_ => _onGameRunEnded());
                _gamePauseBinding = new EventBinding<GamePauseCommandEvent>(_onGamePause);
                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_onGameResumeRequested);
                _gameResetBinding = new EventBinding<GameResetRequestedEvent>(_onGameResetRequested);
                _readinessBinding = new EventBinding<ReadinessChangedEvent>(_onReadinessChanged);

                EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
                EventBus<GameRunStartedEvent>.Register(_gameRunStartedBinding);
                EventBus<GameRunEndedEvent>.Register(_gameRunEndedBinding);
                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
                EventBus<ReadinessChangedEvent>.Register(_readinessBinding);

                _bindingsRegistered = true;
                return true;
            }
            catch
            {
                _bindingsRegistered = false;
                return false;
            }
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
            {
                return;
            }

            EventBus<GameStartRequestedEvent>.Unregister(_gameStartRequestedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_gameRunStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_gameRunEndedBinding);
            EventBus<GamePauseCommandEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_gameResetBinding);
            EventBus<ReadinessChangedEvent>.Unregister(_readinessBinding);

            _bindingsRegistered = false;
        }
    }
}
