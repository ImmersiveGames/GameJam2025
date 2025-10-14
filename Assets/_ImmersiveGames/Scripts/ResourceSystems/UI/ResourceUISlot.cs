using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceUISlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image pendingFillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        private IResourceSlotStrategy _slotStrategy;
    
        private float _currentFill;
        private float _previousFill;
        private string _currentText = "";
        private bool _isFirstConfigure = true;
        private ResourceUIStyle _currentStyle;
    
        public ResourceType Type { get; private set; }
        private ResourceInstanceConfig InstanceConfig { get; set; }
        public Image FillImage => fillImage;
        public Image PendingFillImage => pendingFillImage;
        public TextMeshProUGUI ValueText => valueText;
        public Image IconImage => iconImage;
        public GameObject RootPanel => rootPanel;
        
        public float GetCurrentFill() => _currentFill;
        public ResourceUIStyle GetCurrentStyle() => _currentStyle;

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
            _slotStrategy = new InstantSlotStrategy();
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig config)
        {
            Type = type;
            InstanceConfig = config;
            _currentStyle = config?.slotStyle;

            // Aplicar estilo básico imediatamente
            ApplyBaseStyleImmediate();
        
            // Usa factory se disponível
            _slotStrategy = DependencyManager.Instance.TryGetGlobal(out IResourceSlotStrategyFactory factory) 
                ? factory.CreateStrategy(config?.fillAnimationType ?? FillAnimationType.Instant) 
                : CreateStrategyDirectly(config?.fillAnimationType ?? FillAnimationType.Instant);

            // CORREÇÃO: Log da estratégia criada
            DebugUtility.LogVerbose<ResourceUISlot>($"🎯 Strategy created: {_slotStrategy.GetType().Name} for {config?.fillAnimationType ?? FillAnimationType.Instant}");
        
            if (InstanceConfig?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = InstanceConfig.resourceDefinition.icon;
            
            gameObject.name = $"{actorId}_{type}";
        
            // Estado inicial
            _currentFill = 1f;
            _previousFill = 1f;
            
            // Aplicar visuals imediatamente
            ApplyVisualsImmediate();
            
            DebugUtility.LogVerbose<ResourceUISlot>($"✅ Slot initialized for {actorId}.{type} - Style: {_currentStyle?.name ?? "None"}");
        }

        // NOVO: Método melhorado para aplicar estilo base
        private void ApplyBaseStyleImmediate()
        {
            if (_currentStyle == null) return;

            DebugUtility.LogVerbose<ResourceUISlot>($"🎨 Applying base style: {_currentStyle.name}");

            // Aplicar cores básicas apenas se não forem sobrescritas pela estratégia
            if (pendingFillImage != null && _currentStyle.pendingColor != default)
            {
                pendingFillImage.color = _currentStyle.pendingColor;
            }

            // CORREÇÃO: Garantir que as imagens estão com os valores iniciais corretos
            if (fillImage != null) 
                fillImage.fillAmount = 1f;
            if (pendingFillImage != null) 
                pendingFillImage.fillAmount = 0f;
        }

        private IResourceSlotStrategy CreateStrategyDirectly(FillAnimationType animationType)
        {
            return animationType switch
            {
                FillAnimationType.BasicAnimated => new BasicAnimatedFillStrategy(),
                FillAnimationType.AdvancedAnimated => new AdvancedAnimatedFillStrategy(),
                FillAnimationType.SmoothAnimated => new SmoothAnimatedFillStrategy(),
                FillAnimationType.PulseAnimated => new PulseAnimatedFillStrategy(),
                _ => new InstantSlotStrategy()
            };
        }

        public void Configure(IResourceValue data)
        {
            if (data == null) return;

            float newValue = data.GetPercentage();
        
            DebugUtility.LogVerbose<ResourceUISlot>($"🔄 Slot Configure: {Type} - Previous: {_currentFill}, New: {newValue}, Style: {_currentStyle?.name ?? "None"}");
        
            _previousFill = _currentFill;
            _currentFill = newValue;
            _currentText = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";
        
            // CORREÇÃO: Verificar se o estilo mudou
            if (InstanceConfig?.slotStyle != _currentStyle)
            {
                _currentStyle = InstanceConfig?.slotStyle;
                ApplyBaseStyleImmediate();
            }
        
            if (_isFirstConfigure)
            {
                ApplyVisualsImmediate();
                _isFirstConfigure = false;
            }
            else
            {
                ApplyVisuals();
            }
            
            SetVisible(true);
        }

        public ResourceInstanceConfig GetInstanceConfig() => InstanceConfig;

        private void ApplyVisualsImmediate()
        {
            DebugUtility.LogVerbose<ResourceUISlot>($"⚡ ApplyVisualsImmediate: {Type} - Current: {_currentFill}, Style: {_currentStyle?.name}");
            
            var instantStrategy = new InstantSlotStrategy();
            instantStrategy.ApplyFill(this, _currentFill, _currentFill, _currentStyle);
            instantStrategy.ApplyText(this, _currentText, _currentStyle);
            
            ClearAllTween();
        }

        private void ApplyVisuals()
        {
            DebugUtility.LogVerbose<ResourceUISlot>($"🎨 ApplyVisuals: {Type} - Current: {_currentFill}, Previous: {_previousFill}, Style: {_currentStyle?.name}");
        
            _slotStrategy.ApplyFill(this, _currentFill, _previousFill, _currentStyle);
            _slotStrategy.ApplyText(this, _currentText, _currentStyle);
        }

        // MÉTODO ADICIONAL: Forçar reaplicação do estilo
        public void RefreshStyle()
        {
            _currentStyle = InstanceConfig?.slotStyle;
            ApplyBaseStyleImmediate();
            ApplyVisualsImmediate();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔄 Style refreshed: {Type} - {_currentStyle?.name}");
        }

        public void Clear()
        {
            _slotStrategy.ClearVisuals(this);
            StopAllCoroutines();
            _currentFill = 0f;
            _previousFill = 0f;
            _isFirstConfigure = true;
            _currentStyle = null;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
        }

        [ContextMenu("Force Visual Update")]
        public void ForceVisualUpdate()
        {
            ApplyBaseStyleImmediate();
            ApplyVisualsImmediate();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 ForceVisualUpdate: {Type} - Fill: {_currentFill}, Style: {_currentStyle?.name}");
        }

        private void OnDestroy()
        {
            ClearAllTween();
        }

        private void ClearAllTween()
        {
            if (FillImage != null) FillImage.DOKill();
            if (PendingFillImage != null) PendingFillImage.DOKill();
            if (ValueText != null) ValueText.transform.DOKill();
        }
    }
}