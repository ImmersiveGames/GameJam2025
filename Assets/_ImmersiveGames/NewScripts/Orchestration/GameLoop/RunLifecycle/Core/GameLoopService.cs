using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IPauseStateService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        private bool _runStartedEmittedThisRun;
        private GameLoopStateId _lastStateId = GameLoopStateId.Boot;
        private readonly GameLoopStateTransitionEffects _stateTransitionEffects;
        private string _pendingPauseWillEnterReason;
        private string _pendingPauseWillExitReason;

        public GameLoopService()
        {
            _stateTransitionEffects = new GameLoopStateTransitionEffects();
        }

        public string CurrentStateIdName { get; private set; } = string.Empty;
        public bool IsPaused => string.Equals(CurrentStateIdName, nameof(GameLoopStateId.Paused), StringComparison.Ordinal);

        public void RequestStart()
        {
            if (_stateMachine is { IsGameActive: true })
            {
                DebugUtility.LogVerbose<GameLoopService>(
                    $"[GameLoop] RequestStart ignored (already active). state={_stateMachine.Current}.");
                return;
            }

            DebugUtility.Log<GameLoopService>(
                "[OBS][GameLoop][Operational] RequestStart accepted for compatibility rail 'GameplaySessionFlow -> Level -> EnterStage -> Playing' handshake='GameLoopIntroStageBridge'.",
                DebugUtility.Colors.Info);

            _signals.MarkStart();
        }

        public void RequestPause(string reason = null)
        {
            if (_stateMachine == null || _stateMachine.Current != GameLoopStateId.Playing)
            {
                _pendingPauseWillEnterReason = null;
                return;
            }

            _pendingPauseWillEnterReason = reason;
            _signals.MarkPause();
        }

        public void RequestResume(string reason = null)
        {
            if (_stateMachine == null || _stateMachine.Current != GameLoopStateId.Paused)
            {
                _pendingPauseWillExitReason = null;
                return;
            }

            _pendingPauseWillExitReason = reason;
            _signals.MarkResume();
        }
        public void RequestReady() => _signals.MarkReady();

        public void RequestReset() => _signals.MarkReset();
        public void RequestRunEnd()
        {
            DebugUtility.LogVerbose<GameLoopService>(
                $"[OBS][ExitStage] GameRunEndRequested accepted state='{CurrentStateIdName}' handshake='GameRunEndedEventBridge'.");

            _signals.MarkEnd();
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _stateMachine = new GameLoopStateMachine(_signals, this);
            _initialized = true;

            DebugUtility.LogVerbose<GameLoopService>("[GameLoop] Initialized.");
        }

        public void Tick(float dt)
        {
            if (!_initialized || _stateMachine == null)
            {
                return;
            }

            _stateMachine.Update();
            _signals.ResetTransientSignals();
        }

        public void Dispose()
        {
            _initialized = false;
            _stateMachine = null;
            CurrentStateIdName = string.Empty;
            _pendingPauseWillEnterReason = null;
            _pendingPauseWillExitReason = null;

            _runStartedEmittedThisRun = false;

            _signals.ClearStartPending();
            _signals.ResetTransientSignals();
        }

        public void OnStateEntered(GameLoopStateId stateId, bool isActive)
        {
            var previousState = _lastStateId;
            UpdateCurrentState(stateId, isActive, previousState);
            PublishPauseStateChangedIfNeeded(previousState, stateId);
            UpdateRunStartedFlag(stateId);
            HandlePlayingEnteredIfNeeded(stateId);
            _lastStateId = stateId;
        }

        public void OnStateExited(GameLoopStateId stateId)
        {
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] EXIT: {GetLogStateName(stateId)}");
            PublishPauseWillChangeIfNeeded(stateId);
        }

        public void OnGameActivityChanged(bool isActive)
        {
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] Activity: {isActive}");

            var currentState = GameLoopStateId.Boot;
            if (_stateMachine != null)
            {
                currentState = _stateMachine.Current;
            }

            EventBus<GameLoopActivityChangedEvent>.Raise(
                new GameLoopActivityChangedEvent(currentState, isActive));
        }

        private void UpdateCurrentState(GameLoopStateId stateId, bool isActive, GameLoopStateId previousState)
        {
            CurrentStateIdName = stateId.ToString();

            string logStateName = GetLogStateName(stateId);
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] ENTER: {logStateName} (active={isActive})");

            if (stateId == GameLoopStateId.Boot && previousState != GameLoopStateId.Boot)
            {
                DebugUtility.Log<GameLoopService>(
                    "[GameLoop] Restart->Boot confirmado (reinicio deterministico).",
                    DebugUtility.Colors.Info);
            }
        }

        private void UpdateRunStartedFlag(GameLoopStateId stateId)
        {
            if (stateId.IsPreGameplayState() || stateId.IsTerminalRunState())
            {
                _runStartedEmittedThisRun = false;
            }
        }

        private void HandlePlayingEnteredIfNeeded(GameLoopStateId stateId)
        {
            if (!stateId.IsActiveGameplayState())
            {
                return;
            }

            // Boundary note: Playing is the operational release signal of the loop; semantic ownership lives above this layer.
            DebugUtility.Log<GameLoopService>(
                "[OBS][GameLoop][Operational] PlayingEntered state='Playing' rail='GameplaySessionFlow -> Level -> EnterStage -> Playing' canonical='operational_release'.",
                DebugUtility.Colors.Success);

            _signals.ClearStartPending();

            _stateTransitionEffects.ApplyGameplayInputMode();

            if (!_runStartedEmittedThisRun)
            {
                _runStartedEmittedThisRun = true;
                DebugUtility.Log<GameLoopService>(
                    "[OBS][GameLoop][Operational] RunStartPublished state='Playing' handshake='GameplaySessionFlow' canonical='run_start'.",
                    DebugUtility.Colors.Info);
                EventBus<GameRunStartedEvent>.Raise(new GameRunStartedEvent(stateId));
            }
        }

        private static void PublishPauseStateChangedIfNeeded(GameLoopStateId previousState, GameLoopStateId nextState)
        {
            if (nextState == GameLoopStateId.Paused)
            {
                DebugUtility.LogVerbose<GameLoopService>(
                    $"[Pause] PauseStateChangedEvent raised isPaused='true' previous='{previousState}' next='{nextState}'.",
                    DebugUtility.Colors.Info);
                EventBus<PauseStateChangedEvent>.Raise(new PauseStateChangedEvent(true));
                return;
            }

            if (previousState == GameLoopStateId.Paused)
            {
                DebugUtility.LogVerbose<GameLoopService>(
                    $"[Pause] PauseStateChangedEvent raised isPaused='false' previous='{previousState}' next='{nextState}'.",
                    DebugUtility.Colors.Info);
                EventBus<PauseStateChangedEvent>.Raise(new PauseStateChangedEvent(false));
            }
        }

        private void PublishPauseWillChangeIfNeeded(GameLoopStateId previousState)
        {
            if (previousState == GameLoopStateId.Playing && !string.IsNullOrWhiteSpace(_pendingPauseWillEnterReason))
            {
                string reason = _pendingPauseWillEnterReason;
                _pendingPauseWillEnterReason = null;

                DebugUtility.LogVerbose<GameLoopService>(
                    $"[Pause] PauseWillEnterEvent raised reason='{GameLoopReasonFormatter.Format(reason)}' previous='{previousState}' phase='before_final_state_change'.",
                    DebugUtility.Colors.Info);
                EventBus<PauseWillEnterEvent>.Raise(new PauseWillEnterEvent(reason));
                return;
            }

            if (previousState == GameLoopStateId.Paused && !string.IsNullOrWhiteSpace(_pendingPauseWillExitReason))
            {
                string reason = _pendingPauseWillExitReason;
                _pendingPauseWillExitReason = null;

                DebugUtility.LogVerbose<GameLoopService>(
                    $"[Pause] PauseWillExitEvent raised reason='{GameLoopReasonFormatter.Format(reason)}' previous='{previousState}' phase='before_final_state_change'.",
                    DebugUtility.Colors.Info);
                EventBus<PauseWillExitEvent>.Raise(new PauseWillExitEvent(reason));
            }
        }

        private static string GetLogStateName(GameLoopStateId stateId)
        {
            return stateId.ToString();
        }

        private sealed class MutableGameLoopSignals : IGameLoopSignals
        {
            public bool StartRequested { get; private set; }
            public bool PauseRequested { get; private set; }
            public bool ResumeRequested { get; private set; }
            public bool ReadyRequested { get; private set; }
            public bool ResetRequested { get; private set; }
            public bool EndRequested { get; private set; }

            bool IGameLoopSignals.EndRequested
            {
                get => EndRequested;
                set => EndRequested = value;
            }

            public void MarkStart() => StartRequested = true;
            public void MarkPause() => PauseRequested = true;
            public void MarkResume() => ResumeRequested = true;
            public void MarkReady() => ReadyRequested = true;
            public void MarkReset() => ResetRequested = true;
            public void MarkEnd() => EndRequested = true;
            public void ClearStartPending() => StartRequested = false;

            public void ResetTransientSignals()
            {
                PauseRequested = false;
                ResumeRequested = false;
                ReadyRequested = false;
                ResetRequested = false;
                EndRequested = false;
            }
        }
    }
}
