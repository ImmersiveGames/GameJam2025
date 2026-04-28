using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.RuntimeSignals;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Gate
{
    /// <summary>
    /// Gate de ações baseado em:
    /// - SimulationGate (bloqueia quando gate fechado, ex.: transição/reset)
    /// - Pausa (token Pause e eventos de pausa)
    /// - Readiness técnica de SceneFlow
    /// - Readiness operacional canônica de ActorsExecution
    /// - GameLoop (opcional): usado quando expõe estados que indicam "jogável" vs "não jogável"
    ///
    /// Boundary:
    /// - Este gate consome readiness/gate e eventos de reset de run.
    /// - Não decide policy de reset e não publica intenção de reset.
    ///
    /// Ajuste importante:
    /// - Logs de "Move bloqueado/liberado" são emitidos SOMENTE quando a situação muda
    ///   E apenas após o primeiro consumo de CanExecuteAction(Move) (clean option).
    /// </summary>
    // Boundary note: this gate is a consumer of readiness and loop state.
    // It does not own the final gameplay-release signal.
    public sealed class GameplayStateGate : IGameplayStateGate
    {
        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;
        private IGameplayInteractionReadinessService _interactionReadinessService;

        private readonly GameplayStateSnapshot _snapshot = new();
        private readonly GameplayMoveGateDecisionLogger _moveGateDecisionLogger = new();
        private GameplayRuntimeSignalsAdapter _runtimeSignalsAdapter;
        private Action<GameplayInteractionReadinessSnapshot> _interactionReadinessChangedHandler;

        public GameplayStateGate(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveGameLoopService();
            TryResolveInteractionReadinessService();
            RegisterEvents();
            RegisterInteractionReadinessEvents();
            PrimeInteractionReadiness();

            // Clean option: NÃO logar nada no construtor (evita "Move bloqueada" no bootstrap).
        }

        public bool CanExecuteGameplayAction(GameplayAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();
            TryResolveInteractionReadinessService();

            if (action == GameplayAction.Move)
            {
                _moveGateDecisionLogger.Arm();

                bool allowed = _snapshot.EvaluateMoveAllowed(_gateService, _gameLoopService, out var decision, out var resolvedState, out string loopStateName);
                _moveGateDecisionLogger.LogIfChanged(_gateService, _snapshot, decision, resolvedState, loopStateName);
                return allowed;
            }

            if (!_snapshot.IsInfraReady(_gateService))
            {
                return false;
            }

            return _snapshot.ResolveServiceState(_gateService, _gameLoopService) == StateDependentServiceState.Playing;
        }

        public bool CanExecuteUiAction(UiAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();
            TryResolveInteractionReadinessService();

            StateDependentServiceState state = _snapshot.ResolveServiceState(_gateService, _gameLoopService);
            return state == StateDependentServiceState.Playing || state == StateDependentServiceState.Ready || state == StateDependentServiceState.Paused;
        }

        public bool CanExecuteSystemAction(SystemAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();
            TryResolveInteractionReadinessService();
            return true;
        }

        public bool IsGameActive()
        {
            TryResolveGateService();
            TryResolveGameLoopService();
            TryResolveInteractionReadinessService();

            if (!_snapshot.IsInfraReady(_gateService))
            {
                return false;
            }

            return _snapshot.ResolveServiceState(_gateService, _gameLoopService) == StateDependentServiceState.Playing;
        }

        public void Dispose()
        {
            if (_interactionReadinessService != null && _interactionReadinessChangedHandler != null)
            {
                _interactionReadinessService.Changed -= _interactionReadinessChangedHandler;
            }

            _runtimeSignalsAdapter?.Dispose();
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

            if (_gameLoopService == null)
            {
                return;
            }

            if (string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal))
            {
                _snapshot.SetGameRunStarted();
            }
            else if (string.Equals(_gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Paused), StringComparison.Ordinal))
            {
                _snapshot.SetState(StateDependentServiceState.Paused);
            }
        }

        private void TryResolveInteractionReadinessService()
        {
            if (_interactionReadinessService != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal(out _interactionReadinessService);

            if (_interactionReadinessService != null)
            {
                RegisterInteractionReadinessEvents();
                PrimeInteractionReadiness();
            }
        }

        private void RegisterEvents()
        {
            _runtimeSignalsAdapter = new GameplayRuntimeSignalsAdapter(
                HandleGameStartRequested,
                HandleGameRunStarted,
                HandleGameRunEnded,
                OnPauseStateChanged,
                OnGameResetRequested,
                OnReadinessChanged);

            _runtimeSignalsAdapter.TryRegister();
        }

        private void RegisterInteractionReadinessEvents()
        {
            if (_interactionReadinessService == null || _interactionReadinessChangedHandler != null)
            {
                return;
            }

            _interactionReadinessChangedHandler = OnGameplayInteractionReadinessChanged;
            _interactionReadinessService.Changed += _interactionReadinessChangedHandler;
        }

        private void PrimeInteractionReadiness()
        {
            if (_interactionReadinessService == null)
            {
                return;
            }

            if (_interactionReadinessService.TryGetCurrent(out var snapshot))
            {
                _snapshot.UpdateGameplayInteractionReadiness(snapshot);
            }
        }

        private void HandleGameStartRequested()
        {
            _snapshot.SetGameRunEnded();
            SyncMoveDecisionLogIfChanged();
        }

        private void HandleGameRunStarted()
        {
            _snapshot.SetGameRunStarted();
            SyncMoveDecisionLogIfChanged();
        }

        private void HandleGameRunEnded()
        {
            _snapshot.SetGameRunEnded();
            SyncMoveDecisionLogIfChanged();
        }

        private void OnGameResetRequested(GameResetRequestedEvent evt)
        {
            string reason = evt?.Reason ?? string.Empty;
            int frame = Time.frameCount;
            if (!_snapshot.TryConsumeReset(reason, frame))
            {
                DebugUtility.LogVerbose<GameplayStateGate>(
                    $"[OBS][GRS] StateDependent reset dedupe consumer='GameplayStateGate' event='GameResetRequestedEvent' reason='{reason}' frame={frame} reasonCode='duplicate_same_frame'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _snapshot.SetGameRunEnded();
            SyncMoveDecisionLogIfChanged();
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            DebugUtility.LogVerbose<GameplayStateGate>(
                $"[OBS][GRS] PauseStateChangedEvent consumed consumer='{nameof(GameplayStateGate)}' isPaused='{evt.IsPaused}'.",
                DebugUtility.Colors.Info);

            _snapshot.SetState(evt.IsPaused ? StateDependentServiceState.Paused : StateDependentServiceState.Ready);
            SyncMoveDecisionLogIfChanged();
        }

        private void OnReadinessChanged(ReadinessChangedEvent evt)
        {
            _snapshot.UpdateSceneReadiness(evt);
            SyncMoveDecisionLogIfChanged();
        }

        private void OnGameplayInteractionReadinessChanged(GameplayInteractionReadinessSnapshot snapshot)
        {
            _snapshot.UpdateGameplayInteractionReadiness(snapshot);
            SyncMoveDecisionLogIfChanged();
        }

        private void SyncMoveDecisionLogIfChanged()
        {
            if (!_moveGateDecisionLogger.IsArmed)
            {
                return;
            }

            _ = _snapshot.EvaluateMoveAllowed(_gateService, _gameLoopService, out var decision, out var resolvedState, out string loopStateName);
            _moveGateDecisionLogger.LogIfChanged(_gateService, _snapshot, decision, resolvedState, loopStateName);
        }
    }
}
