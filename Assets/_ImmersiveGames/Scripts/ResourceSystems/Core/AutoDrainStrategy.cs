using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AutoDrainStrategy : IAutoChangeStrategy
    {
        public bool ShouldApply(ValueCore valueCore) =>
            valueCore.GetCurrentValue() > 0;

        public float GetBaseRate(ResourceConfigSo config) =>
            Mathf.Max(0, config.AutoDrainRate);

        public bool IsIncreasing => false;
    }
}