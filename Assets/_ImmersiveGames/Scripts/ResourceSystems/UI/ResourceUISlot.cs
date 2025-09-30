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

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig config)
        {
            _type = type;
            _config = config;
            if (_config?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = _config.resourceDefinition.icon;
            gameObject.name = $"{actorId}_{type}";
            ApplyFillStrategy(0f);
        }
        public void Configure(IResourceValue data)
        {
            if (data == null) return;

            float pct = data.GetPercentage();
            if (valueText != null)
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            ApplyFillStrategy(pct);
            SetVisible(true);
        }

        private void ApplyFillStrategy(float pct)
        {
            // Strategy must be the only place that modifies image.fillAmount/color
            var strat = _config?.animationStrategy;
            var style = _config?.animationStyle;

            if (strat != null && _config?.enableAnimation == true)
            {
                strat.ApplyFill(fillImage, pendingFillImage, pct, style);
            }
            else
            {
                // fallback instant strategy
                var instant = ScriptableObject.CreateInstance<InstantFillStrategy>();
                instant.ApplyFill(fillImage, pendingFillImage, pct, style);
                DestroyImmediate(instant); // avoid leaking transient SO in editor/runtime
            }
        }

        public void Clear()
        {
            // Clear visual via strategy as well.
            ApplyFillStrategy(0f);

            if (valueText != null) valueText.text = "";
            SetVisible(false);
        }
        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
        }
    }
}
