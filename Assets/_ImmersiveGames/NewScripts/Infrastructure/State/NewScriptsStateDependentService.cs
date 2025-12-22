/*
 * ChangeLog
 * - Gate de pause (SimulationGateTokens.Pause) agora bloqueia ActionType.Move via IStateDependentService sem congelar física/timeScale.
 */
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;

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
        private bool _loggedGateBlock;
        private bool _loggedGameLoopUsage;

        private readonly ISimulationGateService _gateService;
        private readonly IGameLoopService _gameLoopService;

        public NewScriptsStateDependentService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            if (_gateService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _gateService);
            }

            if (DependencyManager.Provider.TryGetGlobal(out IGameLoopService gameLoopService))
            {
                _gameLoopService = gameLoopService;
            }

            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            if (IsPausedByGate(action))
            {
                return false;
            }

            var serviceState = ResolveServiceState();

            switch (serviceState)
            {
                case ServiceState.Playing:
                    return action switch
                    {
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
                    _ => ServiceState.Menu
                };
            }

            return _state;
        }

        private bool IsPausedByGate(ActionType action)
        {
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
