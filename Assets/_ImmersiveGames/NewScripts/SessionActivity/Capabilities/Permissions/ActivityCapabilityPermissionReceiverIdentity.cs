using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionReceiverIdentity : IEquatable<ActivityCapabilityPermissionReceiverIdentity>
    {
        public ActivityCapabilityPermissionReceiverIdentity(
            string pipelineId,
            string sessionStateId,
            string activityId,
            int entrySequence,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            PipelineId = pipelineId.TrimToEmpty();
            SessionStateId = sessionStateId.TrimToEmpty();
            ActivityId = activityId.TrimToEmpty();
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
        }

        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0 &&
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            PlayerActorId.IsValid;

        public bool Equals(ActivityCapabilityPermissionReceiverIdentity other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   EntrySequence == other.EntrySequence &&
                   ActorId == other.ActorId &&
                   ActorInstanceRuntimeId == other.ActorInstanceRuntimeId &&
                   PlayerActorId == other.PlayerActorId &&
                   PlayerSlotId == other.PlayerSlotId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityCapabilityPermissionReceiverIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                PipelineId ?? string.Empty,
                SessionStateId ?? string.Empty,
                ActivityId ?? string.Empty,
                EntrySequence,
                ActorId,
                ActorInstanceRuntimeId,
                PlayerActorId,
                PlayerSlotId);
        }

        public override string ToString()
        {
            return $"pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', entrySequence='{EntrySequence}', actorId='{ActorId}', actorInstanceRuntimeId='{ActorInstanceRuntimeId}', playerActorId='{PlayerActorId}', playerSlotId='{PlayerSlotId}'";
        }
}
}
