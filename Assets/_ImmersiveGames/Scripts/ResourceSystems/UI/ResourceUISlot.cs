using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceUISlot : MonoBehaviour, IResourceUISlot
    {
        [SerializeField] private string expectedActorId;
        [SerializeField] private ResourceType expectedType;
        
        [Header("UI Components")]
        [SerializeField] private Color32 resourceColor = Color.red;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        public string SlotId => $"{expectedActorId}_{expectedType}";
        public string ExpectedActorId => expectedActorId;
        public ResourceType ExpectedType => expectedType;

        private void Awake()
        {
            // Validar componentes
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            if (rootPanel == null) rootPanel = gameObject;
            
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 Slot inicializado: {SlotId}");
        }

        public void Configure(IResourceValue data)
        {
            if (fillImage != null)
            {
                fillImage.color = resourceColor;
                fillImage.fillAmount = data.GetPercentage();
            }
            
            if (valueText != null) 
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            SetVisible(true);
            DebugUtility.LogVerbose<ResourceUISlot>($"✅ Slot configurado: {SlotId} = {data.GetCurrentValue():0}/{data.GetMaxValue():0}");
        }

        public void Clear()
        {
            if (fillImage != null) fillImage.fillAmount = 0f;
            if (valueText != null) valueText.text = "";
            SetVisible(false);
            DebugUtility.LogVerbose<ResourceUISlot>($"🔓 Slot limpo: {SlotId}");
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) 
                rootPanel.SetActive(visible);
        }
    }
}