using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
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
        private bool _isResettingTimer;
        
        // Eventos que podem ser assinados por outros scripts
        private GameManager _gameManager;
        
        // Propriedade para acessar o tempo restante
        public float RemainingTime => _countdownTimer != null ? Mathf.Max(_countdownTimer.CurrentTime, 0f) : 0f;

        public float ConfiguredDuration => _gameDurationSeconds;
        
        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        
        protected override void Awake()
        {
            base.Awake();
            _gameManager = GameManager.Instance;
            _gameDurationSeconds = Mathf.Max(0f, _gameManager.GameConfig.timerGame);

            _countdownTimer = new CountdownTimer(_gameDurationSeconds);

            // Registrar eventos internos da biblioteca de timer
            _countdownTimer.OnTimerStop += HandleTimerComplete;
            _countdownTimer.OnTimerStart += HandleTimerStart;
        }

        private void OnEnable()
        {
            _startBinding = new EventBinding<GameStartEvent>(StartGameTimer);
            EventBus<GameStartEvent>.Register(_startBinding);

            _gameOverBinding = new EventBinding<GameOverEvent>(PauseTimer);
            EventBus<GameOverEvent>.Register(_gameOverBinding);

            _victoryBinding = new EventBinding<GameVictoryEvent>(PauseTimer);
            EventBus<GameVictoryEvent>.Register(_victoryBinding);

            _pauseBinding = new EventBinding<GamePauseEvent>(PauseTimerHandler);
            EventBus<GamePauseEvent>.Register(_pauseBinding);
            
        }
        private void OnDisable()
        {
            EventBus<GameStartEvent>.Unregister(_startBinding);
            EventBus<GameOverEvent>.Unregister(_gameOverBinding);
            EventBus<GameVictoryEvent>.Unregister(_victoryBinding);
            EventBus<GamePauseEvent>.Unregister(_pauseBinding);
        }
        private void PauseTimerHandler(GamePauseEvent evt)
        {
            if(evt.IsPaused)
                PauseTimer();
            else
            {
                ResumeTimer();
            }
        }


        private void HandleTimerStart()
        {
            DebugUtility.LogVerbose<GameTimer>($"Jogo iniciado! Timer de {_gameDurationSeconds} segundo(s) começou.");

        }
        

        private void StartGameTimer()
        {
            DebugUtility.LogVerbose<GameTimer>($"Começou o timer {_countdownTimer.IsRunning}, {_countdownTimer.IsFinished}");
    
            // Resetar o timer para a duração inicial
            float configuredSeconds = ResolveConfiguredDuration();

            if (_countdownTimer == null)
            {
                _countdownTimer = new CountdownTimer(configuredSeconds);
                _countdownTimer.OnTimerStop += HandleTimerComplete;
                _countdownTimer.OnTimerStart += HandleTimerStart;
            }

            _isResettingTimer = true;
            _countdownTimer.Stop();
            _countdownTimer.Reset(configuredSeconds);
            _isResettingTimer = false;

            if (Mathf.Approximately(configuredSeconds, 0f))
            {
                HandleTimerComplete();
                return;
            }

            // Iniciar o timer
            _countdownTimer.Start();
            EventBus<EventTimerStarted>.Raise(new EventTimerStarted(configuredSeconds));
            DebugUtility.LogVerbose<GameTimer>($"Começou");
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

            DebugUtility.LogVerbose<GameTimer>("Tempo acabou!");
            EventBus<EventTimeEnded>.Raise(new EventTimeEnded(_gameDurationSeconds));
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }
        
        public void PauseTimer()
        {
            _countdownTimer?.Pause();
        }

        public void ResumeTimer()
        {
            if (_countdownTimer != null && !_countdownTimer.IsRunning)
            {
                _countdownTimer.Resume();
            }
        }
        
        public void RestartTimer()
        {
            StartGameTimer();
        }

        public string GetFormattedTime()
        {
            if (_countdownTimer == null)
                return "00:00";

            if (!_countdownTimer.IsRunning && _countdownTimer.IsFinished)
                return "00:00";

            float timeRemaining = _countdownTimer.CurrentTime;
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
        
        
        private void OnDestroy()
        {
            // Garantir que o timer seja limpo corretamente
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
