using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.State.RuntimeSignals
{
    internal sealed class GameplayRuntimeSignalsAdapter : IDisposable
    {
        private readonly Action _onGameStartRequested;
        private readonly Action _onGameRunStarted;
        private readonly Action _onGameRunEnded;
        private readonly Action<PauseStateChangedEvent> _onPauseStateChanged;
        private readonly Action<GameResetRequestedEvent> _onGameResetRequested;
        private readonly Action<ReadinessChangedEvent> _onReadinessChanged;

        private EventBinding<GameStartRequestedEvent> _gameStartRequestedBinding;
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private EventBinding<GameRunEndedEvent> _gameRunEndedBinding;
        private EventBinding<PauseStateChangedEvent> _pauseStateBinding;
        private EventBinding<GameResetRequestedEvent> _gameResetBinding;
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        public GameplayRuntimeSignalsAdapter(
            Action onGameStartRequested,
            Action onGameRunStarted,
            Action onGameRunEnded,
            Action<PauseStateChangedEvent> onPauseStateChanged,
            Action<GameResetRequestedEvent> onGameResetRequested,
            Action<ReadinessChangedEvent> onReadinessChanged)
        {
            _onGameStartRequested = onGameStartRequested;
            _onGameRunStarted = onGameRunStarted;
            _onGameRunEnded = onGameRunEnded;
            _onPauseStateChanged = onPauseStateChanged;
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
                _pauseStateBinding = new EventBinding<PauseStateChangedEvent>(_onPauseStateChanged);
                _gameResetBinding = new EventBinding<GameResetRequestedEvent>(_onGameResetRequested);
                _readinessBinding = new EventBinding<ReadinessChangedEvent>(_onReadinessChanged);

                EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
                EventBus<GameRunStartedEvent>.Register(_gameRunStartedBinding);
                EventBus<GameRunEndedEvent>.Register(_gameRunEndedBinding);
                EventBus<PauseStateChangedEvent>.Register(_pauseStateBinding);
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
            EventBus<PauseStateChangedEvent>.Unregister(_pauseStateBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_gameResetBinding);
            EventBus<ReadinessChangedEvent>.Unregister(_readinessBinding);

            _bindingsRegistered = false;
        }
    }
}
