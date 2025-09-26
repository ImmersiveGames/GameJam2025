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

            // Transições baseadas em eventos
            var gameOverPredicate = new EventTriggeredPredicate<GameOverEvent>(() => { });
            EventBus<GameOverEvent>.Register(new EventBinding<GameOverEvent>(() => gameOverPredicate.Trigger()));
            builder.At(playingState, gameOverState, gameOverPredicate);

            var victoryPredicate = new EventTriggeredPredicate<GameVictoryEvent>(() => { });
            EventBus<GameVictoryEvent>.Register(new EventBinding<GameVictoryEvent>(() => victoryPredicate.Trigger()));
            builder.At(playingState, victoryState, victoryPredicate);

            // Transições para reset
            var resetPredicate = new FuncPredicate(() => Input.GetKeyDown(KeyCode.R));
            builder.At(gameOverState, menuState, resetPredicate);
            builder.At(victoryState, menuState, resetPredicate);

            builder.StateInitial(menuState);
            _stateMachine = builder.Build();
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(menuState.IsGameActive()));
        }
    }
}