using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.Utils.Extensions
{
    /// <summary>
    /// Extensões para DOTween gratuitas que simulam DOFillAmount (versão PRO) com Image.fillAmount.
    /// </summary>
    public static class DoFillAmountExtension
    {
        /// <summary>
        /// Cria um tween que anima o fillAmount de uma Image (0..1).
        /// Compatível com DOTween free.
        /// </summary>
        public static Tweener DoFillAmount(this Image target, float endValue, float duration)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            // Cria o tween manualmente, interpolando de target.fillAmount até endValue
            float startValue = target.fillAmount;
            Tweener tween = DOTween.To(() => startValue, x =>
            {
                startValue = x;
                if (target != null)
                    target.fillAmount = x;
            }, endValue, duration);

            tween.SetTarget(target);
            return tween;
        }
    }

    /// <summary>
    /// Extensão para interpolar cores de Image usando DOTween gratuito.
    /// </summary>
    public static class DoColorExtension
    {
        /// <summary>
        /// Cria um tween que anima a cor de uma Image.
        /// Compatível com DOTween free.
        /// </summary>
        public static Tweener DoColor(this Image target, Color endValue, float duration)
        {
            if (target == null)
                throw new System.ArgumentNullException(nameof(target));

            Color startValue = target.color;
            Tweener tween = DOTween.To(() => startValue, x =>
            {
                startValue = x;
                if (target != null)
                    target.color = x;
            }, endValue, duration);

            tween.SetTarget(target);
            return tween;
        }
    }
}
