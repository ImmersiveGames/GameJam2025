using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Nova orquestração do GameLoop usando a infraestrutura FSM de NewScripts.
    /// Mantém a mesma lógica do GameManagerStateMachine legado sem acoplar registradores/singletons.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class GameLoopStateMachine
    {
        private StateMachine _stateMachine;
        private GameManager _gameManager;

        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GameStartRequestedEvent> _startRequestedBinding;
        private EventBinding<GamePauseRequestedEvent> _pauseRequestedBinding;
        private EventBinding<GameResumeRequestedEvent> _resumeRequestedBinding;
        private EventBinding<GameResetRequestedEvent> _resetRequestedBinding;
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

        public IState CurrentState => _stateMachine?.CurrentState;

        /// <summary>
        /// Inicializa a FSM com um GameManager fornecido (sem registrar singletons).
        /// </summary>
        public void Initialize(GameManager gameManager)
        {
            Preconditions.CheckNotNull(gameManager, "GameManager não pode ser nulo.");
            _gameManager = gameManager;

            BuildStateMachine();
        }

        /// <summary>
        /// Atualiza a FSM (chamado tipicamente em Update do host).
        /// </summary>
        public void Update()
        {
            _stateMachine?.Update();
        }

        /// <summary>
        /// Atualiza a FSM em FixedUpdate do host.
        /// </summary>
        public void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        /// <summary>
        /// Desfaz o setup e desregistra listeners.
        /// </summary>
        public void Dispose()
        {
            TearDownStateMachine();
        }

        private void BuildStateMachine()
        {
            TearDownStateMachine();

            var builder = new StateMachineBuilder();

            builder.AddState(new MenuState(_gameManager), out var menuState);
            builder.AddState(new PlayingState(_gameManager), out var playingState);
            builder.AddState(new PausedState(_gameManager), out var pausedState);
            builder.AddState(new GameOverState(_gameManager), out var gameOverState);
            builder.AddState(new VictoryState(_gameManager), out var victoryState);

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

            builder.At(playingState, menuState, _resetRequestedPredicate);
            builder.At(pausedState, menuState, _resetRequestedPredicate);
            builder.At(gameOverState, menuState, _resetRequestedPredicate);
            builder.At(victoryState, menuState, _resetRequestedPredicate);

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
            _startRequestedBinding = new EventBinding<GameStartRequestedEvent>(OnStartRequested);
            EventBus<GameStartRequestedEvent>.Register(_startRequestedBinding);

            _pauseRequestedBinding = new EventBinding<GamePauseRequestedEvent>(OnPauseRequested);
            EventBus<GamePauseRequestedEvent>.Register(_pauseRequestedBinding);

            _resumeRequestedBinding = new EventBinding<GameResumeRequestedEvent>(OnResumeRequested);
            EventBus<GameResumeRequestedEvent>.Register(_resumeRequestedBinding);

            _resetRequestedBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetRequestedBinding);

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

            _lastStartRequestedFrame = -1;
            _lastPauseRequestedFrame = -1;
            _lastResumeRequestedFrame = -1;
            _lastResetRequestedFrame = -1;
            _lastReturnToMenuRequestedFrame = -1;
            _lastGameOverFrame = -1;
            _lastVictoryFrame = -1;
        }

        private void OnStartRequested(GameStartRequestedEvent _)
        {
            if (_startRequestedPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastStartRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is not MenuState) return;

            _lastStartRequestedFrame = frame;
            _startRequestedPredicate.Trigger();
        }

        private void OnPauseRequested(GamePauseRequestedEvent _)
        {
            if (_pauseRequestedPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastPauseRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is PausedState) return;

            _lastPauseRequestedFrame = frame;
            _pauseRequestedPredicate.Trigger();
        }

        private void OnResumeRequested(GameResumeRequestedEvent _)
        {
            if (_resumeRequestedPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastResumeRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is not PausedState) return;

            _lastResumeRequestedFrame = frame;
            _resumeRequestedPredicate.Trigger();
        }

        private void OnResetRequested(GameResetRequestedEvent _)
        {
            if (_resetRequestedPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastResetRequestedFrame == frame) return;

            _lastResetRequestedFrame = frame;
            _resetRequestedPredicate.Trigger();
        }

        private void OnReturnToMenuRequested(GameReturnToMenuRequestedEvent _)
        {
            if (_returnToMenuRequestedPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastReturnToMenuRequestedFrame == frame) return;

            if (_stateMachine?.CurrentState is MenuState) return;

            _lastReturnToMenuRequestedFrame = frame;
            _returnToMenuRequestedPredicate.Trigger();
        }

        private void OnGameOver(GameOverEvent _)
        {
            if (_gameOverPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastGameOverFrame == frame) return;

            if (!IsPlaying())
            {
                DebugUtility.LogWarning<GameLoopStateMachine>("GameOverEvent ignorado: estado atual não é Playing.");
                return;
            }

            _lastGameOverFrame = frame;
            _gameOverPredicate.Trigger();
        }

        private void OnVictory(GameVictoryEvent _)
        {
            if (_victoryPredicate == null) return;

            var frame = Time.frameCount;
            if (_lastVictoryFrame == frame) return;

            if (!IsPlaying())
            {
                DebugUtility.LogWarning<GameLoopStateMachine>("GameVictoryEvent ignorado: estado atual não é Playing.");
                return;
            }

            _lastVictoryFrame = frame;
            _victoryPredicate.Trigger();
        }

        private bool IsPlaying() => _stateMachine?.CurrentState is PlayingState;
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal abstract class GameLoopStateBase : IState
    {
        protected readonly GameManager gameManager;
        private readonly HashSet<ActionType> _allowedActions;

        protected GameLoopStateBase(GameManager gameManager)
        {
            this.gameManager = gameManager;
            _allowedActions = new HashSet<ActionType>();
        }

        /// <summary>
        /// Acesso tardio ao gate global. Não falha caso o serviço ainda não exista.
        /// </summary>
        protected ISimulationGateService Gate
        {
            get
            {
                return DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) ? gate : null;
            }
        }

        protected void AcquireGate(string token)
        {
            var gate = Gate;
            if (gate == null) return;

            if (gate.IsTokenActive(token)) return;

            gate.Acquire(token);
        }

        protected void ReleaseGate(string token)
        {
            var gate = Gate;
            if (gate == null) return;

            if (!gate.IsTokenActive(token)) return;

            gate.Release(token);
        }

        protected void AllowActions(params ActionType[] actions)
        {
            foreach (var action in actions)
            {
                _allowedActions.Add(action);
            }
        }

        protected void AllowMenuNavigationWithExitShortcuts()
        {
            AllowActions(
                ActionType.Navigate,
                ActionType.UiSubmit,
                ActionType.UiCancel,
                ActionType.RequestReset,
                ActionType.RequestQuit
            );
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }

        public virtual bool CanPerformAction(ActionType action)
        {
            return _allowedActions.Contains(action);
        }

        public virtual bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal class MenuState : GameLoopStateBase
    {
        public MenuState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override void OnEnter()
        {
            AcquireGate(SimulationGateTokens.Menu);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<MenuState>("Iniciando o menu do jogo.");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.Menu);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<MenuState>("Saindo do menu do jogo.");
        }

        public override bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal class PlayingState : GameLoopStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager)
        {
            AllowActions(ActionType.Move, ActionType.Shoot, ActionType.Spawn, ActionType.Interact);
        }

        public override void OnEnter()
        {
            ReleaseGate(SimulationGateTokens.Menu);
            ReleaseGate(SimulationGateTokens.Pause);
            ReleaseGate(SimulationGateTokens.GameOver);
            ReleaseGate(SimulationGateTokens.Victory);
            ReleaseGate(SimulationGateTokens.SceneTransition);
            ReleaseGate(SimulationGateTokens.Cinematic);
            ReleaseGate(SimulationGateTokens.SoftReset);
            ReleaseGate(SimulationGateTokens.Loading);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<PlayingState>("Entrou no estado Playing.");
        }

        public override bool IsGameActive() => true;
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal class PausedState : GameLoopStateBase
    {
        public PausedState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override void OnEnter()
        {
            AcquireGate(SimulationGateTokens.Pause);

            Time.timeScale = 0f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<PausedState>("Entrou no estado Paused.");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.Pause);

            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<PausedState>("Saiu do estado Paused.");
        }

        public override bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal class GameOverState : GameLoopStateBase
    {
        public GameOverState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            Debug.Log("Gate IsOpen=" + Gate?.IsOpen);
            AcquireGate(SimulationGateTokens.GameOver);

            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<GameOverState>("game over!");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.GameOver);

            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    internal class VictoryState : GameLoopStateBase
    {
        public VictoryState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            Debug.Log("Gate IsOpen=" + Gate?.IsOpen);
            AcquireGate(SimulationGateTokens.Victory);

            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<VictoryState>("Terminou o jogo!");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.Victory);

            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }
}
