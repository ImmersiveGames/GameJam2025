using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AutoFillStrategy : IAutoChangeStrategy
    {
        public bool ShouldApply(ValueCore valueCore) =>
            valueCore.GetCurrentValue() < valueCore.GetMaxValue();

        public float GetBaseRate(ResourceConfigSo config) =>
            Mathf.Max(0, config.AutoFillRate);

        public bool IsIncreasing => true;
    }
}