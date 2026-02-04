using System;
namespace _ImmersiveGames.NewScripts.Runtime.World.Reset
{
    [Flags]
    public enum ResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }
}

