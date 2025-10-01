using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class BasicAnimatedFillStrategy : IResourceSlotStrategy
    {
        private Tweener _pendingTween;

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            // BARRA CURRENT: SEMPRE instantânea
            if (slot.FillImage != null)
            {
                // Para qualquer animação anterior
                slot.FillImage.DOKill();
            
                // Define instantaneamente
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
            
                // Aplica cor do gradiente
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA PENDING: Animação com delay do valor ANTERIOR para o valor ATUAL
            if (slot.PendingFillImage != null)
            {
                // Para animação anterior
                _pendingTween?.Kill();

                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;

                // Configura cor
                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                // LÓGICA CORRETA: 
                // A pending começa no valor ANTERIOR (pendingPct) e anima para o valor ATUAL (currentPct)
                // Isso cria o efeito de "rastro" estilo jogos de luta
            
                // Primeiro, define a pending para o valor anterior
                slot.PendingFillImage.fillAmount = Mathf.Clamp01(pendingPct);
            
                // Depois anima para o valor atual
                _pendingTween = DOTween.To(
                    () => slot.PendingFillImage.fillAmount,
                    x => slot.PendingFillImage.fillAmount = x,
                    Mathf.Clamp01(currentPct), // Vai para o valor ATUAL
                    duration
                ).SetDelay(delay).SetEase(Ease.OutQuad);
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
            slot.FillImage?.DOKill();
            slot.PendingFillImage?.DOKill();
        }
    }
}