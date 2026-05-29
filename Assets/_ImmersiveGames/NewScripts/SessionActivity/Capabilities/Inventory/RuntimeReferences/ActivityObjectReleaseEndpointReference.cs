using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityObjectReleaseEndpointReference : IActivityCapabilityRuntimeReference
    {
        public ActivityObjectReleaseEndpointReference(
            string capabilityId,
            string ownerId,
            string targetId,
            string componentPath,
            IActivityObjectReleaseEndpoint endpoint)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            TargetId = Normalize(targetId);
            ComponentPath = Normalize(componentPath);
            Endpoint = endpoint;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActivityObjectReleaseEndpoint Endpoint { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Endpoint != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
