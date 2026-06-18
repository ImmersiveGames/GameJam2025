using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityObjectSnapshotRestoreEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActivityObjectSnapshotRestoreEndpointReference(
            string capabilityId,
            string ownerId,
            string targetId,
            string componentPath,
            IActivityObjectSnapshotRestoreEndpoint endpoint)
        {
            CapabilityId = capabilityId.TrimToEmpty();
            OwnerId = ownerId.TrimToEmpty();
            TargetId = targetId.TrimToEmpty();
            ComponentPath = componentPath.TrimToEmpty();
            Endpoint = endpoint;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActivityObjectSnapshotRestoreEndpoint Endpoint { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Endpoint != null;
}
}
