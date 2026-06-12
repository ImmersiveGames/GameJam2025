using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class ActivityCapabilityInventoryBuilder
    {
        private readonly ActivityCapabilityScannerRegistry _registry;

        public ActivityCapabilityInventoryBuilder(ActivityCapabilityScannerRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public ActivityCapabilityInventory Build(
            ActivityCapabilityScanContext context,
            out IReadOnlyList<ActorCameraBindingContribution> cameraBindingContributions,
            out IReadOnlyList<ActorAttributeSetupContribution> attributeSetupContributions,
            out IReadOnlyList<ActorPresentationSetupContribution> presentationSetupContributions,
            out IReadOnlyList<ActivityPermissionReceiverContribution> permissionReceiverContributions)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("Activity capability inventory scan context is invalid.");
            }

            var inventoryId = context.InventoryId;
            List<ActivityCapabilityOwnerDescriptor> owners = new();
            List<ActivityCapabilityDescriptor> capabilities = new();
            Dictionary<string, IActivityCapabilityRuntimeReference> runtimeReferences = new(StringComparer.Ordinal);
            List<ActorCameraBindingContribution> cameraContributions = new();
            List<ActorAttributeSetupContribution> attributeContributions = new();
            List<ActorPresentationSetupContribution> presentationContributions = new();
            List<ActivityPermissionReceiverContribution> contributions = new();

            for (int scannerIndex = 0; scannerIndex < _registry.OrderedScanners.Count; scannerIndex++)
            {
                var scanner = _registry.OrderedScanners[scannerIndex];
                var scanResult = scanner.Scan(context);
                if (!scanResult.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability scanner '{scanner.ScannerId}' returned invalid result.");
                }

                AppendOwners(owners, scanResult);
                AppendCapabilities(capabilities, scanResult, inventoryId);
                AppendRuntimeReferences(runtimeReferences, scanResult);
                AppendCameraBindingContributions(cameraContributions, scanResult);
                AppendAttributeSetupContributions(attributeContributions, scanResult);
                AppendPresentationSetupContributions(presentationContributions, scanResult);
                AppendPermissionReceiverContributions(contributions, scanResult);
            }

            owners.Sort(CompareOwners);
            capabilities.Sort(CompareCapabilities);
            cameraBindingContributions = cameraContributions;
            attributeSetupContributions = attributeContributions;
            presentationSetupContributions = presentationContributions;
            permissionReceiverContributions = contributions;

            return new ActivityCapabilityInventory(inventoryId, owners, capabilities, runtimeReferences, context.Source, context.Reason);
        }

        private static void AppendCameraBindingContributions(
            List<ActorCameraBindingContribution> contributions,
            ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.CameraBindingContributions.Count; index++)
            {
                var contribution = scanResult.CameraBindingContributions[index];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability camera binding contribution at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                contributions.Add(contribution);
            }
        }

        private static void AppendAttributeSetupContributions(
            List<ActorAttributeSetupContribution> contributions,
            ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.AttributeSetupContributions.Count; index++)
            {
                var contribution = scanResult.AttributeSetupContributions[index];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability attribute setup contribution at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                contributions.Add(contribution);
            }
        }

        private static void AppendPresentationSetupContributions(
            List<ActorPresentationSetupContribution> contributions,
            ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.PresentationSetupContributions.Count; index++)
            {
                var contribution = scanResult.PresentationSetupContributions[index];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability presentation setup contribution at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                contributions.Add(contribution);
            }
        }

        private static void AppendPermissionReceiverContributions(
            List<ActivityPermissionReceiverContribution> contributions,
            ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.PermissionReceiverContributions.Count; index++)
            {
                var contribution = scanResult.PermissionReceiverContributions[index];
                if (!contribution.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability permission receiver contribution at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                contributions.Add(contribution);
            }
        }

        private static void AppendOwners(List<ActivityCapabilityOwnerDescriptor> owners, ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.Owners.Count; index++)
            {
                var owner = scanResult.Owners[index];
                if (!owner.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability owner at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                owners.Add(owner);
            }
        }

        private static void AppendCapabilities(
            List<ActivityCapabilityDescriptor> capabilities,
            ActivityCapabilityScanResult scanResult,
            ActivityCapabilityInventoryId inventoryId)
        {
            for (int index = 0; index < scanResult.Capabilities.Count; index++)
            {
                var capability = scanResult.Capabilities[index];
                if (!capability.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                capabilities.Add(new ActivityCapabilityDescriptor(
                    capability.CapabilityId,
                    capability.CapabilityKind,
                    capability.ModuleId,
                    capability.OwnerId,
                    capability.ComponentPath,
                    capability.ComponentType,
                    capability.Required,
                    capability.Priority,
                    capability.PolicyMetadata,
                    capability.Source));
            }
        }

        private static void AppendRuntimeReferences(
            Dictionary<string, IActivityCapabilityRuntimeReference> runtimeReferences,
            ActivityCapabilityScanResult scanResult)
        {
            for (int index = 0; index < scanResult.RuntimeReferences.Count; index++)
            {
                var runtimeReference = scanResult.RuntimeReferences[index];
                if (runtimeReference == null || !runtimeReference.IsValid)
                {
                    throw new InvalidOperationException($"Activity capability runtime reference at scanner='{scanResult.ScannerId}' index='{index}' is invalid.");
                }

                string capabilityId = ResolveCapabilityId(scanResult.Capabilities, runtimeReference.CapabilityId);
                if (!runtimeReferences.ContainsKey(capabilityId))
                {
                    runtimeReferences.Add(capabilityId, runtimeReference);
                }
            }
        }

        private static string ResolveCapabilityId(
            IReadOnlyList<ActivityCapabilityDescriptor> capabilities,
            string capabilityId)
        {
            for (int index = 0; index < capabilities.Count; index++)
            {
                if (string.Equals(capabilities[index].CapabilityId, capabilityId, StringComparison.Ordinal))
                {
                    return capabilities[index].CapabilityId;
                }
            }

            throw new InvalidOperationException($"Runtime reference capabilityId='{capabilityId}' has no matching descriptor.");
        }

        private static int CompareOwners(ActivityCapabilityOwnerDescriptor left, ActivityCapabilityOwnerDescriptor right)
        {
            int kindCompare = left.OwnerKind.CompareTo(right.OwnerKind);
            if (kindCompare != 0)
            {
                return kindCompare;
            }

            int idCompare = string.Compare(left.OwnerId, right.OwnerId, StringComparison.Ordinal);
            if (idCompare != 0)
            {
                return idCompare;
            }

            return string.Compare(left.OwnerPath, right.OwnerPath, StringComparison.Ordinal);
        }

        private static int CompareCapabilities(ActivityCapabilityDescriptor left, ActivityCapabilityDescriptor right)
        {
            int ownerCompare = string.Compare(left.OwnerId, right.OwnerId, StringComparison.Ordinal);
            if (ownerCompare != 0)
            {
                return ownerCompare;
            }

            int kindCompare = left.CapabilityKind.CompareTo(right.CapabilityKind);
            if (kindCompare != 0)
            {
                return kindCompare;
            }

            int priorityCompare = left.Priority.CompareTo(right.Priority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            int moduleCompare = string.Compare(left.ModuleId, right.ModuleId, StringComparison.Ordinal);
            if (moduleCompare != 0)
            {
                return moduleCompare;
            }

            int componentTypeCompare = string.Compare(left.ComponentType, right.ComponentType, StringComparison.Ordinal);
            if (componentTypeCompare != 0)
            {
                return componentTypeCompare;
            }

            int componentPathCompare = string.Compare(left.ComponentPath, right.ComponentPath, StringComparison.Ordinal);
            if (componentPathCompare != 0)
            {
                return componentPathCompare;
            }

            return string.Compare(left.CapabilityId, right.CapabilityId, StringComparison.Ordinal);
        }
    }
}
