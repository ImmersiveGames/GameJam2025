using _ImmersiveGames.Scripts.ResourceSystems.Animation;
using _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceUISlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image pendingFillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        [Header("Animation")]
        [SerializeField] private FillAnimationProfile animationProfile;

        private IFillAnimationStrategy _fillStrategy;
        private Tween _colorTween;

        private float _currentFill;
        private float _previousFill;
        private string _currentText = "";
        private bool _isFirstConfigure = true;
        private ResourceUIStyle _currentStyle;

        public ResourceType Type { get; private set; }
        public ResourceInstanceConfig InstanceConfig { get; private set; }

        public Image FillImage => fillImage;
        public Image PendingFillImage => pendingFillImage;
        public TextMeshProUGUI ValueText => valueText;
        public Image IconImage => iconImage;
        public GameObject RootPanel => rootPanel;

        public float GetCurrentFill() => _currentFill;
        public ResourceUIStyle GetCurrentStyle() => _currentStyle;

        private void Awake()
        {
            if (rootPanel == null) rootPanel = gameObject;
        }

        private IFillAnimationStrategy CreateStrategyFromProfile(FillAnimationProfile profile)
        {
            if (profile == null) return new InstantFillAnimationStrategy();

            return profile.animationType switch
            {
                FillAnimationType.BasicReactive => new BasicReactiveFillAnimationStrategy(),
                _ => new InstantFillAnimationStrategy()
            };
        }

        public void InitializeForActorId(string actorId, ResourceType type, ResourceInstanceConfig config)
        {
            Type = type;
            InstanceConfig = config;
            _currentStyle = config?.slotStyle;

            _currentFill = 1f;
            _previousFill = 1f;

            ApplyBaseStyleImmediate();

            // Cria estratégia a partir do perfil (ou Instant como fallback)
            _fillStrategy = CreateStrategyFromProfile(animationProfile);
            _fillStrategy.Initialize(fillImage, pendingFillImage, animationProfile, this);

            if (InstanceConfig?.resourceDefinition != null && iconImage != null)
                iconImage.sprite = InstanceConfig.resourceDefinition.icon;

            gameObject.name = $"{actorId}_{type}";

            ApplyVisualsImmediate();

            DebugUtility.LogVerbose<ResourceUISlot>($"✅ Slot initialized for {actorId}.{type} - Style: {_currentStyle?.name ?? "None"}");
        }

        private void ApplyBaseStyleImmediate()
        {
            if (_currentStyle == null) return;

            DebugUtility.LogVerbose<ResourceUISlot>($"🎨 Applying base style: {_currentStyle.name}");

            if (fillImage != null)
                fillImage.fillAmount = _currentFill;

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = _currentFill;

                if (_currentStyle.pendingColor != default)
                    pendingFillImage.color = _currentStyle.pendingColor;
            }

            ApplyStyleColors(_currentFill);
        }

        public void Configure(IResourceValue data)
        {
            if (data == null) return;

            float newValue = data.GetPercentage();

            DebugUtility.LogVerbose<ResourceUISlot>($"🔄 Slot Configure: {Type} - Previous: {_currentFill}, New: {newValue}, Style: {_currentStyle?.name ?? "None"}");

            _previousFill = _currentFill;
            _currentFill = newValue;
            _currentText = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            if (InstanceConfig?.slotStyle != _currentStyle)
            {
                _currentStyle = InstanceConfig?.slotStyle;
                ApplyBaseStyleImmediate();
            }

            if (_isFirstConfigure)
            {
                _fillStrategy?.SetInstant(_currentFill);
                ApplyVisualsImmediate();
                _isFirstConfigure = false;
            }
            else
            {
                _fillStrategy?.AnimateTo(_currentFill);
            }

            if (valueText != null)
                valueText.text = _currentText;

            ApplyStyleColors(_currentFill);

            SetVisible(true);
        }

        private void ApplyVisualsImmediate()
        {
            DebugUtility.LogVerbose<ResourceUISlot>($"⚡ ApplyVisualsImmediate: {Type} - Current: {_currentFill}, Style: {_currentStyle?.name}");
            _fillStrategy?.SetInstant(_currentFill);
            ClearAllTween();
            ApplyStyleColors(_currentFill);
        }

        public void RefreshStyle()
        {
            _currentStyle = InstanceConfig?.slotStyle;
            ApplyBaseStyleImmediate();
            ApplyVisualsImmediate();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔄 Style refreshed: {Type} - {_currentStyle?.name}");
        }

        public void Clear()
        {
            try { _fillStrategy?.Cancel(); }
            catch
            {
                // ignored
            }
            StopAllCoroutines();
            _currentFill = 0f;
            _previousFill = 0f;
            _isFirstConfigure = true;
            _currentStyle = null;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) rootPanel.SetActive(visible);
        }

        [ContextMenu("Force Visual Update")]
        public void ForceVisualUpdate()
        {
            ApplyBaseStyleImmediate();
            ApplyVisualsImmediate();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 ForceVisualUpdate: {Type} - Fill: {_currentFill}, Style: {_currentStyle?.name}");
        }

        private void OnDestroy()
        {
            ClearAllTween();
            try { _fillStrategy?.Cancel(); }
            catch
            {
                // ignored
            }
        }

        private void ClearAllTween()
        {
            if (FillImage != null) FillImage.DOKill();
            if (PendingFillImage != null) PendingFillImage.DOKill();
            if (ValueText != null) ValueText.transform.DOKill();
            _colorTween?.Kill();
            _colorTween = null;
        }

        private void ApplyStyleColors(float targetFill)
        {
            if (_currentStyle == null)
                return;

            if (FillImage != null && HasValidGradient(_currentStyle))
            {
                Color targetColor = _currentStyle.fillGradient.Evaluate(Mathf.Clamp01(targetFill));
                float transitionDuration = _isFirstConfigure ? 0f : Mathf.Max(0f, _currentStyle.quickDuration);

                if (transitionDuration <= 0f)
                {
                    FillImage.color = targetColor;
                    _colorTween?.Kill();
                    _colorTween = null;
                }
                else
                {
                    _colorTween?.Kill();
                    _colorTween = FillImage.DoColor(targetColor, transitionDuration)
                        .SetEase(_currentStyle.basicEase);
                }
            }

            if (PendingFillImage != null && _currentStyle.pendingColor != default)
            {
                PendingFillImage.color = _currentStyle.pendingColor;
            }
        }

        private static bool HasValidGradient(ResourceUIStyle style)
        {
            return style.fillGradient != null && style.fillGradient.colorKeys != null && style.fillGradient.colorKeys.Length > 0;
        }
    }
}
