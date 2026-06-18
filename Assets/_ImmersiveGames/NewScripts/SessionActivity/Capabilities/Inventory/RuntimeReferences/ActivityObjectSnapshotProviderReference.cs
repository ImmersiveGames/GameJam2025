using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityObjectSnapshotProviderReference : IActivityCapabilityRuntimeReference
    {
        public ActivityObjectSnapshotProviderReference(
            string capabilityId,
            string ownerId,
            string targetId,
            string componentPath,
            IActivityObjectSnapshotProvider provider)
        {
            CapabilityId = capabilityId.TrimToEmpty();
            OwnerId = ownerId.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            ComponentPath = componentPath.TrimToEmpty();
            Provider = provider;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActivityObjectSnapshotProvider Provider { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Provider != null;
}
}
