using System;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorsBindingConflictCode
    {
        Unknown = 0,
        DuplicateParticipant = 1,
        DuplicateAxisActorId = 2,
        DuplicateRuntimeActorId = 3,
        MissingMapping = 4,
        InvalidBindingState = 5
    }

    public readonly struct ActorsBindingConflict : IEquatable<ActorsBindingConflict>
    {
        public ActorsBindingConflict(
            ActorsBindingConflictCode code,
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            string source,
            string reason)
        {
            Code = code;
            ParticipantId = Normalize(participantId);
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorsBindingConflictCode Code { get; }
        public string ParticipantId { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => Code != ActorsBindingConflictCode.Unknown;

        public bool Equals(ActorsBindingConflict other)
        {
            return Code == other.Code &&
                   string.Equals(ParticipantId, other.ParticipantId, StringComparison.Ordinal) &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsBindingConflict other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Code;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
