using System;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset
{
    [Flags]
    public enum WorldResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }
}

