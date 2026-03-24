using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actions.States
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
    public sealed class StateDependentService : IStateDependentService
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
        private EventBinding<GameRunStartedEvent> _gameRunStartedBinding;
        private EventBinding<GameRunEndedEvent> _gameRunEndedBinding;
        private EventBinding<GamePauseCommandEvent> _gamePauseBinding;
        private EventBinding<GameResumeRequestedEvent> _gameResumeBinding;
        private EventBinding<GameResetRequestedEvent> _gameResetBinding;
        private EventBinding<ReadinessChangedEvent> _readinessBinding;

        private bool _bindingsRegistered;

        private ISimulationGateService _gateService;
        private IGameLoopService _gameLoopService;

        // Readiness snapshot
        private bool _hasReadinessSnapshot;
        private bool _gameplayReady;

        // Move logging (only on transitions; clean option arms on the first Move query)
        private bool _moveLoggingArmed;
        private bool _hasMoveDecision;
        private MoveDecision _lastMoveDecision = MoveDecision.Allowed;
        private ServiceState _lastResolvedState = ServiceState.Ready;
        private string _lastLoopStateName = string.Empty;

        private const float NonGameplayBlockedLogCooldownSeconds = 1f;
        private float _lastNonGameplayBlockedLogTimestamp = float.NegativeInfinity;
        private string _lastNonGameplayBlockedLogKey = string.Empty;
        private int _lastResetFrame = -1;
        private string _lastResetReason = string.Empty;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;

        public StateDependentService(ISimulationGateService gateService = null)
        {
            _gateService = gateService;

            TryResolveGateService();
            TryResolveGameLoopService();
            TryRegisterEvents();

            // Clean option: NÃO logar nada no construtor (evita "Move bloqueada" no bootstrap).
        }

        public bool CanExecuteGameplayAction(GameplayAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            if (action == GameplayAction.Move)
            {
                _moveLoggingArmed = true;

                bool allowed = EvaluateMoveAllowed(out var decision, out var resolvedState, out string loopStateName);
                MaybeLogMoveDecisionTransition(decision, resolvedState, loopStateName);
                return allowed;
            }

            if (!IsInfraReady())
            {
                return false;
            }

            return ResolveServiceState() == ServiceState.Playing;
        }

        public bool CanExecuteUiAction(UiAction action)
        {
            TryResolveGateService();
            TryResolveGameLoopService();

            var state = ResolveServiceState();
            return state == ServiceState.Playing || state == ServiceState.Ready || state == ServiceState.Paused;
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

            if (!IsInfraReady())
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
            EventBus<GameRunStartedEvent>.Unregister(_gameRunStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_gameRunEndedBinding);
            EventBus<GamePauseCommandEvent>.Unregister(_gamePauseBinding);
            EventBus<GameResumeRequestedEvent>.Unregister(_gameResumeBinding);
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

                _gameRunStartedBinding = new EventBinding<GameRunStartedEvent>(_ =>
                {
                    SetState(ServiceState.Playing);
                    SyncMoveDecisionLogIfChanged();
                });

                _gameRunEndedBinding = new EventBinding<GameRunEndedEvent>(_ =>
                {
                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged();
                });

                _gamePauseBinding = new EventBinding<GamePauseCommandEvent>(OnGamePauseEvent);

                _gameResumeBinding = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);

                _gameResetBinding = new EventBinding<GameResetRequestedEvent>(evt =>
                {
                    string reason = evt?.Reason ?? string.Empty;
                    int frame = Time.frameCount;
                    if (_lastResetFrame == frame && string.Equals(_lastResetReason, reason, StringComparison.Ordinal))
                    {
                        DebugUtility.LogVerbose<StateDependentService>(
                            $"[OBS][GRS] StateDependent reset dedupe event='GameResetRequestedEvent' reason='{reason}' frame={frame} reasonCode='duplicate_same_frame'.",
                            DebugUtility.Colors.Info);
                        return;
                    }

                    _lastResetFrame = frame;
                    _lastResetReason = reason;

                    SetState(ServiceState.Ready);
                    SyncMoveDecisionLogIfChanged();
                });

                _readinessBinding = new EventBinding<ReadinessChangedEvent>(evt =>
                {
                    OnReadinessChanged(evt);
                    SyncMoveDecisionLogIfChanged();
                });

                EventBus<GameStartRequestedEvent>.Register(_gameStartRequestedBinding);
                EventBus<GameRunStartedEvent>.Register(_gameRunStartedBinding);
                EventBus<GameRunEndedEvent>.Register(_gameRunEndedBinding);
                EventBus<GamePauseCommandEvent>.Register(_gamePauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_gameResumeBinding);
                EventBus<GameResetRequestedEvent>.Register(_gameResetBinding);
                EventBus<ReadinessChangedEvent>.Register(_readinessBinding);

                _bindingsRegistered = true;
            }
            catch
            {
                _bindingsRegistered = false;
            }
        }

        private void OnGamePauseEvent(GamePauseCommandEvent evt)
        {
            string key = BuildPauseKey(evt);
            int frame = Time.frameCount;
            if (_lastPauseFrame == frame && string.Equals(_lastPauseKey, key, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<StateDependentService>(
                    $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame consumer='StateDependentService' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastPauseFrame = frame;
            _lastPauseKey = key;
            DebugUtility.LogVerbose<StateDependentService>(
                $"[OBS][GRS] GamePauseCommandEvent consumed consumer='StateDependentService' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            OnGamePause(evt);
            SyncMoveDecisionLogIfChanged();
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            string key = BuildResumeKey(evt);
            int frame = Time.frameCount;
            if (_lastResumeFrame == frame && string.Equals(_lastResumeKey, key, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<StateDependentService>(
                    $"[OBS][GRS] GameResumeRequestedEvent dedupe_same_frame consumer='StateDependentService' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastResumeFrame = frame;
            _lastResumeKey = key;
            DebugUtility.LogVerbose<StateDependentService>(
                $"[OBS][GRS] GameResumeRequestedEvent consumed consumer='StateDependentService' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            SetState(ServiceState.Playing);
            SyncMoveDecisionLogIfChanged();
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

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason=<null>";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
        {
            return "resume|reason=<null>";
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
                nameof(GameLoopStateId.IntroStage) => ServiceState.Ready,
                nameof(GameLoopStateId.PostPlay) => ServiceState.Ready,
                _ => null
            };
        }

        /// <summary>
        /// Pausa "pura" significa: pause está ativo e é o ÚNICO token ativo.
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
            bool isNonGameplayContext = !gameplayReady || !string.Equals(_lastLoopStateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

            if (isNonGameplayContext && decision != MoveDecision.Allowed)
            {
                float now = Time.unscaledTime;
                string effectiveCauseKey = $"{decision}|gate={gateIsOpen}|paused={pausedOnly}|state={resolvedState}|loop={_lastLoopStateName}";
                bool sameCauseWithinCooldown =
                    string.Equals(effectiveCauseKey, _lastNonGameplayBlockedLogKey, StringComparison.Ordinal)
                    && (now - _lastNonGameplayBlockedLogTimestamp) < NonGameplayBlockedLogCooldownSeconds;

                if (sameCauseWithinCooldown)
                {
                    return;
                }

                _lastNonGameplayBlockedLogKey = effectiveCauseKey;
                _lastNonGameplayBlockedLogTimestamp = now;
            }
            else if (decision == MoveDecision.Allowed)
            {
                _lastNonGameplayBlockedLogKey = string.Empty;
                _lastNonGameplayBlockedLogTimestamp = float.NegativeInfinity;
            }

            DebugUtility.LogVerbose<StateDependentService>(
                decision == MoveDecision.Allowed
                    ? $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens})."
                    : $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens}).");
        }
    }
}







