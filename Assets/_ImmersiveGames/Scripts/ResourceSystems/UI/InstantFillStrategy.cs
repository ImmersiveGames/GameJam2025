using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class InstantSlotStrategy : IResourceSlotStrategy
    {
        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            ApplyFill(slot, currentPct, style);
            float clamped = Mathf.Clamp01(pendingPct);
            if (slot.PendingFillImage != null)
            {
                slot.PendingFillImage.fillAmount = clamped;
                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;
            }
        }
        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            float clamped = Mathf.Clamp01(currentPct);
            if (slot.FillImage != null)
            {
                slot.FillImage.fillAmount = clamped;
                if (style != null && style.fillGradient != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(clamped);
            }
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
            // No state to clear in instant strategy
        }
    }
}