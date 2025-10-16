using _ImmersiveGames.Scripts.ResourceSystems.Animation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    /// <summary>
    /// Estratégia instantânea: aplica os valores diretamente sem animações.
    /// Ideal para debug, modo de desempenho ou barras estáticas.
    /// </summary>
    public class InstantFillAnimationStrategy : IFillAnimationStrategy
    {
        private Image _main;
        private Image _residual;

        public void Initialize(Image main, Image residual, FillAnimationProfile profile, MonoBehaviour owner)
        {
            _main = main;
            _residual = residual;

            if (_residual != null)
                _residual.gameObject.SetActive(false); // desativa a residual nesse modo
        }

        public void SetInstant(float value)
        {
            if (_main != null)
                _main.fillAmount = value;

            if (_residual != null)
                _residual.fillAmount = value;
        }

        public void AnimateTo(float target)
        {
            // sem animação, só aplica diretamente
            SetInstant(target);
        }

        public void Cancel()
        {
            _main?.DOKill();
            _residual?.DOKill();
        }
    }
}