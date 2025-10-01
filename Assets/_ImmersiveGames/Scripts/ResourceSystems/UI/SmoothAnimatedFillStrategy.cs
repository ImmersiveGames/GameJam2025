using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class SmoothAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _currentTween;
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            float quickDuration = style?.quickDuration ?? 0.3f;
            float slowDuration = style?.slowDuration ?? 0.8f;
            float delay = style?.delayBeforeSlow ?? 0.3f;

            // BARRA CURRENT: Animação rápida para o valor atual
            if (slot.FillImage != null)
            {
                _currentTween?.Kill();
            
                _currentTween = DOTween.To(
                    () => slot.FillImage.fillAmount,
                    x => slot.FillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    quickDuration
                ).SetEase(Ease.OutCubic);
                
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA PENDING: Começa no pendingStart e anima para currentPct
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                // Pending COMEÇA no valor anterior
                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
            
                // E anima PARA o valor atual
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    slowDuration
                ).SetDelay(delay).SetEase(Ease.OutCubic);
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