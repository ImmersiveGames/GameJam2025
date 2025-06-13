using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour resource; // Referência ao recurso (IResource)
        [SerializeField] private string targetResourceId; // Identificador único para o recurso
        [SerializeField] private ResourceType targetResourceType; // Tipo de recurso para filtrar (opcional)
        [SerializeField] private Image resourceBar; // Barra de preenchimento
        [SerializeField] private Image backgroundImage; // Imagem de fundo
        [SerializeField] private Image resourceIcon; // Ícone do recurso
        [SerializeField] private TextMeshProUGUI resourceNameText; // Texto para nome do recurso
        [SerializeField] private Color[] thresholdColors = {
            Color.green, // 100%-75%
            Color.yellow, // 75%-50%
            new(1f, 0.5f, 0f), // 50%-25%
            Color.red // 25%-0%
        };
        [SerializeField] private float smoothTransitionSpeed = 5f; // Velocidade da transição suave
        private IResource _resource; // Interface para o recurso
        private ResourceConfigSo _config; // Configuração do recurso
        private float _targetFillAmount; // Alvo para preenchimento suave
        private EventBinding<ResourceBindEvent> _bindEventBinding;
        private EventBinding<ResourceEvent> _resourceEventBinding;

        private void Awake()
        {
            if (string.IsNullOrEmpty(targetResourceId))
            {
                DebugUtility.LogError<ResourceUI>($"targetResourceId não configurado! Certifique-se de definir um UniqueId correspondente ao ResourceConfigSo.", this);
            }
        }

        private void OnEnable()
        {
            _bindEventBinding = new EventBinding<ResourceBindEvent>(OnResourceBindEvent);
            EventBus<ResourceBindEvent>.Register(_bindEventBinding);
            _resourceEventBinding = new EventBinding<ResourceEvent>(OnResourceEvent);
            EventBus<ResourceEvent>.Register(_resourceEventBinding);
        }

        private void OnDisable()
        {
            if (_bindEventBinding != null)
                EventBus<ResourceBindEvent>.Unregister(_bindEventBinding);
            if (_resourceEventBinding != null)
                EventBus<ResourceEvent>.Unregister(_resourceEventBinding);

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.onValueChanged.RemoveListener(UpdateResourceBar);
                resourceSystem.onThresholdReached.RemoveListener(UpdateThresholdColor);
            }
        }

        private void Initialize()
        {
            if (_config != null)
            {
                if (resourceIcon != null)
                    resourceIcon.sprite = _config.ResourceIcon;
                if (resourceNameText != null)
                    resourceNameText.text = _config.ResourceName;
            }

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.onValueChanged.AddListener(UpdateResourceBar);
                resourceSystem.onThresholdReached.AddListener(UpdateThresholdColor);
            }

            ResetUI();
        }

        private void Update()
        {
            if (resourceBar && resourceBar.gameObject.activeSelf && Mathf.Abs(resourceBar.fillAmount - _targetFillAmount) > 0.01f)
            {
                resourceBar.fillAmount = Mathf.Lerp(resourceBar.fillAmount, _targetFillAmount, Time.deltaTime * smoothTransitionSpeed);
            }
        }

        private void ResetUI()
        {
            if (resourceBar) resourceBar.gameObject.SetActive(true);
            if (backgroundImage) backgroundImage.gameObject.SetActive(true);

            if (_resource != null)
            {
                UpdateResourceBar(_resource.GetPercentage());
                UpdateThresholdColor(_resource.GetPercentage());
            }
        }

        private void UpdateResourceBar(float percentage)
        {
            if (!resourceBar) return;
            _targetFillAmount = percentage;
            resourceBar.gameObject.SetActive(!(percentage <= 0));
        }

        private void UpdateThresholdColor(float threshold)
        {
            if (!resourceBar) return;

            float percentage = _resource?.GetPercentage() ?? 0f;
            resourceBar.color = percentage switch
            {
                > 0.75f => thresholdColors[0],
                > 0.5f => thresholdColors[1],
                > 0.25f => thresholdColors[2],
                _ => thresholdColors[3]
            };
        }

        private void OnResourceBindEvent(ResourceBindEvent evt)
        {
            if (evt.UniqueId != targetResourceId) return;

            DebugUtility.Log<ResourceUI>($"Recebido bind event para UniqueId {evt.UniqueId}, inicializando recurso em {evt.Source.name}");
            _resource = evt.Resource;
            _config = (_resource as ResourceSystem)?.Config;
            if (_resource != null)
            {
                Initialize();
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>($"Recurso nulo para ID {targetResourceId} no GameObject {evt.Source.name}!", this);
            }
        }

        private void OnResourceEvent(ResourceEvent evt)
        {
            if (evt.UniqueId != targetResourceId) return;

            if (_resource == null)
            {
                DebugUtility.LogWarning<ResourceUI>($"Recebido ResourceEvent antes do bind para ID {targetResourceId}, esperando ResourceBindEvent...", this);
                return;
            }

            if (evt.Percentage >= 1f)
            {
                ResetUI();
            }
            UpdateResourceBar(evt.Percentage);
        }

        public void SetResource(IResource newResource)
        {
            if (_resource is ResourceSystem oldResourceSystem)
            {
                oldResourceSystem.onValueChanged.RemoveListener(UpdateResourceBar);
                oldResourceSystem.onThresholdReached.RemoveListener(UpdateThresholdColor);
            }

            _resource = newResource;
            _config = (newResource as ResourceSystem)?.Config;

            if (_resource != null)
            {
                Initialize();
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>("Recurso nulo ao tentar configurar!", this);
            }
        }
    }
}