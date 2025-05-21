using UnityEngine;
using System;
using ImprovedTimers;
using UnityEngine.Serialization;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    public class GameTimer : MonoBehaviour
    {
        [SerializeField] private float gameDuration = 300f; // 5 minutos em segundos
        
        private CountdownTimer _countdownTimer;
        
        // Eventos que podem ser assinados por outros scripts
        public event Action OnGameStarted;
        public event Action OnGameEnded;
        
        // Propriedade para acessar o tempo restante
        public float RemainingTime => _countdownTimer.IsRunning ? _countdownTimer.CurrentTime : 0f;
        
        private void Awake()
        {
            // Criar o CountdownTimer
            _countdownTimer = new CountdownTimer(gameDuration);
            
            // Registrar eventos internos da biblioteca de timer
            _countdownTimer.OnTimerStop += HandleTimerComplete;
            _countdownTimer.OnTimerStart += HandleTimerStart;
        }
        
        private void Start()
        {
            // Inicia o timer quando o jogo começa
            StartGameTimer();
        }
        
        private void HandleTimerStart()
        {
            Debug.Log("Jogo iniciado! Timer de 5 minutos começou.");
            OnGameStarted?.Invoke();
        }
        
        public void StartGameTimer()
        {
            if (_countdownTimer.IsRunning)
                return;
            
            // Reiniciar o timer para a duração completa
            _countdownTimer.Reset(gameDuration);
            
            // Iniciar o timer
            _countdownTimer.Start();
        }
        
        private void HandleTimerComplete()
        {
            Debug.Log("Tempo acabou!");
            OnGameEnded?.Invoke();
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
