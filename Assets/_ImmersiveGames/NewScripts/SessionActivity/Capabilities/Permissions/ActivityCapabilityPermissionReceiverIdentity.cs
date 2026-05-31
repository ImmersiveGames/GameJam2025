using System;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

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
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            PipelineId = Normalize(pipelineId);
            SessionStateId = Normalize(sessionStateId);
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
        }

        public string PipelineId { get; }
        public string SessionStateId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            EntrySequence > 0 &&
            PlayerActorId.IsValid;

        public bool Equals(ActivityCapabilityPermissionReceiverIdentity other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   EntrySequence == other.EntrySequence &&
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
                PlayerActorId,
                PlayerSlotId);
        }

        public override string ToString()
        {
            return $"pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', entrySequence='{EntrySequence}', playerActorId='{PlayerActorId}', playerSlotId='{PlayerSlotId}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
