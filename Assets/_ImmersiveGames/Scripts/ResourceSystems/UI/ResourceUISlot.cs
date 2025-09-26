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
        [SerializeField] private bool showAnimationDebug;

        public string SlotId => $"{expectedActorId}_{expectedType}";
        public string ExpectedActorId => expectedActorId;
        public ResourceType ExpectedType => expectedType;

        // Referência para o animator (pode ser injetado ou encontrado)
        private ResourceBarAnimator _animator;

        private void Awake()
        {
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            if (rootPanel == null) rootPanel = gameObject;

            // Encontra o animator (alternativa: injetar via código)
            _animator = GetComponentInParent<ResourceBarAnimator>();
            ResetToFull();
            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 Slot inicializado: {SlotId}");
        }

        public bool Matches(string actorId, ResourceType type)
        {
            return string.Equals(ExpectedActorId, actorId, StringComparison.OrdinalIgnoreCase)
                && ExpectedType == type;
        }

        public void Configure(IResourceValue data)
        {
            float targetFill = data.GetPercentage();

            if (valueText != null)
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";

            // Delega a animação para o ResourceBarAnimator
            if (_animator != null)
            {
                _animator.StartAnimation(this, targetFill, style);
            }
            else
            {
                // Fallback: aplica imediatamente se não houver animator
                SetFillValues(targetFill, targetFill);
            }

            SetVisible(true);

            if (showAnimationDebug)
            {
                DebugUtility.LogVerbose<ResourceUISlot>(
                    $"🎯 Configurado: {data.GetCurrentValue():0}/{data.GetMaxValue():0} (Target: {targetFill:F2})");
            }
        }

        // Métodos para o animator controlar os valores
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
            DebugUtility.LogVerbose<ResourceUISlot>($"🔓 Slot limpo: {SlotId}");
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) 
                rootPanel.SetActive(visible);
        }

        // 🔹 Testes rápidos pelo inspector (agora delegam para o animator)
        [ContextMenu("Test Damage Animation")] 
        private void TestDamage() { Simulate(1f, 0.9f); }
        
        [ContextMenu("Test Big Damage Animation")] 
        private void TestBigDamage() { Simulate(1f, 0.5f); }
        
        [ContextMenu("Test Heal Animation")] 
        private void TestHeal() { Simulate(0.5f, 0.8f); }
        
        [ContextMenu("Test Full Heal Animation")] 
        private void TestFullHeal() { Simulate(0.3f, 1f); }

        private void Simulate(float from, float to)
        {
            SetFillValues(from, from);
            if (_animator != null)
                _animator.StartAnimation(this, to, style);
        }
    }
}