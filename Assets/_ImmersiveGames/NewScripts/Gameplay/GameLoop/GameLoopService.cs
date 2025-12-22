using System;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço runtime que encapsula a GameLoopStateMachine sem dependências de DI ou MonoBehaviours.
    /// </summary>
    public class GameLoopService : IGameLoopService, IGameLoopStateObserver
    {
        private readonly MutableGameLoopSignals _signals = new();
        private GameLoopStateMachine _stateMachine;
        private bool _initialized;

        public string CurrentStateName { get; private set; } = string.Empty;

        /// <summary>
        /// Marca a intenção de iniciar o jogo.
        /// </summary>
        public void RequestStart() => _signals.MarkStart();

        /// <summary>
        /// Marca a intenção de pausar o jogo.
        /// </summary>
        public void RequestPause() => _signals.MarkPause();

        /// <summary>
        /// Marca a intenção de retomar o jogo.
        /// </summary>
        public void RequestResume() => _signals.MarkResume();

        /// <summary>
        /// Marca a intenção de resetar o loop.
        /// </summary>
        public void RequestReset() => _signals.MarkReset();

        public void Initialize()
        {
            if (_initialized)
                return;

            _stateMachine = new GameLoopStateMachine(_signals, this);
            _initialized = true;
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
        }

        public void OnStateEntered(GameLoopStateId stateId, bool isActive)
        {
            CurrentStateName = stateId.ToString();
        }

        public void OnStateExited(GameLoopStateId stateId)
        {
            // Nenhuma ação adicional necessária para o esqueleto runtime.
        }

        public void OnGameActivityChanged(bool isActive)
        {
            // Mantido para compatibilidade futura (ex.: telemetria).
        }

        private class MutableGameLoopSignals : IGameLoopSignals
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
