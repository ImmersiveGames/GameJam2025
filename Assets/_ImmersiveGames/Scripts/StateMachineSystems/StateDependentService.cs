using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class StateDependentService : IStateDependentService
    {
        private bool _isGameActive;
        private readonly GameManagerStateMachine _stateMachine;
        private readonly EventBinding<StateChangedEvent> _stateChangedEvent;

        public StateDependentService(GameManagerStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _isGameActive = stateMachine.CurrentState?.IsGameActive() ?? false;

            _stateChangedEvent = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedEvent);
        }

        public void Dispose()
        {
            EventBus<StateChangedEvent>.Unregister(_stateChangedEvent);
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.isGameActive;
        }

        public bool CanExecuteAction(ActionType action)
        {
            return _isGameActive && (_stateMachine.CurrentState?.CanPerformAction(action) ?? false);
        }

        public bool IsGameActive()
        {
            return _isGameActive;
        }
    }
    public interface IStateDependentService
    {
        bool CanExecuteAction(ActionType action);
        bool IsGameActive();
    }
}