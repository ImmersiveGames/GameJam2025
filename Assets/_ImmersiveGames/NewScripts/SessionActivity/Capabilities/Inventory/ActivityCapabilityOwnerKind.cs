using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public enum ActivityCapabilityOwnerKind
    {
        Unknown = 0,
        [Obsolete("PlayerActor is historical only. Use Actor for active discovery and lifecycle ownership.")]
        PlayerActor = 1,
        ActivityObject = 3,
        SceneContributor = 4,
        Unsupported = 5,
        Actor = 6,
        RuntimeSpawnedActor = 7,
    }
}
