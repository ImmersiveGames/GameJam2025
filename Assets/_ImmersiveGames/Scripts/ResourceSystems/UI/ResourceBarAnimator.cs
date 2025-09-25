using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceBarAnimator : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image pendingFillImage;

        private Gradient _fillGradient;
        private Color _pendingColor;

        private float _currentFill = 1f;
        private float _currentPending = 1f;

        public void Initialize(ResourceUIStyle style)
        {
            _fillGradient = style.fillGradient;
            _pendingColor = style.pendingColor;

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
                fillImage.color = _fillGradient.Evaluate(1f);
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = 1f;
                pendingFillImage.color = _pendingColor;
            }
        }

        public void SetFillImmediate(float percentage)
        {
            _currentFill = percentage;
            _currentPending = percentage;

            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
                fillImage.color = _fillGradient?.Evaluate(percentage) ?? Color.white;
            }

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = percentage;
        }

        public void SetFill(float percentage)
        {
            _currentFill = percentage;

            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
                fillImage.color = _fillGradient?.Evaluate(percentage) ?? Color.white;
            }
        }

        public void SetPending(float percentage)
        {
            _currentPending = percentage;

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = percentage;
        }
    }
}