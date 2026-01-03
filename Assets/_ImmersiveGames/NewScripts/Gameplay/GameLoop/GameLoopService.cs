using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

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
            _signals.ResetTransientSignals();
        }

        public void OnStateEntered(GameLoopStateId stateId, bool isActive)
        {
            CurrentStateIdName = stateId.ToString();
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] ENTER: {stateId} (active={isActive})");

            // Etapa 3: delimita o “início de run”.
            // Regras:
            // - Em Boot/Ready/PostPlay, consideramos que ainda não iniciou uma run ativa.
            // - Ao entrar em Playing pela primeira vez nesta run, publicamos GameRunStartedEvent.
            // - Em Resume (Paused -> Playing), NÃO publicamos de novo.
            if (stateId == GameLoopStateId.Boot ||
                stateId == GameLoopStateId.Ready ||
                stateId == GameLoopStateId.PostPlay)
            {
                _runStartedEmittedThisRun = false;
            }

            if (stateId == GameLoopStateId.Playing)
            {
                // Garantia: StartPending nunca deve ficar “colado” após entrar em Playing.
                _signals.ClearStartPending();

                // Correção: se já emitimos nesta run (ex.: Paused -> Playing), apenas não publica novamente.
                // Importante: NÃO gerar log extra de "resume/duplicate" para evitar ruído no baseline/smoke.
                if (_runStartedEmittedThisRun)
                {
                    return;
                }

                _runStartedEmittedThisRun = true;
                EventBus<GameRunStartedEvent>.Raise(new GameRunStartedEvent(stateId));
            }
        }

        public void OnStateExited(GameLoopStateId stateId) =>
            DebugUtility.LogVerbose<GameLoopService>($"[GameLoop] EXIT: {stateId}");

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

        private sealed class MutableGameLoopSignals : IGameLoopSignals
        {
            private bool _startPending;

            public bool StartRequested => _startPending;
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

            public void MarkStart() => _startPending = true;
            public void MarkPause() => PauseRequested = true;
            public void MarkResume() => ResumeRequested = true;
            public void MarkReady() => ReadyRequested = true;
            public void MarkReset() => ResetRequested = true;
            public void MarkEnd() => EndRequested = true;
            public void ClearStartPending() => _startPending = false;

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
