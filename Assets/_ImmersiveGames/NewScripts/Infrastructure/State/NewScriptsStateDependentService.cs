using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    /// <summary>
    /// Serviço mínimo para coordenar permissões de ações no baseline NewScripts.
    /// Inicia em Playing e troca de estado conforme eventos globais (se disponíveis).
    /// </summary>
    public sealed class NewScriptsStateDependentService : IStateDependentService
    {
        private enum ServiceState
        {
            Menu,
            Playing,
            Paused
        }

        private ServiceState _state = ServiceState.Playing;

        private EventBinding<GameStartEvent> _gameStartBinding;
        private EventBinding<GamePauseEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private bool _bindingsRegistered;

        public NewScriptsStateDependentService()
        {
            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            switch (_state)
            {
                case ServiceState.Playing:
                    return action switch
                    {
                        ActionType.Move => true,
                        ActionType.Shoot => true,
                        ActionType.Spawn => true,
                        ActionType.Interact => true,
                        ActionType.Navigate => true,
                        ActionType.UiSubmit => true,
                        ActionType.UiCancel => true,
                        ActionType.RequestReset => true,
                        ActionType.RequestQuit => true,
                        _ => true
                    };
                case ServiceState.Menu:
                case ServiceState.Paused:
                    return action switch
                    {
                        ActionType.Navigate => true,
                        ActionType.UiSubmit => true,
                        ActionType.UiCancel => true,
                        ActionType.RequestReset => true,
                        ActionType.RequestQuit => true,
                        _ => false
                    };
                default:
                    return false;
            }
        }

        public bool IsGameActive()
        {
            return _state == ServiceState.Playing;
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
            {
                return;
            }

            EventBus<GameStartEvent>.Unregister(_gameStartBinding);
            EventBus<GamePauseEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);

            _bindingsRegistered = false;
        }

        private void TryRegisterEvents()
        {
            try
            {
                _gameStartBinding = new EventBinding<GameStartEvent>(_ => SetState(ServiceState.Playing));
                _gamePauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => SetState(ServiceState.Playing));

                EventBus<GameStartEvent>.Register(_gameStartBinding);
                EventBus<GamePauseEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);

                _bindingsRegistered = true;
            }
            catch
            {
                // EventBus ou eventos não estão disponíveis; segue sem assinaturas.
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            SetState(evt != null && evt.IsPaused ? ServiceState.Paused : ServiceState.Playing);
        }

        private void SetState(ServiceState next)
        {
            _state = next;
        }
    }
}
