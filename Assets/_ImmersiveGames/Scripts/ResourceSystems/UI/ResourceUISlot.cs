using System;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceUISlot : MonoBehaviour, IResourceUISlot
    {
        [SerializeField] private string expectedActorId;
        [SerializeField] private ResourceType expectedType;

        [Header("UI Components")]
        [SerializeField] private Image fillImage;        
        [SerializeField] private Image pendingFillImage; 
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        [Header("Style (SO)")]
        [SerializeField] private ResourceUIStyle style;

        [Header("Debug")]
        [SerializeField] private bool showAnimationDebug = false;

        public string SlotId => $"{expectedActorId}_{expectedType}";
        public string ExpectedActorId => expectedActorId;
        public ResourceType ExpectedType => expectedType;

        // runtime
        private float _currentFill = 1f;      
        private float _currentPending = 1f;   
        private float _targetFill = 1f;
        private float _animationTimer = 0f;
        private AnimationPhase _currentPhase = AnimationPhase.Idle;

        private enum AnimationPhase
        {
            Idle,
            QuickAnimation,
            WaitingDelay,
            SlowAnimation
        }

        private void Awake()
        {
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            if (rootPanel == null) rootPanel = gameObject;

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
                fillImage.color = style != null ? style.fillGradient.Evaluate(1f) : Color.green;
                _currentFill = 1f;
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = 1f;
                pendingFillImage.color = style != null ? style.pendingColor : Color.red;
                _currentPending = 1f;
            }

            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 Slot inicializado: {SlotId}");
        }

        private void Update()
        {
            if (_currentPhase != AnimationPhase.Idle)
                AnimateFill();
        }

        public bool Matches(string actorId, ResourceType type)
        {
            return string.Equals(ExpectedActorId, actorId, StringComparison.OrdinalIgnoreCase)
                && ExpectedType == type;
        }

        public void Configure(IResourceValue data)
        {
            _targetFill = data.GetPercentage();

            if (valueText != null)
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            if (Mathf.Abs(_currentFill - _targetFill) > 0.01f)
                StartAnimation();
            else
                ApplyImmediate(_targetFill);

            SetVisible(true);

            if (showAnimationDebug)
            {
                DebugUtility.LogVerbose<ResourceUISlot>(
                    $"🎯 Configurado: {data.GetCurrentValue():0}/{data.GetMaxValue():0} (Target: {_targetFill:F2})");
            }
        }

        private void StartAnimation()
        {
            _animationTimer = 0f;
            _currentPhase = AnimationPhase.QuickAnimation;

            if (showAnimationDebug)
            {
                bool isDamage = _targetFill < _currentFill;
                DebugUtility.LogVerbose<ResourceUISlot>(
                    $"🎬 Iniciando animação: {(isDamage ? "DANO" : "CURA")} de {_currentFill:F2} → {_targetFill:F2}");
            }
        }

        private void AnimateFill()
        {
            _animationTimer += Time.deltaTime;

            switch (_currentPhase)
            {
                case AnimationPhase.QuickAnimation: AnimateQuickPhase(); break;
                case AnimationPhase.WaitingDelay:   AnimateWaitPhase();  break;
                case AnimationPhase.SlowAnimation:  AnimateSlowPhase();  break;
            }
        }

        private void AnimateQuickPhase()
        {
            float progress = Mathf.Clamp01(_animationTimer / style.quickDuration);
            float eased = EaseOutCubic(progress);

            _currentFill = Mathf.Lerp(_currentFill, _targetFill, eased);

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = style.fillGradient.Evaluate(_currentFill);
            }

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = _currentPending;

            if (progress >= 1f)
            {
                _animationTimer = 0f;
                _currentPhase = AnimationPhase.WaitingDelay;

                if (showAnimationDebug)
                    DebugUtility.LogVerbose<ResourceUISlot>(
                        $"⚡ Fase rápida concluída: Verde={_currentFill:F2}, Vermelha={_currentPending:F2}");
            }
        }

        private void AnimateWaitPhase()
        {
            if (_animationTimer >= style.delayBeforeSlow)
            {
                _animationTimer = 0f;
                _currentPhase = AnimationPhase.SlowAnimation;

                if (showAnimationDebug)
                    DebugUtility.LogVerbose<ResourceUISlot>($"⏳ Delay concluído, iniciando animação lenta");
            }
        }

        private void AnimateSlowPhase()
        {
            float progress = Mathf.Clamp01(_animationTimer / style.slowDuration);
            float eased = EaseOutCubic(progress);

            _currentPending = Mathf.Lerp(_currentPending, _currentFill, eased);

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = _currentPending;

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = style.fillGradient.Evaluate(_currentFill);
            }

            if (progress >= 1f)
                FinishAnimation();
        }

        private void FinishAnimation()
        {
            _currentPending = _currentFill;

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = _currentPending;

            if (fillImage != null)
                fillImage.fillAmount = _currentFill;

            _currentPhase = AnimationPhase.Idle;

            if (showAnimationDebug)
                DebugUtility.LogVerbose<ResourceUISlot>($"🏁 Animação concluída: {_currentFill:F2}");
        }

        private void ApplyImmediate(float targetFill)
        {
            _currentFill = targetFill;
            _currentPending = targetFill;
            _targetFill = targetFill;

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = style.fillGradient.Evaluate(_currentFill);
            }

            if (pendingFillImage != null)
                pendingFillImage.fillAmount = _currentPending;

            _currentPhase = AnimationPhase.Idle;
        }

        private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        public void Clear()
        {
            _currentPhase = AnimationPhase.Idle;
            ApplyImmediate(0f);

            if (valueText != null) 
                valueText.text = "";

            SetVisible(false);
            DebugUtility.LogVerbose<ResourceUISlot>($"🔓 Slot limpo: {SlotId}");
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) 
                rootPanel.SetActive(visible);
        }

        // 🔹 Testes rápidos pelo inspector
        [ContextMenu("Test Damage Animation")] private void TestDamage() { Simulate(1f, 0.9f); }
        [ContextMenu("Test Big Damage Animation")] private void TestBigDamage() { Simulate(1f, 0.5f); }
        [ContextMenu("Test Heal Animation")] private void TestHeal() { Simulate(0.5f, 0.8f); }
        [ContextMenu("Test Full Heal Animation")] private void TestFullHeal() { Simulate(0.3f, 1f); }

        private void Simulate(float from, float to)
        {
            _currentFill = from;
            _currentPending = from;
            _targetFill = to;
            StartAnimation();
        }
    }
}
