using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IAnimationProvider
    {
        IEnumerator AnimateFill(Image image, float targetFill, float duration, float delay = 0f);
        IEnumerator AnimateColor(Image image, Color targetColor, float duration);
    }

    public class TweenAnimationProvider : IAnimationProvider
    {
        public IEnumerator AnimateFill(Image image, float targetFill, float duration, float delay = 0f)
        {
            yield return new WaitForSeconds(delay);
        
            float startFill = image.fillAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - (1f - progress) * (1f - progress); // Ease Out Quad
                image.fillAmount = Mathf.Lerp(startFill, targetFill, easedProgress);
                yield return null;
            }

            image.fillAmount = targetFill;
        }

        public IEnumerator AnimateColor(Image image, Color targetColor, float duration)
        {
            Color startColor = image.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                image.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            image.color = targetColor;
        }
    }
}