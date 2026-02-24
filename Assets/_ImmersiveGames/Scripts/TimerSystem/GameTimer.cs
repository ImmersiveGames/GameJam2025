using ImprovedTimers;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.TimerSystem.Events;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    /// <summary>
    /// Controla o cronômetro principal da partida utilizando o ImprovedTimer.
    /// Responsável por iniciar, pausar, retomar e encerrar a contagem com base
    /// nos eventos do ciclo de jogo e por manter o valor restante disponível
    /// para outros sistemas.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class GameTimer : Singleton<GameTimer>
    {
        [Inject] private IGameManager _gameManager;
        [Inject] private GameConfig _gameConfig;

        private CountdownTimer _timer;

        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<OldGameResetRequestedEvent> _resetBinding;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        private float _configuredDuration;
        private float _remainingTime;
        private bool _sessionActive;
        private bool _isPaused;
        private bool _autoStartLocked;
        private int _lastLoggedSecond = -1;

        /// <summary>Duração configurada (segundos).</summary>
        public float ConfiguredDuration => Mathf.Max(_configuredDuration, 0f);

        /// <summary>Tempo restante atual (segundos).</summary>
        public float RemainingTime => Mathf.Max(_remainingTime, 0f);

        /// <summary>Indica se há uma sessão de contagem ativa.</summary>
        public bool HasActiveSession => _sessionActive;

        /// <summary>Indica se o cronômetro está contando no momento.</summary>
        public bool IsRunning => _sessionActive && !_isPaused;

        private IGameManager ResolvedGameManager => _gameManager ?? GameManager.Instance;
        private GameConfig Config => _gameConfig ?? ResolvedGameManager?.GameConfig;

        protected override void Awake()
        {
            base.Awake();

            DependencyManager.Provider.InjectDependencies(this);

            _configuredDuration = LoadConfiguredDuration();
            _remainingTime = _configuredDuration;
            _autoStartLocked = false;

            EnsureTimerInstance();
            PrepareTimer(_configuredDuration);

            DebugUtility.LogVerbose<GameTimer>($"Timer configurado com {_configuredDuration:F2}s.", context: this);
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(_ => HandleGameStart());
            EventBus<GameStartEvent>.Register(_startBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(HandlePauseEvent);
            EventBus<GamePauseEvent>.Register(_pauseBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(_ => HandleGameOver());
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(_ =>
            {
                float snapshot = Mathf.Max(_remainingTime, 0f);
                StopSession(false, snapshot, "Victory recebido");
                DebugUtility.LogVerbose<GameTimer>($"Vitória detectada. Cronômetro finalizado em {snapshot:F2}s.", context: this);
            });
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

            // MACRO: reset de jogo (fase inteira)
            _resetBinding = new EventBinding<OldGameResetRequestedEvent>(_ => StopSession(true, reason: "Reset solicitado (OldGameResetRequestedEvent)"));
            EventBus<OldGameResetRequestedEvent>.Register(_resetBinding);

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
                EventBus<OldGameResetRequestedEvent>.Unregister(_resetBinding);
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
                return;

            AdvanceCountdown(Time.deltaTime);
        }

        /// <summary>
        /// API explícita: “full reset” do timer para o valor configurado.
        /// Use SOMENTE quando a fase inteira é reiniciada (ex.: Reset IN-PLACE AllActorsInScene).
        /// </summary>
        public void ResetToConfiguredDuration(string reason = "Manual Full Reset")
        {
            StopSession(true, reason: reason);
        }

        /// <summary>Inicia uma nova sessão utilizando o valor configurado.</summary>
        private void StartSession()
        {
            if (_sessionActive)
                return;

            float duration = LoadConfiguredDuration();
            _configuredDuration = duration;

            if (duration <= 0f)
            {
                DebugUtility.LogWarning<GameTimer>(
                    "Duração configurada inválida para o cronômetro; sessão não iniciada.",
                    context: this);
                StopSession(true, reason: "Duração inválida");
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
            DebugUtility.LogVerbose<GameTimer>($"Cronômetro iniciado com {_configuredDuration:F2}s.", context: this);
        }

        private void HandleGameStart()
        {
            _autoStartLocked = false;
            StartSession();
        }

        private void HandleGameOver()
        {
            StopSession(false, reason: "GameOver recebido");
            _autoStartLocked = true;
        }

        private void PauseSession()
        {
            if (!_sessionActive || _isPaused)
                return;

            _remainingTime = ReadTimerTime();
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Reset(_remainingTime);
            }

            _isPaused = true;
            DebugUtility.LogVerbose<GameTimer>($"Cronômetro pausado em {_remainingTime:F2}s.", context: this);
        }

        private void ResumeSession()
        {
            if (!_sessionActive || !_isPaused || _remainingTime <= 0f)
                return;

            EnsureTimerInstance();
            if (_timer != null)
            {
                _timer.Reset(_remainingTime);
                _timer.Start();
            }

            _isPaused = false;
            _lastLoggedSecond = Mathf.CeilToInt(_remainingTime);
            DebugUtility.LogVerbose<GameTimer>($"Cronômetro retomado com {_remainingTime:F2}s.", context: this);
        }

        private void HandleTimeEnded()
        {
            if (!_sessionActive)
                return;

            StopSession(false, 0f, "Tempo esgotado");

            DebugUtility.LogVerbose<GameTimer>("Tempo esgotado. Disparando GameOver.", context: this);

            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_configuredDuration));

            var manager = ResolvedGameManager;
            if (manager == null || !manager.TryTriggerGameOver("Timer expired"))
            {
                DebugUtility.LogWarning<GameTimer>("GameManager indisponível ou estado atual não permite GameOver.", context: this);
            }
        }

        private void StopSession(bool resetToConfigured, float? overrideRemaining = null, string reason = null)
        {
            float current = overrideRemaining ?? ReadTimerTime();
            current = Mathf.Clamp(current, 0f, Mathf.Max(_configuredDuration, current));

            _timer?.Stop();

            _sessionActive = false;
            _isPaused = false;
            _lastLoggedSecond = -1;

            if (resetToConfigured)
            {
                _configuredDuration = LoadConfiguredDuration();
                _remainingTime = Mathf.Max(_configuredDuration, 0f);
                PrepareTimer(_configuredDuration);
                _autoStartLocked = false;
            }
            else
            {
                _remainingTime = current;
                if (_timer != null)
                {
                    _timer.Reset(_remainingTime);
                }
            }

            string label = string.IsNullOrEmpty(reason) ? "sem motivo" : reason;
            DebugUtility.LogVerbose<GameTimer>($"Cronômetro parado ({label}). Reset={resetToConfigured}, restante={_remainingTime:F2}s.", context: this);
        }

        private void HandlePauseEvent(GamePauseEvent pauseEvent)
        {
            if (pauseEvent.IsPaused) PauseSession();
            else ResumeSession();
        }

        private float ReadTimerTime()
        {
            if (_timer == null)
                return Mathf.Max(_remainingTime, 0f);

            float pluginTime = Mathf.Max(_timer.CurrentTime, 0f);
            return Mathf.Max(Mathf.Min(_remainingTime, pluginTime), 0f);
        }

        private void AdvanceCountdown(float deltaTime)
        {
            if (deltaTime > 0f)
                _remainingTime = Mathf.Max(_remainingTime - deltaTime, 0f);

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
                HandleTimeEnded();
        }

        private void LogWholeSecond()
        {
            if (!_sessionActive)
                return;

            int second = Mathf.CeilToInt(_remainingTime);
            if (second == _lastLoggedSecond)
                return;

            _lastLoggedSecond = second;
            DebugUtility.LogVerbose<GameTimer>($"Tempo restante: {Mathf.Max(second, 0)}s.", context: this);
        }

        private void TryStartWhenPlaying()
        {
            if (_sessionActive || _autoStartLocked)
                return;

            var stateMachine = OldGameManagerStateMachine.Instance;
            if (stateMachine == null || stateMachine.CurrentState is not OldPlayingState)
                return;

            StartSession();
        }

        private void HandleStateChanged(StateChangedEvent stateEvent)
        {
            if (stateEvent.isGameActive)
            {
                if (_autoStartLocked)
                    return;

                if (_sessionActive) ResumeSession();
                else StartSession();
            }
            else if (_sessionActive)
            {
                PauseSession();
            }
        }

        private void EnsureTimerInstance()
        {
            if (_timer != null)
                return;

            float safeDuration = Mathf.Max(_configuredDuration, 0f);
            _timer = new CountdownTimer(safeDuration);
            _timer.Stop();
            _timer.Reset(safeDuration);
        }

        private void PrepareTimer(float duration)
        {
            if (_timer == null)
                EnsureTimerInstance();

            if (_timer == null)
                return;

            float safeDuration = Mathf.Max(duration, 0f);
            _timer.Stop();
            _timer.Reset(safeDuration);
        }

        private float LoadConfiguredDuration()
        {
            if (Config != null)
                return Mathf.Max(Config.TimerSeconds, 0f);

            return Mathf.Max(_configuredDuration, 0f);
        }
    }
}


