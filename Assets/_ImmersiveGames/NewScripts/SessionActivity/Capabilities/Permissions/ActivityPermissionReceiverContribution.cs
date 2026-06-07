using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

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
            string receiverId,
            string componentPath,
            IActivityPermissionReceiverProvider provider,
            string source,
            string reason)
        {
            Identity = identity;
            CapabilityId = Normalize(capabilityId);
            OwnerId = Normalize(ownerId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            PermissionId = permissionId;
            ReceiverIdentity = receiverIdentity;
            ReceiverId = Normalize(receiverId);
            ComponentPath = Normalize(componentPath);
            Provider = provider;
            Source = Normalize(source);
            Reason = Normalize(reason);
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
        public string ReceiverId { get; }
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
            !string.IsNullOrWhiteSpace(ReceiverId) &&
            Provider != null &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
