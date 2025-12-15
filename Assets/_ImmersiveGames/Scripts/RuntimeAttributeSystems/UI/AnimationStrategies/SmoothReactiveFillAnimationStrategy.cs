using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Animation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.AnimationStrategies
{
    /// <summary>
    /// Estratégia "Smooth Reactive".
    /// Ambas as barras (principal e residual) reagem de forma contínua e fluida,
    /// sem delay perceptível, porém com velocidades diferentes (residual mais lenta).
    /// Ideal para HUDs modernos e feedbacks suaves de dano/recuperação.
    /// </summary>
    public class SmoothReactiveFillAnimationStrategy : IFillAnimationStrategy
    {
        private Image _main;
        private Image _residual;
        private FillAnimationProfile _profile;

        private Tween _mainTween;
        private Tween _residualTween;

        private float _targetValue;

        public void Initialize(Image main, Image residual, FillAnimationProfile profile, MonoBehaviour owner)
        {
            _main = main;
            _residual = residual;
            _profile = profile;

            _mainTween?.Kill();
            _residualTween?.Kill();

            if (_main != null)
                _targetValue = _main.fillAmount;
        }

        public void SetInstant(float value)
        {
            _mainTween?.Kill();
            _residualTween?.Kill();

            _targetValue = value;

            if (_main != null)
                _main.fillAmount = value;

            if (_residual != null)
                _residual.fillAmount = value;
        }

        public void AnimateTo(float target)
        {
            if (_main == null) return;

            _targetValue = Mathf.Clamp01(target);
            _mainTween?.Kill();
            _residualTween?.Kill();

            float mainSpeed = _profile.mainSpeed;
            float residualSpeed = _profile.residualSpeed;

            // Define easings
            var mainEase = _profile.mainEase;
            var residualEase = _profile.residualEase;

            // === Barra Principal ===
            _mainTween = DOTween.To(
                () => _main.fillAmount,
                x => _main.fillAmount = x,
                _targetValue,
                mainSpeed
            ).SetEase(mainEase);

            // === Barra Residual ===
            if (_residual != null)
            {
                // sempre se ajusta suavemente, mas com uma velocidade diferente
                _residualTween = DOTween.To(
                    () => _residual.fillAmount,
                    x => _residual.fillAmount = x,
                    _targetValue,
                    residualSpeed
                ).SetEase(residualEase);
            }
        }

        public void Cancel()
        {
            _mainTween?.Kill();
            _residualTween?.Kill();
        }
    }
}
