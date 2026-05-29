using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

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
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            TargetId = Normalize(targetId);
            ComponentPath = Normalize(componentPath);
            Endpoint = endpoint;
            ContractView = endpoint as IActivityObjectSnapshotRestoreEndpointContractView;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActivityObjectSnapshotRestoreEndpoint Endpoint { get; }
        public IActivityObjectSnapshotRestoreEndpointContractView ContractView { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Endpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
