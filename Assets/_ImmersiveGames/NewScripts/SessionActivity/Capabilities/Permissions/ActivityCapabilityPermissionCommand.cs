using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionCommand
    {
        public ActivityCapabilityPermissionCommand(
            ActivityCapabilityPermissionId permissionId,
            ActivityCapabilityPermissionScope scope,
            ActivityCapabilityPermissionState state,
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            PermissionId = permissionId;
            Scope = scope;
            State = state;
            PipelineId = pipelineId.TrimToEmpty();
            SessionStateId = sessionStateId.TrimToEmpty();
            ActivityId = activityId.TrimToEmpty();
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActivityCapabilityPermissionId PermissionId { get; }
        public ActivityCapabilityPermissionScope Scope { get; }
        public ActivityCapabilityPermissionState State { get; }
        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasIdentity =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0;

        public bool IsValid =>
            PermissionId != ActivityCapabilityPermissionId.Unknown &&
            Scope != ActivityCapabilityPermissionScope.Unknown &&
            State != ActivityCapabilityPermissionState.Unknown &&
            HasIdentity &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }
}
