using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityScanContext
    {
        public ActivityCapabilityScanContext(
            SessionActivityIdentity identity,
            IReadOnlyList<ActivityObjectCapabilityScanTarget> activityObjectTargets,
            IReadOnlyList<ActorScanTarget> actorTargets,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityObjectTargets = activityObjectTargets ?? Array.Empty<ActivityObjectCapabilityScanTarget>();
            ActorTargets = actorTargets ?? Array.Empty<ActorScanTarget>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActivityCapabilityScanContext(
            SessionActivityIdentity identity,
            string source,
            string reason)
            : this(identity, Array.Empty<ActivityObjectCapabilityScanTarget>(), Array.Empty<ActorScanTarget>(), source, reason)
        {
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActivityObjectCapabilityScanTarget> ActivityObjectTargets { get; }
        public IReadOnlyList<ActorScanTarget> ActorTargets { get; }
        public string Source { get; }
        public string Reason { get; }
        public ActivityCapabilityInventoryId InventoryId => ActivityCapabilityInventoryId.FromIdentity(Identity);

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
}
}
