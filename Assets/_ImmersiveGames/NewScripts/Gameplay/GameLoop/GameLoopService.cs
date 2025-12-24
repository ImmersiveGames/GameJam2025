using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço runtime que encapsula a GameLoopStateMachine sem dependências de DI ou MonoBehaviours.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopService : IGameLoopService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        public string CurrentStateName { get; private set; } = string.Empty;

        public void RequestStart() => _signals.MarkStart();
        public void RequestPause() => _signals.MarkPause();
        public void RequestResume() => _signals.MarkResume();
        public void RequestReset() => _signals.MarkReset();

        public void Initialize()
        {
            if (_initialized)
                return;

            _stateMachine = new GameLoopStateMachine(_signals, this);
            _initialized = true;

            DebugUtility.LogVerbose<GameLoopService>("[GameLoop] Initialize() concluído.");
        }

        public void Tick(float dt)
        {
            if (!_initialized || _stateMachine == null)
                return;

            _stateMachine.Update();
            _stateMachine.FixedUpdate();

            _signals.ResetTransientSignals();
        }

        public void Dispose()
        {
            _initialized = false;
            _stateMachine = null;
            CurrentStateName = string.Empty;
            _signals.ResetTransientSignals();

            DebugUtility.LogVerbose<GameLoopService>("[GameLoop] Dispose() concluído.");
        }

        public void OnStateEntered(GameLoopStateId stateId, bool isActive)
        {
            CurrentStateName = stateId.ToString();

            DebugUtility.LogVerbose<GameLoopService>(
                $"[GameLoop] State ENTER: {stateId} (isActive={isActive})");
        }

        public void OnStateExited(GameLoopStateId stateId)
        {
            DebugUtility.LogVerbose<GameLoopService>(
                $"[GameLoop] State EXIT: {stateId}");
        }

        public void OnGameActivityChanged(bool isActive)
        {
            DebugUtility.LogVerbose<GameLoopService>(
                $"[GameLoop] ActivityChanged: isActive={isActive}");
        }

        private sealed class MutableGameLoopSignals : IGameLoopSignals
        {
            public bool StartRequested { get; private set; }
            public bool PauseRequested { get; private set; }
            public bool ResumeRequested { get; private set; }
            public bool ResetRequested { get; private set; }

            public void MarkStart() => StartRequested = true;
            public void MarkPause() => PauseRequested = true;
            public void MarkResume() => ResumeRequested = true;
            public void MarkReset() => ResetRequested = true;

            public void ResetTransientSignals()
            {
                StartRequested = false;
                PauseRequested = false;
                ResumeRequested = false;
                ResetRequested = false;
            }
        }
    }
}
