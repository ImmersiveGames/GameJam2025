using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityCameraTargetReference : IActivityCapabilityRuntimeReference
    {
        public ActivityCameraTargetReference(
            string capabilityId,
            string ownerId,
            string playerActorId,
            string playerSlotId,
            string componentPath,
            Transform trackingTarget,
            Transform lookAtTarget)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            PlayerActorId = Normalize(playerActorId);
            PlayerSlotId = Normalize(playerSlotId);
            ComponentPath = Normalize(componentPath);
            TrackingTarget = trackingTarget;
            LookAtTarget = lookAtTarget;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string PlayerActorId { get; }
        public string PlayerSlotId { get; }
        public string ComponentPath { get; }
        public Transform TrackingTarget { get; }
        public Transform LookAtTarget { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(CapabilityId) && TrackingTarget != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
