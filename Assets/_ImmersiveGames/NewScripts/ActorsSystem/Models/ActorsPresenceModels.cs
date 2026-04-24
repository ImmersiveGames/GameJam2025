using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorPresenceStatus
    {
        ExpectedNotMaterialized = 0,
        ExpectedMaterialized = 1,
        UnexpectedMaterialized = 2,
        Inconsistent = 3
    }

    public readonly struct ActorsPresenceRecord : IEquatable<ActorsPresenceRecord>
    {
        public ActorsPresenceRecord(
            AxisActorId axisActorId,
            ActorRole role,
            ActorRelevance relevance,
            bool isExpected,
            bool isMaterialized,
            bool isInconsistent,
            RuntimeActorId runtimeActorId,
            string semanticParticipantId,
            ActorPresenceStatus status,
            string reason)
        {
            AxisActorId = axisActorId;
            Role = role;
            Relevance = relevance;
            IsExpected = isExpected;
            IsMaterialized = isMaterialized;
            IsInconsistent = isInconsistent;
            RuntimeActorId = runtimeActorId;
            SemanticParticipantId = Normalize(semanticParticipantId);
            Status = status;
            Reason = Normalize(reason);
        }

        public AxisActorId AxisActorId { get; }
        public ActorRole Role { get; }
        public ActorRelevance Relevance { get; }
        public bool IsExpected { get; }
        public bool IsMaterialized { get; }
        public bool IsInconsistent { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string SemanticParticipantId { get; }
        public ActorPresenceStatus Status { get; }
        public string Reason { get; }

        public bool IsValid => AxisActorId.IsValid && Role != ActorRole.Unknown;

        public bool Equals(ActorsPresenceRecord other)
        {
            return AxisActorId.Equals(other.AxisActorId) &&
                   Role == other.Role &&
                   Relevance == other.Relevance &&
                   IsExpected == other.IsExpected &&
                   IsMaterialized == other.IsMaterialized &&
                   IsInconsistent == other.IsInconsistent &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   Status == other.Status &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsPresenceRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)Relevance;
                hashCode = (hashCode * 397) ^ IsExpected.GetHashCode();
                hashCode = (hashCode * 397) ^ IsMaterialized.GetHashCode();
                hashCode = (hashCode * 397) ^ IsInconsistent.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"axisActorId='{AxisActorId}', role='{Role}', relevance='{Relevance}', expected='{IsExpected}', materialized='{IsMaterialized}', inconsistent='{IsInconsistent}', runtimeActorId='{RuntimeActorId}', semanticParticipantId='{AsText(SemanticParticipantId)}', status='{Status}', reason='{AsText(Reason)}'";
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

    public readonly struct ActorsPresenceSnapshot : IEquatable<ActorsPresenceSnapshot>
    {
        public ActorsPresenceSnapshot(
            string ensembleSignature,
            string runtimeObservationSignature,
            string presenceSignature,
            ActorsPresenceRecord[] entries,
            int expectedCount,
            int materializedCount,
            int absentCount,
            int inconsistentCount,
            int runtimeOrphanCount,
            string reason)
        {
            EnsembleSignature = Normalize(ensembleSignature);
            RuntimeObservationSignature = Normalize(runtimeObservationSignature);
            PresenceSignature = Normalize(presenceSignature);
            Entries = entries == null ? Array.Empty<ActorsPresenceRecord>() : (ActorsPresenceRecord[])entries.Clone();
            ExpectedCount = Clamp(expectedCount);
            MaterializedCount = Clamp(materializedCount);
            AbsentCount = Clamp(absentCount);
            InconsistentCount = Clamp(inconsistentCount);
            RuntimeOrphanCount = Clamp(runtimeOrphanCount);
            Reason = Normalize(reason);
        }

        public string EnsembleSignature { get; }
        public string RuntimeObservationSignature { get; }
        public string PresenceSignature { get; }
        public ActorsPresenceRecord[] Entries { get; }
        public int ExpectedCount { get; }
        public int MaterializedCount { get; }
        public int AbsentCount { get; }
        public int InconsistentCount { get; }
        public int RuntimeOrphanCount { get; }
        public string Reason { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(PresenceSignature);

        public static ActorsPresenceSnapshot Empty => new(
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<ActorsPresenceRecord>(),
            0,
            0,
            0,
            0,
            0,
            string.Empty);

        public bool Equals(ActorsPresenceSnapshot other)
        {
            return string.Equals(EnsembleSignature, other.EnsembleSignature, StringComparison.Ordinal) &&
                   string.Equals(RuntimeObservationSignature, other.RuntimeObservationSignature, StringComparison.Ordinal) &&
                   string.Equals(PresenceSignature, other.PresenceSignature, StringComparison.Ordinal) &&
                   ExpectedCount == other.ExpectedCount &&
                   MaterializedCount == other.MaterializedCount &&
                   AbsentCount == other.AbsentCount &&
                   InconsistentCount == other.InconsistentCount &&
                   RuntimeOrphanCount == other.RuntimeOrphanCount &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsPresenceSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(EnsembleSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RuntimeObservationSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PresenceSignature ?? string.Empty);
                hashCode = (hashCode * 397) ^ ExpectedCount;
                hashCode = (hashCode * 397) ^ MaterializedCount;
                hashCode = (hashCode * 397) ^ AbsentCount;
                hashCode = (hashCode * 397) ^ InconsistentCount;
                hashCode = (hashCode * 397) ^ RuntimeOrphanCount;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"presenceSignature='{AsText(PresenceSignature)}', count='{Count}', expected='{ExpectedCount}', materialized='{MaterializedCount}', absent='{AbsentCount}', inconsistent='{InconsistentCount}', runtimeOrphan='{RuntimeOrphanCount}', reason='{AsText(Reason)}'";
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value;
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

    public readonly struct ActorsRegistryEntry : IEquatable<ActorsRegistryEntry>
    {
        public ActorsRegistryEntry(
            AxisActorId axisActorId,
            ActorRole role,
            ActorRelevance relevance,
            bool isExpected,
            bool isMaterialized,
            bool isInconsistent,
            RuntimeActorId runtimeActorId,
            string semanticParticipantId,
            ActorPresenceStatus status,
            string reason)
        {
            AxisActorId = axisActorId;
            Role = role;
            Relevance = relevance;
            IsExpected = isExpected;
            IsMaterialized = isMaterialized;
            IsInconsistent = isInconsistent;
            RuntimeActorId = runtimeActorId;
            SemanticParticipantId = Normalize(semanticParticipantId);
            Status = status;
            Reason = Normalize(reason);
        }

        public AxisActorId AxisActorId { get; }
        public ActorRole Role { get; }
        public ActorRelevance Relevance { get; }
        public bool IsExpected { get; }
        public bool IsMaterialized { get; }
        public bool IsInconsistent { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public string SemanticParticipantId { get; }
        public ActorPresenceStatus Status { get; }
        public string Reason { get; }

        public bool IsValid => AxisActorId.IsValid && Role != ActorRole.Unknown;

        public bool Equals(ActorsRegistryEntry other)
        {
            return AxisActorId.Equals(other.AxisActorId) &&
                   Role == other.Role &&
                   Relevance == other.Relevance &&
                   IsExpected == other.IsExpected &&
                   IsMaterialized == other.IsMaterialized &&
                   IsInconsistent == other.IsInconsistent &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   Status == other.Status &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsRegistryEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Role;
                hashCode = (hashCode * 397) ^ (int)Relevance;
                hashCode = (hashCode * 397) ^ IsExpected.GetHashCode();
                hashCode = (hashCode * 397) ^ IsMaterialized.GetHashCode();
                hashCode = (hashCode * 397) ^ IsInconsistent.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"axisActorId='{AxisActorId}', role='{Role}', relevance='{Relevance}', expected='{IsExpected}', materialized='{IsMaterialized}', inconsistent='{IsInconsistent}', runtimeActorId='{RuntimeActorId}', semanticParticipantId='{AsText(SemanticParticipantId)}', status='{Status}', reason='{AsText(Reason)}'";
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
