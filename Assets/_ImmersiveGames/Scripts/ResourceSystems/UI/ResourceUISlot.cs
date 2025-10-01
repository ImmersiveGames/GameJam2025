using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
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
    
        // ESTADO CORRETO: 
        private float _currentFill;
        private float _previousFill; // ← VALOR ANTERIOR para a pending
        private string _currentText = "";
    
        public ResourceType Type { get; private set; }
        private ResourceInstanceConfig InstanceConfig { get; set; }
        public Image FillImage => fillImage;
        public Image PendingFillImage => pendingFillImage;
        public TextMeshProUGUI ValueText => valueText;
        public Image IconImage => iconImage;
        public GameObject RootPanel => rootPanel;

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
            _slotStrategy = new InstantSlotStrategy();
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig config)
        {
            Type = type;
            InstanceConfig = config;
        
            // Usa factory se disponível
            _slotStrategy = DependencyManager.Instance.TryGetGlobal(out IResourceSlotStrategyFactory factory) 
                ? factory.CreateStrategy(config?.fillAnimationType ?? FillAnimationType.Instant) 
                : CreateStrategyDirectly(config?.fillAnimationType ?? FillAnimationType.Instant);
        
            if (InstanceConfig?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = InstanceConfig.resourceDefinition.icon;
            
            gameObject.name = $"{actorId}_{type}";
        
            // Estado inicial
            _currentFill = 0f;
            _previousFill = 0f;
            ApplyVisuals();
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
        
            // LÓGICA CORRETA:
            // 1. Guarda o valor anterior ANTES de atualizar
            _previousFill = _currentFill;
            // 2. Atualiza o valor atual
            _currentFill = newValue;

            _currentText = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";
        
            ApplyVisuals();
            SetVisible(true);
        }

        private void ApplyVisuals()
        {
            var style = InstanceConfig?.slotStyle;
        
            // PASSA OS VALORES CORRETOS:
            // - Current: valor ATUAL (novo)
            // - Pending: valor ANTERIOR (para criar o efeito de rastro)
            _slotStrategy.ApplyFill(this, _currentFill, _previousFill, style);
            _slotStrategy.ApplyText(this, _currentText, style);
        }

        public void Clear()
        {
            _slotStrategy.ClearVisuals(this);
            StopAllCoroutines();
            _currentFill = 0f;
            _previousFill = 0f;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
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