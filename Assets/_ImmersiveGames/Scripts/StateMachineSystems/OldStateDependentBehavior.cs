using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.NewScripts.Core.Events;
using UnityEngine;
namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class OldStateDependentBehavior : MonoBehaviour
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

        public bool CanExecuteAction(OldActionType action)
        {
            return _isGameActive && (OldGameManagerStateMachine.Instance.CurrentState?.CanPerformAction(action) ?? false);
        }
    }
}


