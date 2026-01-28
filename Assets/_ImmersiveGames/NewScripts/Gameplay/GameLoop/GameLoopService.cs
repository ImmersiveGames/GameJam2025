using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Gameplay.PostGame;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        // Etapa 3: evita publicar GameRunStartedEvent em Resume (Paused -> Playing).
        private bool _runStartedEmittedThisRun;
        private GameLoopStateId _lastStateId = GameLoopStateId.Boot;

        public string CurrentStateIdName { get; private set; } = string.Empty;

        public void RequestStart()
        {
            if (_stateMachine != null && _stateMachine.IsGameActive)
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
            if (previousState == GameLoopStateId.PostPlay && stateId != GameLoopStateId.PostPlay)
            {
                HandlePostPlayExited(stateId);
            }

            CurrentStateIdName = stateId.ToString();
            var logStateName = GetLogStateName(stateId);
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] ENTER: {logStateName} (active={isActive})");

            if (stateId == GameLoopStateId.Boot && previousState != GameLoopStateId.Boot)
            {
                DebugUtility.Log<GameLoopService>(
                    "[GameLoop] Restart->Boot confirmado (reinício determinístico).",
                    DebugUtility.Colors.Info);
            }

            // Etapa 3: delimita o “início de run”.
            // Regras:
            // - Em Boot/Ready/PostPlay, consideramos que ainda não iniciou uma run ativa.
            // - Ao entrar em Playing pela primeira vez nesta run, publicamos GameRunStartedEvent.
            // - Em Resume (Paused -> Playing), NÃO publicamos de novo.
            if (stateId == GameLoopStateId.Boot ||
                stateId == GameLoopStateId.Ready ||
                stateId == GameLoopStateId.IntroStage ||
                stateId == GameLoopStateId.PostPlay)
            {
                _runStartedEmittedThisRun = false;
            }

            if (stateId == GameLoopStateId.IntroStage)
            {
                _signals.ClearIntroStagePending();
            }
            else if (stateId == GameLoopStateId.Boot ||
                     stateId == GameLoopStateId.Ready ||
                     stateId == GameLoopStateId.PostPlay)
            {
                if (!_signals.IntroStageRequested)
                {
                    _signals.ClearIntroStageFlags();
                }
            }

            if (stateId == GameLoopStateId.PostPlay)
            {
                HandlePostPlayEntered();
            }

            if (stateId == GameLoopStateId.Playing)
            {
                // Garantia: StartPending nunca deve ficar “colado” após entrar em Playing.
                _signals.ClearStartPending();
                _signals.ClearIntroStageFlags();

                ApplyGameplayInputMode();

                // Correção: se já emitimos nesta run (ex.: Paused -> Playing), apenas não publica novamente.
                // Importante: NÃO gerar log extra de "resume/duplicate" para evitar ruído no baseline/smoke.
                if (!_runStartedEmittedThisRun)
                {
                    _runStartedEmittedThisRun = true;
                    EventBus<GameRunStartedEvent>.Raise(new GameRunStartedEvent(stateId));
                }
            }

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

        private void HandlePostPlayEntered()
        {
            if (!IsGameplayScene())
            {
                DebugUtility.LogWarning<GameLoopService>(
                    $"[OBS][PostGame] PostGameSkipped reason='scene_not_gameplay' scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            var info = BuildSignatureInfo();
            var status = ResolveGameRunStatus();
            var outcome = status?.HasResult == true ? status.Outcome.ToString() : "Unknown";
            var reason = status?.HasResult == true ? status.Reason ?? "<null>" : "<none>";

            DebugUtility.Log<GameLoopService>(
                $"[OBS][PostGame] PostGameEntered signature='{info.Signature}' outcome='{outcome}' reason='{reason}' " +
                $"scene='{info.SceneName}' profile='{info.Profile}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            NotifyPostPlayOwnerEntered(info);
        }

        private void HandlePostPlayExited(GameLoopStateId nextState)
        {
            var info = BuildSignatureInfo();
            var reason = ResolvePostPlayExitReason(nextState);

            DebugUtility.Log<GameLoopService>(
                $"[OBS][PostGame] PostGameExited signature='{info.Signature}' reason='{reason}' nextState='{nextState}' " +
                $"scene='{info.SceneName}' profile='{info.Profile}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            NotifyPostPlayOwnerExited(info, nextState, reason);
        }

        private string ResolvePostPlayExitReason(GameLoopStateId nextState)
        {
            // Esta decisão precisa ser determinística e legível no log.
            // Como os sinais são limpos no fim do Tick, aqui ainda conseguimos
            // observar a intenção que causou a saída do PostPlay.

            // Prioridade: Reset/Reinício explícito.
            if (_signals.ResetRequested)
            {
                return "Restart";
            }

            // Ready é usado como estado-alvo “não ativo” durante navegações.
            // Em PostPlay, ReadyRequested normalmente vem de ExitToMenu.
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

        private void NotifyPostPlayOwnerEntered(SignatureInfo info)
        {
            var owner = ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostPlayEntered(new PostPlayOwnershipContext(
                info.Signature,
                info.SceneName,
                info.Profile,
                info.Frame));
        }

        private void NotifyPostPlayOwnerExited(SignatureInfo info, GameLoopStateId nextState, string reason)
        {
            var owner = ResolvePostPlayOwnershipService();
            if (owner == null || !owner.IsOwnerEnabled)
            {
                return;
            }

            owner.OnPostPlayExited(new PostPlayOwnershipExitContext(
                info.Signature,
                info.SceneName,
                info.Profile,
                info.Frame,
                reason,
                nextState.ToString()));
        }

        private void ApplyGameplayInputMode()
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                return;
            }

            var info = BuildSignatureInfo();
            DebugUtility.Log<GameLoopService>(
                $"[OBS][InputMode] Apply mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' " +
                $"signature='{info.Signature}' scene='{info.SceneName}' profile='{info.Profile}' frame={info.Frame}.",
                DebugUtility.Colors.Info);

            inputMode.SetGameplay("GameLoop/Playing");
        }

        private static IInputModeService ResolveInputModeService()
        {
            return DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service)
                ? service
                : null;
        }

        private static IPostPlayOwnershipService ResolvePostPlayOwnershipService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPostPlayOwnershipService>(out var service)
                ? service
                : null;
        }

        private static IGameRunStatusService ResolveGameRunStatus()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var status)
                ? status
                : null;
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
            var sceneName = SceneManager.GetActiveScene().name;
            var profile = "gameplay";
            var signature = "<none>";

            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) && cache != null &&
                cache.TryGetLast(out var cachedSignature, out var cachedProfile, out var cachedScene))
            {
                signature = string.IsNullOrWhiteSpace(cachedSignature) ? "<none>" : cachedSignature.Trim();
                if (!string.IsNullOrWhiteSpace(cachedScene))
                {
                    sceneName = cachedScene;
                }

                if (cachedProfile.IsValid)
                {
                    profile = cachedProfile.Value;
                }
            }

            return new SignatureInfo(signature, sceneName, profile, Time.frameCount);
        }

        private static string GetLogStateName(GameLoopStateId stateId)
        {
            return stateId == GameLoopStateId.PostPlay ? "PostGame" : stateId.ToString();
        }

        private readonly struct SignatureInfo
        {
            public string Signature { get; }
            public string SceneName { get; }
            public string Profile { get; }
            public int Frame { get; }

            public SignatureInfo(string signature, string sceneName, string profile, int frame)
            {
                Signature = string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();
                SceneName = string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
                Profile = string.IsNullOrWhiteSpace(profile) ? "<none>" : profile.Trim();
                Frame = frame;
            }
        }

        private sealed class MutableGameLoopSignals : IGameLoopSignals
        {
            private bool _startPending;
            private bool _introStagePending;
            private bool _introStageCompleted;

            public bool StartRequested => _startPending;
            public bool PauseRequested { get; private set; }
            public bool ResumeRequested { get; private set; }
            public bool ReadyRequested { get; private set; }
            public bool ResetRequested { get; private set; }
            public bool EndRequested { get; private set; }
            public bool IntroStageRequested => _introStagePending;
            public bool IntroStageCompleted => _introStageCompleted;

            bool IGameLoopSignals.EndRequested
            {
                get => EndRequested;
                set => EndRequested = value;
            }

            public void MarkStart() => _startPending = true;
            public void MarkPause() => PauseRequested = true;
            public void MarkResume() => ResumeRequested = true;
            public void MarkReady() => ReadyRequested = true;
            public void MarkReset() => ResetRequested = true;
            public void MarkEnd() => EndRequested = true;
            public void ClearStartPending() => _startPending = false;
            public void MarkIntroStageStart() => _introStagePending = true;
            public void MarkIntroStageComplete() => _introStageCompleted = true;
            public void ClearIntroStagePending() => _introStagePending = false;
            public void ClearIntroStageFlags()
            {
                _introStagePending = false;
                _introStageCompleted = false;
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
