using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems.GameStates
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
        /// Atalho para compor ações permitidas sem acoplamento com UI específica.
        /// </summary>
        /// <param name="actions">Ações liberadas para o estado.</param>
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
            AllowActions(ActionType.Navigate, ActionType.UiSubmit, ActionType.UiCancel, ActionType.RequestReset, ActionType.RequestQuit);
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

    public class MenuState : GameStateBase
    {
        public MenuState(GameManager gameManager) : base(gameManager)
        {
            // Definir ações permitidas no estado Menu
            AllowMenuNavigationWithExitShortcuts();
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
            AllowActions(ActionType.Move, ActionType.Shoot, ActionType.Spawn, ActionType.Interact);
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
            // Nenhuma ação de gameplay, mas o usuário pode navegar na UI ou solicitar reset/quit.
            AllowMenuNavigationWithExitShortcuts();
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
        public GameOverState(GameManager gameManager) : base(gameManager)
        {
            // Permite navegação e confirmação em menus de pós-jogo (reiniciar, sair, etc.).
            AllowMenuNavigationWithExitShortcuts();
        }
        public override bool IsGameActive() => false;
        public override void OnEnter()
        {
            // Mostrar tela de game over
            // Mostrar pontuação final
            Time.timeScale = 0f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));

            DebugUtility.LogVerbose<GameOverState>("game over!");
        }
        public override void OnExit()
        {
            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }

    public class VictoryState : GameStateBase
    {
        public VictoryState(GameManager gameManager) : base(gameManager)
        {
            // Permite navegação e confirmação em menus de pós-jogo (reiniciar, sair, etc.).
            AllowMenuNavigationWithExitShortcuts();
        }
        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            // Mostrar tela de vitória
            // Mostrar estatísticas
            Time.timeScale = 0f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<VictoryState>("Terminou o jogo!");
        }
        public override void OnExit()
        {
            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }
}