using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset
{
    [Flags]
    public enum ResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }
}
