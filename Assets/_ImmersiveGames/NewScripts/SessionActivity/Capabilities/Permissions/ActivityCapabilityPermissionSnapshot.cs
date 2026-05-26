using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionSnapshot
    {
        public ActivityCapabilityPermissionSnapshot(
            ActivityCapabilityPermissionFact lastFact,
            IReadOnlyList<ActivityCapabilityPermissionBinding> bindings)
        {
            LastFact = lastFact;
            Bindings = bindings ?? Array.Empty<ActivityCapabilityPermissionBinding>();
        }

        public ActivityCapabilityPermissionFact LastFact { get; }
        public IReadOnlyList<ActivityCapabilityPermissionBinding> Bindings { get; }
        public bool IsValid => Bindings != null;
    }
}
