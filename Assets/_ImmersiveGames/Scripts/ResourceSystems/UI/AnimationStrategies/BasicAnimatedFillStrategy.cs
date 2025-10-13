using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class BasicAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            DebugUtility.LogVerbose<BasicAnimatedFillStrategy>($"🎨 BasicAnimated ApplyFill - Current: {currentPct}, Style: {style?.name ?? "None"}");

            // BARRA CURRENT: Sempre instantânea
            if (slot.FillImage != null)
            {
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                // CORREÇÃO: Aplicar gradiente do style
                if (style != null && style.fillGradient != null)
                {
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
                    DebugUtility.LogVerbose<BasicAnimatedFillStrategy>($"✅ FillImage color applied from gradient");
                }
                else
                {
                    DebugUtility.LogVerbose<BasicAnimatedFillStrategy>($"⚠️ No gradient available for FillImage");
                }
            }

            // BARRA PENDING: Animação básica com delay
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;
                var ease = style?.basicEase ?? Ease.OutQuad;

                // CORREÇÃO: Aplicar cor pending
                if (style != null)
                {
                    slot.PendingFillImage.color = style.pendingColor;
                    DebugUtility.LogVerbose<BasicAnimatedFillStrategy>($"✅ PendingImage color: {style.pendingColor}");
                }

                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
                
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    duration
                ).SetDelay(delay).SetEase(ease);

                DebugUtility.LogVerbose<BasicAnimatedFillStrategy>($"⏱️ Pending animation: {duration}s with {delay}s delay");
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
                
                // CORREÇÃO: Animação de texto se habilitada
                if (style != null && style.enableTextAnimation)
                {
                    slot.ValueText.transform.DOKill();
                    slot.ValueText.transform.localScale = Vector3.one;
                    slot.ValueText.transform.DOPunchScale(
                        Vector3.one * (style.textScaleIntensity - 1f), 
                        style.textAnimationDuration, 1
                    ).SetEase(style.textEase);
                }
            }
        }

        public void ClearVisuals(ResourceUISlot slot)
        {
            _pendingTween?.Kill();
        }
    }
}