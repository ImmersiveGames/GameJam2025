using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class PulseAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;
        private Tweener _pulseTween;
        private bool _isPulsing;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            // BARRA CURRENT: Instantânea
            if (slot.FillImage != null)
            {
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                // Gradient controla as cores
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA PENDING: Animação normal
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;
                Ease ease = style?.basicEase ?? Ease.OutQuad;

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

            // PULSAÇÃO NO PARENT se habilitado
            if (slot.RootPanel != null && !_isPulsing && style != null && style.enablePulseEffect)
            {
                StartParentPulse(slot.RootPanel.transform, style);
            }
        }

        private void StartParentPulse(Transform parentTransform, ResourceUIStyle style)
        {
            _isPulsing = true;
            
            float scale = style.pulseScale;
            float duration = style.pulseDuration;
            Ease ease = style.pulseEase;

            _pulseTween = parentTransform.DOScale(scale, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(ease)
                .OnKill(() => _isPulsing = false);
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
            
            if (slot.RootPanel != null)
                slot.RootPanel.transform.DOKill();
                
            _isPulsing = false;
            
            // Restaura scale normal
            if (slot.RootPanel != null)
                slot.RootPanel.transform.localScale = Vector3.one;
        }
    }
}