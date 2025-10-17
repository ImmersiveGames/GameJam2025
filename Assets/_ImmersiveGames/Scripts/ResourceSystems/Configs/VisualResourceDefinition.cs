using _ImmersiveGames.Scripts.ResourceSystems.Services;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    /// <summary>
    /// Definição especializada para recursos que exibem apenas informações visuais (ícones).
    /// </summary>
    public class VisualResourceDefinition : ResourceDefinition
    {
        [Header("Visual Overrides")]
        [SerializeField] private Sprite visualIcon;

        public override IResourceValue CreateInitialValue()
        {
            var value = new VisualResourceValue();
            value.SetCurrentValue(1f);
            value.SetIcon(GetIcon());
            return value;
        }

        public override void ApplyTo(IResourceValue value)
        {
            if (value is IResourceVisualValue visualValue)
            {
                visualValue.SetIcon(GetIcon());
            }
        }

        public Sprite GetIcon() => visualIcon != null ? visualIcon : Icon;
    }
}
