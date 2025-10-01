using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class SmoothAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _currentTween;
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            float currentDuration = style?.smoothCurrentDuration ?? 0.4f;
            float pendingDuration = style?.smoothPendingDuration ?? 1.2f;
            float delay = style?.delayBeforeSlow ?? 0.5f;
            Ease currentEase = style?.smoothCurrentEase ?? Ease.InOutCubic;
            Ease pendingEase = style?.smoothPendingEase ?? Ease.InOutSine;

            // BARRA CURRENT: Animação suave
            if (slot.FillImage != null)
            {
                _currentTween?.Kill();
                
                _currentTween = DOTween.To(
                    () => slot.FillImage.fillAmount,
                    x => slot.FillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    currentDuration
                ).SetEase(currentEase);
                    
                // Gradient controla as cores
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA PENDING: Animação muito suave
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
                
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    pendingDuration
                ).SetDelay(delay).SetEase(pendingEase);
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
            _currentTween?.Kill();
            _pendingTween?.Kill();
        }
    }
}