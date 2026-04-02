using System;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain
{
    [Flags]
    public enum WorldResetFlags
    {
        None = 0,
        SoftReset = 1 << 0,
        HardReset = 1 << 1
    }
}

