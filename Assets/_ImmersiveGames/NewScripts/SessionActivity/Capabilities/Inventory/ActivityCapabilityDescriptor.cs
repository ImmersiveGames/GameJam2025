using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityDescriptor
    {
        public ActivityCapabilityDescriptor(
            string capabilityId,
            ActivityCapabilityKind capabilityKind,
            string moduleId,
            string ownerId,
            string componentPath,
            string componentType,
            bool required,
            int priority,
            IReadOnlyList<ActivityCapabilityPolicyEntry> policyMetadata,
            string source)
        {
            CapabilityId = capabilityId.TrimToEmpty();
            CapabilityKind = capabilityKind;
            ModuleId = moduleId.TrimToEmpty();
            OwnerId = ownerId.TrimToEmpty();
            ComponentPath = componentPath.TrimToEmpty();
            ComponentType = componentType.TrimToEmpty();
            Required = required;
            Priority = priority;
            PolicyMetadata = policyMetadata ?? Array.Empty<ActivityCapabilityPolicyEntry>();
            Source = source.TrimToEmpty();
        }

        public string CapabilityId { get; }
        public ActivityCapabilityKind CapabilityKind { get; }
        public string ModuleId { get; }
        public string OwnerId { get; }
        public string ComponentPath { get; }
        public string ComponentType { get; }
        public bool Required { get; }
        public int Priority { get; }
        public IReadOnlyList<ActivityCapabilityPolicyEntry> PolicyMetadata { get; }
        public string Source { get; }

        public bool IsOptional => !Required;
        public bool HasPolicyMetadata => PolicyMetadata.Count > 0;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            CapabilityKind != ActivityCapabilityKind.Unknown &&
            !string.IsNullOrWhiteSpace(ModuleId) &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            !string.IsNullOrWhiteSpace(ComponentPath) &&
            !string.IsNullOrWhiteSpace(ComponentType);

        public override string ToString()
        {
            return $"capabilityId='{CapabilityId}', capabilityKind='{CapabilityKind}', moduleId='{ModuleId}', ownerId='{OwnerId}', componentPath='{ComponentPath}', componentType='{ComponentType}', required='{Required}', priority='{Priority}'";
        }
}
}
