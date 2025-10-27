using System;
using System.Reflection;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using _ImmersiveGames.Scripts.StatesMachines;
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
        private static readonly Action<CountdownTimer, float> CountdownUpdateInvoker = ResolveCountdownUpdateInvoker();

        private CountdownTimer _countdownTimer;
        private GameManager _gameManager;

        private float _configuredDurationSeconds;
        private float _remainingTime;

        private bool _sessionActive;
        private bool _isRunning;
        private bool _useManualFallback;
        private float _lastPluginSample;

        private int _lastWholeSecondLogged = -1;

        private IState _lastObservedState;

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

            _stateChangedBinding = new EventBinding<StateChangedEvent>(HandleStateChangedEvent);
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
            ObserveStateTransitions();
            AdvanceTimer(Time.deltaTime);
        }

        private void HandleGameStart(GameStartEvent _)
        {
            DebugUtility.Log<GameTimer>(
                "GameStartEvent recebido; preparando sessão.",
                context: this);

            PrepareNewSession();

            TryActivateCountdown("GameStartEvent");
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

        private void PrepareNewSession()
        {
            if (_sessionActive)
            {
                StopCountdownInternal();
            }

            float duration = ResolveConfiguredDuration();
            _configuredDurationSeconds = duration;
            _remainingTime = duration;

            EnsureTimerInstance(duration);
            ResetTimerInstance(duration);

            _sessionActive = true;
            _isRunning = false;
            _useManualFallback = CountdownUpdateInvoker == null;
            _lastPluginSample = _countdownTimer != null ? _countdownTimer.CurrentTime : duration;
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
            _useManualFallback = CountdownUpdateInvoker == null;

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

            if (_countdownTimer != null)
            {
                _remainingTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);
            }

            StopCountdownInternal();

            _sessionActive = false;
            _isRunning = false;
            _useManualFallback = CountdownUpdateInvoker == null;

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
            _useManualFallback = CountdownUpdateInvoker == null;
            _lastPluginSample = _countdownTimer.CurrentTime;
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
                DebugUtility.Log<GameTimer>(
                    $"Solicitação de pausa ignorada ({origin}); sessão ativa={_sessionActive}, rodando={_isRunning}.",
                    context: this);
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
                DebugUtility.Log<GameTimer>(
                    $"Solicitação de retomada ignorada ({origin}); sessão ativa={_sessionActive}, rodando={_isRunning}.",
                    context: this);
                return;
            }

            if (Mathf.Approximately(_remainingTime, 0f))
            {
                CompleteSession();
                return;
            }

            _countdownTimer.Resume();
            _isRunning = true;
            _useManualFallback = CountdownUpdateInvoker == null;
            _lastPluginSample = _countdownTimer.CurrentTime;
            _lastWholeSecondLogged = Mathf.FloorToInt(_remainingTime);

            DebugUtility.Log<GameTimer>(
                $"Contagem retomada ({origin}) com {_remainingTime:F2}s restantes.",
                context: this);
        }

        private void TryActivateCountdown(string origin)
        {
            var currentState = GameManagerStateMachine.Instance?.CurrentState;
            if (currentState is not PlayingState)
            {
                DebugUtility.Log<GameTimer>(
                    $"{origin}: PlayingState inativo; aguardando para iniciar. Estado atual={(currentState?.GetType().Name ?? "<null>")}",
                    context: this);
                return;
            }

            if (!_sessionActive)
            {
                PrepareNewSession();
            }

            if (_isRunning)
            {
                DebugUtility.Log<GameTimer>(
                    $"Contagem já ativa ({origin}); nenhum ajuste necessário.",
                    context: this);
                return;
            }

            if (Mathf.Approximately(_remainingTime, 0f))
            {
                CompleteSession();
                return;
            }

            if (Mathf.Approximately(_remainingTime, _configuredDurationSeconds))
            {
                StartCountdown(origin);
            }
            else
            {
                ResumeCountdown(origin);
            }
        }

        private void ObserveStateTransitions()
        {
            var stateMachine = GameManagerStateMachine.Instance;
            var currentState = stateMachine?.CurrentState;

            if (currentState == _lastObservedState)
            {
                return;
            }

            var previousName = _lastObservedState?.GetType().Name ?? "<null>";
            var currentName = currentState?.GetType().Name ?? "<null>";

            DebugUtility.Log<GameTimer>(
                $"Transição de estado observada: {previousName} → {currentName}.",
                context: this);

            _lastObservedState = currentState;

            switch (currentState)
            {
                case PlayingState:
                    TryActivateCountdown("StateObserver");
                    break;

                case PausedState:
                    PauseCountdown("StateObserver");
                    break;

                case MenuState:
                    StopSession("StateObserver");
                    ResetDisplayToConfigured();
                    break;

                case null:
                    PauseCountdown("StateObserver");
                    break;

                default:
                    StopSession("StateObserver");
                    break;
            }
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

        private void AdvanceTimer(float deltaTime)
        {
            if (!_sessionActive || !_isRunning || _countdownTimer == null)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            float previousPluginTime = _countdownTimer.CurrentTime;

            if (CountdownUpdateInvoker != null)
            {
                CountdownUpdateInvoker(_countdownTimer, deltaTime);
            }

            float pluginTime = Mathf.Max(_countdownTimer.CurrentTime, 0f);

            if (!_useManualFallback)
            {
                bool pluginAdvanced = !Mathf.Approximately(pluginTime, previousPluginTime);

                if (pluginAdvanced)
                {
                    ApplyRemaining(pluginTime);
                    _lastPluginSample = pluginTime;
                }
                else if (CountdownUpdateInvoker == null || Mathf.Approximately(pluginTime, _lastPluginSample))
                {
                    _useManualFallback = true;
                    DebugUtility.LogWarning<GameTimer>(
                        "CountdownTimer não atualizou automaticamente; usando deltaTime manual como fallback.",
                        context: this);
                    ApplyRemaining(Mathf.Max(_remainingTime - deltaTime, 0f));
                }
                else
                {
                    ApplyRemaining(pluginTime);
                    _lastPluginSample = pluginTime;
                }
            }
            else
            {
                ApplyRemaining(Mathf.Max(_remainingTime - deltaTime, 0f));
            }

            if (!_useManualFallback && _countdownTimer.IsFinished)
            {
                DebugUtility.Log<GameTimer>(
                    "Tempo esgotado; disparando GameOver.",
                    context: this);
                CompleteSession();
                return;
            }

            if (_remainingTime <= 0f)
            {
                DebugUtility.Log<GameTimer>(
                    "Tempo esgotado (fallback manual); disparando GameOver.",
                    context: this);
                CompleteSession();
            }
        }

        private void ApplyRemaining(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, _configuredDurationSeconds > 0f ? _configuredDurationSeconds : value);

            if (Mathf.Approximately(clamped, _remainingTime))
            {
                return;
            }

            _remainingTime = clamped;
            LogWholeSecondProgress(_remainingTime);
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

        private void HandleStateChangedEvent(StateChangedEvent evt)
        {
            var currentState = GameManagerStateMachine.Instance?.CurrentState;

            if (evt.IsGameActive)
            {
                if (currentState is PlayingState)
                {
                    TryActivateCountdown("StateChangedEvent");
                }
                return;
            }

            if (currentState is MenuState)
            {
                StopSession("StateChangedEvent");
                ResetDisplayToConfigured();
            }
            else if (currentState is PausedState)
            {
                PauseCountdown("StateChangedEvent");
            }
            else
            {
                StopSession("StateChangedEvent");
            }
        }

        private static Action<CountdownTimer, float> ResolveCountdownUpdateInvoker()
        {
            MethodInfo method = typeof(CountdownTimer).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
            method ??= typeof(CountdownTimer).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);

            if (method == null)
            {
                return null;
            }

            try
            {
                return (Action<CountdownTimer, float>)method.CreateDelegate(typeof(Action<CountdownTimer, float>));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
