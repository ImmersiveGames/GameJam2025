using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityInventoryBuildResult
    {
        public ActivityCapabilityInventoryBuildResult(
            ActivityCapabilityInventory inventory,
            IReadOnlyList<ActorCameraBindingContribution> cameraBindingContributions,
            IReadOnlyList<ActorAttributeSetupContribution> attributeSetupContributions,
            IReadOnlyList<ActorPresentationSetupContribution> presentationSetupContributions,
            IReadOnlyList<ActivityPermissionReceiverContribution> permissionReceiverContributions,
            int activityObjectLifecycleCapabilityCount,
            string activityObjectLifecycleCapabilityKindsSummary,
            int actorLifecycleCapabilityCount,
            string actorLifecycleCapabilityKindsSummary,
            int objectTargetCount,
            int unresolvedReportCount,
            string source,
            string reason)
        {
            Inventory = inventory;
            CameraBindingContributions = cameraBindingContributions ?? Array.Empty<ActorCameraBindingContribution>();
            AttributeSetupContributions = attributeSetupContributions ?? Array.Empty<ActorAttributeSetupContribution>();
            PresentationSetupContributions = presentationSetupContributions ?? Array.Empty<ActorPresentationSetupContribution>();
            PermissionReceiverContributions = permissionReceiverContributions ?? Array.Empty<ActivityPermissionReceiverContribution>();
            ActivityObjectLifecycleCapabilityCount = activityObjectLifecycleCapabilityCount < 0 ? 0 : activityObjectLifecycleCapabilityCount;
            ActivityObjectLifecycleCapabilityKindsSummary = Normalize(activityObjectLifecycleCapabilityKindsSummary);
            ActorLifecycleCapabilityCount = actorLifecycleCapabilityCount < 0 ? 0 : actorLifecycleCapabilityCount;
            ActorLifecycleCapabilityKindsSummary = Normalize(actorLifecycleCapabilityKindsSummary);
            ObjectTargetCount = objectTargetCount < 0 ? 0 : objectTargetCount;
            UnresolvedReportCount = unresolvedReportCount < 0 ? 0 : unresolvedReportCount;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityCapabilityInventory Inventory { get; }
        public IReadOnlyList<ActorCameraBindingContribution> CameraBindingContributions { get; }
        public IReadOnlyList<ActorAttributeSetupContribution> AttributeSetupContributions { get; }
        public IReadOnlyList<ActorPresentationSetupContribution> PresentationSetupContributions { get; }
        public IReadOnlyList<ActivityPermissionReceiverContribution> PermissionReceiverContributions { get; }
        public int ActivityObjectLifecycleCapabilityCount { get; }
        public string ActivityObjectLifecycleCapabilityKindsSummary { get; }
        public int ActorLifecycleCapabilityCount { get; }
        public string ActorLifecycleCapabilityKindsSummary { get; }
        public int ObjectTargetCount { get; }
        public int UnresolvedReportCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Inventory.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
