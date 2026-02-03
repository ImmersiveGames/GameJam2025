using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.Scripts.StateMachineSystems
{
    public class OldStateDependentService : IStateDependentService
    {
        private bool _isGameActive;
        private readonly OldGameManagerStateMachine _stateMachine;
        private readonly EventBinding<StateChangedEvent> _stateChangedEvent;

        public OldStateDependentService(OldGameManagerStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _isGameActive = stateMachine.CurrentState?.IsGameActive() ?? false;

            _stateChangedEvent = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedEvent);
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.isGameActive;
        }

        public bool CanExecuteAction(OldActionType action)
        {
            return _isGameActive &&
                (_stateMachine.CurrentState?.CanPerformAction(action) ?? false);
        }

        public bool IsGameActive()
        {
            return _isGameActive;
        }

        public void Dispose()
        {
            EventBus<StateChangedEvent>.Unregister(_stateChangedEvent);
        }
    }

    public interface IStateDependentService : IDisposable
    {
        bool CanExecuteAction(OldActionType action);
        bool IsGameActive();
    }
}



