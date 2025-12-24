// (arquivo completo no download)

using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// FSM concreta do GameLoop usando a infraestrutura modular criada em NewScripts.
    /// Mantém transições básicas de Boot → Menu → Playing → Paused e resets genéricos.
    /// </summary>
    public class GameLoopStateMachine
    {
        private readonly IGameLoopSignals _signals;
        private readonly IGameLoopStateObserver _observer;
        private readonly StateMachine _stateMachine;

        private BootState _bootState;
        private MenuState _menuState;
        private PlayingState _playingState;
        private PausedState _pausedState;

        public GameLoopStateMachine(IGameLoopSignals signals, IGameLoopStateObserver observer = null)
        {
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
            _observer = observer;
            _stateMachine = BuildStateMachine();
        }

        public IState CurrentState => _stateMachine.CurrentState;

        public void Update() => _stateMachine.Update();
        public void FixedUpdate() => _stateMachine.FixedUpdate();

        public bool CanPerform(ActionType action) => _stateMachine.CurrentState?.CanPerformAction(action) ?? false;
        public bool IsGameActive() => _stateMachine.CurrentState?.IsGameActive() ?? false;

        private StateMachine BuildStateMachine()
        {
            _bootState = new BootState(_observer);
            _menuState = new MenuState(_observer);
            _playingState = new PlayingState(_observer);
            _pausedState = new PausedState(_observer);

            var builder = new StateMachineBuilder();

            builder.AddState(_bootState, out var boot);
            builder.AddState(_menuState, out var menu);
            builder.AddState(_playingState, out var playing);
            builder.AddState(_pausedState, out var paused);

            // Boot é um estado transitório: avança automaticamente para Menu no primeiro tick.
            builder.At(boot, menu, new FuncPredicate(() => true));

            // Menu → Playing quando o usuário inicia o jogo.
            builder.At(menu, playing, new FuncPredicate(() => _signals.StartRequested));

            // Playing ↔ Paused
            builder.At(playing, paused, new FuncPredicate(() => _signals.PauseRequested));
            builder.At(paused, playing, new FuncPredicate(() => _signals.ResumeRequested));

            // Reset global para Boot.
            builder.Any(boot, new FuncPredicate(() => _signals.ResetRequested));

            builder.StateInitial(boot);
            return builder.Build();
        }
    }

    internal abstract class GameLoopStateBase : IState
    {
        private readonly HashSet<ActionType> _allowedActions = new();
        private readonly IGameLoopStateObserver _observer;

        protected GameLoopStateBase(GameLoopStateId stateId, IGameLoopStateObserver observer)
        {
            StateId = stateId;
            _observer = observer;
        }

        protected GameLoopStateId StateId { get; }

        protected void AllowActions(params ActionType[] actions)
        {
            foreach (var action in actions)
            {
                _allowedActions.Add(action);
            }
        }

        public virtual void OnEnter()
        {
            _observer?.OnStateEntered(StateId, IsGameActive());
            _observer?.OnGameActivityChanged(IsGameActive());
        }

        public virtual void OnExit()
        {
            _observer?.OnStateExited(StateId);
        }

        public virtual void Update() { }
        public virtual void FixedUpdate() { }

        public virtual bool CanPerformAction(ActionType action) => _allowedActions.Contains(action);

        public abstract bool IsGameActive();
    }

    internal sealed class BootState : GameLoopStateBase
    {
        public BootState(IGameLoopStateObserver observer) : base(GameLoopStateId.Boot, observer)
        {
            AllowActions(ActionType.Navigate, ActionType.UiSubmit, ActionType.UiCancel);
        }

        public override bool IsGameActive() => false;
    }

    /// <summary>
    /// Estado de menu onde a simulação ainda não está ativa e apenas ações de UI/navegação são liberadas.
    /// </summary>
    internal sealed class MenuState : GameLoopStateBase
    {
        public MenuState(IGameLoopStateObserver observer) : base(GameLoopStateId.Menu, observer)
        {
            AllowActions(
                ActionType.Navigate,
                ActionType.UiSubmit,
                ActionType.UiCancel,
                ActionType.RequestReset,
                ActionType.RequestQuit);
        }

        public override bool IsGameActive() => false;
    }

    internal sealed class PlayingState : GameLoopStateBase
    {
        public PlayingState(IGameLoopStateObserver observer) : base(GameLoopStateId.Playing, observer)
        {
            AllowActions(ActionType.Move, ActionType.Shoot, ActionType.Interact, ActionType.Spawn);
        }

        public override bool IsGameActive() => true;
    }

    internal sealed class PausedState : GameLoopStateBase
    {
        public PausedState(IGameLoopStateObserver observer) : base(GameLoopStateId.Paused, observer)
        {
            AllowActions(ActionType.Navigate, ActionType.UiSubmit, ActionType.UiCancel, ActionType.RequestReset, ActionType.RequestQuit);
        }

        public override bool IsGameActive() => false;
    }
}
