using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AnimatedFillStrategy : IResourceSlotStrategy
    {
        private readonly Dictionary<ResourceUISlot, Sequence> _activeSequences = new();

        public void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style)
        {
            // BARRA PRINCIPAL: Preenchimento instantâneo (como jogos de luta)
            if (slot.FillImage != null)
            {
                // Kill tween anterior se existir
                slot.FillImage.DOKill();
            
                slot.FillImage.fillAmount = Mathf.Clamp01(currentPct);
            
                if (style != null)
                    slot.FillImage.color = style.fillGradient.Evaluate(currentPct);
            }

            // BARRA SECUNDÁRIA: Animação com delay (estilo jogos de luta)
            if (slot.PendingFillImage != null)
            {
                // Kill sequência anterior se existir
                if (_activeSequences.TryGetValue(slot, out var oldSequence))
                {
                    oldSequence.Kill();
                    _activeSequences.Remove(slot);
                }

                float targetFill = Mathf.Clamp01(pendingPct);
                float duration = style?.slowDuration ?? 0.8f;
                float delay = style?.delayBeforeSlow ?? 0.3f;

                // Configura cor se houver estilo
                if (style != null)
                    slot.PendingFillImage.color = style.pendingColor;

                // Cria sequência DOTween
                Sequence sequence = DOTween.Sequence();
            
                // Delay inicial (como jogos de luta)
                sequence.AppendInterval(delay);
            
                // Animação do fillAmount com ease out quad
                sequence.Append(
                    DOTween.To(
                        () => slot.PendingFillImage.fillAmount,
                        x => slot.PendingFillImage.fillAmount = x,
                        targetFill,
                        duration
                    ).SetEase(Ease.OutQuad)
                );

                // Guarda referência da sequência
                _activeSequences[slot] = sequence;

                // Configura callbacks para limpeza
                sequence.OnComplete(() => 
                {
                    if (_activeSequences.ContainsKey(slot))
                        _activeSequences.Remove(slot);
                });

                sequence.OnKill(() => 
                {
                    if (_activeSequences.ContainsKey(slot))
                        _activeSequences.Remove(slot);
                });
            }
        }

        public void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style)
        {
            // Para quando não há valor pendente
            ApplyFill(slot, currentPct, currentPct, style);
        }

        public void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style)
        {
            if (slot.ValueText != null)
            {
                slot.ValueText.text = target;
            
                // Opcional: Adicionar animação de texto se quiser
                if (style != null && style.enableTextAnimation)
                {
                    // Exemplo: efeito de scale no texto
                    slot.ValueText.transform.localScale = Vector3.one * 1.2f;
                    slot.ValueText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
            }
        }

        public void ClearVisuals(ResourceUISlot slot)
        {
            // Mata todos os tweens ativos para este slot
            if (slot.FillImage != null)
                slot.FillImage.DOKill();
            
            if (slot.PendingFillImage != null)
                slot.PendingFillImage.DOKill();
            
            if (slot.ValueText != null)
                slot.ValueText.transform.DOKill();

            // Limpa sequências
            if (_activeSequences.TryGetValue(slot, out var sequence))
            {
                sequence.Kill();
                _activeSequences.Remove(slot);
            }
        }
    }
}