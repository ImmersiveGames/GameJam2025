using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
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
            public float quickDuration;
            public float delayBeforeSlow;
            public float slowDuration;
            public AnimationPhase phase;
        }

        private enum AnimationPhase { Quick, Wait, Slow }

        private readonly Dictionary<ResourceUISlot, AnimationData> _active = new();

        private void Update()
        {
            if (_active.Count == 0) return;

            var finished = new List<ResourceUISlot>();
            foreach (var kv in _active)
            {
                var slot = kv.Key;
                var d = kv.Value;

                if (slot == null) { finished.Add(slot); continue; }

                d.timer += Time.deltaTime;

                if (d.phase == AnimationPhase.Quick)
                {
                    float p = Mathf.Clamp01(d.timer / Mathf.Max(0.0001f, d.quickDuration));
                    float eased = EaseOutCubic(p);
                    float cur = Mathf.Lerp(d.startFill, d.targetFill, eased);
                    slot.SetFillValues(cur, d.targetFill);
                    if (p >= 1f) { d.timer = 0f; d.phase = AnimationPhase.Wait; }
                }
                else if (d.phase == AnimationPhase.Wait)
                {
                    if (d.timer >= d.delayBeforeSlow)
                    {
                        d.timer = 0f;
                        d.phase = AnimationPhase.Slow;
                    }
                }
                else
                {
                    float p = Mathf.Clamp01(d.timer / Mathf.Max(0.0001f, d.slowDuration));
                    float eased = EaseOutCubic(p);
                    float pending = Mathf.Lerp(d.startFill, d.targetFill, eased);
                    slot.SetFillValues(d.targetFill, pending);
                    if (p >= 1f) finished.Add(slot);
                }
            }

            foreach (var s in finished) _active.Remove(s);
        }

        public void StartAnimation(ResourceUISlot slot, float targetFill, ResourceUIStyle style = null)
        {
            if (slot == null) return;
            var st = style ?? defaultStyle;
            if (st == null)
            {
                slot.SetFillValues(targetFill, targetFill);
                return;
            }

            if (_active.TryGetValue(slot, out var data))
            {
                data.startFill = slot.GetCurrentFill();
                data.targetFill = targetFill;
                data.timer = 0f;
                data.phase = AnimationPhase.Quick;
                data.quickDuration = st.quickDuration;
                data.delayBeforeSlow = st.delayBeforeSlow;
                data.slowDuration = st.slowDuration;
            }
            else
            {
                _active[slot] = new AnimationData
                {
                    slot = slot,
                    startFill = slot.GetCurrentFill(),
                    targetFill = targetFill,
                    timer = 0f,
                    quickDuration = st.quickDuration,
                    delayBeforeSlow = st.delayBeforeSlow,
                    slowDuration = st.slowDuration,
                    phase = AnimationPhase.Quick
                };
            }
        }

        private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    }
}
