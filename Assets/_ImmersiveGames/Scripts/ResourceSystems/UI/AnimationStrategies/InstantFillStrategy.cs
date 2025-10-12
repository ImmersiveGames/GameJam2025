using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public class InstantSlotStrategy : IResourceSlotStrategy
    {
        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            DebugUtility.LogVerbose<InstantSlotStrategy>($"🎯 Instant ApplyFill - Current: {currentPct}, Pending: {pendingPct}");

            // BARRA PRINCIPAL: Sempre atualizar
            if (slot.FillImage != null)
            {
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
                DebugUtility.LogVerbose<InstantSlotStrategy>($"✅ FillImage set to: {currentPct}");

                // Gradient controla as cores
                if (style != null)
                {
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
                    DebugUtility.LogVerbose<InstantSlotStrategy>($"🎨 FillImage color: {slot.FillImage.color}");
                }
            }

            // CORREÇÃO CRÍTICA: Na estratégia Instant, a barra pending DEVE ser escondida
            // ou ter o mesmo valor da barra principal para não causar sobreposição
            if (slot.PendingFillImage != null)
            {
                // OPÇÃO 1: Esconder completamente (recomendado para Instant)
                slot.PendingFillImage.fillAmount = 0f;
                
                // OPÇÃO 2: Usar mesmo valor da principal (se quiser manter visível)
                // slot.PendingFillImage.fillAmount = Mathf.Clamp01(currentPct);
                
                DebugUtility.LogVerbose<InstantSlotStrategy>($"🚫 PendingImage hidden (Instant strategy)");
            }
        }

        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            ApplyFill(slot, currentPct, currentPct, style);
        }

        public void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style)
        {
            if (slot.ValueText != null)
            {
                slot.ValueText.text = target;
                DebugUtility.LogVerbose<InstantSlotStrategy>($"📝 Text set to: {target}");
            }
        }

        public void ClearVisuals(ResourceUISlot slot)
        {
            // Sem estado para limpar
        }
    }
}