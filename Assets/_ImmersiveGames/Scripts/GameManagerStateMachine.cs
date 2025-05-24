using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.StateMachine.GameStates;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts
{
    public class GameManagerStateMachine : Singleton<GameManagerStateMachine>
    {
        private StateMachine.StateMachine _stateMachine;
        private IState _menuState;
        private IState _playingState;
        private IState _pausedState;
        private IState _gameOverState;
        private IState _victoryState;

        private void Update()
        {
            _stateMachine?.Update();
            //DebugUtility.LogVerbose<GameManagerStateMachine>($"Estado: {_stateMachine?.CurrentState?.GetType().Name}");
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        private void OnDestroy()
        {
            // Limpar a máquina de estados
            _stateMachine = null;
            _menuState = null;
            _playingState = null;
            _pausedState = null;
            _gameOverState = null;
            _victoryState = null;
        }

        public void InitializeStateMachine(GameManager gameManager)
        {
            if (_stateMachine != null)
            {
                DebugUtility.LogWarning<GameManagerStateMachine>("Máquina de estados já inicializada.", this);
                return;
            }

            var builder = new StateMachineBuilder();

            // Criar estados
            builder.AddState(new MenuState(gameManager), out _menuState);
            builder.AddState(new PlayingState(gameManager), out _playingState);
            builder.AddState(new PausedState(gameManager), out _pausedState);
            builder.AddState(new GameOverState(gameManager), out _gameOverState);
            builder.AddState(new VictoryState(gameManager), out _victoryState);

            // Definir transições
            // Menu -> Playing
            builder.At(_menuState, _playingState,
                new FuncPredicate(() => Input.GetKeyDown(KeyCode.I)));

            // Playing -> Paused
            builder.At(_playingState, _pausedState,
                new FuncPredicate(() => Input.GetKeyDown(KeyCode.Escape)));

            // Paused -> Playing
            builder.At(_pausedState, _playingState,
                new FuncPredicate(() => Input.GetKeyDown(KeyCode.Escape)));

            // Playing -> GameOver
            builder.At(_playingState, _gameOverState,
                new FuncPredicate(gameManager.CheckGameOver));

            // Playing -> Victory
            builder.At(_playingState, _victoryState,
                new FuncPredicate(gameManager.CheckVictory));

            // GameOver/Victory -> Menu
            builder.At(_gameOverState, _menuState,
                new FuncPredicate(() =>
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        gameManager.ForceReset(); // Força reinicialização
                        return true;
                    }
                    return false;
                }));

            builder.At(_victoryState, _menuState,
                new FuncPredicate(() =>
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        gameManager.ForceReset(); // Força reinicialização
                        return true;
                    }
                    return false;
                }));

            // Definir estado inicial
            builder.StateInitial(_menuState);

            // Construir máquina de estados
            _stateMachine = builder.Build();
        }
    }
}