using System;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterHealthUI : MonoBehaviour
    {
        [SerializeField] private EaterHealth healthSystem; // Referência ao HealthSystem
        [SerializeField] private Image healthBar; // Imagem da barra de vida (fill)
        [SerializeField] private Image backgroundImage; // Imagem que muda de cor
        [SerializeField] private Color[] thresholdColors = new Color[4] // Cores para cada faixa de HP
        {
            Color.green, // 100%-75%
            Color.yellow, // 75%-50%
            Color.red, // 50%-25%
            Color.gray // 25%-0%
        };

        private void Start()
        {
            if (!healthSystem)
            {
                healthSystem = GetComponentInParent<EaterHealth>();
                if (!healthSystem)
                {
                    Debug.LogWarning("HealthSystem não encontrado!", this);
                    return;
                }
            }

            // Conecta aos eventos do HealthSystem
            healthSystem.onValueChanged.AddListener(UpdateHealthBar);
            healthSystem.onThresholdReached.AddListener(UpdateThresholdColor);
            healthSystem.EventDeath += OnDeath;

            // Inicializa a UI
            UpdateHealthBar(healthSystem.GetPercentage());
            UpdateThresholdColor(healthSystem.GetPercentage());
        }

        private void UpdateHealthBar(float healthPercentage)
        {
            if (healthBar != null)
            {
                healthBar.fillAmount = healthPercentage; // Atualiza o preenchimento da barra
            }
        }

        private void UpdateThresholdColor(float threshold)
        {
            if (backgroundImage == null) return;

            // Determina a cor com base na porcentagem de HP
            float healthPercentage = healthSystem.GetPercentage();
            if (healthPercentage > 0.75f)
                backgroundImage.color = thresholdColors[0]; // Verde
            else if (healthPercentage > 0.5f)
                backgroundImage.color = thresholdColors[1]; // Amarelo
            else if (healthPercentage > 0.25f)
                backgroundImage.color = thresholdColors[2]; // Vermelho
            else
                backgroundImage.color = thresholdColors[3]; // Cinza
        }

        private void OnDeath()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = thresholdColors[3]; // Cinza ao morrer
            }
        }

        private void OnDisable()
        {
            healthSystem.EventDeath -= OnDeath;
        }
    }
}