using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceBarAnimator : MonoBehaviour
    {
        [SerializeField] private ResourceUIStyle defaultStyle;
        
        private class AnimationData
        {
            public ResourceUISlot slot;
            public float startFill;
            public float targetFill;
            public float timer;
            public AnimationPhase phase;
            public ResourceUIStyle style;
        }
        
        private readonly Dictionary<ResourceUISlot, AnimationData> _activeAnimations = new Dictionary<ResourceUISlot, AnimationData>();
        
        private enum AnimationPhase
        {
            QuickAnimation,
            WaitingDelay,
            SlowAnimation
        }

        private void Update()
        {
            AnimateAllSlots();
        }

        public void StartAnimation(ResourceUISlot slot, float targetFill, ResourceUIStyle style = null)
        {
            var animationStyle = style ?? defaultStyle;
            
            if (_activeAnimations.TryGetValue(slot, out var uiData))
            {
                // Atualiza animação existente
                uiData.targetFill = targetFill;
                uiData.style = animationStyle;
                
                // Se estava na fase lenta, volta para a fase rápida
                if (uiData.phase == AnimationPhase.SlowAnimation)
                {
                    uiData.phase = AnimationPhase.QuickAnimation;
                    uiData.timer = 0f;
                }
            }
            else
            {
                // Cria nova animação
                var data = new AnimationData
                {
                    slot = slot,
                    startFill = slot.GetCurrentFill(),
                    targetFill = targetFill,
                    timer = 0f,
                    phase = AnimationPhase.QuickAnimation,
                    style = animationStyle
                };
                
                _activeAnimations[slot] = data;
            }
        }

        public void StopAnimation(ResourceUISlot slot)
        {
            _activeAnimations.Remove(slot);
        }

        private void AnimateAllSlots()
        {
            var slotsToRemove = new List<ResourceUISlot>();

            foreach (var kvp in _activeAnimations)
            {
                var slot = kvp.Key;
                var data = kvp.Value;
                
                data.timer += Time.deltaTime;

                switch (data.phase)
                {
                    case AnimationPhase.QuickAnimation:
                        AnimateQuickPhase(slot, data);
                        break;
                    case AnimationPhase.WaitingDelay:
                        AnimateWaitPhase(data);
                        break;
                    case AnimationPhase.SlowAnimation:
                        AnimateSlowPhase(slot, data);
                        break;
                }

                // Verifica se a animação terminou
                if (data.phase == AnimationPhase.SlowAnimation && 
                    data.timer >= data.style.slowDuration)
                {
                    slot.SetFillValues(data.targetFill, data.targetFill);
                    slotsToRemove.Add(slot);
                }
            }

            // Remove animações concluídas
            foreach (var slot in slotsToRemove)
            {
                _activeAnimations.Remove(slot);
            }
        }

        private void AnimateQuickPhase(ResourceUISlot slot, AnimationData data)
        {
            float progress = Mathf.Clamp01(data.timer / data.style.quickDuration);
            float eased = EaseOutCubic(progress);

            float currentFill = Mathf.Lerp(data.startFill, data.targetFill, eased);
            slot.SetFillValues(currentFill, slot.GetPendingFill());

            if (progress >= 1f)
            {
                data.timer = 0f;
                data.phase = AnimationPhase.WaitingDelay;
            }
        }

        private void AnimateWaitPhase(AnimationData data)
        {
            if (data.timer >= data.style.delayBeforeSlow)
            {
                data.timer = 0f;
                data.phase = AnimationPhase.SlowAnimation;
            }
        }

        private void AnimateSlowPhase(ResourceUISlot slot, AnimationData data)
        {
            float progress = Mathf.Clamp01(data.timer / data.style.slowDuration);
            float eased = EaseOutCubic(progress);

            float currentPending = Mathf.Lerp(data.startFill, data.targetFill, eased);
            slot.SetFillValues(data.targetFill, currentPending);
        }

        private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    }
}