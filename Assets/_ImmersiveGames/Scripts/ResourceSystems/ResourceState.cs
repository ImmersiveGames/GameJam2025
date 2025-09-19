using System;
using System.Collections.Generic;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public struct ResourceState
    {
        public float CurrentValue;
        public float MaxValue;
        public List<float> TriggeredThresholds;

        public ResourceState(float currentValue, float maxValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
            TriggeredThresholds = new List<float>();
        }

        public float GetPercentage() => MaxValue > 0 ? CurrentValue / MaxValue : 0f;
    }
}