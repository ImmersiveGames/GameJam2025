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
    /// <summary>
    /// Controla a contagem regressiva global da sessão, utilizando ImprovedTimer como relógio
    /// base e sincronizando com os estados do ciclo de jogo.
    /// </summary>
    [DefaultExecutionOrder(-90), DebugLevel(DebugLevel.Logs)]
    public class GameTimer : Singleton<GameTimer>
    {
        private CountdownTimer _countdownTimer;
        private GameManager _gameManager;

        private float _configuredDurationSeconds;
        private float _remainingTime;

        private bool _sessionActive;
        private bool _isRunning;

        private int _lastWholeSecondLogged = -1;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        public float RemainingTime => Mathf.Max(_remainingTime, 0f);
        public float ConfiguredDuration => Mathf.Max(_configuredDurationSeconds, 0f);
        public bool HasActiveSession => _sessionActive;

        protected override void Awake()
        {
            base.Awake();

            _gameManager = GameManager.Instance;
            _configuredDurationSeconds = ResolveConfiguredDuration();
            _remainingTime = _configuredDurationSeconds;

            EnsureTimerInstance(_configuredDurationSeconds);

            DebugUtility.Log<GameTimer>(
                $"Timer inicializado com {_configuredDurationSeconds:F2}s configurados.",
                context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(HandleGameStart);
            EventBus<GameStartEvent>.Register(_startBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(HandlePauseEvent);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(_ => StopSession("GameOverEvent"));
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(_ => StopSession("GameVictoryEvent"));
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

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

            if (_pauseBinding != null)
            {
                EventBus<GamePauseEvent>.Unregister(_pauseBinding);
                _pauseBinding = null;
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

            if (_stateChangedBinding != null)
            {
                EventBus<StateChangedEvent>.Unregister(_stateChangedBinding);
                _stateChangedBinding = null;
            }
        }

        private void Update()
        {
            if (!_sessionActive || !_isRunning)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);

            LogWholeSecondProgress(_remainingTime);

            if (_remainingTime <= 0f)
            {
                DebugUtility.Log<GameTimer>(
                    "Tempo esgotado; disparando GameOver.",
                    context: this);
                CompleteSession();
            }
        }

        private void HandleGameStart(GameStartEvent _)
        {
            if (!_sessionActive)
            {
                PrepareNewSession();
            }

            var currentState = GameManagerStateMachine.Instance?.CurrentState;
            if (currentState is PlayingState)
            {
                StartCountdown("GameStartEvent");
                return;
            }

            DebugUtility.Log<GameTimer>(
                "GameStartEvent recebido; aguardando PlayingState para iniciar a sessão.",
                context: this);
        }

        private void HandlePauseEvent(GamePauseEvent evt)
        {
            if (evt.IsPaused)
            {
                PauseCountdown("GamePauseEvent");
            }
            else
            {
                ResumeCountdown("GamePauseEvent");
            }
        }

        private void HandleStateChanged(StateChangedEvent evt)
        {
            DebugUtility.Log<GameTimer>(
                $"StateChangedEvent: ativo={evt.isGameActive}, sessão={_sessionActive}, rodando={_isRunning}.",
                context: this);

            var currentState = GameManagerStateMachine.Instance?.CurrentState;

            if (evt.isGameActive)
            {
                if (currentState is PlayingState)
                {
                    if (!_sessionActive)
                    {
                        PrepareNewSession();
                        StartCountdown("StateChangedEvent");
                    }
                    else if (!_isRunning)
                    {
                        if (Mathf.Approximately(_remainingTime, _configuredDurationSeconds))
                        {
                            StartCountdown("StateChangedEvent");
                        }
                        else
                        {
                            ResumeCountdown("StateChangedEvent");
                        }
                    }
                }
                else if (_sessionActive && !_isRunning)
                {
                    ResumeCountdown("StateChangedEvent");
                }

                return;
            }

            if (currentState is PausedState)
            {
                PauseCountdown("StateChangedEvent");
            }
            else if (currentState is MenuState)
            {
                StopSession("StateChangedEvent");
                ResetDisplayToConfigured();
            }
            else
            {
                StopSession("StateChangedEvent");
            }
        }

        private void PrepareNewSession()
        {
            float duration = ResolveConfiguredDuration();
            _configuredDurationSeconds = duration;
            _remainingTime = duration;

            EnsureTimerInstance(duration);
            ResetTimerInstance(duration);

            _sessionActive = true;
            _isRunning = false;
            _lastWholeSecondLogged = Mathf.FloorToInt(duration);

            DebugUtility.Log<GameTimer>(
                $"Sessão preparada com duração de {duration:F2}s.",
                context: this);
        }

        private void CompleteSession()
        {
            _remainingTime = 0f;
            _sessionActive = false;
            _isRunning = false;

            StopCountdownInternal();

            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDurationSeconds));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        private void StopSession(string origin)
        {
            if (!_sessionActive)
            {
                return;
            }

            StopCountdownInternal();

            _sessionActive = false;
            _isRunning = false;

            DebugUtility.Log<GameTimer>(
                $"Sessão encerrada ({origin}) com {_remainingTime:F2}s restantes.",
                context: this);
        }

        private void StartCountdown(string origin)
        {
            if (!_sessionActive || _isRunning)
            {
                return;
            }

            if (Mathf.Approximately(_remainingTime, 0f))
            {
                CompleteSession();
                return;
            }

            _isRunning = true;

            _countdownTimer.Start();
            _lastWholeSecondLogged = Mathf.FloorToInt(_remainingTime);

            DebugUtility.Log<GameTimer>(
                $"Contagem iniciada ({origin}) com {_remainingTime:F2}s restantes.",
                context: this);

            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(_remainingTime));
        }

        private void PauseCountdown(string origin)
        {
            if (!_sessionActive || !_isRunning)
            {
                return;
            }

            _countdownTimer.Pause();
            _isRunning = false;

            DebugUtility.Log<GameTimer>(
                $"Contagem pausada ({origin}) com {_remainingTime:F2}s restantes.",
                context: this);
        }

        private void ResumeCountdown(string origin)
        {
            if (!_sessionActive || _isRunning)
            {
                return;
            }

            if (Mathf.Approximately(_remainingTime, 0f))
            {
                CompleteSession();
                return;
            }

            _countdownTimer.Resume();
            _isRunning = true;
            _lastWholeSecondLogged = Mathf.FloorToInt(_remainingTime);

            DebugUtility.Log<GameTimer>(
                $"Contagem retomada ({origin}) com {_remainingTime:F2}s restantes.",
                context: this);
        }

        private void StopCountdownInternal()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            _countdownTimer.Stop();
        }

        private float ResolveConfiguredDuration()
        {
            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
            }

            if (_gameManager != null && _gameManager.GameConfig != null)
            {
                _configuredDurationSeconds = Mathf.Max(0f, _gameManager.GameConfig.timerGame);
            }

            return _configuredDurationSeconds;
        }

        private void EnsureTimerInstance(float duration)
        {
            if (_countdownTimer != null)
            {
                return;
            }

            _countdownTimer = new CountdownTimer(Mathf.Max(duration, 0f));
        }

        private void ResetTimerInstance(float duration)
        {
            if (_countdownTimer == null)
            {
                return;
            }

            _countdownTimer.Stop();
            _countdownTimer.Reset(Mathf.Max(duration, 0f));
        }

        private void LogWholeSecondProgress(float current)
        {
            int currentWhole = Mathf.FloorToInt(current);

            if (currentWhole == _lastWholeSecondLogged)
            {
                return;
            }

            _lastWholeSecondLogged = currentWhole;

            DebugUtility.Log<GameTimer>(
                $"Tempo restante: {current:F2}s.",
                context: this);
        }

        private void ResetDisplayToConfigured()
        {
            float configured = ResolveConfiguredDuration();
            if (!Mathf.Approximately(_remainingTime, configured))
            {
                _remainingTime = configured;
                DebugUtility.Log<GameTimer>(
                    $"Timer alinhado para exibir duração inicial de {configured:F2}s.",
                    context: this);
            }
        }
    }
}
