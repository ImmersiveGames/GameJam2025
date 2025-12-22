using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class GameStateBase : IState
    {
        protected readonly GameManager gameManager;
        private readonly HashSet<ActionType> _allowedActions;

        protected GameStateBase(GameManager gameManager)
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

            // Idempotente: evita "double acquire" no mesmo token (que leva a logs duplicados)
            if (gate.IsTokenActive(token)) return;

            gate.Acquire(token);
        }

        protected void ReleaseGate(string token)
        {
            var gate = Gate;
            if (gate == null) return;

            // Idempotente: evita spam de "Release ignorado (token não estava ativo)"
            if (!gate.IsTokenActive(token)) return;

            gate.Release(token);
        }

        protected void AllowActions(params ActionType[] actions)
        {
            foreach (ActionType action in actions)
            {
                _allowedActions.Add(action);
            }
        }

        /// <summary>
        /// Perfil comum para telas que exibem UI e pausam o jogo, mas precisam aceitar navegação.
        /// </summary>
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

        public virtual bool CanPerformAction(ActionType action)
        {
            return _allowedActions.Contains(action);
        }

        public virtual bool IsGameActive() => false;

        public virtual void FixedUpdate() { }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class MenuState : GameStateBase
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
    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager)
        {
            AllowActions(ActionType.Move, ActionType.Shoot, ActionType.Spawn, ActionType.Interact);
        }

        public override void OnEnter()
        {
            // Libera tokens conhecidos de forma defensiva e silenciosa (idempotente).
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
    public class PausedState : GameStateBase
    {
        public PausedState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override void OnEnter()
        {
            AcquireGate(SimulationGateTokens.Pause);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<PausedState>("Entrou no estado Paused.");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.Pause);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<PausedState>("Saiu do estado Paused.");
        }

        public override bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class GameOverState : GameStateBase
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

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<GameOverState>("game over!");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.GameOver);
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class VictoryState : GameStateBase
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

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<VictoryState>("Terminou o jogo!");
        }

        public override void OnExit()
        {
            ReleaseGate(SimulationGateTokens.Victory);
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }
}
