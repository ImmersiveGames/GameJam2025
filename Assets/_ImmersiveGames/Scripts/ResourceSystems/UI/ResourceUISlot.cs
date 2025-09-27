using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class ResourceUISlot : MonoBehaviour, IResourceUISlot
    {
        private IActor _expectedActor;
        private ResourceType _expectedType;

        [Header("UI Components")]
        [SerializeField] private Image fillImage;        
        [SerializeField] private Image pendingFillImage; 
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        [Header("Style (SO)")]
        [SerializeField] private ResourceUIStyle style;

        [Header("Debug")]
        [SerializeField] private bool showAnimationDebug;

        public string SlotId => $"{_expectedActor?.ActorName ?? "Unknown"}_{_expectedType}";
        public string ExpectedActorId => _expectedActor?.ActorName ?? string.Empty;
        public ResourceType ExpectedType => _expectedType;

        private ResourceBarAnimator _animator;

        private void Awake()
        {
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            if (rootPanel == null) rootPanel = gameObject;

            _animator = GetComponentInParent<ResourceBarAnimator>();
            ResetToFull();
        }

        public void InitializeForActor(IActor actor, ResourceType type)
        {
            _expectedActor = actor;
            _expectedType = type;
            
            if (actor is MonoBehaviour monoActor)
            {
                gameObject.name = $"{monoActor.gameObject.name}_{type}";
            }
            
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 Slot inicializado: {SlotId}");
        }

        public bool Matches(string actorId, ResourceType type)
        {
            return string.Equals(ExpectedActorId, actorId, StringComparison.OrdinalIgnoreCase)
                && ExpectedType == type;
        }

        public bool Matches(IActor actor, ResourceType type)
        {
            return _expectedActor == actor && ExpectedType == type;
        }

        public void Configure(IResourceValue data)
        {
            float targetFill = data.GetPercentage();

            if (valueText != null)
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            if (_animator != null)
            {
                _animator.StartAnimation(this, targetFill, style);
            }
            else
            {
                SetFillValues(targetFill, targetFill);
            }

            SetVisible(true);

            if (showAnimationDebug)
            {
                DebugUtility.LogVerbose<ResourceUISlot>(
                    $"🎯 Configurado: {data.GetCurrentValue():0}/{data.GetMaxValue():0} (Target: {targetFill:F2})");
            }
        }

        public void SetFillValues(float currentFill, float pendingFill)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentFill;
                fillImage.color = style != null ? style.fillGradient.Evaluate(currentFill) : Color.green;
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = pendingFill;
                pendingFillImage.color = style != null ? style.pendingColor : Color.red;
            }
        }

        public float GetCurrentFill() => fillImage != null ? fillImage.fillAmount : 0f;
        public float GetPendingFill() => pendingFillImage != null ? pendingFillImage.fillAmount : 0f;

        private void ResetToFull()
        {
            SetFillValues(1f, 1f);
        }

        public void Clear()
        {
            if (_animator != null)
                _animator.StopAnimation(this);

            SetFillValues(0f, 0f);

            if (valueText != null) 
                valueText.text = "";

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) 
                rootPanel.SetActive(visible);
        }
    }
}