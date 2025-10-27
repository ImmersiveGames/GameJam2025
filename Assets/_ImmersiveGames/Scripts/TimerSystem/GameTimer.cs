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
        private GameManager _gameManager;

        private float _configuredDurationSeconds = 300f;
        private float _remainingTime;
        private bool _hasActiveSession;
        private bool _isPaused;
        private bool _isResettingTimer;
        private bool _hasLoggedUnexpectedZero;
        private int _lastWholeSecondReported = -1;
        private bool _pendingStartRequest;
        private string _pendingStartOrigin;

        public float RemainingTime => Mathf.Max(_remainingTime, 0f);

        public float ConfiguredDuration => Mathf.Max(_configuredDurationSeconds, 0f);

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
            _configuredDurationSeconds = ResolveConfiguredDuration();
            _remainingTime = _configuredDurationSeconds;

            EnsureTimerInstance(_configuredDurationSeconds);
            DebugUtility.Log<GameTimer>(
                $"Timer inicializado com {_configuredDurationSeconds:F2}s configurados.",
                context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(() => QueueTimerStart("GameStartEvent"));
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

        private void StartGameTimer()
        {
            StartTimerSession("ManualStart");
        }

        private void QueueTimerStart(string origin)
        {
            _pendingStartRequest = true;
            _pendingStartOrigin = origin;

            DebugUtility.Log<GameTimer>(
                $"Início do timer agendado (origem={origin}). Estado atual={(GameManagerStateMachine.Instance?.CurrentState?.GetType().Name ?? "<null>")}",
                context: this);
        }

        private void StartTimerSession(string origin)
        {
            _pendingStartRequest = false;
            _pendingStartOrigin = null;

            float configuredSeconds = ResolveConfiguredDuration();
            PrepareTimer(configuredSeconds);

            _remainingTime = configuredSeconds;
            _hasActiveSession = true;
            _isPaused = false;
            _hasLoggedUnexpectedZero = false;
            _lastWholeSecondReported = -1;

            DebugUtility.Log<GameTimer>(
                $"Solicitado início da contagem com {configuredSeconds:F2}s (origem={origin}).",
                context: this);

            if (Mathf.Approximately(configuredSeconds, 0f))
            {
                CompleteTimerSession("duração configurada igual ou inferior a zero");
                return;
            }

            _countdownTimer.Start();
            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(configuredSeconds));
        }

        private void HandleTimerStart()
        {
            _hasActiveSession = true;
            _isPaused = false;
            _hasLoggedUnexpectedZero = false;
            DebugUtility.Log<GameTimer>(
                $"ImprovedTimer iniciou sessão com {_remainingTime:F2}s restantes (running={_countdownTimer.IsRunning}).",
                context: this);
        }

        private void HandleTimerComplete()
        {
            if (_isResettingTimer)
            {
                DebugUtility.Log<GameTimer>(
                    "ImprovedTimer sinalizou parada durante reset; ignorando.",
                    context: this);
                return;
            }

            CompleteTimerSession("ImprovedTimer sinalizou conclusão");
        }

        public void PauseTimer()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            if (!_hasActiveSession || _isPaused)
            {
                return;
            }

            _countdownTimer.Pause();
            _isPaused = true;
            DebugUtility.Log<GameTimer>(
                $"Timer pausado com {_remainingTime:F2}s restantes (running={_countdownTimer.IsRunning}).",
                context: this);
        }

        public void ResumeTimer()
        {
            if (_countdownTimer == null)
            {
                return;
            }

            if (!_hasActiveSession || !_isPaused)
            {
                return;
            }

            if (Mathf.Approximately(_remainingTime, 0f))
            {
                DebugUtility.LogWarning<GameTimer>(
                    "Solicitada retomada com 0s restantes; ignorando.",
                    context: this);
                return;
            }

            if (!_countdownTimer.IsRunning)
            {
                _countdownTimer.Resume();
            }

            _isPaused = false;
            DebugUtility.Log<GameTimer>(
                $"Timer retomado com {_remainingTime:F2}s restantes (running={_countdownTimer.IsRunning}).",
                context: this);
        }

        public void RestartTimer()
        {
            StartTimerSession("RestartTimer");
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
            if (_gameManager == null)
            {
                _gameManager = GameManager.Instance;
            }

            if (_gameManager != null && _gameManager.GameConfig != null)
            {
                float resolved = Mathf.Max(0f, _gameManager.GameConfig.timerGame);
                if (!Mathf.Approximately(resolved, _configuredDurationSeconds))
                {
                    DebugUtility.Log<GameTimer>(
                        $"Atualizando duração configurada para {resolved:F2}s.",
                        context: this);
                }

                _configuredDurationSeconds = resolved;
            }

            return _configuredDurationSeconds;
        }

        private void HandleStateChanged(StateChangedEvent _)
        {
            var currentState = GameManagerStateMachine.Instance.CurrentState;
            string stateName = currentState?.GetType().Name ?? "<null>";

            DebugUtility.Log<GameTimer>(
                $"StateChangedEvent recebido. Estado atual={stateName}, sessão ativa={_hasActiveSession}, pausado={_isPaused}.",
                context: this);

            switch (currentState)
            {
                case PlayingState:
                    if (!_hasActiveSession || _countdownTimer == null || _countdownTimer.IsFinished || Mathf.Approximately(_remainingTime, 0f))
                    {
                        QueueTimerStart("StateChangedEvent-PlayingState");
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
            PrepareTimer(configuredSeconds);

            _remainingTime = configuredSeconds;
            _hasActiveSession = false;
            _isPaused = false;
            _hasLoggedUnexpectedZero = false;
            _lastWholeSecondReported = -1;

            _pendingStartRequest = false;
            _pendingStartOrigin = null;

            DebugUtility.Log<GameTimer>(
                $"Timer resetado para o valor inicial de {configuredSeconds:F2}s.",
                context: this);
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

            _remainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
            _hasActiveSession = false;
            _isPaused = false;
            _hasLoggedUnexpectedZero = false;
            _lastWholeSecondReported = -1;
            _pendingStartRequest = false;
            _pendingStartOrigin = null;

            DebugUtility.Log<GameTimer>(
                $"Timer interrompido manualmente com {_remainingTime:F2}s registrados.",
                context: this);
        }

        private void Update()
        {
            ProcessPendingOperations();

            if (_countdownTimer == null)
            {
                return;
            }

            if (!_hasActiveSession || _isPaused)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            float previousTime = _remainingTime;
            _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);

            float timerReportedTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
            if (!_hasLoggedUnexpectedZero && timerReportedTime <= 0f && previousTime > 0f && _countdownTimer.IsRunning)
            {
                _hasLoggedUnexpectedZero = true;
                DebugUtility.Log<GameTimer>(
                    $"ImprovedTimer reportou 0s enquanto ainda restavam {previousTime:F2}s locais (running={_countdownTimer.IsRunning}, finished={_countdownTimer.IsFinished}).",
                    context: this);
            }

            if (!Mathf.Approximately(previousTime, _remainingTime))
            {
                int wholeSeconds = Mathf.FloorToInt(_remainingTime);
                if (wholeSeconds != _lastWholeSecondReported)
                {
                    _lastWholeSecondReported = wholeSeconds;
                    DebugUtility.Log<GameTimer>(
                        $"Contagem regressiva atualizada para {_remainingTime:F2}s (running={_countdownTimer.IsRunning}).",
                        context: this);
                }
            }

            if (_remainingTime <= 0f)
            {
                DebugUtility.Log<GameTimer>(
                    "Contagem regressiva atingiu 0 via Update(); finalizando sessão.",
                    context: this);
                CompleteTimerSession("contagem regressiva atingiu zero");
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
            DebugUtility.Log<GameTimer>(
                $"Instância do ImprovedTimer criada com {safeDuration:F2}s.",
                context: this);
        }

        private void PrepareTimer(float duration)
        {
            EnsureTimerInstance(duration);

            _isResettingTimer = true;
            _countdownTimer.Stop();
            _countdownTimer.Reset(duration);
            _isResettingTimer = false;

            DebugUtility.Log<GameTimer>(
                $"Timer preparado para {duration:F2}s (CurrentTime={_countdownTimer.CurrentTime:F2}).",
                context: this);
        }

        private void CompleteTimerSession(string reason)
        {
            if (!_hasActiveSession)
            {
                DebugUtility.Log<GameTimer>(
                    $"Solicitação de finalização ignorada ({reason}). Nenhuma sessão ativa.",
                    context: this);
                return;
            }

            _remainingTime = 0f;
            _hasActiveSession = false;
            _isPaused = false;
            _hasLoggedUnexpectedZero = false;
            _lastWholeSecondReported = -1;
            _pendingStartRequest = false;
            _pendingStartOrigin = null;

            if (_countdownTimer != null && _countdownTimer.IsRunning)
            {
                _isResettingTimer = true;
                _countdownTimer.Stop();
                _isResettingTimer = false;
            }

            DebugUtility.Log<GameTimer>(
                $"Tempo esgotado ({reason}). Disparando GameOver.",
                context: this);

            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDurationSeconds));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
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

        private void ProcessPendingOperations()
        {
            if (_pendingStartRequest)
            {
                if (IsInPlayingState())
                {
                    _pendingStartRequest = false;
                    string origin = _pendingStartOrigin ?? "PendingStart";
                    _pendingStartOrigin = null;
                    StartTimerSession(origin);
                }
            }
        }

        private static bool IsInPlayingState()
        {
            return GameManagerStateMachine.Instance?.CurrentState is PlayingState;
        }
    }
}
