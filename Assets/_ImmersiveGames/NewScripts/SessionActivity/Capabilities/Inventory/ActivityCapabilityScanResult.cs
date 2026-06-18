using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityScanResult
    {
        public ActivityCapabilityScanResult(
            string scannerId,
            IReadOnlyList<ActivityCapabilityOwnerDescriptor> owners,
            IReadOnlyList<ActivityCapabilityDescriptor> capabilities,
            IReadOnlyList<IActivityCapabilityRuntimeReference> runtimeReferences,
            IReadOnlyList<ActorCameraBindingContribution> cameraBindingContributions,
            IReadOnlyList<ActorAttributeSetupContribution> attributeSetupContributions,
            IReadOnlyList<ActorPresentationSetupContribution> presentationSetupContributions,
            IReadOnlyList<ActivityPermissionReceiverContribution> permissionReceiverContributions,
            string source,
            string reason)
        {
            ScannerId = scannerId.TrimToEmpty();
            Owners = owners ?? Array.Empty<ActivityCapabilityOwnerDescriptor>();
            Capabilities = capabilities ?? Array.Empty<ActivityCapabilityDescriptor>();
            RuntimeReferences = runtimeReferences ?? Array.Empty<IActivityCapabilityRuntimeReference>();
            CameraBindingContributions = cameraBindingContributions ?? Array.Empty<ActorCameraBindingContribution>();
            AttributeSetupContributions = attributeSetupContributions ?? Array.Empty<ActorAttributeSetupContribution>();
            PresentationSetupContributions = presentationSetupContributions ?? Array.Empty<ActorPresentationSetupContribution>();
            PermissionReceiverContributions = permissionReceiverContributions ?? Array.Empty<ActivityPermissionReceiverContribution>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string ScannerId { get; }
        public IReadOnlyList<ActivityCapabilityOwnerDescriptor> Owners { get; }
        public IReadOnlyList<ActivityCapabilityDescriptor> Capabilities { get; }
        public IReadOnlyList<IActivityCapabilityRuntimeReference> RuntimeReferences { get; }
        public IReadOnlyList<ActorCameraBindingContribution> CameraBindingContributions { get; }
        public IReadOnlyList<ActorAttributeSetupContribution> AttributeSetupContributions { get; }
        public IReadOnlyList<ActorPresentationSetupContribution> PresentationSetupContributions { get; }
        public IReadOnlyList<ActivityPermissionReceiverContribution> PermissionReceiverContributions { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ScannerId);

        public static ActivityCapabilityScanResult Empty(string scannerId, string source, string reason)
        {
            return new ActivityCapabilityScanResult(
                scannerId,
                Array.Empty<ActivityCapabilityOwnerDescriptor>(),
                Array.Empty<ActivityCapabilityDescriptor>(),
                Array.Empty<IActivityCapabilityRuntimeReference>(),
                Array.Empty<ActorCameraBindingContribution>(),
                Array.Empty<ActorAttributeSetupContribution>(),
                Array.Empty<ActorPresentationSetupContribution>(),
                Array.Empty<ActivityPermissionReceiverContribution>(),
                source,
                reason);
        }
    }
}
