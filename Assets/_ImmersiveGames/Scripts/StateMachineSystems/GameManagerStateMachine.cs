using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class GameManagerStateMachine : Singleton<GameManagerStateMachine>
    {
        private StateMachine _stateMachine;

        public IState CurrentState => _stateMachine?.CurrentState;

        private void Update() => _stateMachine?.Update();
        private void FixedUpdate() => _stateMachine?.FixedUpdate();

        private void OnDestroy()
        {
            _stateMachine = null;
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
            builder.AddState(new MenuState(gameManager), out var menuState);
            builder.AddState(new PlayingState(gameManager), out var playingState);
            builder.AddState(new PausedState(gameManager), out var pausedState);
            builder.AddState(new GameOverState(gameManager), out var gameOverState);
            builder.AddState(new VictoryState(gameManager), out var victoryState);

            // Definir transições
            builder.At(menuState, playingState, new FuncPredicate(() => Input.GetKeyDown(KeyCode.I)));
            builder.At(playingState, pausedState, new FuncPredicate(() => Input.GetKeyDown(KeyCode.Escape)));
            builder.At(pausedState, playingState, new FuncPredicate(() => Input.GetKeyDown(KeyCode.Escape)));
            builder.At(playingState, gameOverState, new FuncPredicate(gameManager.CheckGameOver));
            builder.At(playingState, victoryState, new FuncPredicate(gameManager.CheckVictory));
            builder.At(gameOverState, menuState, new FuncPredicate(() =>
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gameManager.ForceReset();
                    return true;
                }
                return false;
            }));
            builder.At(victoryState, menuState, new FuncPredicate(() =>
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    gameManager.ForceReset();
                    return true;
                }
                return false;
            }));

            builder.StateInitial(menuState);
            _stateMachine = builder.Build();
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(menuState.IsGameActive()));
        }

        public void ChangeState(IState newState, GameManager manager)
        {
            _stateMachine?.SetState(newState);
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(newState.IsGameActive()));
        }
    }
}