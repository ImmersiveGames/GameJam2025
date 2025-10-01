using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Representa um slot de recurso no UI. 
    /// Não aplica lógica de preenchimento — apenas expõe referências visuais.
    /// </summary>
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
        
        public ResourceType Type => _type;
        public ResourceInstanceConfig InstanceConfig => _config;
        public Image FillImage => fillImage;
        public Image PendingFillImage => pendingFillImage;
        public TextMeshProUGUI ValueText => valueText;
        public Image IconImage => iconImage;
        public GameObject RootPanel => rootPanel;
        private IResourceSlotStrategy _slotStrategy;

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
            //var instant = new InstantSlotStrategy();
            _slotStrategy = new AnimatedFillStrategy();
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig config)
        {
            _type = type;
            _config = config;
            if (_config?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = _config.resourceDefinition.icon;
            gameObject.name = $"{actorId}_{type}";
            ApplyFillStrategy(0,0);
        }
        public void Configure(IResourceValue data)
        {
            if (data == null) return;

            ApplyFillStrategy(data.GetPercentage(),data.GetPercentage(),$"{data.GetCurrentValue():0}/{data.GetMaxValue():0}");
            SetVisible(true);
        }

        private void ApplyFillStrategy(float current, float pending, string text = null)
        {
            // Strategy must be the only place that modifies image.fillAmount/color
            var style = _config?.slotStyle;
            
            _slotStrategy.ApplyFill(this, current, pending, style);
            _slotStrategy.ApplyText(this, text,style);
        }

        public void Clear()
        {
            // Clear visual via strategy as well.
            _slotStrategy.ClearVisuals(this);
            SetVisible(false);
        }
        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
        }
    }
}
