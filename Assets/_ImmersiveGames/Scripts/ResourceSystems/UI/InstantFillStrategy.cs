using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [CreateAssetMenu(menuName = "ImmersiveGames/UI/FillStrategy/Instant")]
    public class InstantFillStrategy : ResourceFillStrategy
    {
        public override void ApplyFill(Image fillImage, Image pendingFillImage, float target, ResourceUIStyle style)
        {
            float clamped = Mathf.Clamp01(target);
            if (fillImage != null)
            {
                fillImage.fillAmount = clamped;
                if (style != null && style.fillGradient != null)
                    fillImage.color = style.fillGradient.Evaluate(clamped);
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = clamped;
                if (style != null)
                    pendingFillImage.color = style.pendingColor;
            }
        }
    }
}