using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityInventory
    {
        public ActivityCapabilityInventory(
            ActivityCapabilityInventoryId id,
            IReadOnlyList<ActivityCapabilityOwnerDescriptor> owners,
            IReadOnlyList<ActivityCapabilityDescriptor> capabilities,
            IReadOnlyDictionary<string, IActivityCapabilityRuntimeReference> runtimeReferences,
            string source,
            string reason)
        {
            Id = id;
            Owners = owners ?? Array.Empty<ActivityCapabilityOwnerDescriptor>();
            Capabilities = capabilities ?? Array.Empty<ActivityCapabilityDescriptor>();
            RuntimeReferences = runtimeReferences ?? new Dictionary<string, IActivityCapabilityRuntimeReference>(StringComparer.Ordinal);
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActivityCapabilityInventoryId Id { get; }
        public IReadOnlyList<ActivityCapabilityOwnerDescriptor> Owners { get; }
        public IReadOnlyList<ActivityCapabilityDescriptor> Capabilities { get; }
        public IReadOnlyDictionary<string, IActivityCapabilityRuntimeReference> RuntimeReferences { get; }
        public string Source { get; }
        public string Reason { get; }

        public int OwnerCount => Owners.Count;
        public int CapabilityCount => Capabilities.Count;
        public int RuntimeReferenceCount => RuntimeReferences.Count;
        public bool HasCapabilities => CapabilityCount > 0;
        public bool IsValid => Id.IsValid;

        public bool TryGetRuntimeReference<TReference>(string capabilityId, out TReference runtimeReference)
            where TReference : class, IActivityCapabilityRuntimeReference
        {
            runtimeReference = null;
            if (string.IsNullOrWhiteSpace(capabilityId) ||
                RuntimeReferences == null ||
                !RuntimeReferences.TryGetValue(capabilityId.Trim(), out var value) ||
                value is not TReference typedValue)
            {
                return false;
            }

            runtimeReference = typedValue;
            return true;
        }

        public IReadOnlyList<TReference> GetRuntimeReferences<TReference>()
            where TReference : class, IActivityCapabilityRuntimeReference
        {
            if (RuntimeReferences == null || RuntimeReferences.Count == 0)
            {
                return Array.Empty<TReference>();
            }

            List<string> capabilityIds = new(RuntimeReferences.Keys);
            capabilityIds.Sort(StringComparer.Ordinal);

            List<TReference> typedReferences = new(capabilityIds.Count);
            for (int index = 0; index < capabilityIds.Count; index++)
            {
                string capabilityId = capabilityIds[index];
                if (RuntimeReferences.TryGetValue(capabilityId, out var runtimeReference) &&
                    runtimeReference is TReference typedReference)
                {
                    typedReferences.Add(typedReference);
                }
            }

            return typedReferences;
        }

        public IReadOnlyList<TReference> EnumerateRuntimeReferences<TReference>()
            where TReference : class, IActivityCapabilityRuntimeReference
        {
            return GetRuntimeReferences<TReference>();
        }

        public override string ToString()
        {
            return $"id='{Id}', owners='{OwnerCount}', capabilities='{CapabilityCount}', source='{Source}', reason='{Reason}'";
        }
}
}
