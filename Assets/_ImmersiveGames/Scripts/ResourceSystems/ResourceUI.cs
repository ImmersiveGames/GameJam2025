using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class ResourceUI : MonoBehaviour
    {
        [SerializeField] protected string targetResourceId;
        [SerializeField] protected ResourceType targetResourceType;
        [SerializeField] protected Image resourceBar;
        [SerializeField] protected Image backgroundImage;
        [SerializeField] protected Image resourceIcon;
        [SerializeField] protected TextMeshProUGUI resourceNameText;
        [SerializeField] protected Color[] thresholdColors = {
            Color.green,
            Color.yellow,
            new Color(1f, 0.5f, 0f),
            Color.red
        };
        [SerializeField] protected float smoothTransitionSpeed = 50f; // Aumentado para interpolação mais suave
        protected IResourceValue _resource;
        protected ResourceConfigSo _config;
        protected float _targetFillAmount;
        private EventBinding<ResourceBindEvent> _bindEventBinding;
        private EventBinding<ResourceValueChangedEvent> _valueChangedBinding;
        private EventBinding<ResourceThresholdCrossedEvent> _thresholdCrossedBinding;

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(targetResourceId))
            {
                DebugUtility.LogError<ResourceUI>($"Awake: targetResourceId não configurado! Certifique-se de definir um UniqueId correspondente ao ResourceConfigSo.", this);
            }
            DebugUtility.LogVerbose<ResourceUI>($"Awake: targetResourceId={targetResourceId}, targetResourceType={targetResourceType}");
        }

        protected virtual void OnEnable()
        {
            _bindEventBinding = new EventBinding<ResourceBindEvent>(OnResourceBindEvent);
            EventBus<ResourceBindEvent>.Register(_bindEventBinding);
            _valueChangedBinding = new EventBinding<ResourceValueChangedEvent>(OnResourceValueChanged);
            EventBus<ResourceValueChangedEvent>.Register(_valueChangedBinding);
            _thresholdCrossedBinding = new EventBinding<ResourceThresholdCrossedEvent>(OnResourceThresholdCrossed);
            EventBus<ResourceThresholdCrossedEvent>.Register(_thresholdCrossedBinding);
            DebugUtility.LogVerbose<ResourceUI>($"OnEnable: Registrados bindings para ResourceBindEvent, ResourceValueChangedEvent e ResourceThresholdCrossedEvent");
            InitializeCustomBindings();
        }

        protected virtual void OnDisable()
        {
            if (_bindEventBinding != null)
                EventBus<ResourceBindEvent>.Unregister(_bindEventBinding);
            if (_valueChangedBinding != null)
                EventBus<ResourceValueChangedEvent>.Unregister(_valueChangedBinding);
            if (_thresholdCrossedBinding != null)
                EventBus<ResourceThresholdCrossedEvent>.Unregister(_thresholdCrossedBinding);

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.EventValueChanged -= UpdateResourceBar;
                if (resourceSystem is IResourceThreshold thresholdSystem)
                {
                    thresholdSystem.OnThresholdReached -= UpdateThresholdColor;
                }
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnDisable: Desregistrados todos os bindings");
            UnregisterCustomBindings();
        }

        protected virtual void Initialize()
        {
            if (_config != null)
            {
                if (resourceIcon != null)
                    resourceIcon.sprite = _config.ResourceIcon;
                if (resourceNameText != null)
                    resourceNameText.text = _config.ResourceName;
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Configurado ícone e nome para ResourceName={_config.ResourceName}, UniqueId={_config.UniqueId}");
            }

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.EventValueChanged += UpdateResourceBar;
                if (resourceSystem is IResourceThreshold thresholdSystem)
                {
                    thresholdSystem.OnThresholdReached += UpdateThresholdColor;
                }
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Vinculado eventos EventValueChanged e OnThresholdReached para UniqueId={_config?.UniqueId}");
            }

            _targetFillAmount = _resource?.GetPercentage() ?? 0f;
            if (resourceBar)
            {
                resourceBar.fillAmount = _targetFillAmount;
                resourceBar.gameObject.SetActive(_targetFillAmount > 0);
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Barra inicializada com fillAmount={_targetFillAmount:F3}, Ativa={resourceBar.gameObject.activeSelf}");
            }
            UpdateThresholdColor(_targetFillAmount);
        }

        protected virtual void Update()
        {
            if (resourceBar && resourceBar.gameObject.activeSelf && !Mathf.Approximately(resourceBar.fillAmount, _targetFillAmount))
            {
                resourceBar.fillAmount = Mathf.Lerp(resourceBar.fillAmount, _targetFillAmount, Time.deltaTime * smoothTransitionSpeed);
            }
        }

        protected virtual void ResetUI()
        {
            if (resourceBar) resourceBar.gameObject.SetActive(true);
            if (backgroundImage) backgroundImage.gameObject.SetActive(true);

            if (_resource != null)
            {
                float percentage = _resource.GetPercentage();
                _targetFillAmount = percentage;
                if (resourceBar)
                {
                    resourceBar.fillAmount = percentage;
                    resourceBar.gameObject.SetActive(percentage > 0);
                }
                UpdateThresholdColor(percentage);
                DebugUtility.LogVerbose<ResourceUI>($"ResetUI: Barra redefinida, Percentage={percentage:F3}, fillAmount={resourceBar?.fillAmount:F3}, UniqueId={_config?.UniqueId}");
            }
        }

        protected virtual void UpdateResourceBar(float percentage)
        {
            if (!resourceBar)
            {
                DebugUtility.LogWarning<ResourceUI>($"UpdateResourceBar: resourceBar é nulo!");
                return;
            }
            _targetFillAmount = percentage;
            resourceBar.gameObject.SetActive(percentage > 0);
            DebugUtility.LogVerbose<ResourceUI>($"UpdateResourceBar: Percentage={percentage:F3}, Ativa={resourceBar.gameObject.activeSelf}, UniqueId={_config?.UniqueId}");
        }

        protected virtual void UpdateThresholdColor(float threshold)
        {
            if (!resourceBar)
            {
                DebugUtility.LogWarning<ResourceUI>($"UpdateThresholdColor: resourceBar é nulo!");
                return;
            }
            float percentage = _resource?.GetPercentage() ?? 0f;
            resourceBar.color = percentage switch
            {
                > 0.75f => thresholdColors[0],
                > 0.5f => thresholdColors[1],
                > 0.25f => thresholdColors[2],
                _ => thresholdColors[3]
            };
            DebugUtility.LogVerbose<ResourceUI>($"UpdateThresholdColor: Percentage={percentage:F3}, Cor={resourceBar.color}, UniqueId={_config?.UniqueId}");
        }

        protected virtual void OnResourceBindEvent(ResourceBindEvent evt)
        {
            if (evt.UniqueId != targetResourceId || (targetResourceType != ResourceType.Custom && evt.Type != targetResourceType))
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, Expected={targetResourceId}, Type={evt.Type}, ExpectedType={targetResourceType}");
                return;
            }

            DebugUtility.LogVerbose<ResourceUI>($"OnResourceBindEvent: Recebido bind para UniqueId={evt.UniqueId}, Source={evt.Source.name}");
            _resource = evt.Resource;
            _config = (_resource as ResourceSystem)?.Config;
            if (_resource != null)
            {
                Initialize();
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>($"OnResourceBindEvent: Recurso nulo para ID={targetResourceId} no Actor={evt.Source.name}!");
            }
        }

        protected virtual void OnResourceValueChanged(ResourceValueChangedEvent evt)
        {
            if (evt.UniqueId != targetResourceId || evt.Source != (_resource as ResourceSystem)?.gameObject)
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnResourceValueChanged ignorado: UniqueId={evt.UniqueId}, Expected={targetResourceId}, Source={evt.Source?.name}");
                return;
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnResourceValueChanged: Percentage={evt.Percentage:F3}, Ascending={evt.IsAscending}, UniqueId={evt.UniqueId}");
            UpdateResourceBar(evt.Percentage);
        }

        protected virtual void OnResourceThresholdCrossed(ResourceThresholdCrossedEvent evt)
        {
            if (evt.UniqueId != targetResourceId || evt.Source != (_resource as ResourceSystem)?.gameObject)
                return;
            DebugUtility.LogVerbose<ResourceUI>($"OnResourceThresholdCrossed: Threshold={evt.Info.Threshold:F3}, Ascending={evt.Info.IsAscending}, UniqueId={evt.UniqueId}");
            UpdateThresholdColor(evt.Info.Threshold);
        }

        public void SetResource(IResourceValue newResource)
        {
            if (_resource is ResourceSystem oldResourceSystem)
            {
                oldResourceSystem.EventValueChanged -= UpdateResourceBar;
                if (oldResourceSystem is IResourceThreshold oldThresholdSystem)
                {
                    oldThresholdSystem.OnThresholdReached -= UpdateThresholdColor;
                }
                DebugUtility.LogVerbose<ResourceUI>($"SetResource: Desvinculado eventos do recurso antigo");
            }

            _resource = newResource;
            _config = (newResource as ResourceSystem)?.Config;

            if (_resource != null)
            {
                Initialize();
                DebugUtility.LogVerbose<ResourceUI>($"SetResource: Novo recurso configurado, UniqueId={_config?.UniqueId}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>($"SetResource: Recurso nulo ao tentar configurar!");
            }
        }

        protected abstract void InitializeCustomBindings();
        protected abstract void UnregisterCustomBindings();
    }
}