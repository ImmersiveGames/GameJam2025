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
        [SerializeField] protected string targetResourceId; // ID base do recurso (ex.: Health)
        [SerializeField] protected string targetActorId = ""; // ID do IActor (ex.: Player1)
        [SerializeField] protected ResourceType targetResourceType;
        [SerializeField] protected Image resourceBar;
        [SerializeField] protected Image backgroundImage;
        [SerializeField] protected Image resourceIcon;
        [SerializeField] protected TextMeshProUGUI resourceNameText;
        [SerializeField] protected Color[] thresholdColors = {
            Color.green,
            new Color(1f, 0.922f, 0.016f),
            new Color(1f, 0.5f, 0f),
            Color.red
        };
        [SerializeField] protected float smoothTransitionSpeed = 5f;
        protected IResourceValue _resource;
        protected ResourceConfigSo _config;
        protected float _targetFillAmount;
        private EventBinding<ResourceBindEvent> _bindEventBinding;
        private EventBinding<ResourceValueChangedEvent> _valueChangedBinding;
        private EventBinding<ResourceThresholdCrossedEvent> _thresholdCrossedBinding;
        private EventBinding<ModifierAppliedEvent> _modifierEventBinding;

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(targetResourceId))
            {
                DebugUtility.LogError<ResourceUI>($"Awake: targetResourceId não configurado! Defina o ResourceId base do ResourceConfigSo (ex.: 'Health').", this);
            }
            else if (targetResourceId.Contains("_"))
            {
                DebugUtility.LogError<ResourceUI>($"Awake: targetResourceId='{targetResourceId}' parece conter um prefixo (ex.: 'Player1_Health'). Use o ID base do ResourceConfigSo (ex.: 'Health') e configure targetActorId para 'Player1'.", this);
            }
            DebugUtility.LogVerbose<ResourceUI>($"Awake: targetResourceId={targetResourceId}, targetActorId={targetActorId}, targetResourceType={targetResourceType}, Source={gameObject.name}");
        }

        protected virtual void OnEnable()
        {
            _bindEventBinding = new EventBinding<ResourceBindEvent>(OnResourceBindEvent);
            EventBus<ResourceBindEvent>.Register(_bindEventBinding);
            _valueChangedBinding = new EventBinding<ResourceValueChangedEvent>(OnResourceValueChanged);
            EventBus<ResourceValueChangedEvent>.Register(_valueChangedBinding);
            _thresholdCrossedBinding = new EventBinding<ResourceThresholdCrossedEvent>(OnResourceThresholdCrossed);
            EventBus<ResourceThresholdCrossedEvent>.Register(_thresholdCrossedBinding);
            _modifierEventBinding = new EventBinding<ModifierAppliedEvent>(OnModifierApplied);
            EventBus<ModifierAppliedEvent>.Register(_modifierEventBinding);
            DebugUtility.LogVerbose<ResourceUI>($"OnEnable: Registrados bindings para ResourceBindEvent, ResourceValueChangedEvent, ResourceThresholdCrossedEvent e ModifierAppliedEvent, Source={gameObject.name}");
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
            if (_modifierEventBinding != null)
                EventBus<ModifierAppliedEvent>.Unregister(_modifierEventBinding);

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.EventValueChanged -= UpdateResourceBar;
                if (resourceSystem is IResourceThreshold thresholdSystem)
                {
                    thresholdSystem.OnThresholdReached -= UpdateThresholdColor;
                }
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnDisable: Desregistrados todos os bindings, Source={gameObject.name}");
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
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Configurado ícone e nome para ResourceName={_config.ResourceName}, UniqueId={_config.UniqueId}, Source={gameObject.name}");
            }

            if (_resource is ResourceSystem resourceSystem)
            {
                resourceSystem.EventValueChanged += UpdateResourceBar;
                if (resourceSystem is IResourceThreshold thresholdSystem)
                {
                    thresholdSystem.OnThresholdReached += UpdateThresholdColor;
                }
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Vinculado eventos EventValueChanged e OnThresholdReached para UniqueId={_config?.UniqueId}, Source={gameObject.name}");
            }

            _targetFillAmount = _resource?.GetPercentage() ?? 0f;
            if (resourceBar)
            {
                resourceBar.fillAmount = _targetFillAmount;
                resourceBar.gameObject.SetActive(_targetFillAmount > 0);
                DebugUtility.LogVerbose<ResourceUI>($"Initialize: Barra inicializada com fillAmount={_targetFillAmount:F3}, Ativa={resourceBar.gameObject.activeSelf}, UniqueId={_config?.UniqueId}");
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
                DebugUtility.LogVerbose<ResourceUI>($"ResetUI: Barra redefinida, Percentage={percentage:F3}, fillAmount={resourceBar?.fillAmount:F3}, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
            }
        }

        protected virtual void UpdateResourceBar(float percentage)
        {
            if (!resourceBar)
            {
                DebugUtility.LogWarning<ResourceUI>($"UpdateResourceBar: resourceBar é nulo!, Source={gameObject.name}");
                return;
            }
            _targetFillAmount = percentage;
            resourceBar.gameObject.SetActive(percentage > 0);
            UpdateThresholdColor(percentage);
            DebugUtility.LogVerbose<ResourceUI>($"UpdateResourceBar: Percentage={percentage:F3}, Ativa={resourceBar.gameObject.activeSelf}, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
        }

        protected virtual void UpdateThresholdColor(float percentage)
        {
            if (!resourceBar)
            {
                DebugUtility.LogWarning<ResourceUI>($"UpdateThresholdColor: resourceBar é nulo!, Source={gameObject.name}");
                return;
            }
            resourceBar.color = percentage switch
            {
                > 0.75f => thresholdColors[0],
                > 0.5f => thresholdColors[1],
                > 0.25f => thresholdColors[2],
                _ => thresholdColors[3]
            };
            DebugUtility.LogVerbose<ResourceUI>($"UpdateThresholdColor: Percentage={percentage:F3}, Cor={resourceBar.color}, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
        }

        protected virtual void OnResourceBindEvent(ResourceBindEvent evt)
        {
            // Extrair o ResourceId base do UniqueId (remover prefixo ActorId_)
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1); // Remove "ActorId_"
            }

            // Validar ResourceId, ActorId e Type
            if (resourceId != targetResourceId || 
                (targetResourceType != ResourceType.Custom && evt.Type != targetResourceType) ||
                (!string.IsNullOrEmpty(targetActorId) && evt.ActorId != targetActorId))
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnResourceBindEvent ignorado: UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={targetActorId}, Type={evt.Type}, ExpectedType={targetResourceType}, Source={evt.Source.name}, UI Source={gameObject.name}");
                return;
            }

            DebugUtility.LogVerbose<ResourceUI>($"OnResourceBindEvent: Recebido bind para UniqueId={evt.UniqueId}, ResourceId={resourceId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            _resource = evt.Resource;
            _config = (_resource as ResourceSystem)?.Config;
            if (_resource != null)
            {
                Initialize();
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>($"OnResourceBindEvent: Recurso nulo para UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            }
        }

        protected virtual void OnResourceValueChanged(ResourceValueChangedEvent evt)
        {
            // Extrair ResourceId base
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            if (resourceId != targetResourceId || 
                evt.Source != (_resource as ResourceSystem)?.gameObject ||
                (!string.IsNullOrEmpty(targetActorId) && evt.ActorId != targetActorId))
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnResourceValueChanged ignorado: UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={targetActorId}, Source={evt.Source?.name}, ExpectedSource={(_resource as ResourceSystem)?.gameObject.name}, UI Source={gameObject.name}");
                return;
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnResourceValueChanged: Percentage={evt.Percentage:F3}, Ascending={evt.IsAscending}, UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            UpdateResourceBar(evt.Percentage);
        }

        protected virtual void OnResourceThresholdCrossed(ResourceThresholdCrossedEvent evt)
        {
            // Extrair ResourceId base
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            if (resourceId != targetResourceId || 
                evt.Source != (_resource as ResourceSystem)?.gameObject ||
                (!string.IsNullOrEmpty(targetActorId) && evt.ActorId != targetActorId))
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnResourceThresholdCrossed ignorado: UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={targetActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
                return;
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnResourceThresholdCrossed: Threshold={evt.Info.Threshold:F3}, Ascending={evt.Info.IsAscending}, UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            UpdateThresholdColor(evt.Info.Threshold);
        }

        protected virtual void OnModifierApplied(ModifierAppliedEvent evt)
        {
            // Extrair ResourceId base
            string resourceId = evt.UniqueId;
            if (!string.IsNullOrEmpty(evt.ActorId) && evt.UniqueId.StartsWith(evt.ActorId + "_"))
            {
                resourceId = evt.UniqueId.Substring(evt.ActorId.Length + 1);
            }

            if (resourceId != targetResourceId || 
                (!string.IsNullOrEmpty(targetActorId) && evt.ActorId != targetActorId))
            {
                DebugUtility.LogVerbose<ResourceUI>($"OnModifierApplied ignorado: UniqueId={evt.UniqueId}, ResourceId={resourceId}, ExpectedResourceId={targetResourceId}, ActorId={evt.ActorId}, ExpectedActorId={targetActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
                return;
            }
            DebugUtility.LogVerbose<ResourceUI>($"OnModifierApplied: {(evt.IsAdded ? "Adicionado" : "Removido")} modifier {evt.Modifier.amountPerSecond:F2}/s, UniqueId={evt.UniqueId}, ActorId={evt.ActorId}, Source={evt.Source.name}, UI Source={gameObject.name}");
            // Futuro: Atualizar UI (ex.: ícone de buff)
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
            }

            _resource = newResource;
            _config = (newResource as ResourceSystem)?.Config;

            if (_resource != null)
            {
                Initialize();
                DebugUtility.LogVerbose<ResourceUI>($"SetResource: Novo recurso configurado, UniqueId={_config?.UniqueId}, Source={gameObject.name}");
            }
            else
            {
                DebugUtility.LogWarning<ResourceUI>($"SetResource: Recurso nulo ao tentar configurar!, Source={gameObject.name}");
            }
        }

        protected abstract void InitializeCustomBindings();
        protected abstract void UnregisterCustomBindings();
    }
}