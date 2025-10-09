using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StatesMachines;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class StateDependentBehavior : MonoBehaviour
    {
        private bool _isGameActive;
        private EventBinding<StateChangedEvent> _stateChangedEvent;
        private void OnEnable()
        {
            _stateChangedEvent = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedEvent);
        }

        private void OnDisable()
        {
            EventBus<StateChangedEvent>.Unregister(_stateChangedEvent);
        }
        
        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.isGameActive;
        }

        public bool CanExecuteAction(ActionType action)
        {
            return _isGameActive && (GameManagerStateMachine.Instance.CurrentState?.CanPerformAction(action) ?? false);
        }
    }
}