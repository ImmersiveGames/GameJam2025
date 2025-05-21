using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _ImmersiveGames.Scripts.TimerSystem
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private GameTimer gameTimer;
        [SerializeField] private TextMeshProUGUI timerText;
    
        [SerializeField] private Image timerFillImage;
    
        [SerializeField] private Color normalColor = Color.green;
        
        [SerializeField] private Color warningColor = Color.yellow;
        
        [SerializeField] private Color dangerColor = Color.red;
       
        [SerializeField] private float warningThreshold = 120f; // 2 minutos
        
        [SerializeField] private float dangerThreshold = 30f;  // 30 segundos
        
        private float _initialDuration = 300f; // 5 minutos

        
        private void Start()
        {
            if (!gameTimer)
            {
                gameTimer = FindFirstObjectByType<GameTimer>();
            }
            
            if (gameTimer)
            {
                _initialDuration = 300f; // 5 minutos em segundos
                
                // Acessar o CountdownTimer através da reflexão ou expondo-o no GameTimer
                // Como não podemos modificar GameTimer diretamente, vamos criar um método na classe GameTimer
                // que retorna o CountdownTimer para podermos registrar nos eventos
                
                // Registrar nos eventos disponíveis
                RegisterTimerEvents();
            }
            
            // Inicializar UI
            UpdateTimerDisplay();
        }
        
        private void RegisterTimerEvents()
        {
            if (gameTimer)
            {
                // Como não temos acesso ao CountdownTimer diretamente, 
                // vamos detectar eventos por mudanças no estado
                Debug.Log("UI: Timer inicializado!");
            }
        }
        
        private void Update()
        {
            UpdateTimerDisplay();
        }
        
        private void UpdateTimerDisplay()
        {
            if (!gameTimer)
                return;
                
            // Atualizar texto
            if (timerText)
            {
                timerText.text = gameTimer.GetFormattedTime();
            }
            
            // Atualizar barra de progresso
            if (!timerFillImage) return;
            float remainingTime = gameTimer.RemainingTime;
            float normalizedTime = remainingTime / _initialDuration;
            timerFillImage.fillAmount = normalizedTime;
                
            // Mudar cor com base no tempo restante
            if (remainingTime <= dangerThreshold)
            {
                timerFillImage.color = dangerColor;
            }
            else if (remainingTime <= warningThreshold)
            {
                timerFillImage.color = warningColor;
            }
            else
            {
                timerFillImage.color = normalColor;
            }
                
            // Verificar quando o timer chega a 0 para mostrar mensagem
            if (remainingTime <= 0)
            {
                Debug.Log("UI: Tempo esgotado!");
            }
        }
    }
}
