using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class PulseAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;
        private Tweener _pulseTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            // BARRA CURRENT: Instantânea
            if (slot.FillImage != null)
            {
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
            
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);

                // Pulso apenas quando há mudança significativa
                float previousValue = slot.FillImage.fillAmount;
                if (Mathf.Abs(currentPct - previousValue) > 0.01f)
                {
                    StartPulse(slot.FillImage);
                }
            }

            // BARRA PENDING: Começa no pendingStart e anima para currentPct
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;

                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                // Pending COMEÇA no valor anterior
                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
            
                // E anima PARA o valor atual
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    duration
                ).SetDelay(delay).SetEase(Ease.OutQuad);
            }
        }

        private void StartPulse(Image image)
        {
            _pulseTween?.Kill();
        
            // Pulso temporário (2 ciclos apenas)
            image.transform.localScale = Vector3.one;
            _pulseTween = image.transform.DOScale(1.05f, 0.2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => {
                    image.transform.localScale = Vector3.one;
                });
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
            _pulseTween?.Kill();
            slot.FillImage?.transform.DOKill();
        
            // Garante scale normal
            if (slot.FillImage != null)
                slot.FillImage.transform.localScale = Vector3.one;
        }
    }
}