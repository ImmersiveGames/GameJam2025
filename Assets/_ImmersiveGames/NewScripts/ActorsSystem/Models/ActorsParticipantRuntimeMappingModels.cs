using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorsRuntimeReplacementCause
    {
        None = 0,
        Materialized = 1,
        Rematerialized = 2,
        PreserveExisting = 3,
        ReplacedRuntime = 4
    }

    public readonly struct ActorsParticipantRuntimeMappingEntry : IEquatable<ActorsParticipantRuntimeMappingEntry>
    {
        public ActorsParticipantRuntimeMappingEntry(
            string participantId,
            AxisActorId axisActorId,
            RuntimeActorId runtimeActorId,
            ActorsRuntimeReplacementCause replacementCause,
            string source,
            string reason)
        {
            ParticipantId = Normalize(participantId);
            AxisActorId = axisActorId;
            RuntimeActorId = runtimeActorId;
            ReplacementCause = replacementCause;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ParticipantId { get; }
        public AxisActorId AxisActorId { get; }
        public RuntimeActorId RuntimeActorId { get; }
        public ActorsRuntimeReplacementCause ReplacementCause { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ParticipantId) &&
            AxisActorId.IsValid &&
            RuntimeActorId.IsValid;

        public bool Equals(ActorsParticipantRuntimeMappingEntry other)
        {
            return string.Equals(ParticipantId, other.ParticipantId, StringComparison.Ordinal) &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   RuntimeActorId.Equals(other.RuntimeActorId) &&
                   ReplacementCause == other.ReplacementCause &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsParticipantRuntimeMappingEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ReplacementCause;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"participantId='{AsText(ParticipantId)}', axisActorId='{AxisActorId}', runtimeActorId='{RuntimeActorId}', replacementCause='{ReplacementCause}', source='{AsText(Source)}', reason='{AsText(Reason)}'";
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

    public readonly struct ActorsParticipantRuntimeMappingSnapshot : IEquatable<ActorsParticipantRuntimeMappingSnapshot>
    {
        public ActorsParticipantRuntimeMappingSnapshot(
            string signature,
            ActorsParticipantRuntimeMappingEntry[] entries,
            string reason)
        {
            Signature = Normalize(signature);
            Entries = entries == null ? Array.Empty<ActorsParticipantRuntimeMappingEntry>() : (ActorsParticipantRuntimeMappingEntry[])entries.Clone();
            Reason = Normalize(reason);
        }

        public string Signature { get; }
        public ActorsParticipantRuntimeMappingEntry[] Entries { get; }
        public string Reason { get; }

        public int Count => Entries?.Length ?? 0;
        public bool HasEntries => Count > 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public static ActorsParticipantRuntimeMappingSnapshot Empty => new(
            string.Empty,
            Array.Empty<ActorsParticipantRuntimeMappingEntry>(),
            string.Empty);

        public bool Equals(ActorsParticipantRuntimeMappingSnapshot other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsParticipantRuntimeMappingSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Signature ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ Count;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"signature='{AsText(Signature)}', count='{Count}', reason='{AsText(Reason)}'";
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
