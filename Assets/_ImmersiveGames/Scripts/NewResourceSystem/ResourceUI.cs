using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.NewResourceSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceUI : MonoBehaviour, IResourceUI
    {
        [SerializeField] private ResourceType targetResourceType;
        [SerializeField] protected Image resourceBar;
        [SerializeField] protected TextMeshProUGUI textMeshProUGUI;
        private string _actorId;
        private ResourceType _resourceType;

        public bool IsVisible { get; set; }
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            gameObject.SetActive(visible);
        }
        
        public void Bind(string actorId, IResourceValue data)
        {
            _actorId = actorId;
            _resourceType = targetResourceType;
            UpdateValue(data);
            // ✅ Detalhes completos para debugging
            string resourceInfo = $"{data.GetCurrentValue()}/{data.GetMaxValue()}";
            DebugUtility.LogVerbose<ResourceUI>($"🔗 [{targetResourceType}] → {actorId} | {resourceInfo} | UI#{GetInstanceID()}");
        }
        public void Unbind()
        {
            _actorId = null;
            SetVisible(false);
            DebugUtility.LogVerbose<ResourceUI>("🔓 ResourceUI unbound");
        }
        public void UpdateValue(IResourceValue newValue)
        {
            if (resourceBar != null)
            {
                resourceBar.fillAmount = newValue.GetPercentage();
            }
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = $"{_resourceType}: {newValue.GetCurrentValue()}/{newValue.GetMaxValue()} - Actor {_actorId}";
            }
        }

        // ✅ IResourceUI specific methods
        void IResourceUI.SetResourceType(ResourceType type)
        {
            _resourceType = type;
            // Atualizar ícone, cor, etc.
            if (resourceBar != null)
            {
                resourceBar.color = GetColorForType(type);
            }
        }

        private Color GetColorForType(ResourceType type)
        {
            return type switch
            {
                ResourceType.Health => Color.red,
                ResourceType.Mana => Color.blue,
                ResourceType.Stamina => Color.green,
                ResourceType.Energy => Color.yellow,
                _ => Color.white
            };
        }
    }
}