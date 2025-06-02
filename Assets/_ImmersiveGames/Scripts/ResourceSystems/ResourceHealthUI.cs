using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceHealthUI : MonoBehaviour
    {
        [SerializeField] private HealthResource healthSystem; // Referência ao HealthSystem
        [SerializeField] private Image healthBar; // Imagem da barra de vida (fill)
        [SerializeField] private Image backgroundImage; // Imagem que muda de cor
        [SerializeField] private Color[] thresholdColors = new Color[4] // Cores para cada faixa de HP
        {
            Color.green, // 100%-75%
            Color.yellow, // 75%-50%
            new Color(1f, 0.5f, 0f), // 50%-25%
            Color.red // 25%-0%
        };
        private EventBinding<DeathEvent> _deathEventBinding;
        private void OnDisable()
        {
            if (_deathEventBinding == null) return;
            EventBus<DeathEvent>.Unregister(_deathEventBinding);
        }

        private void Initialization()
        {
            if (!healthSystem)
            {
                healthSystem = GetComponentInParent<HealthResource>();
                if (!healthSystem)
                {
                    Debug.LogWarning("HealthSystem não encontrado!", this);
                    return;
                }
            }
            // Conecta aos eventos do HealthSystem
            healthSystem.onValueChanged.AddListener(UpdateHealthBar);
            healthSystem.onThresholdReached.AddListener(UpdateThresholdColor);
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeath);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            ResetUI(); // ⭐ Garante que a UI comece ativada e atualizada
        }

        private void Start()
        {
            Initialization();
        }

        private void ResetUI()
        {
            if (healthBar) healthBar.gameObject.SetActive(true);
            if (backgroundImage) backgroundImage.gameObject.SetActive(true);

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
            if (!healthBar) return;

            float healthPercentage = healthSystem.GetPercentage();
            if (healthPercentage > 0.75f)
                healthBar.color = thresholdColors[0];
            else if (healthPercentage > 0.5f)
                healthBar.color = thresholdColors[1];
            else if (healthPercentage > 0.25f)
                healthBar.color = thresholdColors[2];
            else
                healthBar.color = thresholdColors[3];
        }

        private void OnDeath(DeathEvent evt)
        {
            if (evt.Source != gameObject && !transform.IsChildOrSelf(evt.Source)) return;
            if (backgroundImage)
            {
                backgroundImage.color = thresholdColors[3]; // Cinza ao morrer
            }
            if (healthBar) healthBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
        }
        
    }
}