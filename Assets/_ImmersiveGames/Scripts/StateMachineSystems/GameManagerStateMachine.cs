using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityUtils;

namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class GameManagerStateMachine : Singleton<GameManagerStateMachine>
    {
        private StateMachine _stateMachine;
        private GameManager _gameManager;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;
        private EventTriggeredPredicate<GameOverEvent> _gameOverPredicate;
        private EventTriggeredPredicate<GameVictoryEvent> _victoryPredicate;
        private EventTriggeredPredicate<GameStartRequestedEvent> _startRequestedPredicate;
        private EventTriggeredPredicate<GamePauseRequestedEvent> _pauseRequestedPredicate;
        private EventTriggeredPredicate<GameResumeRequestedEvent> _resumeRequestedPredicate;
        private EventTriggeredPredicate<GameResetRequestedEvent> _resetRequestedPredicate;

        public IState CurrentState => _stateMachine?.CurrentState;

        private void Update() => _stateMachine?.Update();
        private void FixedUpdate() => _stateMachine?.FixedUpdate();

        private void OnDestroy()
        {
            TearDownStateMachine();
        }

        public void InitializeStateMachine(GameManager gameManager)
        {
            Preconditions.CheckNotNull(gameManager, "GameManager não pode ser nulo.");
            _gameManager = gameManager;

            BuildStateMachine();
        }

        public void Rebuild(GameManager gameManager)
        {
            Preconditions.CheckNotNull(gameManager, "GameManager não pode ser nulo.");
            _gameManager = gameManager;

            BuildStateMachine();
        }

        private void BuildStateMachine()
        {
            TearDownStateMachine();

            var builder = new StateMachineBuilder();

            // Criar estados explicitamente registrados
            builder.AddState(new MenuState(_gameManager), out var menuState);
            builder.AddState(new PlayingState(_gameManager), out var playingState);
            builder.AddState(new PausedState(_gameManager), out var pausedState);
            builder.AddState(new GameOverState(_gameManager), out var gameOverState);
            builder.AddState(new VictoryState(_gameManager), out var victoryState);

            // Transições baseadas em eventos do ciclo de jogo
            _startRequestedPredicate = new EventTriggeredPredicate<GameStartRequestedEvent>(() => { });
            _pauseRequestedPredicate = new EventTriggeredPredicate<GamePauseRequestedEvent>(() => { });
            _resumeRequestedPredicate = new EventTriggeredPredicate<GameResumeRequestedEvent>(() => { });
            _resetRequestedPredicate = new EventTriggeredPredicate<GameResetRequestedEvent>(() => { });
            _gameOverPredicate = new EventTriggeredPredicate<GameOverEvent>(() => { });
            _victoryPredicate = new EventTriggeredPredicate<GameVictoryEvent>(() => { });
            builder.At(menuState, playingState, _startRequestedPredicate);
            builder.At(playingState, pausedState, _pauseRequestedPredicate);
            builder.At(pausedState, playingState, _resumeRequestedPredicate);
            builder.At(playingState, gameOverState, _gameOverPredicate);
            builder.At(playingState, victoryState, _victoryPredicate);

            // Transições para reset
            builder.At(playingState, menuState, _resetRequestedPredicate);
            builder.At(pausedState, menuState, _resetRequestedPredicate);
            builder.At(gameOverState, menuState, _resetRequestedPredicate);
            builder.At(victoryState, menuState, _resetRequestedPredicate);

            builder.StateInitial(menuState);
            _stateMachine = builder.Build();

            RegisterEventListeners();

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(menuState.IsGameActive()));
        }

        private void RegisterEventListeners()
        {
            // Vincula eventos do ciclo de jogo garantindo descarte explícito no teardown
            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(OnStartRequested);
            EventBus<GameStartRequestedEvent>.Register(_startRequestedBinding);

            _pauseRequestedBinding = new EventBinding<GamePauseRequestedEvent>(OnPauseRequested);
            EventBus<GamePauseRequestedEvent>.Register(_pauseRequestedBinding);

            _resumeRequestedBinding = new EventBinding<GameResumeRequestedEvent>(OnResumeRequested);
            EventBus<GameResumeRequestedEvent>.Register(_resumeRequestedBinding);

            _resetRequestedBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetRequestedBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(OnGameOver);
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(OnVictory);
            EventBus<GameVictoryEvent>.Register(_victoryBinding);
        }

        private void TearDownStateMachine()
        {
            if (_stateMachine?.CurrentState != null)
            {
                _stateMachine.CurrentState.OnExit();
            }

            if (_startRequestedBinding != null)
            {
                EventBus<GameStartRequestedEvent>.Unregister(_startRequestedBinding);
                _startRequestedBinding = null;
            }

            if (_pauseRequestedBinding != null)
            {
                EventBus<GamePauseRequestedEvent>.Unregister(_pauseRequestedBinding);
                _pauseRequestedBinding = null;
            }

            if (_resumeRequestedBinding != null)
            {
                EventBus<GameResumeRequestedEvent>.Unregister(_resumeRequestedBinding);
                _resumeRequestedBinding = null;
            }

            if (_resetRequestedBinding != null)
            {
                EventBus<GameResetRequestedEvent>.Unregister(_resetRequestedBinding);
                _resetRequestedBinding = null;
            }

            if (_gameOverBinding != null)
            {
                EventBus<GameOverEvent>.Unregister(_gameOverBinding);
                _gameOverBinding = null;
            }

            if (_victoryBinding != null)
            {
                EventBus<GameVictoryEvent>.Unregister(_victoryBinding);
                _victoryBinding = null;
            }

            _startRequestedPredicate = null;
            _pauseRequestedPredicate = null;
            _resumeRequestedPredicate = null;
            _resetRequestedPredicate = null;
            _gameOverPredicate = null;
            _victoryPredicate = null;
            _stateMachine = null;
        }

        private void OnStartRequested(GameStartRequestedEvent _) => _startRequestedPredicate?.Trigger();

        private void OnPauseRequested(GamePauseRequestedEvent _) => _pauseRequestedPredicate?.Trigger();

        private void OnResumeRequested(GameResumeRequestedEvent _) => _resumeRequestedPredicate?.Trigger();

        private void OnResetRequested(GameResetRequestedEvent _) => _resetRequestedPredicate?.Trigger();

        private void OnGameOver(GameOverEvent _) => _gameOverPredicate?.Trigger();

        private void OnVictory(GameVictoryEvent _) => _victoryPredicate?.Trigger();
    }

}
}
