using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Services
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        private bool _runStartedEmittedThisRun;
        private GameLoopStateId _lastStateId = GameLoopStateId.Boot;

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
        public void RequestReset() => _signals.MarkReset();
        public void RequestEnd() => _signals.MarkEnd();
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
                HandlePostPlayEntered();
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

            ApplyGameplayInputMode();

            if (!_runStartedEmittedThisRun)
            {
                _runStartedEmittedThisRun = true;
                EventBus<GameRunStartedEvent>.Raise(new GameRunStartedEvent(stateId));
            }
        }

        private void HandlePostPlayEntered()
        {
            if (!IsGameplayScene())
            {
                DebugUtility.LogWarning<GameLoopService>(
                    $"[OBS][PostGame] PostGameSkipped reason='scene_not_gameplay' scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            var info = BuildSignatureInfo();
            var resultSnapshot = ResolvePostGameSnapshot();

            DebugUtility.Log<GameLoopService>(
                $"[OBS][PostGame] PostGameEntered signature='{info.Signature}' result='{resultSnapshot.Result}' reason='{resultSnapshot.Reason}' scene='{info.SceneName}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            NotifyPostPlayOwnerEntered(info, resultSnapshot);
        }

        private void HandlePostPlayExited(GameLoopStateId nextState)
        {
            var info = BuildSignatureInfo();
            string reason = ResolvePostPlayExitReason(nextState);
            var resultSnapshot = ResolvePostGameSnapshot();

            DebugUtility.Log<GameLoopService>(
                $"[OBS][PostGame] PostGameExited signature='{info.Signature}' reason='{reason}' nextState='{nextState}' scene='{info.SceneName}' frame={info.Frame} result='{resultSnapshot.Result}'.",
                DebugUtility.Colors.Info);

            NotifyPostPlayOwnerExited(info, nextState, reason, resultSnapshot);
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

        private void NotifyPostPlayOwnerEntered(SignatureInfo info, PostGameSnapshot resultSnapshot)
        {
            var owner = ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostGameEntered(new PostGameOwnershipContext(
                info.Signature,
                info.SceneName,
                string.Empty,
                info.Frame,
                resultSnapshot.Result,
                resultSnapshot.Reason));
        }

        private void NotifyPostPlayOwnerExited(SignatureInfo info, GameLoopStateId nextState, string reason, PostGameSnapshot resultSnapshot)
        {
            var owner = ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostGameExited(new PostGameOwnershipExitContext(
                info.Signature,
                info.SceneName,
                string.Empty,
                info.Frame,
                reason,
                nextState.ToString(),
                resultSnapshot.Result));
        }

        private void ApplyGameplayInputMode()
        {
            var info = BuildSignatureInfo();
            DebugUtility.Log<GameLoopService>(
                $"[OBS][InputMode] Request mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' signature='{info.Signature}' scene='{info.SceneName}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.Gameplay, "GameLoop/Playing", "GameLoop", info.Signature));
        }

        private static IPostGameOwnershipService ResolvePostPlayOwnershipService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var service)
                ? service
                : null;
        }

        private static IPostGameResultService ResolvePostGameResultService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var service)
                ? service
                : null;
        }

        private static IGameRunStateService ResolveGameRunStatus()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameRunStateService>(out var status)
                ? status
                : null;
        }

        private static PostGameSnapshot ResolvePostGameSnapshot()
        {
            var postGameResult = ResolvePostGameResultService();
            if (postGameResult != null && postGameResult.HasResult)
            {
                return new PostGameSnapshot(postGameResult.Result, NormalizeValue(postGameResult.Reason));
            }

            var status = ResolveGameRunStatus();
            if (status?.HasResult == true)
            {
                PostGameResult fallbackResult = status.Outcome switch
                {
                    GameRunOutcome.Victory => PostGameResult.Victory,
                    GameRunOutcome.Defeat => PostGameResult.Defeat,
                    _ => PostGameResult.None,
                };

                return new PostGameSnapshot(fallbackResult, NormalizeValue(status.Reason));
            }

            return new PostGameSnapshot(PostGameResult.None, "<none>");
        }

        private static bool IsGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) && classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return string.Equals(SceneManager.GetActiveScene().name, "GameplayScene", System.StringComparison.Ordinal);
        }

        private static SignatureInfo BuildSignatureInfo()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string signature = "<none>";

            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) && cache != null &&
                cache.TryGetLast(out string cachedSignature, out string cachedScene))
            {
                signature = string.IsNullOrWhiteSpace(cachedSignature) ? "<none>" : cachedSignature.Trim();
                if (!string.IsNullOrWhiteSpace(cachedScene))
                {
                    sceneName = cachedScene;
                }
            }

            return new SignatureInfo(signature, sceneName, Time.frameCount);
        }

        private static string GetLogStateName(GameLoopStateId stateId)
        {
            return stateId == GameLoopStateId.PostPlay ? "PostGame" : stateId.ToString();
        }

        private static string NormalizeValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private readonly struct SignatureInfo
        {
            public string Signature { get; }
            public string SceneName { get; }
            public int Frame { get; }

            public SignatureInfo(string signature, string sceneName, int frame)
            {
                Signature = string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();
                SceneName = string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
                Frame = frame;
            }
        }

        private readonly struct PostGameSnapshot
        {
            public PostGameSnapshot(PostGameResult result, string reason)
            {
                Result = result;
                Reason = string.IsNullOrWhiteSpace(reason) ? "<none>" : reason.Trim();
            }

            public PostGameResult Result { get; }
            public string Reason { get; }
        }

        private sealed class MutableGameLoopSignals : IGameLoopSignals
        {
            public bool StartRequested { get; private set; }
            public bool PauseRequested { get; private set; }
            public bool ResumeRequested { get; private set; }
            public bool ReadyRequested { get; private set; }
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
                ResetRequested = false;
                EndRequested = false;
            }
        }
    }
}
