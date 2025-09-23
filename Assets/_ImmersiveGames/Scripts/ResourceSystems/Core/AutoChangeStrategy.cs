using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class AutoFillStrategy : IAutoChangeStrategy
    {
        public float GetBaseRate(ResourceConfigSo config)
        {
            if (!config.AutoFillEnabled)
            {
                DebugUtility.LogVerbose<AutoFillStrategy>("AutoFill desativado, retornando taxa 0.");
                return 0f;
            }
            return config.AutoFillRate;
        }
    }

    public class AutoDrainStrategy : IAutoChangeStrategy
    {
        public float GetBaseRate(ResourceConfigSo config)
        {
            if (!config.AutoDrainEnabled)
            {
                DebugUtility.LogVerbose<AutoDrainStrategy>("AutoDrain desativado, retornando taxa 0.");
                return 0f;
            }
            return -config.AutoDrainRate;
        }
    }
}