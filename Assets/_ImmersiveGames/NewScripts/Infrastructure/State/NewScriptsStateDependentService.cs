using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.State
{
    /// <summary>
    /// Gate de ações baseado em:
    /// - SimulationGate (bloqueia quando gate fechado, ex.: transição/reset)
    /// - Pausa (token Pause e eventos de pausa)
    /// - Readiness (GameplayReady) para liberar ações quando o mundo + fluxo estão prontos
    /// - GameLoop (opcional): usado quando expõe estados que indicam "jogável" vs "não jogável"
    ///
    /// Ajuste importante:
    /// - Logs de "Move bloqueado/liberado" são emitidos SOMENTE quando a situação muda
    ///   E apenas após o primeiro consumo de CanExecuteAction(Move) (clean option).
    /// </summary>
    public sealed class NewScriptsStateDependentService : IStateDependentService
    {
        private enum ServiceState
        {
            Ready,
            Playing,
            Paused
        }

        private enum MoveDecision
        {
            Allowed,
            GateClosed,
            Paused,
            GameplayNotReady,
            NotPlaying
        }

        private ServiceState _state = ServiceState.Ready;

        private EventBinding<GameStartRequestedEvent> _gameStartRequestedBinding;
        private EventBinding<GamePauseCommandEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private EventBinding<GameExitToMenuRequestedEvent> _gameExitToMenuBinding;
        private EventBinding<GameResetRequestedEvent> _gameResetBinding;
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        // Readiness snapshot
        private bool _hasReadinessSnapshot;
        private bool _gameplayReady;

        // Move logging (only on transitions; clean option arms on first Move query)
        private bool _moveLoggingArmed;
        private bool _hasMoveDecision;
        private MoveDecision _lastMoveDecision = MoveDecision.Allowed;
        private ServiceState _lastResolvedState = ServiceState.Ready;
        private string _lastLoopStateName = string.Empty;

        public NewScriptsStateDependentService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveGameLoopService();
            TryRegisterEvents();

            // Clean option: NÃO logar nada no construtor (evita "Move bloqueada" no bootstrap).
        }

        public bool CanExecuteAction(ActionType action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            if (action == ActionType.Move)
            {
                _moveLoggingArmed = true;

                bool allowed = EvaluateMoveAllowed(out var decision, out var resolvedState, out string loopStateName);
                MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);
                return allowed;
            }

            bool infraAllows = EvaluateInfraAllowsAction(action);
            if (!infraAllows)
            {
                return false;
            }

            var state = ResolveServiceState();

            return state switch
            {
                ServiceState.Playing => true,

                ServiceState.Ready or ServiceState.Paused => action switch
                {
                    ActionType.Navigate => true,
                    ActionType.UiSubmit => true,
                    ActionType.UiCancel => true,
                    ActionType.RequestReset => true,
                    ActionType.RequestQuit => true,
                    _ => false
                },

                _ => false
            };
        }

        public bool IsGameActive()
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            if (!EvaluateInfraAllowsAction(ActionType.Move))
            {
                return false;
            }

            return ResolveServiceState() == ServiceState.Playing;
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
            {
                return;
            }

            EventBus<GameStartRequestedEvent>.Unregister(_gameStartRequestedBinding);
            EventBus<GamePauseCommandEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_gameExitToMenuBinding);
            EventBus<GameResetRequestedEvent>.Unregister(_gameResetBinding);
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
                _gameStartRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged();
                });

                _gamePauseBinding = new EventBinding<GamePauseCommandEvent>(evt =>
                {
                    OnGamePause(evt);
                    SyncMoveDecisionLogIfChanged();
                });

                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Playing);
                    SyncMoveDecisionLogIfChanged();
                });

                _gameExitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged();
                });

                _gameResetBinding = new EventBinding<GameResetRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged();
                });

                _readinessBinding = new EventBinding<ReadinessChangedEvent>(evt =>
                {
                    OnReadinessChanged(evt);
                    SyncMoveDecisionLogIfChanged();
                });

                EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
                EventBus<GameExitToMenuRequestedEvent>.Register(_gameExitToMenuBinding);
                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
                EventBus<ReadinessChangedEvent>.Register(_readinessBinding);

                _bindingsRegistered = true;
            }
            catch
            {
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseCommandEvent evt)
        {
            SetState(evt is { IsPaused: true } ? ServiceState.Paused : ServiceState.Playing);
        }

        private void OnReadinessChanged(ReadinessChangedEvent evt)
        {
            _hasReadinessSnapshot = true;
            _gameplayReady = evt.Snapshot.GameplayReady;
        }

        private void SetState(ServiceState next)
        {
            _state = next;
        }

        private ServiceState ResolveServiceState()
        {
            if (IsPausedOnlyByGate())
            {
                return ServiceState.Paused;
            }

            if (!IsInfraReady())
            {
                return ServiceState.Ready;
            }

            ServiceState? loopState = ResolveFromGameLoop();
            if (loopState.HasValue)
            {
                return loopState.Value;
            }

            return _state;
        }

        private ServiceState? ResolveFromGameLoop()
        {
            if (_gameLoopService == null)
            {
                return null;
            }

            string name = _gameLoopService.CurrentStateIdName;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return name switch
            {
                nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                nameof(GameLoopStateId.Ready) => ServiceState.Ready,
                nameof(GameLoopStateId.Boot) => ServiceState.Ready,
                nameof(GameLoopStateId.Pregame) => ServiceState.Ready,
                _ => null
            };
        }

        /// <summary>
        /// Pausa "pura" significa: Pause está ativo e é o ÚNICO token ativo.
        /// Isso permite classificar pause como "Paused" e não como "GateClosed" genérico.
        /// </summary>
        private bool IsPausedOnlyByGate()
        {
            TryResolveGateService();

            if (_gateService == null)
            {
                return false;
            }

            if (!_gateService.IsTokenActive(SimulationGateTokens.Pause))
            {
                return false;
            }

            return _gateService.ActiveTokenCount == 1;
        }

        /// <summary>
        /// Durante Pregame, o gate pode estar fechado somente pelo token de simulação gameplay.
        /// Nesse caso, devemos manter UI/menu habilitados.
        /// </summary>
        private bool IsGameplaySimulationOnlyByGate()
        {
            TryResolveGateService();

            if (_gateService == null)
            {
                return false;
            }

            if (!_gateService.IsTokenActive(SimulationGateTokens.GameplaySimulation))
            {
                return false;
            }

            return _gateService.ActiveTokenCount == 1;
        }

        private bool IsInfraReady()
        {
            if (_gateService is { IsOpen: false })
            {
                return false;
            }

            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                return false;
            }

            return true;
        }

        private bool EvaluateInfraAllowsAction(ActionType action)
        {
            if (IsPausedOnlyByGate())
            {
                return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel
                    or ActionType.RequestReset or ActionType.RequestQuit;
            }

            if (_gateService is { IsOpen: false })
            {
                if (IsGameplaySimulationOnlyByGate())
                {
                    return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel
                        or ActionType.RequestReset or ActionType.RequestQuit;
                }

                return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel;
            }

            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel;
            }

            return true;
        }

        private bool EvaluateMoveAllowed(out MoveDecision decision, out ServiceState resolvedState, out string loopStateName)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            resolvedState = ResolveServiceState();
            loopStateName = _gameLoopService?.CurrentStateIdName ?? string.Empty;

            if (_gateService is { IsOpen: false })
            {
                decision = IsPausedOnlyByGate() ? MoveDecision.Paused : MoveDecision.GateClosed;
                return false;
            }

            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                decision = MoveDecision.GameplayNotReady;
                return false;
            }

            if (resolvedState != ServiceState.Playing)
            {
                decision = MoveDecision.NotPlaying;
                return false;
            }

            decision = MoveDecision.Allowed;
            return true;
        }

        private void SyncMoveDecisionLogIfChanged()
        {
            // Clean option: se ninguém perguntou por Move ainda, não loga transições automáticas via eventos.
            if (!_moveLoggingArmed)
            {
                return;
            }

            _ = EvaluateMoveAllowed(out var decision, out var resolvedState, out string loopStateName);
            MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);
        }

        private void MaybeLogMoveDecisionTransition(
            MoveDecision decision,
            ServiceState resolvedState,
            string loopStateName,
            bool force = false)
        {
            // Clean option: só loga depois de armado.
            if (!_moveLoggingArmed)
            {
                return;
            }

            if (!force && _hasMoveDecision &&
                decision == _lastMoveDecision &&
                resolvedState == _lastResolvedState &&
                string.Equals(loopStateName, _lastLoopStateName, StringComparison.Ordinal))
            {
                return;
            }

            _hasMoveDecision = true;
            _lastMoveDecision = decision;
            _lastResolvedState = resolvedState;
            _lastLoopStateName = loopStateName ?? string.Empty;

            bool gateIsOpen = _gateService?.IsOpen ?? true;
            int activeTokens = _gateService?.ActiveTokenCount ?? 0;
            bool pausedOnly = IsPausedOnlyByGate();
            bool gameplayReady = !_hasReadinessSnapshot || _gameplayReady;

            DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                decision == MoveDecision.Allowed
                    ? $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens})."
                    : $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens}).");
        }
    }
}
