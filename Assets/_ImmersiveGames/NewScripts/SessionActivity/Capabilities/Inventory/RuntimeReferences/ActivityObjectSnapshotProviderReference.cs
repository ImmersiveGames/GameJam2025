using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

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
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            TargetId = Normalize(targetId);
            ComponentPath = Normalize(componentPath);
            Provider = provider;
            ContractView = provider as IActivityObjectSnapshotProviderContractView;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public IActivityObjectSnapshotProvider Provider { get; }
        public IActivityObjectSnapshotProviderContractView ContractView { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && Provider != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
