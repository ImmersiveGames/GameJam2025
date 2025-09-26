using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class BasicResourceValue: IResourceValue
    {
        private float CurrentValue { get; set; }
        private float MaxValue { get; set; }
        public BasicResourceValue(int current, int max = 100)
        {
            CurrentValue = Mathf.Clamp(current, 0f, max);
            MaxValue = max;
        }
        public float GetCurrentValue() => CurrentValue;
        public float GetMaxValue() => MaxValue;
        public float GetPercentage()=> MaxValue > 0 ? CurrentValue / MaxValue : 0f;
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
    public enum ResourceType { Health, Mana, Energy, Stamina, Custom }
}