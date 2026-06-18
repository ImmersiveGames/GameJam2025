using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityPermissionReceiverContribution
    {
        public ActivityPermissionReceiverContribution(
            SessionActivityIdentity identity,
            string capabilityId,
            string ownerId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionReceiverIdentity receiverIdentity,
            ActivityCapabilityPermissionReceiverId receiverId,
            string componentPath,
            IActivityPermissionReceiverProvider provider,
            string source,
            string reason)
        {
            Identity = identity;
            CapabilityId = capabilityId.TrimToEmpty();
            OwnerId = ownerId.TrimToEmpty();
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            PermissionId = permissionId;
            ReceiverIdentity = receiverIdentity;
            ReceiverId = receiverId;
            ComponentPath = componentPath.TrimToEmpty();
            Provider = provider;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public string CapabilityId { get; }
        public string OwnerId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionReceiverIdentity ReceiverIdentity { get; }
        public ActivityCapabilityPermissionReceiverId ReceiverId { get; }
        public string ComponentPath { get; }
        public IActivityPermissionReceiverProvider Provider { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(CapabilityId) &&
            !string.IsNullOrWhiteSpace(OwnerId) &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid &&
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            ReceiverIdentity.IsValid &&
            ReceiverId.IsValid &&
            Provider != null &&
            !string.IsNullOrWhiteSpace(Source);
}
}
