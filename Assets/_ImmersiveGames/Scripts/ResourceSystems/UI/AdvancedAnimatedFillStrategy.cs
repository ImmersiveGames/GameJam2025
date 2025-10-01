using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AdvancedAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;
        private float _lastCurrentValue = 0f;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            // BARRA CURRENT: Instantânea com efeitos
            if (slot.FillImage != null)
            {
                float previousValue = slot.FillImage.fillAmount;
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
            
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);

                // Efeitos baseados na direção
                if (currentPct > previousValue)
                {
                    // Cura - pulso
                    slot.FillImage.transform.localScale = Vector3.one;
                    slot.FillImage.transform.DOScale(1.1f, 0.2f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.OutSine);
                }
                else if (currentPct < previousValue)
                {
                    // Dano - efeito visual
                    slot.FillImage.transform.DOScaleX(0.95f, 0.1f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
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
            _pendingTween?.Kill();
            slot.FillImage?.transform.DOKill();
        }
    }
}