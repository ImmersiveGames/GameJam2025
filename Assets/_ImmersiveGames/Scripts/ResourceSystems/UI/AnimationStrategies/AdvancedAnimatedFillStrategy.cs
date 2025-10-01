using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class AdvancedAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            UpdateFillImageWithEffects(slot, currentPct, style);

            ApplyPendingFillEffects(slot, currentPct, pendingStart, style);
        }
        private void ApplyPendingFillEffects(ResourceUISlot slot, float currentPct, float pendingStart, ResourceUIStyle style)
        {
            // BARRA PENDING: Animação com efeitos especiais
            if (slot.PendingFillImage != null)
            {
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;
                var ease = style?.advancedEase ?? Ease.OutBounce;

                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingStart);
                
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct),
                    duration
                ).SetDelay(delay).SetEase(ease);

                // Efeito de escala na pending
                if (style == null || !style.enableAdvancedEffects) return;
                var pendingEffect = DOTween.Sequence();
                pendingEffect.Append(slot.PendingFillImage.transform.DOScaleY(1.05f, duration * 0.5f));
                pendingEffect.Append(slot.PendingFillImage.transform.DOScaleY(1f, duration * 0.5f));
            }
        }
        private void UpdateFillImageWithEffects(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            // BARRA CURRENT: Instantânea com efeitos avançados
            if (slot.FillImage != null)
            {
                float previousValue = slot.FillImage.fillAmount;
                slot.FillImage.DOKill();
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                // Gradient já controla as cores automaticamente
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);

                // Efeitos avançados se habilitados
                if (style != null && style.enableAdvancedEffects)
                {
                    float changeAmount = Mathf.Abs(currentPct - previousValue);
                    
                    if (currentPct > previousValue)
                    {
                        ApplyHealEffects(slot.FillImage.transform, style, changeAmount);
                    }
                    else if (currentPct < previousValue)
                    {
                        ApplyDamageEffects(slot.FillImage.transform, style, changeAmount);
                    }
                }
            }
        }

        private void ApplyHealEffects(Transform transform, ResourceUIStyle style, float changeAmount)
        {
            var healSequence = DOTween.Sequence();
            
            healSequence.Append(transform.DOScaleY(style.healScaleIntensity, style.healScaleDuration * 0.5f));
            healSequence.Append(transform.DOScaleX(style.healScaleIntensity, style.healScaleDuration * 0.5f));
            healSequence.Append(transform.DOScale(Vector3.one, style.healScaleDuration));
            
            healSequence.SetEase(style.healEase);

            if (changeAmount > 0.2f)
            {
                transform.DOLocalMoveY(style.healMoveDistance.y, style.healScaleDuration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.OutQuad);
            }
        }

        private void ApplyDamageEffects(Transform transform, ResourceUIStyle style, float changeAmount)
        {
            var damageSequence = DOTween.Sequence();
            
            damageSequence.Append(transform.DOScaleX(style.damageScaleIntensity, style.damageScaleDuration));
            damageSequence.Append(transform.DOLocalMoveX(-3f, 0.05f));
            damageSequence.Append(transform.DOLocalMoveX(3f, 0.05f));
            damageSequence.Append(transform.DOLocalMoveX(0f, 0.05f));
            damageSequence.Append(transform.DOScale(Vector3.one, style.damageScaleDuration));
            
            damageSequence.SetEase(style.damageEase);

            if (changeAmount > 0.3f)
            {
                transform.DOShakePosition(
                    style.damageShakeDuration, 
                    new Vector3(style.damageShakeStrength, 0f, 0f), 
                    style.damageShakeVibrato
                );
            }
        }

        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            ApplyFill(slot, currentPct, currentPct, style);
        }

        public void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style)
        {
            if (slot.ValueText == null) return;
            slot.ValueText.text = target;

            if (style == null || !style.enableTextAnimation) return;
            slot.ValueText.transform.localScale = Vector3.one * style.textScaleIntensity;
            slot.ValueText.transform.DOScale(Vector3.one, style.textAnimationDuration)
                .SetEase(style.textEase);
        }

        public void ClearVisuals(ResourceUISlot slot)
        {
            _pendingTween?.Kill();
            slot.FillImage?.transform.DOKill();
            slot.PendingFillImage?.transform.DOKill();
            slot.ValueText?.transform.DOKill();
            
            // Restaura transforms
            if (slot.FillImage != null)
            {
                slot.FillImage.transform.localScale = Vector3.one;
                slot.FillImage.transform.localPosition = Vector3.zero;
            }
            if (slot.PendingFillImage != null)
                slot.PendingFillImage.transform.localScale = Vector3.one;
            if (slot.ValueText != null)
                slot.ValueText.transform.localScale = Vector3.one;
        }
    }
}