using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    // UI para exibir a saúde do recurso
    public class ResourceHealthUI : MonoBehaviour
    {
        [SerializeField] private HealthResource healthSystem; // Referência ao sistema de saúde
        [SerializeField] private Image healthBar; // Barra de preenchimento
        [SerializeField] private Image backgroundImage; // Imagem de fundo
        [SerializeField] private Image resourceIcon; // Ícone do recurso
        [SerializeField] private Color[] thresholdColors = {
            Color.green, // 100%-75%
            Color.yellow, // 75%-50%
            new(1f, 0.5f, 0f), // 50%-25%
            Color.red // 25%-0%
        };
        [SerializeField] private float smoothTransitionSpeed = 5f; // Velocidade da transição suave
        private float _targetFillAmount; // Alvo para preenchimento suave
        private EventBinding<DeathEvent> _deathEventBinding; // Binding para evento de morte
        private EventBinding<ResourceEvent> _resourceEventBinding; // Binding para eventos de recurso

        // Desregistra eventos ao desativar
        private void OnDisable()
        {
            if (_deathEventBinding != null)
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
            if (_resourceEventBinding != null)
                EventBus<ResourceEvent>.Unregister(_resourceEventBinding);
        }

        // Inicializa a UI
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
            // Define o ícone do recurso
            if (healthSystem.Config && resourceIcon)
                resourceIcon.sprite = healthSystem.Config.ResourceIcon;

            healthSystem.onValueChanged.AddListener(UpdateHealthBar);
            healthSystem.onThresholdReached.AddListener(UpdateThresholdColor);
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeath);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            _resourceEventBinding = new EventBinding<ResourceEvent>(OnResourceEvent);
            EventBus<ResourceEvent>.Register(_resourceEventBinding);
            ResetUI();
        }

        private void Start()
        {
            Initialization();
        }

        // Atualiza a transição suave da barra
        private void Update()
        {
            if (healthBar && healthBar.gameObject.activeSelf && Mathf.Abs(healthBar.fillAmount - _targetFillAmount) > 0.01f)
            {
                healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, _targetFillAmount, Time.deltaTime * smoothTransitionSpeed);
            }
        }

        // Reseta a UI para estado inicial
        private void ResetUI()
        {
            if (healthBar) healthBar.gameObject.SetActive(true);
            if (backgroundImage) backgroundImage.gameObject.SetActive(true);

            UpdateHealthBar(healthSystem.GetPercentage());
            UpdateThresholdColor(healthSystem.GetPercentage());
        }

        // Atualiza o preenchimento da barra
        private void UpdateHealthBar(float healthPercentage)
        {
            if (!healthBar) return;
            _targetFillAmount = healthPercentage;
            // Desativa a barra se a saúde chegar a zero
            healthBar.gameObject.SetActive(!(healthPercentage <= 0));
        }

        // Atualiza a cor com base na porcentagem
        private void UpdateThresholdColor(float threshold)
        {
            if (!healthBar) return;

            float healthPercentage = healthSystem.GetPercentage();
            healthBar.color = healthPercentage switch
            {
                > 0.75f => thresholdColors[0],
                > 0.5f => thresholdColors[1],
                > 0.25f => thresholdColors[2],
                _ => thresholdColors[3]
            };
        }

        // Reage ao evento de morte
        private void OnDeath(DeathEvent evt)
        {
            if (evt.Source != gameObject && !transform.IsChildOrSelf(evt.Source)) return;
            if (backgroundImage)
            {
                backgroundImage.color = thresholdColors[3]; // Vermelho ao morrer
            }
            if (healthBar) healthBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
        }

        // Reage a eventos de recurso (ex.: reset)
        private void OnResourceEvent(ResourceEvent evt)
        {
            if (evt.Source != healthSystem.gameObject) return;
            if (evt.Percentage >= 1f) // Recurso cheio (reset)
            {
                ResetUI();
            }
        }
    }
}