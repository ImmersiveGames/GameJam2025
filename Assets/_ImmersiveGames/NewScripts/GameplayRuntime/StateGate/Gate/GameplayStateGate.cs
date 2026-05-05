using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.LegacySimulationGate;
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
    /// - LegacySimulationGate (bloqueia quando gate fechado, ex.: transição/reset)
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
        private ILegacySimulationGateService _gateService;
        private IGameplayInteractionReadinessService _interactionReadinessService;

        private readonly GameplayStateSnapshot _snapshot = new();
        private readonly GameplayMoveGateDecisionLogger _moveGateDecisionLogger = new();
        private GameplayRuntimeSignalsAdapter _runtimeSignalsAdapter;
        private Action<bool> _gateChangedHandler;
        private Action<GameplayInteractionReadinessSnapshot> _interactionReadinessChangedHandler;
        private bool _gateEventsSubscribed;
        private bool _hasPublishedOperationalStateSnapshot;
        private GameplayOperationalStateSnapshot _lastPublishedOperationalStateSnapshot = GameplayOperationalStateSnapshot.Empty;

        public event Action<GameplayOperationalStateSnapshot> OperationalStateChanged;

        public GameplayStateGate(ILegacySimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveInteractionReadinessService();
            RegisterEvents();
            RegisterGateEvents();
            RegisterInteractionReadinessEvents();
            PrimeInteractionReadiness();
            PublishOperationalStateIfChanged("bootstrap");

            // Clean option: NÃO logar nada no construtor (evita "Move bloqueada" no bootstrap).
        }

        public bool CanExecuteGameplayAction(GameplayAction action)
        {
            TryResolveGateService();
            TryResolveInteractionReadinessService();

            if (action == GameplayAction.Move)
            {
                _moveGateDecisionLogger.Arm();

                bool allowed = _snapshot.EvaluateMoveAllowed(_gateService, out var decision, out var resolvedState, out string loopStateName);
                _moveGateDecisionLogger.LogIfChanged(_gateService, _snapshot, decision, resolvedState, loopStateName);
                return allowed;
            }

            if (!_snapshot.IsInfraReady(_gateService))
            {
                return false;
            }

            return _snapshot.ResolveServiceState(_gateService) == StateDependentServiceState.Playing;
        }

        public bool CanExecuteUiAction(UiAction action)
        {
            TryResolveGateService();
            TryResolveInteractionReadinessService();

            StateDependentServiceState state = _snapshot.ResolveServiceState(_gateService);
            return state == StateDependentServiceState.Playing || state == StateDependentServiceState.Ready || state == StateDependentServiceState.Paused;
        }

        public bool CanExecuteSystemAction(SystemAction action)
        {
            TryResolveGateService();
            TryResolveInteractionReadinessService();
            return true;
        }

        public bool IsGameActive()
        {
            TryResolveGateService();
            TryResolveInteractionReadinessService();

            if (!_snapshot.IsInfraReady(_gateService))
            {
                return false;
            }

            return _snapshot.ResolveServiceState(_gateService) == StateDependentServiceState.Playing;
        }

        public void Dispose()
        {
            UnregisterGateEvents();

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

            if (_gateService != null)
            {
                RegisterGateEvents();
                PublishOperationalStateIfChanged("simulation_gate_service_resolved");
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
                PublishOperationalStateIfChanged("interaction_readiness_service_resolved");
            }
        }

        private void RegisterEvents()
        {
            _runtimeSignalsAdapter = new GameplayRuntimeSignalsAdapter(
                HandleGameStartRequested,
                OnGameRunStarted,
                OnGameRunEnded,
                OnPauseStateChanged,
                OnGameResetRequested,
                OnReadinessChanged);

            _runtimeSignalsAdapter.TryRegister();
        }

        private void RegisterGateEvents()
        {
            if (_gateService == null || _gateChangedHandler != null || _gateEventsSubscribed)
            {
                return;
            }

            _gateChangedHandler = OnLegacySimulationGateChanged;
            _gateService.GateChanged += _gateChangedHandler;
            _gateEventsSubscribed = true;
        }

        private void UnregisterGateEvents()
        {
            if (_gateService == null || _gateChangedHandler == null || !_gateEventsSubscribed)
            {
                return;
            }

            _gateService.GateChanged -= _gateChangedHandler;
            _gateEventsSubscribed = false;
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
            _snapshot.ClearActiveLoopIdentity();
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("game_start_requested");
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!_snapshot.MatchesActiveLoopIdentity(evt.Identity))
            {
                DebugUtility.LogVerbose<GameplayStateGate>(
                    $"[OBS][GRS] GameRunStartedEvent ignorado por identidade fora do ciclo ativo expected='{_snapshot.DescribeActiveLoopIdentity()}' received='{evt.Identity?.Describe() ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _snapshot.SetActiveLoopIdentity(evt.Identity);
            _snapshot.SetGameRunStarted();
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("game_run_started");
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!_snapshot.MatchesActiveLoopIdentity(evt.Identity))
            {
                DebugUtility.LogVerbose<GameplayStateGate>(
                    $"[OBS][GRS] GameRunEndedEvent ignorado por identidade fora do ciclo ativo expected='{_snapshot.DescribeActiveLoopIdentity()}' received='{evt.Identity?.Describe() ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _snapshot.SetGameRunEnded();
            _snapshot.ClearActiveLoopIdentity();
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("game_run_ended");
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

            if (!_snapshot.MatchesActiveLoopIdentity(evt.Identity))
            {
                DebugUtility.LogVerbose<GameplayStateGate>(
                    $"[OBS][GRS] GameResetRequestedEvent ignorado por identidade fora do ciclo ativo expected='{_snapshot.DescribeActiveLoopIdentity()}' received='{evt.Identity?.Describe() ?? "<null>"}' reason='{reason}' frame={frame}.",
                    DebugUtility.Colors.Info);
                return;
            }

            _snapshot.SetGameRunEnded();
            _snapshot.ClearActiveLoopIdentity();
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("game_reset_requested");
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!_snapshot.MatchesActiveLoopIdentity(evt.Identity))
            {
                DebugUtility.LogVerbose<GameplayStateGate>(
                    $"[OBS][GRS] PauseStateChangedEvent ignorado por identidade fora do ciclo ativo expected='{_snapshot.DescribeActiveLoopIdentity()}' received='{evt.Identity?.Describe() ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<GameplayStateGate>(
                $"[OBS][GRS] PauseStateChangedEvent consumed consumer='{nameof(GameplayStateGate)}' isPaused='{evt.IsPaused}'.",
                DebugUtility.Colors.Info);

            _snapshot.SetState(evt.IsPaused ? StateDependentServiceState.Paused : StateDependentServiceState.Ready);
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("pause_state_changed");
        }

        private void OnReadinessChanged(ReadinessChangedEvent evt)
        {
            _snapshot.UpdateSceneReadiness(evt);
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("scene_readiness_changed");
        }


        private void OnGameplayInteractionReadinessChanged(GameplayInteractionReadinessSnapshot snapshot)
        {
            _snapshot.UpdateGameplayInteractionReadiness(snapshot);
            SyncMoveDecisionLogIfChanged();
            PublishOperationalStateIfChanged("gameplay_interaction_readiness_changed");
        }

        private void OnLegacySimulationGateChanged(bool isOpen)
        {
            DebugUtility.LogVerbose<GameplayStateGate>(
                $"[OBS][GRS] LegacySimulationGateChangedEvent consumed consumer='{nameof(GameplayStateGate)}' gateOpen='{isOpen.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);

            PublishOperationalStateIfChanged("simulation_gate_changed");
        }

        private void SyncMoveDecisionLogIfChanged()
        {
            if (!_moveGateDecisionLogger.IsArmed)
            {
                return;
            }

            _ = _snapshot.EvaluateMoveAllowed(_gateService, out var decision, out var resolvedState, out string loopStateName);
            _moveGateDecisionLogger.LogIfChanged(_gateService, _snapshot, decision, resolvedState, loopStateName);
        }

        private void PublishOperationalStateIfChanged(string reason)
        {
            GameplayOperationalStateSnapshot snapshot = BuildOperationalStateSnapshot(reason);

            if (_hasPublishedOperationalStateSnapshot && snapshot == _lastPublishedOperationalStateSnapshot)
            {
                return;
            }

            _lastPublishedOperationalStateSnapshot = snapshot;
            _hasPublishedOperationalStateSnapshot = true;

            DebugUtility.LogVerbose<GameplayStateGate>(
                $"[OBS][GRS][OperationalState] GameplayOperationalStateChanged gateOpen='{snapshot.GateOpen.ToString().ToLowerInvariant()}' sceneReady='{snapshot.SceneReady.ToString().ToLowerInvariant()}' actorsOperationalReady='{snapshot.ActorsOperationalReady.ToString().ToLowerInvariant()}' interactionReady='{snapshot.InteractionReady.ToString().ToLowerInvariant()}' gameRunStarted='{snapshot.GameRunStarted.ToString().ToLowerInvariant()}' paused='{snapshot.Paused.ToString().ToLowerInvariant()}' gameLoopState='{snapshot.GameLoopState}' activeTokens='{snapshot.ActiveTokens}' reason='{snapshot.Reason}' consumer='{nameof(GameplayStateGate)}'.",
                DebugUtility.Colors.Info);

            OperationalStateChanged?.Invoke(snapshot);
        }

        private GameplayOperationalStateSnapshot BuildOperationalStateSnapshot(string reason)
        {
            bool gateOpen = _gateService?.IsOpen ?? false;
            bool sceneReady = _snapshot.IsSceneGameplayReady;
            bool actorsOperationalReady = _snapshot.IsActorsOperationalReady;
            bool interactionReady = _snapshot.IsGameplayInteractionReady;
            bool gameRunStarted = _snapshot.HasGameRunStarted;
            string gameLoopState = string.Empty;
            bool paused = _snapshot.ResolveServiceState(_gateService) == StateDependentServiceState.Paused;
            int activeTokens = _gateService?.ActiveTokenCount ?? 0;

            return new GameplayOperationalStateSnapshot(
                gateOpen,
                sceneReady,
                actorsOperationalReady,
                interactionReady,
                gameRunStarted,
                paused,
                gameLoopState,
                activeTokens,
                reason);
        }
    }
}
