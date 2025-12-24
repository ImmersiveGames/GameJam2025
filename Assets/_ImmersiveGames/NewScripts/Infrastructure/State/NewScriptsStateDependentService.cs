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
    ///
    /// Ajuste importante:
    /// - Logs de "Move bloqueado/liberado" são emitidos SOMENTE quando o status muda, evitando spam.
    /// </summary>
    public sealed class NewScriptsStateDependentService : IStateDependentService, IDisposable
    {
        private enum ServiceState
        {
            Menu,
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

        private ServiceState _state = ServiceState.Menu;

        private EventBinding<GameStartEvent> _gameStartBinding;
        private EventBinding<GamePauseEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        // Readiness snapshot
        private bool _hasReadinessSnapshot;
        private bool _gameplayReady;
        private bool _gateOpen = true;

        // Move logging (only on transitions)
        private bool _hasMoveDecision;
        private MoveDecision _lastMoveDecision = MoveDecision.Allowed;
        private ServiceState _lastResolvedState = ServiceState.Menu;
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

            if (action == ActionType.Move)
            {
                var allowed = EvaluateMoveAllowed(out var decision, out var resolvedState, out var loopStateName);

                // Loga apenas quando o status mudar (ou na primeira decisão, se desejado).
                MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);

                return allowed;
            }

            var serviceState = ResolveServiceState();

            return serviceState switch
            {
                ServiceState.Playing => true,

                ServiceState.Menu or ServiceState.Paused => action switch
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
            return ResolveServiceState() == ServiceState.Playing;
        }

        public void Dispose()
        {
            if (!_bindingsRegistered)
                return;

            EventBus<GameStartEvent>.Unregister(_gameStartBinding);
            EventBus<GamePauseEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);
            EventBus<ReadinessChangedEvent>.Unregister(_readinessBinding);

            _bindingsRegistered = false;
        }

        private void TryResolveGateService()
        {
            if (_gateService != null)
                return;

            DependencyManager.Provider.TryGetGlobal(out _gateService);
        }

        private void TryResolveGameLoopService()
        {
            if (_gameLoopService != null)
                return;

            DependencyManager.Provider.TryGetGlobal(out _gameLoopService);
        }

        private void TryRegisterEvents()
        {
            try
            {
                _gameStartBinding = new EventBinding<GameStartEvent>(_ =>
                {
                    SetState(ServiceState.Playing);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _gamePauseBinding = new EventBinding<GamePauseEvent>(evt =>
                {
                    OnGamePause(evt);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(_ =>
                {
                    SetState(ServiceState.Playing);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

                _readinessBinding = new EventBinding<ReadinessChangedEvent>(evt =>
                {
                    OnReadinessChanged(evt);
                    SyncMoveDecisionLogIfChanged(forceLog: false);
                });

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
            _hasReadinessSnapshot = true;
            _gameplayReady = evt.Snapshot.GameplayReady;
            _gateOpen = evt.Snapshot.GateOpen;
        }

        private void SetState(ServiceState next)
        {
            _state = next;
        }

        private ServiceState ResolveServiceState()
        {
            // 1) Se pausa explícita (via token/evento), prioriza Paused.
            if (IsPausedByGate())
                return ServiceState.Paused;

            // 2) Readiness + eventos determinam Playing.
            // Evita ficar preso em "Menu" quando o GameLoop ainda não tickou para atualizar CurrentStateName.
            if (_state == ServiceState.Playing && (_hasReadinessSnapshot ? _gameplayReady : true))
                return ServiceState.Playing;

            // 3) GameLoop é um sinal adicional, mas só quando expõe estados conclusivos.
            var loopState = ResolveFromGameLoop();
            if (loopState.HasValue)
                return loopState.Value;

            // 4) Fallback: estado interno.
            return _state;
        }

        private ServiceState? ResolveFromGameLoop()
        {
            if (_gameLoopService == null)
                return null;

            var name = _gameLoopService.CurrentStateName;
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Usa GameLoop apenas para estados que já significam "jogável" ou "pausado".
            return name switch
            {
                nameof(GameLoopStateId.Playing) => ServiceState.Playing,
                nameof(GameLoopStateId.Paused) => ServiceState.Paused,
                _ => null
            };
        }

        private bool IsPausedByGate()
        {
            TryResolveGateService();

            if (_gateService == null)
                return false;

            return _gateService.IsTokenActive(SimulationGateTokens.Pause);
        }

        private bool EvaluateMoveAllowed(out MoveDecision decision, out ServiceState resolvedState, out string loopStateName)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            resolvedState = ResolveServiceState();
            loopStateName = _gameLoopService?.CurrentStateName ?? string.Empty;

            // 1) Gate sempre bloqueia Move quando fechado.
            if (_gateService != null && !_gateService.IsOpen)
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

            // 4) Precisa estar em Playing (Menu/Paused bloqueiam Move).
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
            // Avalia e loga se mudou; usado em callbacks para reduzir spam.
            _ = EvaluateMoveAllowed(out var decision, out var resolvedState, out var loopStateName);

            if (forceLog)
            {
                // Força log do estado atual (normalmente desnecessário).
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

            var gateIsOpen = _gateService?.IsOpen ?? true;
            var activeTokens = _gateService?.ActiveTokenCount ?? 0;
            var paused = _gateService != null && _gateService.IsTokenActive(SimulationGateTokens.Pause);

            // Mantém o texto estável e informativo para você bater com log facilmente.
            if (decision == MoveDecision.Allowed)
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={(_hasReadinessSnapshot ? _gameplayReady : true)}, paused={paused}, serviceState={resolvedState}, gameLoopState='{loopStateName}', activeTokens={activeTokens}).");
            }
            else
            {
                DebugUtility.LogVerbose<NewScriptsStateDependentService>(
                    $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={(_hasReadinessSnapshot ? _gameplayReady : true)}, paused={paused}, serviceState={resolvedState}, gameLoopState='{loopStateName}', activeTokens={activeTokens}).");
            }
        }
    }
}
