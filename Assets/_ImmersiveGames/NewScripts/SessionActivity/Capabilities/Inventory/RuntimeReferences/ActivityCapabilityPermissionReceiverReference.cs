using System;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityCapabilityPermissionReceiverReference : IActivityCapabilityRuntimeReference
    {
        public ActivityCapabilityPermissionReceiverReference(
            string capabilityId,
            string ownerId,
            string targetId,
            string componentPath,
            string permissionId,
            ActivityCapabilityPermissionReceiverIdentity identity,
            IActivityCapabilityPermissionReceiver receiver)
        {
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            TargetId = Normalize(targetId);
            ComponentPath = Normalize(componentPath);
            PermissionId = Normalize(permissionId);
            Identity = identity;
            Receiver = receiver;
            ReceiverId = receiver?.ReceiverId ?? string.Empty;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public string ComponentPath { get; }
        public string PermissionId { get; }
        public ActivityCapabilityPermissionReceiverIdentity Identity { get; }
        public string ReceiverId { get; }
        public IActivityCapabilityPermissionReceiver Receiver { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(PermissionId) &&
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ReceiverId) &&
            Receiver != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
