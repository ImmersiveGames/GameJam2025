using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionBinding
    {
        public ActivityCapabilityPermissionBinding(
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionScope scope,
            ActivityCapabilityPermissionState state,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            PermissionId = permissionId;
            Scope = scope;
            State = state;
            ReceiverId = Normalize(receiverId);
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
        }

        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionScope Scope { get; }
        public ActivityCapabilityPermissionState State { get; }
        public string ReceiverId { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }

        public bool IsValid =>
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            Scope != ActivityCapabilityPermissionScope.Unknown &&
            State != ActivityCapabilityPermissionState.Unknown &&
            !string.IsNullOrWhiteSpace(ReceiverId) &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
