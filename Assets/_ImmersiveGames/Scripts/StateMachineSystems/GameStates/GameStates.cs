using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems.GameStates
{
    public abstract class GameStateBase : IState
    {
        protected readonly GameManager gameManager;

        protected GameStateBase(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Update() { }
        public virtual bool CanPerformAction(ActionType action) => false;
        public virtual bool IsGameActive() => false;
        public virtual void FixedUpdate() { }
    }

    public class MenuState : GameStateBase
    {
        public MenuState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            // Ativar UI do menu
            // Desativar elementos do jogo
            Debug.Log("Iniciando o menu do jogo.");
            gameManager.SetPlayGame(false);
        }
    }

    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            gameManager.SetPlayGame(true);
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( true));
        }

        public override bool CanPerformAction(ActionType action) => true; // Permite todas as ações
        public override bool IsGameActive() => true;
    }

    public class PausedState : GameStateBase
    {
        public PausedState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            Time.timeScale = 0f;
            gameManager.SetPlayGame(false);
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( false));
        }

        public override void OnExit()
        {
            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( true));
        }

        public override bool CanPerformAction(ActionType action) => false; // Bloqueia todas as ações
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
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( false));
            
            Debug.Log("game over!");
        }
        public override void OnExit()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( true));
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
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( false));
            Debug.Log("Terminou o jogo!");
        }
        public override void OnExit()
        {
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent( true));
        }
    }
}