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
    /// - SimulationGate (bloqueia quando gate fechado, ex.: transição/reset)
    /// - Pausa (token Pause e eventos de pausa)
    /// - Readiness (GameplayReady) para liberar ações quando o mundo + fluxo estão prontos
    /// - GameLoop (opcional): usado apenas quando expõe um estado conclusivo (Playing/Paused)
    ///
    /// Ajuste importante:
    /// - Logs de "Move bloqueado/liberado" são emitidos SOMENTE quando a situação muda, evitando spam.
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
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        // Readiness snapshot
        private bool _hasReadinessSnapshot;
        private bool _gameplayReady;


        // Move logging (only on transitions)
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

            // Estado inicial de log (se alguém consultar CanExecuteAction cedo)
            SyncMoveDecisionLogIfChanged(forceLog: false);
        }

        public bool CanExecuteAction(ActionType action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            // Move tem logging de transição.
            if (action == ActionType.Move)
            {
                bool allowed = EvaluateMoveAllowed(out var decision, out var resolvedState, out string loopStateName);
                MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);
                return allowed;
            }

            // Para ações gerais, aplica as mesmas “travas infra” (gate/readiness/paused),
            // e então aplica o estado macro (Ready/Playing/Paused) como filtro final.
            bool infraAllows = EvaluateInfraAllowsAction(action);
            if (!infraAllows)
            {
                return false;
            }

            var state = ResolveServiceState();

            return state switch
            {
                ServiceState.Playing => true,

                // Em Ready/Paused permitimos apenas UI/Navegação/Reset/Quit.
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

            // “Ativo” precisa respeitar as travas infra e o estado macro.
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
                // Start é REQUEST. Não marca "Playing" aqui — isso é responsabilidade do Coordinator + GameLoop.
                _gameStartRequestedBinding = new EventBinding<GameStartRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _gamePauseBinding = new EventBinding<GamePauseCommandEvent>(evt =>
                {
                    OnGamePause(evt);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ =>
                {
                    // Resume é intenção; o estado efetivo é resolvido por Gate + GameLoop.
                    SetState(ServiceState.Playing);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _readinessBinding = new EventBinding<ReadinessChangedEvent>(evt =>
                {
                    OnReadinessChanged(evt);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
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
            // 1) Pausa explícita sempre ganha.
            if (IsPausedByGate())
            {
                return ServiceState.Paused;
            }

            // 2) Travamentos infra (gate fechado ou gameplay não pronto) impedem "Playing" efetivo.
            if (!IsInfraReady())
            {
                return ServiceState.Ready;
            }

            // 3) GameLoop é sinal adicional quando expõe estados conclusivos.
            ServiceState? loopState = ResolveFromGameLoop();
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

            string name = _gameLoopService.CurrentStateIdName;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            // Usa GameLoop apenas para estados que já significam "jogável" ou "pausado".
            return name switch
            {
                nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                nameof(GameLoopStateId.Ready) => ServiceState.Ready,
                _ => null
            };
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

        private bool IsInfraReady()
        {
            // Gate fechado: infra bloqueando (transição/reset/loading/etc).
            if (_gateService is { IsOpen: false })
            {
                return false;
            }

            // Readiness: se temos snapshot e diz "não pronto", bloqueia.
            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                return false;
            }

            return true;
        }

        private bool EvaluateInfraAllowsAction(ActionType action)
        {
            // 1) Gate fechado: em geral, bloqueia gameplay, mas pode permitir UI/navegação.
            if (_gateService is { IsOpen: false })
            {
                return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel;
            }

            // 2) Pausa: bloqueia gameplay, permite UI/navegação/reset/quit.
            if (IsPausedByGate())
            {
                return action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel
                    or ActionType.RequestReset or ActionType.RequestQuit;
            }

            // 3) Readiness: se não pronto, permite apenas UI/navegação (não gameplay).
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

            // 1) Gate sempre bloqueia Move quando fechado.
            if (_gateService is { IsOpen: false })
            {
                decision = MoveDecision.GateClosed;
                return false;
            }

            // 2) Pause sempre bloqueia Move.
            if (IsPausedByGate())
            {
                decision = MoveDecision.Paused;
                return false;
            }

            // 3) Readiness: se recebemos snapshot e ele diz "não pronto", bloqueia.
            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                decision = MoveDecision.GameplayNotReady;
                return false;
            }

            // 4) Precisa estar em Playing (Ready/Paused bloqueiam Move).
            if (resolvedState != ServiceState.Playing)
            {
                decision = MoveDecision.NotPlaying;
                return false;
            }

            decision = MoveDecision.Allowed;
            return true;
        }

        private void SyncMoveDecisionLogIfChanged(bool forceLog)
        {
            _ = EvaluateMoveAllowed(out var decision, out var resolvedState, out string loopStateName);

            if (forceLog)
            {
                MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName, force: true);
                return;
            }

            MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);
        }

        private void MaybeLogMoveDecisionTransition(
            MoveDecision decision,
            ServiceState resolvedState,
            string loopStateName,
            bool force = false)
        {
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
            bool paused = _gateService != null && _gateService.IsTokenActive(SimulationGateTokens.Pause);

            DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                decision == MoveDecision.Allowed ? $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={(!_hasReadinessSnapshot || _gameplayReady)}, paused={paused}, serviceState={resolvedState}, gameLoopState='{loopStateName}', activeTokens={activeTokens})."
                    : $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={(!_hasReadinessSnapshot || _gameplayReady)}, paused={paused}, serviceState={resolvedState}, gameLoopState='{loopStateName}', activeTokens={activeTokens}).");
        }
    }
}
