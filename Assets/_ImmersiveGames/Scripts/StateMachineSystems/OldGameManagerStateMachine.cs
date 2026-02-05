using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Validation;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class OldGameManagerStateMachine : PersistentSingleton<OldGameManagerStateMachine>
    {
        private OldStateMachine _stateMachine;
        private GameManager _gameManager;

        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<OldGameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<OldGameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<OldGameResetRequestedEvent> _resetRequestedBinding;
        private EventBinding<GameReturnToMenuRequestedEvent> _returnToMenuRequestedBinding;

        private EventTriggeredPredicate _gameOverPredicate;
        private EventTriggeredPredicate _victoryPredicate;
        private EventTriggeredPredicate _startRequestedPredicate;
        private EventTriggeredPredicate _pauseRequestedPredicate;
        private EventTriggeredPredicate _resumeRequestedPredicate;
        private EventTriggeredPredicate _resetRequestedPredicate;
        private EventTriggeredPredicate _returnToMenuRequestedPredicate;

        private int _lastStartRequestedFrame = -1;
        private int _lastPauseRequestedFrame = -1;
        private int _lastResumeRequestedFrame = -1;
        private int _lastResetRequestedFrame = -1;
        private int _lastReturnToMenuRequestedFrame = -1;
        private int _lastGameOverFrame = -1;
        private int _lastVictoryFrame = -1;

        public OldIState CurrentState => _stateMachine?.CurrentState;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private void Update() => _stateMachine?.Update();

        private void OnDestroy()
        {
            TearDownStateMachine();
        }

        public void InitializeStateMachine(GameManager gameManager)
        {
            Preconditions.CheckNotNull(gameManager, "GameManager n�o pode ser nulo.");
            _gameManager = gameManager;

            BuildStateMachine();
        }

        public void Rebuild(GameManager gameManager)
        {
            Preconditions.CheckNotNull(gameManager, "GameManager n�o pode ser nulo.");
            _gameManager = gameManager;

            BuildStateMachine();
        }

        private void BuildStateMachine()
        {
            TearDownStateMachine();

            var builder = new OldStateMachineBuilder();

            builder.AddState(new OldMenuState(_gameManager), out var menuState);
            builder.AddState(new OldPlayingState(_gameManager), out var playingState);
            builder.AddState(new OldPausedState(_gameManager), out var pausedState);
            builder.AddState(new OldGameOverState(_gameManager), out var gameOverState);
            builder.AddState(new OldVictoryState(_gameManager), out var victoryState);

            _startRequestedPredicate = new EventTriggeredPredicate(() => { });
            _pauseRequestedPredicate = new EventTriggeredPredicate(() => { });
            _resumeRequestedPredicate = new EventTriggeredPredicate(() => { });
            _resetRequestedPredicate = new EventTriggeredPredicate(() => { });
            _returnToMenuRequestedPredicate = new EventTriggeredPredicate(() => { });
            _gameOverPredicate = new EventTriggeredPredicate(() => { });
            _victoryPredicate = new EventTriggeredPredicate(() => { });

            builder.At(menuState, playingState, _startRequestedPredicate);
            builder.At(playingState, pausedState, _pauseRequestedPredicate);
            builder.At(pausedState, playingState, _resumeRequestedPredicate);
            builder.At(playingState, gameOverState, _gameOverPredicate);
            builder.At(playingState, victoryState, _victoryPredicate);

            // Reset: volta ao menu com sem�ntica de "reset" (seu fluxo j� existente).
            builder.At(playingState, menuState, _resetRequestedPredicate);
            builder.At(pausedState, menuState, _resetRequestedPredicate);
            builder.At(gameOverState, menuState, _resetRequestedPredicate);
            builder.At(victoryState, menuState, _resetRequestedPredicate);

            // ReturnToMenu: volta ao menu sem sem�ntica de "reset".
            // O GameManager.SceneFlow � quem executa a transi��o de cenas quando recebe GameReturnToMenuRequestedEvent.
            builder.At(playingState, menuState, _returnToMenuRequestedPredicate);
            builder.At(pausedState, menuState, _returnToMenuRequestedPredicate);
            builder.At(gameOverState, menuState, _returnToMenuRequestedPredicate);
            builder.At(victoryState, menuState, _returnToMenuRequestedPredicate);

            builder.StateInitial(menuState);
            _stateMachine = builder.Build();

            RegisterEventListeners();

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(menuState.IsGameActive()));
        }

        private void RegisterEventListeners()
        {
            _startRequestedBinding = new EventBinding<OldGameStartRequestedEvent>(OnStartRequested);
            EventBus<OldGameStartRequestedEvent>.Register(_startRequestedBinding);

            _pauseRequestedBinding = new EventBinding<GamePauseRequestedEvent>(OnPauseRequested);
            EventBus<GamePauseRequestedEvent>.Register(_pauseRequestedBinding);

            _resumeRequestedBinding = new EventBinding<OldGameResumeRequestedEvent>(OnResumeRequested);
            EventBus<OldGameResumeRequestedEvent>.Register(_resumeRequestedBinding);

            _resetRequestedBinding = new EventBinding<OldGameResetRequestedEvent>(OnResetRequested);
            EventBus<OldGameResetRequestedEvent>.Register(_resetRequestedBinding);

            _returnToMenuRequestedBinding = new EventBinding<GameReturnToMenuRequestedEvent>(OnReturnToMenuRequested);
            EventBus<GameReturnToMenuRequestedEvent>.Register(_returnToMenuRequestedBinding);

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
                EventBus<OldGameStartRequestedEvent>.Unregister(_startRequestedBinding);
                _startRequestedBinding = null;
            }

            if (_pauseRequestedBinding != null)
            {
                EventBus<GamePauseRequestedEvent>.Unregister(_pauseRequestedBinding);
                _pauseRequestedBinding = null;
            }

            if (_resumeRequestedBinding != null)
            {
                EventBus<OldGameResumeRequestedEvent>.Unregister(_resumeRequestedBinding);
                _resumeRequestedBinding = null;
            }

            if (_resetRequestedBinding != null)
            {
                EventBus<OldGameResetRequestedEvent>.Unregister(_resetRequestedBinding);
                _resetRequestedBinding = null;
            }

            if (_returnToMenuRequestedBinding != null)
            {
                EventBus<GameReturnToMenuRequestedEvent>.Unregister(_returnToMenuRequestedBinding);
                _returnToMenuRequestedBinding = null;
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
            _returnToMenuRequestedPredicate = null;
            _gameOverPredicate = null;
            _victoryPredicate = null;
            _stateMachine = null;

            // Opcional: limpa debounce para rebuilds r�pidos em debug/QA
            _lastStartRequestedFrame = -1;
            _lastPauseRequestedFrame = -1;
            _lastResumeRequestedFrame = -1;
            _lastResetRequestedFrame = -1;
            _lastReturnToMenuRequestedFrame = -1;
            _lastGameOverFrame = -1;
            _lastVictoryFrame = -1;
        }

        private void OnStartRequested(OldGameStartRequestedEvent _)
        {
            if (_startRequestedPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastStartRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is not OldMenuState) return;

            _lastStartRequestedFrame = frame;
            _startRequestedPredicate.Trigger();
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (_pauseRequestedPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastPauseRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is OldPausedState) return;

            _lastPauseRequestedFrame = frame;
            _pauseRequestedPredicate.Trigger();
        }

        private void OnResumeRequested(OldGameResumeRequestedEvent _)
        {
            if (_resumeRequestedPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastResumeRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is not OldPausedState) return;

            _lastResumeRequestedFrame = frame;
            _resumeRequestedPredicate.Trigger();
        }

        private void OnResetRequested(OldGameResetRequestedEvent _)
        {
            if (_resetRequestedPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastResetRequestedFrame == frame) return;

            _lastResetRequestedFrame = frame;
            _resetRequestedPredicate.Trigger();
        }

        private void OnReturnToMenuRequested(GameReturnToMenuRequestedEvent _)
        {
            if (_returnToMenuRequestedPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastReturnToMenuRequestedFrame == frame) return;

            // Se j� est� no menu, ignora.
            if (_stateMachine?.CurrentState is OldMenuState) return;

            _lastReturnToMenuRequestedFrame = frame;
            _returnToMenuRequestedPredicate.Trigger();
        }

        private void OnGameOver(GameOverEvent _)
        {
            if (_gameOverPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastGameOverFrame == frame) return;

            if (!IsPlaying())
            {
                DebugUtility.LogWarning<OldGameManagerStateMachine>("GameOverEvent ignorado: estado atual n�o � Playing.");
                return;
            }

            _lastGameOverFrame = frame;
            _gameOverPredicate.Trigger();
        }

        private void OnVictory(GameVictoryEvent _)
        {
            if (_victoryPredicate == null) return;

            int frame = Time.frameCount;
            if (_lastVictoryFrame == frame) return;

            if (!IsPlaying())
            {
                DebugUtility.LogWarning<OldGameManagerStateMachine>("GameVictoryEvent ignorado: estado atual n�o � Playing.");
                return;
            }

            _lastVictoryFrame = frame;
            _victoryPredicate.Trigger();
        }

        private bool IsPlaying() => _stateMachine?.CurrentState is OldPlayingState;
    }
}


