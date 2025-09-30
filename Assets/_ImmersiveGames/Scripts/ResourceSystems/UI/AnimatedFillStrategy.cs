using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems.UI
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/FillStrategy/Animated")]
    public class AnimatedFillStrategy : ResourceFillStrategy
    {
        public override void ApplyFill(Image fillImage, Image pendingFillImage, float target, ResourceUIStyle style)
        {
            if (fillImage == null || style == null)
            {
                // fallback to instant
                if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(target);
                if (pendingFillImage != null) pendingFillImage.fillAmount = Mathf.Clamp01(target);
                return;
            }

            /*LeanTween.cancel(fillImage.gameObject);
            LeanTween.value(fillImage.gameObject, fillImage.fillAmount, target, style.quickDuration)
                .setOnUpdate((float v) =>
                {
                    fillImage.fillAmount = v;
                    if (style.fillGradient != null) fillImage.color = style.fillGradient.Evaluate(v);
                })
                .setOnComplete(() =>
                {
                    if (pendingFillImage != null)
                    {
                        LeanTween.cancel(pendingFillImage.gameObject);
                        LeanTween.value(pendingFillImage.gameObject, pendingFillImage.fillAmount, target, style.slowDuration)
                            .setDelay(style.delayBeforeSlow)
                            .setOnUpdate((float v) => pendingFillImage.fillAmount = v)
                            .setOnComplete(() => { if (style != null) pendingFillImage.color = style.pendingColor; });
                    }
                });*/
        }
    }
}