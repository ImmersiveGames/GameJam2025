using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Flow;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Core
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IPauseStateService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private readonly EventBinding<LevelIntroCompletedEvent> _levelIntroCompletedBinding;
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        private bool _runStartedEmittedThisRun;
        private GameLoopStateId _lastStateId = GameLoopStateId.Boot;
        private readonly GameLoopStateTransitionEffects _stateTransitionEffects;
        private string _pendingPauseWillEnterReason;
        private string _pendingPauseWillExitReason;

        public GameLoopService()
        {
            _stateTransitionEffects = new GameLoopStateTransitionEffects(new GameLoopPostGameSnapshotResolver());
            _levelIntroCompletedBinding = new EventBinding<LevelIntroCompletedEvent>(OnLevelIntroCompleted);
            EventBus<LevelIntroCompletedEvent>.Register(_levelIntroCompletedBinding);
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
                "[OBS][Gameplay] RequestStart accepted for slice rail 'Gameplay -> Level -> EnterStage -> Playing'.",
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

        public void RequestSceneFlowCompletionSync(SceneRouteKind routeKind)
        {
            var currentState = _stateMachine?.Current ?? GameLoopStateId.Boot;

            if (routeKind == SceneRouteKind.Gameplay)
            {
                if (currentState == GameLoopStateId.Boot)
                {
                    DebugUtility.LogVerbose<GameLoopService>(
                        "[GameLoop] SceneFlow completion sync: gameplay em Boot -> RequestReady().",
                        DebugUtility.Colors.Info);
                    RequestReady();
                    return;
                }

                if (currentState == GameLoopStateId.Paused)
                {
                    DebugUtility.LogVerbose<GameLoopService>(
                        "[GameLoop] SceneFlow completion sync: gameplay em Paused -> RequestResume().",
                        DebugUtility.Colors.Info);
                    RequestResume("GameLoop/SceneFlowCompletionSync/GameplayPaused");
                    return;
                }

                DebugUtility.LogVerbose<GameLoopService>(
                    $"[GameLoop] SceneFlow completion sync: gameplay em '{currentState}' -> no-op.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (routeKind == SceneRouteKind.Frontend)
            {
                if (currentState is GameLoopStateId.Playing
                    or GameLoopStateId.Paused
                    or GameLoopStateId.PostPlay)
                {
                    DebugUtility.LogVerbose<GameLoopService>(
                        $"[GameLoop] SceneFlow completion sync: frontend em '{currentState}' -> RequestReady().",
                        DebugUtility.Colors.Info);
                    RequestReady();
                    return;
                }

                DebugUtility.LogVerbose<GameLoopService>(
                    $"[GameLoop] SceneFlow completion sync: frontend em '{currentState}' -> no-op.",
                    DebugUtility.Colors.Info);
            }
        }

        public void RequestReset() => _signals.MarkReset();
        public void RequestRunEnd() => _signals.MarkEnd();

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
            EventBus<LevelIntroCompletedEvent>.Unregister(_levelIntroCompletedBinding);
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
            HandlePostPlayExitIfNeeded(previousState, stateId);
            UpdateCurrentState(stateId, isActive, previousState);
            PublishPauseStateChangedIfNeeded(previousState, stateId);
            UpdateRunStartedFlag(stateId);
            HandlePostPlayEnterIfNeeded(stateId);
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

        private void OnLevelIntroCompleted(LevelIntroCompletedEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                return;
            }

            if (_stateMachine is { IsGameActive: true })
            {
                return;
            }

            DebugUtility.Log<GameLoopService>(
                $"[OBS][Gameplay] LevelHandoffAccepted source='{evt.Source}' levelRef='{evt.Session.LevelRef.name}' rail='Gameplay -> Level -> EnterStage -> Playing' skipped={evt.WasSkipped.ToString().ToLowerInvariant()} reason='{evt.Reason}' state='{CurrentStateIdName}'.",
                DebugUtility.Colors.Info);

            RequestStart();
        }

        private void HandlePostPlayExitIfNeeded(GameLoopStateId previousState, GameLoopStateId nextState)
        {
            if (previousState == GameLoopStateId.PostPlay && nextState != GameLoopStateId.PostPlay)
            {
                HandlePostPlayExited(nextState);
            }
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
            if (stateId == GameLoopStateId.Boot ||
                stateId == GameLoopStateId.Ready ||
                stateId == GameLoopStateId.PostPlay)
            {
                _runStartedEmittedThisRun = false;
            }
        }

        private void HandlePostPlayEnterIfNeeded(GameLoopStateId stateId)
        {
            if (stateId == GameLoopStateId.PostPlay)
            {
                _stateTransitionEffects.HandlePostPlayEntered();
            }
        }

        private void HandlePlayingEnteredIfNeeded(GameLoopStateId stateId)
        {
            if (stateId != GameLoopStateId.Playing)
            {
                return;
            }

            DebugUtility.Log<GameLoopService>(
                "[OBS][Gameplay] PlayingEntered state='Playing' rail='Gameplay -> Level -> EnterStage -> Playing'.",
                DebugUtility.Colors.Success);

            _signals.ClearStartPending();

            _stateTransitionEffects.ApplyGameplayInputMode();

            if (!_runStartedEmittedThisRun)
            {
                _runStartedEmittedThisRun = true;
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

        private void HandlePostPlayExited(GameLoopStateId nextState)
        {
            string reason = ResolvePostPlayExitReason(nextState);
            _stateTransitionEffects.HandlePostPlayExited(nextState, reason);
        }

        private string ResolvePostPlayExitReason(GameLoopStateId nextState)
        {
            if (_signals.ResetRequested)
            {
                return "Restart";
            }

            if (_signals.ReadyRequested)
            {
                return "ExitToMenu";
            }

            if (_signals.StartRequested)
            {
                return "RunStarted";
            }

            return nextState switch
            {
                GameLoopStateId.Playing => "RunStarted",
                GameLoopStateId.Ready => "Ready",
                GameLoopStateId.Boot => "Boot",
                _ => "Unknown"
            };
        }

        private static string GetLogStateName(GameLoopStateId stateId)
        {
            return stateId == GameLoopStateId.PostPlay ? "PostGame" : stateId.ToString();
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
