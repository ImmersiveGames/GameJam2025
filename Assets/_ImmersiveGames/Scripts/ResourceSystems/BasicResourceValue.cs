using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResourceValue
    {
        float GetCurrentValue();
        float GetMaxValue();
        float GetPercentage();
        void Increase(float amount);
        void Decrease(float amount);
        void SetCurrentValue(float value);
        bool IsDepleted();
        bool IsFull();
    }

    public class BasicResourceValue : IResourceValue
    {
        private float CurrentValue { get; set; }
        private float MaxValue { get; set; }

        public BasicResourceValue(float current, float max = 100f)
        {
            MaxValue = Mathf.Max(0f, max);
            CurrentValue = Mathf.Clamp(current, 0f, MaxValue);
        }

        public float GetCurrentValue() => CurrentValue;
        public float GetMaxValue() => MaxValue;
        public float GetPercentage() => MaxValue > 0f ? CurrentValue / MaxValue : 0f;
        public bool IsDepleted() => CurrentValue <= 0f;
        public bool IsFull() => CurrentValue >= MaxValue;

        public void Increase(float amount)
        {
            CurrentValue = Mathf.Clamp(CurrentValue + amount, 0f, MaxValue);
            DebugUtility.LogVerbose<BasicResourceValue>($"Increase: Amount={amount:F2}, NewValue={CurrentValue:F2}");
        }

        public void Decrease(float amount)
        {
            CurrentValue = Mathf.Clamp(CurrentValue - amount, 0f, MaxValue);
            DebugUtility.LogVerbose<BasicResourceValue>($"Decrease: Amount={amount:F2}, NewValue={CurrentValue:F2}");
        }

        public void SetCurrentValue(float value)
        {
            CurrentValue = Mathf.Clamp(value, 0f, MaxValue);
        }
    }
}