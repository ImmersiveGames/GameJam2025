using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceUISlot : MonoBehaviour
    {
        private string _actorId;
        private ResourceType _type;

        // agora público (somente leitura external)
        public ResourceInstanceConfig InstanceConfig { get; private set; }

        [Header("UI Components")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image pendingFillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        [Header("Style fallback")]
        [SerializeField] private ResourceUIStyle defaultStyle;
        public ResourceUIStyle DefaultStyle => defaultStyle;

        public string ActorId => _actorId;
        public ResourceType Type => _type;

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            ResetToZero();
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig instanceConfig = null)
        {
            _actorId = actorId;
            _type = type;
            InstanceConfig = instanceConfig;
            if (instanceConfig?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = instanceConfig.resourceDefinition.icon;
            gameObject.name = $"{actorId}_{type}";
        }

        public void Configure(IResourceValue data)
        {
            if (data == null) return;
            float pct = data.GetPercentage();

            if (valueText != null)
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            SetFillValues(pct, pct);
            SetVisible(true);
        }

        public void SetFillValues(float currentFill, float pendingFill)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(currentFill);
                if (defaultStyle != null)
                    fillImage.color = defaultStyle.fillGradient.Evaluate(currentFill);
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = Mathf.Clamp01(pendingFill);
                if (defaultStyle != null)
                    pendingFillImage.color = defaultStyle.pendingColor;
            }
        }

        public float GetCurrentFill() => fillImage != null ? fillImage.fillAmount : 0f;
        public float GetPendingFill() => pendingFillImage != null ? pendingFillImage.fillAmount : 0f;

        public void Clear()
        {
            SetVisible(false);
            ResetToZero();
            if (valueText != null) valueText.text = "";
        }

        private void ResetToZero() => SetFillValues(0f, 0f);
        public void SetVisible(bool visible) { if (rootPanel != null) rootPanel.SetActive(visible); }
    }
}
