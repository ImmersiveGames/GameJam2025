using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Flow;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Core
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        private bool _runStartedEmittedThisRun;
        private GameLoopStateId _lastStateId = GameLoopStateId.Boot;
        private readonly GameLoopStateTransitionEffects _stateTransitionEffects;

        public GameLoopService()
        {
            _stateTransitionEffects = new GameLoopStateTransitionEffects(new GameLoopPostGameSnapshotResolver());
        }

        public string CurrentStateIdName { get; private set; } = string.Empty;
        public void RequestStart()
        {
            if (_signals.IntroStageRequested && !_signals.IntroStageCompleted)
            {
                DebugUtility.LogVerbose<GameLoopService>(
                    $"[GameLoop] RequestStart ignored (intro stage pending). state={_stateMachine?.Current}.");
                return;
            }

            if (_stateMachine is { IsGameActive: true })
            {
                DebugUtility.LogVerbose<GameLoopService>(
                    $"[GameLoop] RequestStart ignored (already active). state={_stateMachine.Current}.");
                return;
            }

            _signals.MarkStart();
        }

        public void RequestPause() => _signals.MarkPause();
        public void RequestResume() => _signals.MarkResume();
        public void RequestReady() => _signals.MarkReady();
        public void RequestRearm() => _signals.MarkRearm();
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
                    RequestResume();
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
                    or GameLoopStateId.IntroStage
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
        public void RequestIntroStageStart() => _signals.MarkIntroStageStart();
        public void RequestIntroStageComplete() => _signals.MarkIntroStageComplete();

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

            _runStartedEmittedThisRun = false;

            _signals.ClearStartPending();
            _signals.ClearIntroStageFlags();
            _signals.ResetTransientSignals();
        }

        public void OnStateEntered(GameLoopStateId stateId, bool isActive)
        {
            var previousState = _lastStateId;
            HandlePostPlayExitIfNeeded(previousState, stateId);
            UpdateCurrentState(stateId, isActive, previousState);
            EventBus<GameLoopStateEnteredEvent>.Raise(new GameLoopStateEnteredEvent(stateId, isActive));
            UpdateRunStartedFlag(stateId);
            SyncIntroStageFlags(stateId);
            HandlePostPlayEnterIfNeeded(stateId);
            HandlePlayingEnteredIfNeeded(stateId);
            _lastStateId = stateId;
        }

        public void OnStateExited(GameLoopStateId stateId) =>
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] EXIT: {GetLogStateName(stateId)}");

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
                stateId == GameLoopStateId.IntroStage ||
                stateId == GameLoopStateId.PostPlay)
            {
                _runStartedEmittedThisRun = false;
            }
        }

        private void SyncIntroStageFlags(GameLoopStateId stateId)
        {
            if (stateId == GameLoopStateId.IntroStage)
            {
                _signals.ClearIntroStagePending();
                return;
            }

            if (stateId == GameLoopStateId.Boot ||
                stateId == GameLoopStateId.Ready ||
                stateId == GameLoopStateId.PostPlay)
            {
                if (!_signals.IntroStageRequested)
                {
                    _signals.ClearIntroStageFlags();
                }
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

            _signals.ClearStartPending();
            _signals.ClearIntroStageFlags();

            _stateTransitionEffects.ApplyGameplayInputMode();

            if (!_runStartedEmittedThisRun)
            {
                _runStartedEmittedThisRun = true;
                EventBus<GameRunStartedEvent>.Raise(new GameRunStartedEvent(stateId));
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
            public bool RearmRequested { get; private set; }
            public bool ResetRequested { get; private set; }
            public bool EndRequested { get; private set; }
            public bool IntroStageRequested { get; private set; }
            public bool IntroStageCompleted { get; private set; }

            bool IGameLoopSignals.EndRequested
            {
                get => EndRequested;
                set => EndRequested = value;
            }

            public void MarkStart() => StartRequested = true;
            public void MarkPause() => PauseRequested = true;
            public void MarkResume() => ResumeRequested = true;
            public void MarkReady() => ReadyRequested = true;
            public void MarkRearm() => RearmRequested = true;
            public void MarkReset() => ResetRequested = true;
            public void MarkEnd() => EndRequested = true;
            public void ClearStartPending() => StartRequested = false;
            public void MarkIntroStageStart() => IntroStageRequested = true;
            public void MarkIntroStageComplete() => IntroStageCompleted = true;
            public void ClearIntroStagePending() => IntroStageRequested = false;
            public void ClearIntroStageFlags()
            {
                IntroStageRequested = false;
                IntroStageCompleted = false;
            }

            public void ResetTransientSignals()
            {
                PauseRequested = false;
                ResumeRequested = false;
                ReadyRequested = false;
                RearmRequested = false;
                ResetRequested = false;
                EndRequested = false;
            }
        }
    }
}
