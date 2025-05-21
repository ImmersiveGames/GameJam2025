using Unity.VisualScripting;
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
            gameManager.OnEventPauseGame(true);
        }
    }

    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter()
        {
            Debug.Log($"Iniciando o jogo.");
            gameManager.OnEventPauseGame(false);
        }
        public override void OnExit()
        {
            base.OnExit();
            Debug.Log($"Saindo do jogo.");
            gameManager.OnEventPauseGame(true);
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
            gameManager.OnEventPauseGame(true);
            Time.timeScale = 0;
            // Ativar menu de pausa
        }

        public override void OnExit()
        {
            Time.timeScale = 1;
            gameManager.OnEventPauseGame(false);
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
            gameManager.OnEventPauseGame(true);
            gameManager.OnEventGameOver();
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
            gameManager.OnEventPauseGame(true);
            gameManager.OnEventVictory();
            Debug.Log($"Terminou o jogo com {gameManager.Score} pontos!");
        }
    }
}