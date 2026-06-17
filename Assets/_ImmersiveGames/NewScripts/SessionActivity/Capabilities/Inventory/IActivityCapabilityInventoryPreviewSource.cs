using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    internal interface IActivityCapabilityInventoryPreviewSource
    {
        string ActivityObjectScannerId { get; }
        string ActorLifecycleScannerId { get; }

        ActivityCapabilityInventoryBuildResult BuildForEntry(
            SessionActivityIdentity identity,
            ActivityObjectContributorDiscoveryResult activityObjectDiscovery,
            IReadOnlyList<ActorScanTarget> actorTargets,
            string source,
            string reason);
    }
}
