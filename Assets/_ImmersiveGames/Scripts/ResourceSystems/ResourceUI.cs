using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Para TextMeshPro, se quiser exibir o nome do recurso

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour resource; // Referência ao recurso (IResource)
        [SerializeField] private string targetResourceId; // Identificador opcional para encontrar o recurso
        [SerializeField] private ResourceType targetResourceType; // Tipo de recurso para filtrar (opcional)
        [SerializeField] private Image resourceBar; // Barra de preenchimento
        [SerializeField] private Image backgroundImage; // Imagem de fundo
        [SerializeField] private Image resourceIcon; // Ícone do recurso
        [SerializeField] private TextMeshProUGUI resourceNameText; // Texto para nome do recurso (opcional)
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
        private EventBinding<ResourceEvent> _resourceEventBinding; // Binding para eventos de recurso

        private void Awake()
        {
            // Valida e configura o recurso
            if (resource != null && resource is IResource iResource)
            {
                _resource = iResource;
                _config = (resource as ResourceSystem)?.Config;
            }
            else if (!string.IsNullOrEmpty(targetResourceId) || targetResourceType != ResourceType.Custom)
            {
                _resource = FindResource();
                _config = (_resource as ResourceSystem)?.Config;
            }
        }

        private void OnEnable()
        {
            if (_resource == null)
            {
                DebugUtility.LogWarning<ResourceUI>("Recurso não configurado!", this);
                return;
            }
            Initialize();
        }

        private void OnDisable()
        {
            // Desregistra eventos
            if (_resourceEventBinding != null)
                EventBus<ResourceEvent>.Unregister(_resourceEventBinding);

            // Remove listeners do recurso
            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.onValueChanged.RemoveListener(UpdateResourceBar);
                resourceSystem.onThresholdReached.RemoveListener(UpdateThresholdColor);
            }
        }

        // Inicializa a UI
        private void Initialize()
        {
            // Configura elementos visuais com base no ResourceConfigSo
            if (_config != null)
            {
                if (resourceIcon != null)
                    resourceIcon.sprite = _config.ResourceIcon;
                if (resourceNameText != null)
                    resourceNameText.text = _config.ResourceName;
            }

            // Registra listeners para eventos do recurso
            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.onValueChanged.AddListener(UpdateResourceBar);
                resourceSystem.onThresholdReached.AddListener(UpdateThresholdColor);
            }

            // Registra eventos do EventBus
            _resourceEventBinding = new EventBinding<ResourceEvent>(OnResourceEvent);
            EventBus<ResourceEvent>.Register(_resourceEventBinding);

            ResetUI();
        }

        // Atualiza a transição suave da barra
        private void Update()
        {
            if (resourceBar && resourceBar.gameObject.activeSelf && Mathf.Abs(resourceBar.fillAmount - _targetFillAmount) > 0.01f)
            {
                resourceBar.fillAmount = Mathf.Lerp(resourceBar.fillAmount, _targetFillAmount, Time.deltaTime * smoothTransitionSpeed);
            }
        }

        // Reseta a UI para estado inicial
        private void ResetUI()
        {
            if (resourceBar) resourceBar.gameObject.SetActive(true);
            if (backgroundImage) backgroundImage.gameObject.SetActive(true);

            UpdateResourceBar(_resource.GetPercentage());
            UpdateThresholdColor(_resource.GetPercentage());
        }

        // Atualiza o preenchimento da barra
        private void UpdateResourceBar(float percentage)
        {
            if (!resourceBar) return;
            _targetFillAmount = percentage;
            resourceBar.gameObject.SetActive(!(percentage <= 0));
        }

        // Atualiza a cor com base na porcentagem
        private void UpdateThresholdColor(float threshold)
        {
            if (!resourceBar) return;

            float percentage = _resource.GetPercentage();
            resourceBar.color = percentage switch
            {
                > 0.75f => thresholdColors[0],
                > 0.5f => thresholdColors[1],
                > 0.25f => thresholdColors[2],
                _ => thresholdColors[3]
            };
        }

        // Reage a eventos de recurso
        private void OnResourceEvent(ResourceEvent evt)
        {
            if (_resource == null || evt.Source != (_resource as MonoBehaviour)?.gameObject) return;
            if (evt.Percentage >= 1f) // Recurso cheio (reset)
            {
                ResetUI();
            }
            UpdateResourceBar(evt.Percentage);
        }

        // Método para encontrar o recurso por ID ou tipo
        private IResource FindResource()
        {
            IEnumerable<IResource> resources = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IResource>();
            foreach (var findResource in resources)
            {
                if (findResource is ResourceSystem resourceSystem)
                {
                    bool matchesId = !string.IsNullOrEmpty(targetResourceId) && 
                        (resourceSystem.gameObject.name == targetResourceId || resourceSystem.Config.ResourceName == targetResourceId);
                    bool matchesType = targetResourceType != ResourceType.Custom && resourceSystem.Config.ResourceType == targetResourceType;
                    if (matchesId || matchesType)
                    {
                        return findResource;
                    }
                }
            }
            DebugUtility.LogWarning<ResourceUI>($"Recurso com ID {targetResourceId} ou tipo {targetResourceType} não encontrado!", this);
            return null;
        }

        // Método público para configurar o recurso dinamicamente
        public void SetResource(IResource newResource)
        {
            // Remove listeners do recurso anterior
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