using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.RuntimeSignals;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.State.Gate
{
    /// <summary>
    /// Gate de ações baseado em:
    /// - SimulationGate (bloqueia quando gate fechado, ex.: transição/reset)
    /// - Pausa (token Pause e eventos de pausa)
    /// - Readiness (GameplayReady) para liberar ações quando o mundo + fluxo estão prontos
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
    public sealed class GameplayStateGate : IGameplayStateGate
    {
        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        private readonly GameplayStateSnapshot _snapshot = new();
        private readonly GameplayMoveGateDecisionLogger _moveGateDecisionLogger = new();
        private GameplayRuntimeSignalsAdapter _runtimeSignalsAdapter;

        public GameplayStateGate(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveGameLoopService();
            RegisterEvents();

            // Clean option: NÃO logar nada no construtor (evita "Move bloqueada" no bootstrap).
        }

        public bool CanExecuteGameplayAction(GameplayAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

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

            StateDependentServiceState state = _snapshot.ResolveServiceState(_gateService, _gameLoopService);
            return state == StateDependentServiceState.Playing || state == StateDependentServiceState.Ready || state == StateDependentServiceState.Paused;
        }

        public bool CanExecuteSystemAction(SystemAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();
            return true;
        }

        public bool IsGameActive()
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            if (!_snapshot.IsInfraReady(_gateService))
            {
                return false;
            }

            return _snapshot.ResolveServiceState(_gateService, _gameLoopService) == StateDependentServiceState.Playing;
        }

        public void Dispose()
        {
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

        private void HandleGameStartRequested()
        {
            _snapshot.SetState(StateDependentServiceState.Ready);
            SyncMoveDecisionLogIfChanged();
        }

        private void HandleGameRunStarted()
        {
            _snapshot.SetState(StateDependentServiceState.Playing);
            SyncMoveDecisionLogIfChanged();
        }

        private void HandleGameRunEnded()
        {
            _snapshot.SetState(StateDependentServiceState.Ready);
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

            _snapshot.SetState(StateDependentServiceState.Ready);
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
            _snapshot.UpdateReadiness(evt);
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
