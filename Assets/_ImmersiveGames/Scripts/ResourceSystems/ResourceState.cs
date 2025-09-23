using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceState
    {
        public float CurrentValue { get; set; }
        public float MaxValue { get; }
        public List<float> TriggeredThresholds { get; }

        public ResourceState(float currentValue, float maxValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
            TriggeredThresholds = new List<float>();
        }

        public float GetPercentage()
        {
            return MaxValue > 0 ? CurrentValue / MaxValue : 0f;
        }
    }
}