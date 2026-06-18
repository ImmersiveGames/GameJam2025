using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityObjectResetEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActivityObjectResetEndpointReference(
            string capabilityId,
            string ownerId,
            string targetId,
            string componentPath,
            IActivityObjectResetEndpoint endpoint)
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
        public IActivityObjectResetEndpoint Endpoint { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Endpoint != null;
    }

}
