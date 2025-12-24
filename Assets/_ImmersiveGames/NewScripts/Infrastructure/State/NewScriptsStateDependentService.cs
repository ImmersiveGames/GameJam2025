using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    /// <summary>
    /// Gate de ações baseado em:
    /// - SimulationGate (bloqueia Move quando gate fechado, ex.: transição/reset)
    /// - Pausa (token Pause e eventos de pausa)
    /// - Readiness (GameplayReady) para liberar Move quando o mundo + fluxo estão prontos
    /// - GameLoop (opcional): usado apenas quando expõe um estado "conclusivo" (Playing/Paused)
    /// </summary>
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
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;
        private bool _loggedGateBlock;
        private bool _loggedGameLoopUsage;
        private bool _loggedReadinessUsage;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        private bool _gameplayReady;
        private bool _gateOpen = true;

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

            if (IsBlockedByGate(action))
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
            EventBus<ReadinessChangedEvent>.Unregister(_readinessBinding);

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
                _readinessBinding = new EventBinding<ReadinessChangedEvent>(OnReadinessChanged);

                EventBus<GameStartEvent>.Register(_gameStartBinding);
                EventBus<GamePauseEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
                EventBus<ReadinessChangedEvent>.Register(_readinessBinding);

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

        private void OnReadinessChanged(ReadinessChangedEvent evt)
        {
            _gameplayReady = evt.Snapshot.GameplayReady;
            _gateOpen = evt.Snapshot.GateOpen;

            if (!_loggedReadinessUsage)
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    "[StateDependent] Integrado ao Readiness: usando GameplayReady/GateOpen como sinais de liberação de gameplay.");
                _loggedReadinessUsage = true;
            }
        }

        private void SetState(ServiceState next)
        {
            _state = next;
        }

        private ServiceState ResolveServiceState()
        {
            // 1) Se pausa explícita (via token/evento), prioriza Paused.
            if (IsPausedByGate())
            {
                return ServiceState.Paused;
            }

            // 2) Readiness + eventos determinam Playing.
            // Evita ficar preso em "Menu" quando o GameLoop ainda não tickou para atualizar CurrentStateName.
            if (_state == ServiceState.Playing && _gameplayReady)
            {
                return ServiceState.Playing;
            }

            // 3) GameLoop é um sinal adicional, mas só quando expõe estados conclusivos.
            var loopState = ResolveFromGameLoop();
            if (loopState.HasValue)
            {
                return loopState.Value;
            }

            // 4) Fallback: estado interno.
            return _state;
        }

        private ServiceState? ResolveFromGameLoop()
        {
            if (_gameLoopService == null)
            {
                return null;
            }

            var name = _gameLoopService.CurrentStateName;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            // Usa GameLoop apenas para estados que já significam "jogável" ou "pausado".
            // Estados intermediários (Boot/Menu) NÃO devem derrubar um Playing já sinalizado por eventos/readiness.
            ServiceState? resolved = name switch
            {
                nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                _ => null
            };

            if (resolved.HasValue && !_loggedGameLoopUsage)
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    "[StateDependent] Integrado ao GameLoop: detectado estado conclusivo (Playing/Paused) como reforço.");
                _loggedGameLoopUsage = true;
            }

            return resolved;
        }

        private bool IsBlockedByGate(ActionType action)
        {
            TryResolveGateService();

            if (_gateService == null)
            {
                return false;
            }

            // Bloqueia Move sempre que o gate estiver fechado (independente do token específico).
            // Isso cobre transição de cena, reset do mundo, etc.
            if (!_gateService.IsOpen && action == ActionType.Move)
            {
                if (!_loggedGateBlock)
                {
                    DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                        $"[StateDependent] Action 'Move' bloqueada: SimulationGate fechado (activeTokens={_gateService.ActiveTokenCount}).");
                    _loggedGateBlock = true;
                }

                return true;
            }

            _loggedGateBlock = false;
            return false;
        }

        private bool IsPausedByGate()
        {
            TryResolveGateService();

            if (_gateService == null)
            {
                return false;
            }

            return _gateService.IsTokenActive(SimulationGateTokens.Pause);
        }
    }
}
