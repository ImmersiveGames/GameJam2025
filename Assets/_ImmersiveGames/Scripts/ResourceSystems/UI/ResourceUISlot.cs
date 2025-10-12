using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
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
    
        // ESTADO CORRETO: 
        private float _currentFill;
        private float _previousFill;
        private string _currentText = "";
        private bool _isFirstConfigure = true; // NOVO: Flag para primeira configuração
    
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
        
            // Estado inicial - CORREÇÃO: Inicializar com valores realistas
            _currentFill = 1f; // Começar com 100% (valor real)
            _previousFill = 1f;
            
            // Aplicar visuals imediatamente com valor inicial correto
            ApplyVisualsImmediate();
            
            DebugUtility.LogVerbose<ResourceUISlot>($"✅ Slot initialized for {actorId}.{type} - Initial Fill: {_currentFill}");
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
        
            DebugUtility.LogVerbose<ResourceUISlot>($"🔄 Slot Configure: {Type} - Previous: {_currentFill}, New: {newValue}, First: {_isFirstConfigure}");
        
            // LÓGICA CORRETA:
            _previousFill = _currentFill;
            _currentFill = newValue;

            _currentText = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";
        
            // CORREÇÃO: Primeira configuração precisa de tratamento especial
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

        // NOVO: Método para aplicação imediata (sem animação)
        private void ApplyVisualsImmediate()
        {
            var style = InstanceConfig?.slotStyle;
        
            DebugUtility.LogVerbose<ResourceUISlot>($"⚡ ApplyVisualsImmediate: {Type} - Current: {_currentFill}");
            
            // Usar estratégia instantânea para primeira aplicação
            var instantStrategy = new InstantSlotStrategy();
            instantStrategy.ApplyFill(this, _currentFill, _currentFill, style);
            instantStrategy.ApplyText(this, _currentText, style);
            
            // Limpar qualquer animação pendente
            ClearAllTween();
        }

        private void ApplyVisuals()
        {
            var style = InstanceConfig?.slotStyle;
        
            DebugUtility.LogVerbose<ResourceUISlot>($"🎨 ApplyVisuals: {Type} - Current: {_currentFill}, Previous: {_previousFill}");
        
            _slotStrategy.ApplyFill(this, _currentFill, _previousFill, style);
            _slotStrategy.ApplyText(this, _currentText, style);
        }

        public void Clear()
        {
            _slotStrategy.ClearVisuals(this);
            StopAllCoroutines();
            _currentFill = 0f;
            _previousFill = 0f;
            _isFirstConfigure = true;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
        }

        // NOVO: Método para forçar atualização visual
        [ContextMenu("Force Visual Update")]
        public void ForceVisualUpdate()
        {
            ApplyVisualsImmediate();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 ForceVisualUpdate: {Type} - Fill: {_currentFill}");
        }

        // NOVO: Método para debug do estado interno
        [ContextMenu("Debug Slot State")]
        public void DebugSlotState()
        {
            DebugUtility.LogWarning<ResourceUISlot>(
                $"🔍 Slot Debug - {Type}:\n" +
                $"Current Fill: {_currentFill}\n" +
                $"Previous Fill: {_previousFill}\n" +
                $"First Configure: {_isFirstConfigure}\n" +
                $"Fill Image: {fillImage != null} (amount: {fillImage?.fillAmount})\n" +
                $"Pending Image: {pendingFillImage != null} (amount: {pendingFillImage?.fillAmount})"
            );
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