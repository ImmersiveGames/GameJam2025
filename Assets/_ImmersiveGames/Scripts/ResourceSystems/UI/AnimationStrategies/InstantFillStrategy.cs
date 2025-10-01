using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class InstantSlotStrategy : IResourceSlotStrategy
    {
        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            if (slot.FillImage != null)
            {
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                // Gradient controla as cores
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            if (slot.PendingFillImage != null)
            {
                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingPct);
                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;
            }
        }

        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            ApplyFill(slot, currentPct, currentPct, style);
        }

        public void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style)
        {
            if (slot.ValueText != null)
            {
                slot.ValueText.text = target;
            }
        }

        public void ClearVisuals(ResourceUISlot slot)
        {
            // Sem estado para limpar
        }
    }
}