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
using UnityUtils;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    /// <summary>
    /// Responsável por controlar o cronômetro regressivo da partida.
    /// Reage aos estados do jogo e propaga eventos para a UI.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class GameTimer : Singleton<GameTimer>
    {
        private delegate void CountdownAdvance(CountdownTimer timer, float deltaTime);

        private static readonly CountdownAdvance CountdownAdvanceInvoker = ResolveAdvanceDelegate();

        private CountdownTimer _timer;
        private float _configuredDuration;
        private float _remainingTime;
        private bool _sessionActive;
        private bool _isRunning;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        public float ConfiguredDuration => Mathf.Max(_configuredDuration, 0f);
        public float RemainingTime => Mathf.Max(_remainingTime, 0f);
        public bool HasActiveSession => _sessionActive;

        protected override void Awake()
        {
            base.Awake();
            _configuredDuration = ReadConfiguredDuration();
            EnsureTimerInstance();
            ResetToIdle();
            DebugUtility.Log<GameTimer>($"Cronômetro preparado com {_configuredDuration:F2}s configurados.", context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(_ => BeginSession());
            EventBus<GameStartEvent>.Register(_startBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(HandlePauseEvent);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(_ => StopSession("GameOver"));
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(_ => StopSession("Victory"));
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

            _stateChangedBinding = new EventBinding<StateChangedEvent>(_ => HandleStateChange());
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
            AdvanceTimer(Time.deltaTime);
        }

        private void BeginSession()
        {
            if (_sessionActive)
            {
                return;
            }

            float duration = ReadConfiguredDuration();
            _configuredDuration = duration;

            EnsureTimerInstance();
            RestartTimer(duration);

            _remainingTime = Mathf.Max(_timer != null ? _timer.CurrentTime : duration, duration);
            _sessionActive = true;
            _isRunning = true;

            DebugUtility.Log<GameTimer>($"Cronômetro iniciado com {_remainingTime:F2}s.", context: this);
            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(_configuredDuration));
        }

        private void HandlePauseEvent(GamePauseEvent pauseEvent)
        {
            if (!_sessionActive || _timer == null)
            {
                return;
            }

            if (pauseEvent.IsPaused)
            {
                _timer.Pause();
                _isRunning = false;
                _remainingTime = Mathf.Max(_timer.CurrentTime, 0f);
                DebugUtility.Log<GameTimer>("Cronômetro pausado.", context: this);
                return;
            }

            _timer.Resume();
            _isRunning = true;
            DebugUtility.Log<GameTimer>("Cronômetro retomado.", context: this);
        }

        private void HandleStateChange()
        {
            var currentState = GameManagerStateMachine.Instance?.CurrentState;
            switch (currentState)
            {
                case PlayingState:
                    if (!_sessionActive)
                    {
                        BeginSession();
                    }
                    else if (!_isRunning)
                    {
                        _timer.Resume();
                        _isRunning = true;
                        DebugUtility.Log<GameTimer>("Cronômetro retomado pelo PlayingState.", context: this);
                    }
                    break;

                case PausedState:
                    HandlePauseEvent(new GamePauseEvent(true));
                    break;

                case MenuState:
                    StopSession("Menu");
                    ResetToIdle();
                    break;
            }
        }

        private void StopSession(string reason)
        {
            if (!_sessionActive)
            {
                return;
            }

            _sessionActive = false;
            _isRunning = false;

            if (_timer != null)
            {
                _timer.Stop();
                _remainingTime = Mathf.Max(_timer.CurrentTime, 0f);
            }

            DebugUtility.Log<GameTimer>($"Cronômetro encerrado ({reason}) com {_remainingTime:F2}s restantes.", context: this);
        }

        private void CompleteCountdown()
        {
            if (!_sessionActive)
            {
                return;
            }

            _sessionActive = false;
            _isRunning = false;
            _remainingTime = 0f;

            if (_timer != null)
            {
                _timer.Stop();
            }

            DebugUtility.Log<GameTimer>("Tempo esgotado; disparando GameOver.", context: this);
            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDuration));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        private void AdvanceTimer(float deltaTime)
        {
            if (!_sessionActive || !_isRunning || _timer == null)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            float previous = _remainingTime;

            if (CountdownAdvanceInvoker != null)
            {
                CountdownAdvanceInvoker(_timer, deltaTime);
                _remainingTime = Mathf.Clamp(_timer.CurrentTime, 0f, _configuredDuration);
            }
            else
            {
                _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);
            }

            if (!Mathf.Approximately(previous, _remainingTime))
            {
                DebugUtility.LogVerbose<GameTimer>($"Tempo restante: {_remainingTime:F2}s.", context: this);
            }

            if (_remainingTime <= 0f)
            {
                CompleteCountdown();
            }
        }

        private void ResetToIdle()
        {
            float duration = Mathf.Max(ReadConfiguredDuration(), 0f);
            _configuredDuration = duration;
            _remainingTime = duration;
            _sessionActive = false;
            _isRunning = false;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Reset(duration);
            }
        }

        private float ReadConfiguredDuration()
        {
            var manager = GameManager.Instance;
            if (manager != null && manager.GameConfig != null)
            {
                return Mathf.Max(manager.GameConfig.timerGame, 0f);
            }

            return Mathf.Max(_configuredDuration, 0f);
        }

        private void EnsureTimerInstance()
        {
            if (_timer != null)
            {
                return;
            }

            float safeDuration = Mathf.Max(_configuredDuration, 1f);
            _timer = new CountdownTimer(safeDuration);
            _timer.Stop();
            _timer.Reset(safeDuration);
        }

        private void RestartTimer(float duration)
        {
            if (_timer == null)
            {
                EnsureTimerInstance();
            }

            float safeDuration = Mathf.Max(duration, 0.01f);
            _timer.Stop();
            _timer.Reset(safeDuration);
            _timer.Start();
        }

        private static CountdownAdvance ResolveAdvanceDelegate()
        {
            MethodInfo method = typeof(CountdownTimer).GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public);
            method ??= typeof(CountdownTimer).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);

            if (method == null)
            {
                return null;
            }

            try
            {
                return (CountdownAdvance)Delegate.CreateDelegate(typeof(CountdownAdvance), null, method);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
