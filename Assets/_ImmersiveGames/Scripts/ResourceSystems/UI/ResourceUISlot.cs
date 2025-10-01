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
    
        private ResourceInstanceConfig _config;
        private ResourceType _type;
        private IResourceSlotStrategy _slotStrategy;
    
        // ESTADO CORRETO: 
        private float _currentFill = 0f;
        private float _previousFill = 0f; // ← VALOR ANTERIOR para a pending
        private string _currentText = "";
    
        public ResourceType Type => _type;
        public ResourceInstanceConfig InstanceConfig => _config;
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
            _type = type;
            _config = config;
        
            // Usa factory se disponível
            if (DependencyManager.Instance.TryGetGlobal(out IResourceSlotStrategyFactory factory))
            {
                _slotStrategy = factory.CreateStrategy(config?.fillAnimationType ?? FillAnimationType.Instant);
            }
            else
            {
                _slotStrategy = CreateStrategyDirectly(config?.fillAnimationType ?? FillAnimationType.Instant);
            }
        
            if (_config?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = _config.resourceDefinition.icon;
            
            gameObject.name = $"{actorId}_{type}";
        
            // Estado inicial
            _currentFill = 0f;
            _previousFill = 0f;
            ApplyVisuals();
        }

        private IResourceSlotStrategy CreateStrategyDirectly(FillAnimationType animationType)
        {
            switch (animationType)
            {
                case FillAnimationType.BasicAnimated: return new BasicAnimatedFillStrategy();
                case FillAnimationType.AdvancedAnimated: return new AdvancedAnimatedFillStrategy();
                case FillAnimationType.SmoothAnimated: return new SmoothAnimatedFillStrategy();
                case FillAnimationType.PulseAnimated: return new PulseAnimatedFillStrategy();
                default: return new InstantSlotStrategy();
            }
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
            var style = _config?.slotStyle;
        
            // PASSA OS VALORES CORRETOS:
            // - Current: valor ATUAL (novo)
            // - Pending: valor ANTERIOR (para criar o efeito de rastro)
            _slotStrategy.ApplyFill(this, _currentFill, _previousFill, style);
            _slotStrategy.ApplyText(this, _currentText, style);
        }

        public void Clear()
        {
            _slotStrategy.ClearVisuals(this);
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
            ClearAllTweens();
        }

        private void ClearAllTweens()
        {
            if (FillImage != null) FillImage.DOKill();
            if (PendingFillImage != null) PendingFillImage.DOKill();
            if (ValueText != null) ValueText.transform.DOKill();
        }
    }
}