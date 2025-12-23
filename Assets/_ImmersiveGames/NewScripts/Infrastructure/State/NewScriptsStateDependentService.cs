using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    /// <summary>
    /// Serviço mínimo para coordenar permissões de ações no baseline NewScripts.
    /// - Integra com GameLoop quando disponível (fonte primária).
    /// - Mantém fallback simples via eventos (fonte secundária).
    /// - Gate Pause (SimulationGateTokens.Pause) bloqueia Move sem alterar timeScale/física.
    /// </summary>
    public sealed class NewScriptsStateDependentService : IStateDependentService
    {
        private enum ServiceState
        {
            Menu,
            Playing,
            Paused
        }

        // Fallback começa em Menu (mais seguro que "Playing").
        private ServiceState _fallbackState = ServiceState.Menu;

        private EventBinding<GameStartEvent> _gameStartBinding;
        private EventBinding<GamePauseEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;

        private bool _bindingsRegistered;
        private bool _loggedGateBlock;
        private bool _loggedGameLoopUsage;

        private ISimulationGateService _gateService;

        public NewScriptsStateDependentService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            if (_gateService == null)
                DependencyManager.Provider.TryGetGlobal(out _gateService);

            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            if (IsPausedByGate(action))
                return false;

            var state = ResolveServiceState();

            switch (state)
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
            return ResolveServiceState() == ServiceState.Playing;
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
                return;

            EventBus<GameStartEvent>.Unregister(_gameStartBinding);
            EventBus<GamePauseEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);

            _bindingsRegistered = false;
        }

        private void TryRegisterEvents()
        {
            try
            {
                _gameStartBinding = new EventBinding<GameStartEvent>(_ => SetFallbackState(ServiceState.Playing));
                _gamePauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => SetFallbackState(ServiceState.Playing));

                EventBus<GameStartEvent>.Register(_gameStartBinding);
                EventBus<GamePauseEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);

                _bindingsRegistered = true;
            }
            catch
            {
                // EventBus não disponível: segue só com fallback local.
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            SetFallbackState(evt is { IsPaused: true } ? ServiceState.Paused : ServiceState.Playing);
        }

        private void SetFallbackState(ServiceState next)
        {
            _fallbackState = next;
        }

        private ServiceState ResolveServiceState()
        {
            // Resolve sob demanda para observar overrides no DI (QA/harness).
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var loop) &&
                loop != null &&
                !string.IsNullOrWhiteSpace(loop.CurrentStateName))
            {
                if (!_loggedGameLoopUsage)
                {
                    DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                        "[StateDependent] Integrado ao GameLoop: usando estado atual como fonte primária.");
                    _loggedGameLoopUsage = true;
                }

                return loop.CurrentStateName switch
                {
                    nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                    nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                    nameof(GameLoopStateId.Menu) => ServiceState.Menu,
                    nameof(GameLoopStateId.Boot) => ServiceState.Menu,
                    _ => ServiceState.Menu
                };
            }

            return _fallbackState;
        }

        private bool IsPausedByGate(ActionType action)
        {
            if (_gateService == null)
                return false;

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
