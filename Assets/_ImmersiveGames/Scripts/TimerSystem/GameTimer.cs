using UnityEngine;
using System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine.Serialization;
using UnityUtils;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    public class GameTimer : MonoBehaviour
    {
        private CountdownTimer _countdownTimer;
        private int _gameDuration = 300; // Duração do jogo em segundos (5 minutos)
        
        // Eventos que podem ser assinados por outros scripts
        private GameManager _gameManager;
        
        // Propriedade para acessar o tempo restante
        public float RemainingTime => _countdownTimer.IsRunning ? _countdownTimer.CurrentTime : 0f;
        
        public event Action EventTimeEnded;
        public event Action EventTimerStarted;
        protected void Awake()
        {
            _gameManager = GameManager.Instance;
            _gameDuration = _gameManager.gameConfig.timerGame;
            // Criar o CountdownTimer
            _countdownTimer = new CountdownTimer(_gameDuration);
            
            // Registrar eventos internos da biblioteca de timer
            _countdownTimer.OnTimerStop += HandleTimerComplete;
            _countdownTimer.OnTimerStart += HandleTimerStart;
        }

        private void OnEnable()
        {
            _gameManager.EventStartGame += StartGameTimer;
            _gameManager.EventGameOver += PauseTimer;
            _gameManager.EventVictory += PauseTimer;
            _gameManager.EventPauseGame += PauseTimerHandler;
        }
        private void PauseTimerHandler(bool obj)
        {
            if(obj == true)
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
            _countdownTimer.Stop();
            if (_countdownTimer.IsRunning)
                return;
            
            // Reiniciar o timer para a duração completa
            _countdownTimer.Reset(_gameDuration);
            
            // Iniciar o timer
            _countdownTimer.Start();
            EventTimerStarted?.Invoke();
            DebugUtility.LogVerbose<GameTimer>($"Começou");
        }
        
        private void HandleTimerComplete()
        {
            Debug.Log("Tempo acabou!");
            EventTimeEnded?.Invoke();
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
        
        // Método Update para verificar se precisamos atualizar o timer manualmente
        private void Update()
        {
            // Se a biblioteca ImprovedTimers exigir atualização manual, implemente aqui
            // Por exemplo: _countdownTimer.Tick(Time.deltaTime);
            if (_countdownTimer.IsRunning)
            {
                DebugUtility.Log<GameTimer>($"Progresso: {_countdownTimer.Progress}");
            }
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
            
            _gameManager.EventStartGame -= StartGameTimer;
            _gameManager.EventGameOver -= PauseTimer;
            _gameManager.EventVictory -= PauseTimer;
            _gameManager.EventPauseGame -= PauseTimerHandler;
        }
    }
}
