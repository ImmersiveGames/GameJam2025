using ImprovedTimers;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    /// <summary>
    /// Controla o cronômetro principal da partida utilizando o ImprovedTimer.
    /// Responsável por iniciar, pausar, retomar e encerrar a contagem com base
    /// nos eventos do ciclo de jogo e por manter o valor restante disponível
    /// para outros sistemas.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    [DebugLevel(DebugLevel.Logs)]
    public sealed class GameTimer : Singleton<GameTimer>
    {
        private CountdownTimer _timer;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GameResetRequestedEvent> _resetBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        private float _configuredDuration;
        private float _remainingTime;
        private bool _sessionActive;
        private bool _isPaused;
        private int _lastLoggedSecond = -1;

        /// <summary>Duração configurada (segundos).</summary>
        public float ConfiguredDuration => Mathf.Max(_configuredDuration, 0f);
        /// <summary>Tempo restante atual (segundos).</summary>
        public float RemainingTime => Mathf.Max(_remainingTime, 0f);
        /// <summary>Indica se há uma sessão de contagem ativa.</summary>
        public bool HasActiveSession => _sessionActive;
        /// <summary>Indica se o cronômetro está contando no momento.</summary>
        public bool IsRunning => _sessionActive && !_isPaused;

        protected override void Awake()
        {
            base.Awake();

            _configuredDuration = LoadConfiguredDuration();
            _remainingTime = _configuredDuration;
            EnsureTimerInstance();
            PrepareTimer(_configuredDuration);

            DebugUtility.Log<GameTimer>($"Timer configurado com {_configuredDuration:F2}s.", context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(_ => StartSession());
            EventBus<GameStartEvent>.Register(_startBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(HandlePauseEvent);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(_ => StopSession(false));
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(_ => StopSession(false));
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

            _resetBinding = new EventBinding<GameResetRequestedEvent>(_ => StopSession(true));
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

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

            if (_resetBinding != null)
            {
                EventBus<GameResetRequestedEvent>.Unregister(_resetBinding);
                _resetBinding = null;
            }

            if (_stateChangedBinding != null)
            {
                EventBus<StateChangedEvent>.Unregister(_stateChangedBinding);
                _stateChangedBinding = null;
            }
        }

        private void Update()
        {
            TryStartWhenPlaying();

            if (!_sessionActive || _isPaused)
            {
                return;
            }

            AdvanceCountdown(Time.deltaTime);
        }

        /// <summary>Inicia uma nova sessão utilizando o valor configurado.</summary>
        private void StartSession()
        {
            if (_sessionActive)
            {
                return;
            }

            float duration = LoadConfiguredDuration();
            _configuredDuration = duration;

            if (duration <= 0f)
            {
                DebugUtility.LogWarning<GameTimer>(
                    "Duração configurada inválida para o cronômetro; sessão não iniciada.",
                    context: this);
                StopSession(true);
                return;
            }

            EnsureTimerInstance();
            PrepareTimer(duration);

            _remainingTime = duration;
            _sessionActive = true;
            _isPaused = false;
            _lastLoggedSecond = Mathf.CeilToInt(_remainingTime);

            _timer?.Start();

            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(_configuredDuration));
            DebugUtility.Log<GameTimer>($"Cronômetro iniciado com {_configuredDuration:F2}s.", context: this);
        }

        /// <summary>Pausa a contagem preservando o tempo restante.</summary>
        private void PauseSession()
        {
            if (!_sessionActive || _isPaused)
            {
                return;
            }

            _remainingTime = ReadTimerTime();
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Reset(_remainingTime);
            }

            _isPaused = true;
            DebugUtility.Log<GameTimer>($"Cronômetro pausado em {_remainingTime:F2}s.", context: this);
        }

        /// <summary>Retoma a contagem após uma pausa.</summary>
        private void ResumeSession()
        {
            if (!_sessionActive || !_isPaused || _remainingTime <= 0f)
            {
                return;
            }

            EnsureTimerInstance();
            if (_timer != null)
            {
                _timer.Reset(_remainingTime);
                _timer.Start();
            }

            _isPaused = false;
            _lastLoggedSecond = Mathf.CeilToInt(_remainingTime);
            DebugUtility.Log<GameTimer>($"Cronômetro retomado com {_remainingTime:F2}s.", context: this);
        }

        /// <summary>Finaliza a sessão atual e dispara o GameOver.</summary>
        private void HandleTimeEnded()
        {
            if (!_sessionActive)
            {
                return;
            }

            _sessionActive = false;
            _isPaused = false;
            _remainingTime = 0f;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Reset(0f);
            }

            DebugUtility.Log<GameTimer>("Tempo esgotado. Disparando GameOver.", context: this);

            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDuration));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        /// <summary>Interrompe a sessão atual e opcionalmente restaura o valor configurado.</summary>
        private void StopSession(bool resetToConfigured)
        {
            float current = ReadTimerTime();

            if (_timer != null)
            {
                _timer.Stop();
            }

            _sessionActive = false;
            _isPaused = false;
            _lastLoggedSecond = -1;
            _remainingTime = current;

            if (resetToConfigured)
            {
                _configuredDuration = LoadConfiguredDuration();
                _remainingTime = _configuredDuration;
                PrepareTimer(_configuredDuration);
            }

            DebugUtility.Log<GameTimer>($"Cronômetro parado. Reset={resetToConfigured}, restante={_remainingTime:F2}s.", context: this);
        }

        private void HandlePauseEvent(GamePauseEvent pauseEvent)
        {
            if (pauseEvent.IsPaused)
            {
                PauseSession();
            }
            else
            {
                ResumeSession();
            }
        }

        private float ReadTimerTime()
        {
            if (_timer == null)
            {
                return Mathf.Max(_remainingTime, 0f);
            }

            return Mathf.Max(_timer.CurrentTime, 0f);
        }

        private void AdvanceCountdown(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);
            }

            if (_timer != null)
            {
                float pluginRemaining = Mathf.Max(_timer.CurrentTime, 0f);

                if (_timer.IsFinished)
                {
                    _remainingTime = 0f;
                }
                else if (_timer.IsRunning && pluginRemaining <= _configuredDuration + 0.01f)
                {
                    _remainingTime = Mathf.Min(_remainingTime, pluginRemaining);
                }
            }

            LogWholeSecond();

            if (_remainingTime <= 0f)
            {
                HandleTimeEnded();
            }
        }

        private void LogWholeSecond()
        {
            if (!_sessionActive)
            {
                return;
            }

            int second = Mathf.CeilToInt(_remainingTime);
            if (second == _lastLoggedSecond)
            {
                return;
            }

            _lastLoggedSecond = second;
            DebugUtility.Log<GameTimer>($"Tempo restante: {Mathf.Max(second, 0)}s.", context: this);
        }

        private void TryStartWhenPlaying()
        {
            if (_sessionActive)
            {
                return;
            }

            GameManagerStateMachine stateMachine = GameManagerStateMachine.Instance;
            if (stateMachine == null || stateMachine.CurrentState is not PlayingState)
            {
                return;
            }

            StartSession();
        }

        private void HandleStateChanged(StateChangedEvent stateEvent)
        {
            if (stateEvent.isGameActive)
            {
                if (_sessionActive)
                {
                    ResumeSession();
                }
                else
                {
                    StartSession();
                }
            }
            else if (_sessionActive)
            {
                PauseSession();
            }
        }

        private void EnsureTimerInstance()
        {
            if (_timer != null)
            {
                return;
            }

            float safeDuration = Mathf.Max(_configuredDuration, 0f);
            _timer = new CountdownTimer(safeDuration);
            _timer.Stop();
            _timer.Reset(safeDuration);
        }

        private void PrepareTimer(float duration)
        {
            if (_timer == null)
            {
                EnsureTimerInstance();
            }

            if (_timer == null)
            {
                return;
            }

            float safeDuration = Mathf.Max(duration, 0f);
            _timer.Stop();
            _timer.Reset(safeDuration);
        }

        private float LoadConfiguredDuration()
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.GameConfig != null)
            {
                return Mathf.Max(manager.GameConfig.timerGame, 0f);
            }

            return Mathf.Max(_configuredDuration, 0f);
        }
    }
}
