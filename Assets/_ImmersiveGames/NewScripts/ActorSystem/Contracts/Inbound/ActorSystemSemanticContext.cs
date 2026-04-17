using System;

namespace _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound
{
    /// <summary>
    /// Canonical semantic context consumed by ActorSystem.
    /// </summary>
    public readonly struct ActorSystemSemanticContext : IEquatable<ActorSystemSemanticContext>
    {
        public ActorSystemSemanticContext(
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            string primaryParticipantId,
            string localParticipantId,
            int participantCount)
        {
            SessionSignature = Normalize(sessionSignature);
            PhaseSignature = Normalize(phaseSignature);
            ParticipationSignature = Normalize(participationSignature);
            PrimaryParticipantId = Normalize(primaryParticipantId);
            LocalParticipantId = Normalize(localParticipantId);
            ParticipantCount = participantCount < 0 ? 0 : participantCount;
        }

        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public string ParticipationSignature { get; }
        public string PrimaryParticipantId { get; }
        public string LocalParticipantId { get; }
        public int ParticipantCount { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(SessionSignature) && !string.IsNullOrWhiteSpace(PhaseSignature);
        public bool HasPrimaryParticipant => !string.IsNullOrWhiteSpace(PrimaryParticipantId);
        public bool HasLocalParticipant => !string.IsNullOrWhiteSpace(LocalParticipantId);
        public bool HasParticipants => ParticipantCount > 0;

        public static ActorSystemSemanticContext Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0);

        public bool Equals(ActorSystemSemanticContext other)
        {
            return string.Equals(SessionSignature, other.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(PhaseSignature, other.PhaseSignature, StringComparison.Ordinal) &&
                   string.Equals(ParticipationSignature, other.ParticipationSignature, StringComparison.Ordinal) &&
                   string.Equals(PrimaryParticipantId, other.PrimaryParticipantId, StringComparison.Ordinal) &&
                   string.Equals(LocalParticipantId, other.LocalParticipantId, StringComparison.Ordinal) &&
                   ParticipantCount == other.ParticipantCount;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSystemSemanticContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(SessionSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PhaseSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ParticipationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PrimaryParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(LocalParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ ParticipantCount;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"session='{AsText(SessionSignature)}', phase='{AsText(PhaseSignature)}', participation='{AsText(ParticipationSignature)}', primaryParticipantId='{AsText(PrimaryParticipantId)}', localParticipantId='{AsText(LocalParticipantId)}', participantCount='{ParticipantCount}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }
    }
}