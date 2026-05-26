using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityScanResult
    {
        public ActivityCapabilityScanResult(
            string scannerId,
            IReadOnlyList<ActivityCapabilityOwnerDescriptor> owners,
            IReadOnlyList<ActivityCapabilityDescriptor> capabilities,
            IReadOnlyList<IActivityCapabilityRuntimeReference> runtimeReferences,
            string source,
            string reason)
        {
            ScannerId = Normalize(scannerId);
            Owners = owners ?? Array.Empty<ActivityCapabilityOwnerDescriptor>();
            Capabilities = capabilities ?? Array.Empty<ActivityCapabilityDescriptor>();
            RuntimeReferences = runtimeReferences ?? Array.Empty<IActivityCapabilityRuntimeReference>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ScannerId { get; }
        public IReadOnlyList<ActivityCapabilityOwnerDescriptor> Owners { get; }
        public IReadOnlyList<ActivityCapabilityDescriptor> Capabilities { get; }
        public IReadOnlyList<IActivityCapabilityRuntimeReference> RuntimeReferences { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ScannerId);

        public static ActivityCapabilityScanResult Empty(string scannerId, string source, string reason)
        {
            return new ActivityCapabilityScanResult(scannerId, Array.Empty<ActivityCapabilityOwnerDescriptor>(), Array.Empty<ActivityCapabilityDescriptor>(), Array.Empty<IActivityCapabilityRuntimeReference>(), source, reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
