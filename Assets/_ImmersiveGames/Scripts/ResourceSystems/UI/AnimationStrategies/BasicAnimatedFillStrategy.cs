using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class BasicAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            // BARRA CURRENT: Sempre instantânea
            if (slot.FillImage != null)
            {
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                // Usa o gradient do style - já controla cores automaticamente
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA PENDING: Animação básica com delay
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;
                var ease = style?.basicEase ?? Ease.OutQuad;

                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
                
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    duration
                ).SetDelay(delay).SetEase(ease);
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
            _pendingTween?.Kill();
        }
    }
}