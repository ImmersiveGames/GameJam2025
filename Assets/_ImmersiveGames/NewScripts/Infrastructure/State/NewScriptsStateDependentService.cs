// (arquivo completo no download)

using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    public sealed class NewScriptsStateDependentService : IStateDependentService
    {
        private enum ServiceState
        {
            Menu,
            Playing,
            Paused
        }

        private ServiceState _state = ServiceState.Menu;

        private EventBinding<GameStartEvent> _gameStartBinding;
        private EventBinding<GamePauseEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private bool _bindingsRegistered;
        private bool _loggedGateBlock;
        private bool _loggedGameLoopUsage;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        public NewScriptsStateDependentService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveGameLoopService();

            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            if (IsPausedByGate(action))
            {
                return false;
            }

            var serviceState = ResolveServiceState();

            switch (serviceState)
            {
                case ServiceState.Playing:
                    return true;

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
            TryResolveGateService();
            TryResolveGameLoopService();
            return ResolveServiceState() == ServiceState.Playing;
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

        private void TryResolveGateService()
        {
            if (_gateService != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out _gateService);
        }

        private void TryResolveGameLoopService()
        {
            if (_gameLoopService != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
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
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            SetState(evt is { IsPaused: true } ? ServiceState.Paused : ServiceState.Playing);
        }

        private void SetState(ServiceState next)
        {
            _state = next;
        }

        private ServiceState ResolveServiceState()
        {
            if (_gameLoopService != null && !string.IsNullOrWhiteSpace(_gameLoopService.CurrentStateName))
            {
                if (!_loggedGameLoopUsage)
                {
                    DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                        "[StateDependent] Integrado ao GameLoop: usando estado atual como fonte primária.");
                    _loggedGameLoopUsage = true;
                }

                return _gameLoopService.CurrentStateName switch
                {
                    nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                    nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                    nameof(GameLoopStateId.Boot) => ServiceState.Menu,
                    nameof(GameLoopStateId.Menu) => ServiceState.Menu,
                    _ => ServiceState.Menu
                };
            }

            return _state;
        }

        private bool IsPausedByGate(ActionType action)
        {
            TryResolveGateService();

            if (_gateService == null)
            {
                return false;
            }

            if (!_gateService.IsTokenActive(SimulationGateTokens.Pause))
            {
                _loggedGateBlock = false;
                return false;
            }

            var shouldBlock = action == ActionType.Move;
            if (shouldBlock && !_loggedGateBlock)
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    "[StateDependent] Action 'Move' bloqueada por gate Pause (SimulationGateTokens.Pause). Física permanece ativa (sem timeScale/constraints).");
                _loggedGateBlock = true;
            }

            return shouldBlock;
        }
    }
}
