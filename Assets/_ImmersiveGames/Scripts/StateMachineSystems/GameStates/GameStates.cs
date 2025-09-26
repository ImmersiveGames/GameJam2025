using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems.GameStates
{
    public abstract class GameStateBase : IState
    {
        protected readonly GameManager gameManager;
        protected readonly HashSet<ActionType> allowedActions;

        protected GameStateBase(GameManager gameManager)
        {
            this.gameManager = gameManager;
            allowedActions = new HashSet<ActionType>();
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Update() { }
        public virtual bool CanPerformAction(ActionType action)
        {
            return allowedActions.Contains(action);
        }
        public virtual bool IsGameActive() => false;
        public virtual void FixedUpdate() { }
    }

    public class MenuState : GameStateBase
    {
        public MenuState(GameManager gameManager) : base(gameManager)
        {
            // Definir ações permitidas no estado Menu
            allowedActions.Add(ActionType.Navigate);
        }

        public override void OnEnter()
        {
            // Ativar UI do menu
            // Desativar elementos do jogo (via lógica específica, se necessário)
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false)); // Notifica atores
            DebugUtility.LogVerbose<MenuState>("Iniciando o menu do jogo.");
        }

        public override void OnExit()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true)); // Restaura atores
            DebugUtility.LogVerbose<MenuState>("Saindo do menu do jogo.");
        }

        public override bool IsGameActive() => false;
    }

    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager)
        {
            allowedActions.Add(ActionType.Move);
            allowedActions.Add(ActionType.Shoot);
            allowedActions.Add(ActionType.Spawn);
            allowedActions.Add(ActionType.Interact);
        }

        public override void OnEnter()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<PlayingState>("Entrou no estado Playing.");
        }

        public override bool IsGameActive() => true;
    }

    public class PausedState : GameStateBase
    {
        public PausedState(GameManager gameManager) : base(gameManager)
        {
            allowedActions.Add(ActionType.None); // Nenhuma ação permitida
        }

        public override void OnEnter()
        {
            Time.timeScale = 0f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<PausedState>("Entrou no estado Paused.");
        }

        public override void OnExit()
        {
            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<PausedState>("Saiu do estado Paused.");
        }

        public override bool IsGameActive() => false;
    }


    public class GameOverState : GameStateBase
    {
        public GameOverState(GameManager gameManager) : base(gameManager) { }
        public override bool IsGameActive() => false;
        public override void OnEnter()
        {
            // Mostrar tela de game over
            // Mostrar pontuação final
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));

            DebugUtility.LogVerbose<GameOverState>("game over!");
        }
        public override void OnExit()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }

    public class VictoryState : GameStateBase
    {
        public VictoryState(GameManager gameManager) : base(gameManager) { }
        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            // Mostrar tela de vitória
            // Mostrar estatísticas
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<VictoryState>("Terminou o jogo!");
        }
        public override void OnExit()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }
}