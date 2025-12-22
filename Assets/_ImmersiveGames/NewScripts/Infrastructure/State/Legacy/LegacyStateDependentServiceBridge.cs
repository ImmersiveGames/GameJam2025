// TEMP: Legacy bridge. Remove after NewScripts FSM is implemented (NS-FSM-001).
/*
 * ChangeLog
 * - Gate de pause agora bloqueia ActionType.Move (e qualquer Look futuro) via SimulationGateTokens.Pause sem congelar física.
 */
using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;

namespace _ImmersiveGames.NewScripts.Infrastructure.State.Legacy
{
    /// <summary>
    /// Serviço mínimo para coordenar permissões de ações no baseline NewScripts.
    /// Inicia em Playing e troca de estado conforme eventos globais (se disponíveis).
    /// DependencyManager deve chamar Dispose para serviços globais; se o lifecycle mudar, garantir liberação manual.
    /// </summary>
    public sealed class LegacyStateDependentServiceBridge : IStateDependentService
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
        private bool _loggedFallback;
        private bool _loggedGateBlock;

        private readonly ISimulationGateService _gateService;

        public LegacyStateDependentServiceBridge()
        {
            DependencyManager.Provider.TryGetGlobal(out _gateService);
            TryRegisterEvents();
        }

        public bool CanExecuteAction(ActionType action)
        {
            if (IsPausedByGate(action))
            {
                return false;
            }

            switch (_state)
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
            if (_bindingsRegistered)
            {
                return;
            }

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
            catch (Exception ex)
            {
                if (!_loggedFallback)
                {
                    DebugUtility.LogVerbose(
                        typeof(LegacyStateDependentServiceBridge),
                        $"[LegacyBridge] EventBus indisponível; operando sem bindings ({ex.GetType().Name}).");
                    _loggedFallback = true;
                }

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

            var shouldBlock = action == ActionType.Move || action.ToString().Equals("Look", StringComparison.OrdinalIgnoreCase);
            if (shouldBlock && !_loggedGateBlock)
            {
                DebugUtility.LogVerbose(
                    typeof(LegacyStateDependentServiceBridge),
                    $"[LegacyBridge] Action '{action}' bloqueada por gate Pause (SimulationGateTokens.Pause). Física não é congelada.");
                _loggedGateBlock = true;
            }

            return shouldBlock;
        }
    }
}
