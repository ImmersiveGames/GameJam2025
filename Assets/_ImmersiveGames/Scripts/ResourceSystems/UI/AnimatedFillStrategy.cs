using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Preenche barras de recurso usando animação com DOTween Free.
    /// </summary>
    public class AnimatedFillStrategy : IResourceSlotStrategy
    {
        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            float quickDuration = style != null ? style.quickDuration : 0.2f;
            float slowDuration = style != null ? style.slowDuration : 0.8f;
            float delayBeforeSlow = style != null ? style.delayBeforeSlow : 0.3f;

            if (slot.FillImage != null)
            {
                // Interpolação manual para fillAmount com coroutine ou Update
                // Para DOTween Free, usaremos uma abordagem mais simples
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            if (slot.PendingFillImage != null)
            {
                // Para DOTween Free, podemos usar uma coroutine para a animação lenta
                slot.StartCoroutine(AnimatePendingFill(slot.PendingFillImage, pendingPct, slowDuration, delayBeforeSlow, style));
                
                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;
            }
        }
        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            //nope
        }

        private IEnumerator AnimatePendingFill(Image pendingImage, float targetFill, float duration, float delay, ResourceUIStyle style)
        {
            // Aguarda o delay inicial
            yield return new WaitForSeconds(delay);

            float startFill = pendingImage.fillAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                
                // Ease Out Quad approximation
                float easedProgress = 1f - (1f - progress) * (1f - progress);
                
                pendingImage.fillAmount = Mathf.Lerp(startFill, targetFill, easedProgress);
                
                yield return null;
            }

            // Garante o valor final exato
            pendingImage.fillAmount = targetFill;
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
            slot.StopAllCoroutines();
        }
    }
}