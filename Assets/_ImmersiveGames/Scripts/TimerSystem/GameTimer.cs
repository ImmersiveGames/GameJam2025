using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityUtils;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    [DefaultExecutionOrder(-90), DebugLevel(DebugLevel.Logs)]
    public class GameTimer : Singleton<GameTimer>
    {
        private CountdownTimer _countdownTimer;
        private float _gameDurationSeconds = 300f; // Duração do jogo em segundos (5 minutos)
        private float _lastKnownRemainingTime;
        private bool _hasActiveSession;
        private bool _isResettingTimer;

        private GameManager _gameManager;

        public float RemainingTime => Mathf.Max(_lastKnownRemainingTime, 0f);

        public float ConfiguredDuration => _gameDurationSeconds;

        public bool HasActiveSession => _hasActiveSession;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        protected override void Awake()
        {
            base.Awake();
            _gameManager = GameManager.Instance;
            _gameDurationSeconds = Mathf.Max(0f, _gameManager.GameConfig.timerGame);

            EnsureTimerInstance(_gameDurationSeconds);
            _lastKnownRemainingTime = _gameDurationSeconds;
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(StartGameTimer);
            EventBus<GameStartEvent>.Register(_startBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(OnGameOver);
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(OnVictory);
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(PauseTimerHandler);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _stateChangedBinding = new EventBinding<StateChangedEvent>(HandleStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedBinding);
        }

        private void OnDisable()
        {
            if (_startBinding != null)
            {
                EventBus<GameStartEvent>.Unregister(_startBinding);
                _startBinding = null;
            }

            if (_gameOverBinding != null)
            {
                EventBus<GameOverEvent>.Unregister(_gameOverBinding);
                _gameOverBinding = null;
            }

            if (_victoryBinding != null)
            {
                EventBus<GameVictoryEvent>.Unregister(_victoryBinding);
                _victoryBinding = null;
            }

            if (_pauseBinding != null)
            {
                EventBus<GamePauseEvent>.Unregister(_pauseBinding);
                _pauseBinding = null;
            }

            if (_stateChangedBinding != null)
            {
                EventBus<StateChangedEvent>.Unregister(_stateChangedBinding);
                _stateChangedBinding = null;
            }
        }

        private void PauseTimerHandler(GamePauseEvent evt)
        {
            if (evt.IsPaused)
            {
                PauseTimer();
            }
            else
            {
                ResumeTimer();
            }
        }

        private void HandleTimerStart()
        {
            _hasActiveSession = true;
            DebugUtility.LogVerbose<GameTimer>($"Jogo iniciado! Timer de {_gameDurationSeconds} segundo(s) começou.");
        }

        private void StartGameTimer()
        {
            float configuredSeconds = ResolveConfiguredDuration();
            EnsureTimerInstance(configuredSeconds);

            _isResettingTimer = true;
            _countdownTimer.Stop();
            _countdownTimer.Reset(configuredSeconds);
            _isResettingTimer = false;

            _lastKnownRemainingTime = configuredSeconds;

            if (Mathf.Approximately(configuredSeconds, 0f))
            {
                HandleTimerComplete();
                return;
            }

            _countdownTimer.Start();
            _hasActiveSession = true;
            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(configuredSeconds));
            DebugUtility.LogVerbose<GameTimer>("Timer iniciado a partir do estado.");
        }

        private void HandleTimerComplete()
        {
            if (_isResettingTimer)
            {
                return;
            }

            if (_countdownTimer != null && !_countdownTimer.IsFinished && !Mathf.Approximately(_countdownTimer.CurrentTime, 0f))
            {
                return;
            }

            _lastKnownRemainingTime = 0f;
            _hasActiveSession = false;
            DebugUtility.LogVerbose<GameTimer>("Tempo acabou!");
            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_gameDurationSeconds));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        public void PauseTimer()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            _countdownTimer.Pause();
            _lastKnownRemainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
        }

        public void ResumeTimer()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            if (Mathf.Approximately(_lastKnownRemainingTime, 0f))
            {
                return;
            }

            if (!_countdownTimer.IsRunning)
            {
                _countdownTimer.Resume();
                _hasActiveSession = true;
            }
        }

        public void RestartTimer()
        {
            StartGameTimer();
        }

        public string GetFormattedTime()
        {
            float timeRemaining = RemainingTime;
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);

            return $"{minutes:00}:{seconds:00}";
        }

        private float ResolveConfiguredDuration()
        {
            if (_gameManager != null && _gameManager.GameConfig != null)
            {
                _gameDurationSeconds = Mathf.Max(0f, _gameManager.GameConfig.timerGame);
            }

            return _gameDurationSeconds;
        }

        private void HandleStateChanged(StateChangedEvent _)
        {
            var currentState = GameManagerStateMachine.Instance.CurrentState;

            switch (currentState)
            {
                case PlayingState:
                    if (!_hasActiveSession || _countdownTimer == null || _countdownTimer.IsFinished || Mathf.Approximately(_lastKnownRemainingTime, 0f))
                    {
                        StartGameTimer();
                    }
                    else
                    {
                        ResumeTimer();
                    }
                    break;
                case PausedState:
                    PauseTimer();
                    break;
                case MenuState:
                    ResetTimerToInitialDuration();
                    break;
                case GameOverState:
                case VictoryState:
                    StopTimer();
                    break;
            }
        }

        private void ResetTimerToInitialDuration()
        {
            float configuredSeconds = ResolveConfiguredDuration();
            EnsureTimerInstance(configuredSeconds);

            _isResettingTimer = true;
            _countdownTimer.Stop();
            _countdownTimer.Reset(configuredSeconds);
            _isResettingTimer = false;

            _lastKnownRemainingTime = configuredSeconds;
            _hasActiveSession = false;
        }

        private void StopTimer()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            _isResettingTimer = true;
            _countdownTimer.Stop();
            _isResettingTimer = false;

            _lastKnownRemainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
            if (_countdownTimer.IsFinished || Mathf.Approximately(_lastKnownRemainingTime, 0f))
            {
                _lastKnownRemainingTime = 0f;
            }

            _hasActiveSession = false;
        }

        private void Update()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            if (_countdownTimer.IsRunning)
            {
                _lastKnownRemainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
                return;
            }

            if (!_hasActiveSession)
            {
                return;
            }

            _lastKnownRemainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);

            if (_countdownTimer.IsFinished || Mathf.Approximately(_lastKnownRemainingTime, 0f))
            {
                _lastKnownRemainingTime = 0f;
            }
        }

        private void OnGameOver(GameOverEvent _)
        {
            StopTimer();
        }

        private void OnVictory(GameVictoryEvent _)
        {
            StopTimer();
        }

        private void EnsureTimerInstance(float duration)
        {
            if (_countdownTimer != null)
            {
                return;
            }

            float safeDuration = Mathf.Max(duration, 0f);
            _countdownTimer = new CountdownTimer(safeDuration);
            _countdownTimer.OnTimerStop += HandleTimerComplete;
            _countdownTimer.OnTimerStart += HandleTimerStart;
        }

        private void OnDestroy()
        {
            if (_countdownTimer != null)
            {
                _isResettingTimer = true;
                _countdownTimer.Stop();
                _countdownTimer.OnTimerStop -= HandleTimerComplete;
                _countdownTimer.OnTimerStart -= HandleTimerStart;
                _isResettingTimer = false;
            }
        }
    }
}
