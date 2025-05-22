using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachine.GameStates
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
        public virtual void FixedUpdate() { }
    }

    public class MenuState : GameStateBase
    {
        public MenuState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            // Ativar UI do menu
            // Desativar elementos do jogo
            Debug.Log($"Iniciando o menu do jogo.");
            gameManager.SetPlayGame(false);
        }
    }

    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            base.OnEnter();
            gameManager.SetPlayGame(true); // Agora chamamos StartGame() ao entrar no estado
        }

        public override void Update()
        {
            // Lógica principal do jogo
        }
    }

    public class PausedState : GameStateBase
    {
        public PausedState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            Debug.Log($"pausando o jogo.");
            gameManager.SetPlayGame(false);
            Time.timeScale = 0;
            // Ativar menu de pausa
        }

        public override void OnExit()
        {
            Time.timeScale = 1;
            gameManager.SetPlayGame(true);
            // Desativar menu de pausa
        }
    }

    public class GameOverState : GameStateBase
    {
        public GameOverState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            // Mostrar tela de game over
            // Mostrar pontuação final
            Debug.Log($"game over! Você fez {gameManager.Score} pontos!");
        }
    }

    public class VictoryState : GameStateBase
    {
        public VictoryState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            // Mostrar tela de vitória
            // Mostrar estatísticas
            Debug.Log($"Terminou o jogo com {gameManager.Score} pontos!");
        }
    }
}