using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityScanContext
    {
        public ActivityCapabilityScanContext(
            SessionActivityIdentity identity,
            IReadOnlyList<ActivityObjectCapabilityScanTarget> activityObjectTargets,
            IReadOnlyList<ActivityCapabilityPlayerActorScanTarget> playerActorTargets,
            string source,
            string reason)
        {
            Identity = identity;
            ActivityObjectTargets = activityObjectTargets ?? Array.Empty<ActivityObjectCapabilityScanTarget>();
            PlayerActorTargets = playerActorTargets ?? Array.Empty<ActivityCapabilityPlayerActorScanTarget>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityCapabilityScanContext(
            SessionActivityIdentity identity,
            string source,
            string reason)
            : this(identity, Array.Empty<ActivityObjectCapabilityScanTarget>(), Array.Empty<ActivityCapabilityPlayerActorScanTarget>(), source, reason)
        {
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActivityObjectCapabilityScanTarget> ActivityObjectTargets { get; }
        public IReadOnlyList<ActivityCapabilityPlayerActorScanTarget> PlayerActorTargets { get; }
        public string Source { get; }
        public string Reason { get; }
        public ActivityCapabilityInventoryId InventoryId => ActivityCapabilityInventoryId.FromIdentity(Identity);

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
