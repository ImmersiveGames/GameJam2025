using _ImmersiveGames.Scripts.ResourceSystems.Animation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    /// <summary>
    /// Estratégia que anima a barra principal imediatamente e a residual com atraso,
    /// permitindo comportamento tipo beat’n’up: dano aparece na barra principal e
    /// depois a residual se ajusta.
    /// </summary>
    public class BasicReactiveFillAnimationStrategy : IFillAnimationStrategy
    {
        private Image _main;
        private Image _residual;
        private FillAnimationProfile _profile;
        private MonoBehaviour _owner;

        private float _currentValue;
        private Tween _mainTween;
        private Tween _residualTween;
        private float _targetValue;
        private float _lastAppliedValue;

        public void Initialize(Image main, Image residual, FillAnimationProfile profile, MonoBehaviour owner)
        {
            _main = main;
            _residual = residual;
            _profile = profile;
            _owner = owner;

            _mainTween?.Kill();
            _residualTween?.Kill();

            _currentValue = main != null ? main.fillAmount : 1f;
            _targetValue = _currentValue;
            _lastAppliedValue = _currentValue;
        }

        public void SetInstant(float value)
        {
            _mainTween?.Kill();
            _residualTween?.Kill();

            _currentValue = value;
            _targetValue = value;
            _lastAppliedValue = value;

            if (_main != null)
                _main.fillAmount = value;

            if (_residual != null)
                _residual.fillAmount = value;
        }

        public void AnimateTo(float target)
        {
            if (_main == null) return;

            _targetValue = Mathf.Clamp01(target);
            _mainTween?.Kill(false);

            // === 1️⃣ ANIMAÇÃO PRINCIPAL ===
            _mainTween = DOTween.To(
                () => _main.fillAmount,
                x => _main.fillAmount = x,
                _targetValue,
                _profile.mainSpeed
            ).SetEase(_profile.mainEase)
             .OnUpdate(() =>
             {
                 _currentValue = _main.fillAmount;
             })
             .OnComplete(() =>
             {
                 _lastAppliedValue = _main.fillAmount;
                 TryAnimateResidual();
             });
        }

        private void TryAnimateResidual()
        {
            if (_residual == null) return;

            // Cancela se já tiver uma animação em andamento
            _residualTween?.Kill(false);

            // === 2️⃣ ANIMAÇÃO RESIDUAL ===
            _residualTween = DOTween.Sequence()
                .AppendInterval(_profile.residualDelay)
                .Append(DOTween.To(
                    () => _residual.fillAmount,
                    x => _residual.fillAmount = x,
                    _targetValue,
                    _profile.residualSpeed
                ).SetEase(_profile.residualEase))
                .OnKill(() =>
                {
                    // Garante sincronização final
                    if (_residual != null)
                        _residual.fillAmount = _targetValue;
                });
        }

        public void Cancel()
        {
            _mainTween?.Kill();
            _residualTween?.Kill();
        }
    }
}
