using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public sealed class ActivityCapabilityPermissionReceiverReference : IActivityCapabilityRuntimeReference
    {
        public ActivityCapabilityPermissionReceiverReference(
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            string componentPath,
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionReceiverIdentity identity,
            IActivityCapabilityPermissionReceiver receiver)
        {
            CapabilityId = capabilityId.TrimToEmpty();
            OwnerId = ownerId.TrimToEmpty();
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            ComponentPath = componentPath.TrimToEmpty();
            PermissionId = permissionId;
            Identity = identity;
            Receiver = receiver;
            ReceiverId = receiver?.ReceiverId ?? default;
        }

        public string CapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public string ComponentPath { get; }
        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionReceiverIdentity Identity { get; }
        public ActivityCapabilityPermissionReceiverId ReceiverId { get; }
        public IActivityCapabilityPermissionReceiver Receiver { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid &&
            Identity.IsValid &&
            ReceiverId.IsValid &&
            Receiver != null;
}
}
