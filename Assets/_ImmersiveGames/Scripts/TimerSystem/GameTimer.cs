using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.TimerSystem.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityUtils;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    [DebugLevel(DebugLevel.Logs)]
    public class GameTimer : Singleton<GameTimer>
    {
        private CountdownTimer _countdownTimer;
        private int _gameDuration = 300; // Duração do jogo em segundos (5 minutos)
        
        // Eventos que podem ser assinados por outros scripts
        private GameManager _gameManager;
        
        // Propriedade para acessar o tempo restante
        public float RemainingTime => _countdownTimer.IsRunning ? _countdownTimer.CurrentTime : 0f;
        
        private EventBinding<GameStartEvent> _startBinding;
        private EventBinding<GameOverEvent> _gameOverBinding;
        private EventBinding<GameVictoryEvent> _victoryBinding;
        private EventBinding<GamePauseEvent> _pauseBinding;
        
        protected override void Awake()
        {
            base.Awake();
            _gameManager = GameManager.Instance;
            _gameDuration = _gameManager.GameConfig.timerGame;
            // Criar o CountdownTimer
            _countdownTimer = new CountdownTimer(_gameDuration);
            
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
            Debug.Log("Jogo iniciado! Timer de 5 minutos começou.");

        }
        

        private void StartGameTimer()
        {
            DebugUtility.LogVerbose<GameTimer>($"Começou o timer {_countdownTimer.IsRunning}, {_countdownTimer.IsFinished}");
    
            // Resetar o timer para a duração inicial
            _countdownTimer.Stop();
            _countdownTimer.Reset(_gameDuration);

            // Iniciar o timer
            _countdownTimer.Start();
            EventBus<EventTimerStarted>.Raise(new EventTimerStarted());
            DebugUtility.LogVerbose<GameTimer>($"Começou");
        }
        
        private void HandleTimerComplete()
        {
            Debug.Log("Tempo acabou!");
            _gameManager.SetGameOver(true);
            EventBus<EventTimeEnded>.Raise(new EventTimeEnded());
        }
        
        public void PauseTimer()
        {
            _countdownTimer.Pause();
        }
        
        public void ResumeTimer()
        {
            if (!_countdownTimer.IsRunning)
            {
                _countdownTimer.Resume();
            }
        }
        
        public void RestartTimer()
        {
            // Parar e resetar o timer
            _countdownTimer.Stop();
            _countdownTimer.Reset();
            
            // Iniciar novamente
            StartGameTimer();
        }
        
        public string GetFormattedTime()
        {
            if (!_countdownTimer.IsRunning && _countdownTimer.IsFinished)
                return "00:00";
                
            float timeRemaining = _countdownTimer.CurrentTime;
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            
            return $"{minutes:00}:{seconds:00}";
        }
        
        
        private void OnDestroy()
        {
            // Garantir que o timer seja limpo corretamente
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.OnTimerStop -= HandleTimerComplete;
                _countdownTimer.OnTimerStart -= HandleTimerStart;
            }
            
        }
    }
}
