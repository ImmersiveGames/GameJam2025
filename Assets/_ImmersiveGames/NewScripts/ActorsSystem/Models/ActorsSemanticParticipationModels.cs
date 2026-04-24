using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct ActorsSemanticParticipantRecord : IEquatable<ActorsSemanticParticipantRecord>
    {
        public ActorsSemanticParticipantRecord(
            string participantId,
            ActorRole expectedRole,
            bool isPrimary,
            bool isLocal)
        {
            ParticipantId = Normalize(participantId);
            ExpectedRole = expectedRole;
            IsPrimary = isPrimary;
            IsLocal = isLocal;
        }

        public string ParticipantId { get; }
        public ActorRole ExpectedRole { get; }
        public bool IsPrimary { get; }
        public bool IsLocal { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ParticipantId);

        public bool Equals(ActorsSemanticParticipantRecord other)
        {
            return string.Equals(ParticipantId, other.ParticipantId, StringComparison.Ordinal) &&
                   ExpectedRole == other.ExpectedRole &&
                   IsPrimary == other.IsPrimary &&
                   IsLocal == other.IsLocal;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsSemanticParticipantRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)ExpectedRole;
                hashCode = (hashCode * 397) ^ IsPrimary.GetHashCode();
                hashCode = (hashCode * 397) ^ IsLocal.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"participantId='{AsText(ParticipantId)}', expectedRole='{ExpectedRole}', primary='{IsPrimary}', local='{IsLocal}'";
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

    public readonly struct ActorsSemanticParticipationSnapshot : IEquatable<ActorsSemanticParticipationSnapshot>
    {
        public ActorsSemanticParticipationSnapshot(
            string sessionSignature,
            string phaseSignature,
            string participationSignature,
            ActorsSemanticParticipantRecord[] participants)
        {
            SessionSignature = Normalize(sessionSignature);
            PhaseSignature = Normalize(phaseSignature);
            ParticipationSignature = Normalize(participationSignature);
            Participants = participants == null ? Array.Empty<ActorsSemanticParticipantRecord>() : (ActorsSemanticParticipantRecord[])participants.Clone();
        }

        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public string ParticipationSignature { get; }
        public ActorsSemanticParticipantRecord[] Participants { get; }

        public int Count => Participants?.Length ?? 0;
        public bool HasParticipants => Count > 0;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            !string.IsNullOrWhiteSpace(ParticipationSignature);

        public static ActorsSemanticParticipationSnapshot Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<ActorsSemanticParticipantRecord>());

        public bool Equals(ActorsSemanticParticipationSnapshot other)
        {
            return string.Equals(SessionSignature, other.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(PhaseSignature, other.PhaseSignature, StringComparison.Ordinal) &&
                   string.Equals(ParticipationSignature, other.ParticipationSignature, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsSemanticParticipationSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(SessionSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PhaseSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ParticipationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"sessionSignature='{AsText(SessionSignature)}', phaseSignature='{AsText(PhaseSignature)}', participationSignature='{AsText(ParticipationSignature)}', count='{Count}'";
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
