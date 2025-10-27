using System;
using System.Reflection;
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
    /// Controlador central da contagem regressiva da partida.
    /// Mantém a duração configurada, atualiza o relógio a cada frame
    /// e reage aos eventos de ciclo de jogo para iniciar, pausar ou encerrar a sessão.
    /// </summary>
    [DefaultExecutionOrder(-90), DebugLevel(DebugLevel.Logs)]
    public sealed class GameTimer : Singleton<GameTimer>
    {
        private static readonly Action<CountdownTimer, float> CountdownUpdateInvoker = ResolveCountdownUpdateInvoker();

        private CountdownTimer _countdownTimer;
        private Action<float> _pluginTickInvoker;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        private float _configuredDuration;
        private float _remainingTime;

        private bool _sessionActive;
        private bool _isRunning;
        private bool _manualFallback;
        private bool _autoStartArmed;

        private int _lastLoggedSecond = -1;

        /// <summary>Duração configurada em segundos.</summary>
        public float ConfiguredDuration => Mathf.Max(_configuredDuration, 0f);
        /// <summary>Tempo restante da sessão em segundos.</summary>
        public float RemainingTime => Mathf.Max(_remainingTime, 0f);
        /// <summary>Indica se existe uma sessão ativa.</summary>
        public bool HasActiveSession => _sessionActive;
        /// <summary>Indica se o relógio está correndo atualmente.</summary>
        public bool IsRunning => _isRunning;

        protected override void Awake()
        {
            base.Awake();

            _configuredDuration = ResolveConfiguredDuration();
            _remainingTime = _configuredDuration;
            BuildTimer(_configuredDuration);

            DebugUtility.Log<GameTimer>(
                $"Timer configurado para {_configuredDuration:F2}s.",
                context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(_ => BeginSession());
            EventBus<GameStartEvent>.Register(_startBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(HandlePauseEvent);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(_ => StopInternal(false));
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(_ => StopInternal(false));
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
            AdvanceCountdown(Time.deltaTime);
            TryAutoStart();
        }

        /// <summary>Inicia uma nova sessão utilizando o valor configurado.</summary>
        private void BeginSession()
        {
            float duration = ResolveConfiguredDuration();
            if (duration <= 0f)
            {
                DebugUtility.LogWarning<GameTimer>("Duração configurada inválida; sessão não iniciada.", context: this);
                StopInternal(true);
                return;
            }

            if (_sessionActive)
            {
                StopInternal(false);
            }

            _configuredDuration = duration;
            _remainingTime = duration;
            EnsureTimerReset(duration);

            _sessionActive = true;
            _isRunning = true;
            _manualFallback = _pluginTickInvoker == null;
            _autoStartArmed = true;
            _lastLoggedSecond = Mathf.CeilToInt(_remainingTime);

            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(_configuredDuration));
            DebugUtility.Log<GameTimer>(
                $"Sessão iniciada com {_configuredDuration:F2}s.",
                context: this);

            if (_manualFallback)
            {
                DebugUtility.LogWarning<GameTimer>(
                    "CountdownTimer sem atualização automática; utilizando deltaTime manual.",
                    context: this);
            }
        }

        /// <summary>Pausa a contagem corrente.</summary>
        private void PauseSession()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            _countdownTimer?.Pause();

            DebugUtility.Log<GameTimer>(
                $"Contagem pausada com {_remainingTime:F2}s.",
                context: this);
        }

        /// <summary>Retoma a contagem após uma pausa.</summary>
        private void ResumeSession()
        {
            if (!_sessionActive || _isRunning || _remainingTime <= 0f)
            {
                return;
            }

            _isRunning = true;
            EnsureTimerReset(_remainingTime);
            _manualFallback = _pluginTickInvoker == null;
            _lastLoggedSecond = Mathf.CeilToInt(_remainingTime);

            DebugUtility.Log<GameTimer>(
                $"Contagem retomada com {_remainingTime:F2}s.",
                context: this);

            if (_manualFallback)
            {
                DebugUtility.LogWarning<GameTimer>(
                    "Retomando em modo manual por falta de tick do ImprovedTimer.",
                    context: this);
            }
        }

        private void AdvanceCountdown(float deltaTime)
        {
            if (!_sessionActive || !_isRunning || deltaTime <= 0f)
            {
                return;
            }

            bool advanced = !_manualFallback && TryAdvanceWithPlugin(deltaTime);
            if (!advanced)
            {
                _manualFallback = true;
                _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);
            }

            LogWholeSecond();

            if (_remainingTime > 0f)
            {
                return;
            }

            FinishSession();
        }

        private bool TryAdvanceWithPlugin(float deltaTime)
        {
            if (_countdownTimer == null || _pluginTickInvoker == null)
            {
                return false;
            }

            float before = Mathf.Max(_countdownTimer.CurrentTime, 0f);
            _pluginTickInvoker(deltaTime);
            float after = Mathf.Max(_countdownTimer.CurrentTime, 0f);

            if (Mathf.Approximately(before, after))
            {
                return false;
            }

            _remainingTime = after;
            return true;
        }

        private void FinishSession()
        {
            if (!_sessionActive)
            {
                return;
            }

            _remainingTime = 0f;
            _sessionActive = false;
            _isRunning = false;
            _countdownTimer?.Stop();

            DebugUtility.Log<GameTimer>("Tempo esgotado; disparando GameOver.", context: this);

            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDuration));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        private void StopInternal(bool resetToConfigured)
        {
            _countdownTimer?.Stop();
            _isRunning = false;
            _sessionActive = false;

            if (resetToConfigured)
            {
                _configuredDuration = ResolveConfiguredDuration();
                _remainingTime = _configuredDuration;
            }

            _autoStartArmed = false;

            DebugUtility.Log<GameTimer>(
                $"Contagem encerrada. Reset={resetToConfigured}, restante={_remainingTime:F2}s.",
                context: this);
        }

        private void TryAutoStart()
        {
            var stateMachine = GameManagerStateMachine.Instance;
            if (stateMachine == null)
            {
                _autoStartArmed = false;
                return;
            }

            bool isPlaying = stateMachine.CurrentState is PlayingState;
            if (isPlaying && !_sessionActive && !_isRunning && !_autoStartArmed)
            {
                BeginSession();
            }
            else if (!isPlaying)
            {
                _autoStartArmed = false;
            }
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

        private void HandleStateChanged(StateChangedEvent evt)
        {
            if (!evt.isGameActive)
            {
                StopInternal(true);
            }
        }

        private void LogWholeSecond()
        {
            int current = Mathf.CeilToInt(_remainingTime);
            if (current == _lastLoggedSecond)
            {
                return;
            }

            _lastLoggedSecond = current;
            DebugUtility.Log<GameTimer>(
                $"Tempo restante: {Mathf.Max(current, 0)}s.",
                context: this);
        }

        private float ResolveConfiguredDuration()
        {
            var manager = GameManager.Instance;
            if (manager != null && manager.GameConfig != null)
            {
                return Mathf.Max(manager.GameConfig.timerGame, 0f);
            }

            return Mathf.Max(_configuredDuration, 0f);
        }

        private void BuildTimer(float duration)
        {
            float safeDuration = Mathf.Max(duration, 0f);
            _countdownTimer = new CountdownTimer(safeDuration);
            _pluginTickInvoker = CreatePluginInvoker(_countdownTimer);
            _countdownTimer.Stop();
            _countdownTimer.Reset(safeDuration);
        }

        private void EnsureTimerReset(float duration)
        {
            float safeDuration = Mathf.Max(duration, 0f);

            if (_countdownTimer == null)
            {
                BuildTimer(safeDuration);
            }
            else
            {
                _countdownTimer.Stop();
                _countdownTimer.Reset(safeDuration);
            }

            if (_countdownTimer != null)
            {
                _countdownTimer.Start();
            }

            if (_pluginTickInvoker == null && _countdownTimer != null)
            {
                _pluginTickInvoker = CreatePluginInvoker(_countdownTimer);
            }
        }

        private static Action<float> CreatePluginInvoker(CountdownTimer timer)
        {
            if (timer == null)
            {
                return null;
            }

            try
            {
                MethodInfo method = timer.GetType().GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    return (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), timer, method);
                }

                method = timer.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    return (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), timer, method);
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            return null;
        }
    }
}
