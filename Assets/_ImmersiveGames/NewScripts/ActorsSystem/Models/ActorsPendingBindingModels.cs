using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct ActorsPendingBindingEntry : IEquatable<ActorsPendingBindingEntry>
    {
        public ActorsPendingBindingEntry(
            RuntimeActorId runtimeActorId,
            ActorsUnityOperationalHandles unityHandles,
            string semanticParticipantId,
            AxisActorId axisActorId,
            string actorSetRef,
            string scopeKey,
            string source,
            string reason,
            long firstSeenUtcTicks,
            long lastSeenUtcTicks,
            int attempts)
        {
            RuntimeActorId = runtimeActorId;
            UnityHandles = unityHandles;
            SemanticParticipantId = Normalize(semanticParticipantId);
            AxisActorId = axisActorId;
            ActorSetRef = Normalize(actorSetRef);
            ScopeKey = Normalize(scopeKey);
            Source = Normalize(source);
            Reason = Normalize(reason);
            FirstSeenUtcTicks = firstSeenUtcTicks <= 0 ? DateTime.UtcNow.Ticks : firstSeenUtcTicks;
            LastSeenUtcTicks = lastSeenUtcTicks <= 0 ? FirstSeenUtcTicks : lastSeenUtcTicks;
            Attempts = attempts < 0 ? 0 : attempts;
        }

        public RuntimeActorId RuntimeActorId { get; }
        public ActorsUnityOperationalHandles UnityHandles { get; }
        public string SemanticParticipantId { get; }
        public AxisActorId AxisActorId { get; }
        public string ActorSetRef { get; }
        public string ScopeKey { get; }
        public string Source { get; }
        public string Reason { get; }
        public long FirstSeenUtcTicks { get; }
        public long LastSeenUtcTicks { get; }
        public int Attempts { get; }

        public bool IsValid => RuntimeActorId.IsValid;

        public ActorsPendingBindingEntry WithAttempt(string source, string reason, bool incrementAttempts)
        {
            return new ActorsPendingBindingEntry(
                RuntimeActorId,
                UnityHandles,
                SemanticParticipantId,
                AxisActorId,
                ActorSetRef,
                ScopeKey,
                source,
                reason,
                FirstSeenUtcTicks,
                DateTime.UtcNow.Ticks,
                incrementAttempts ? Attempts + 1 : Attempts);
        }

        public bool Equals(ActorsPendingBindingEntry other)
        {
            return RuntimeActorId.Equals(other.RuntimeActorId) &&
                   UnityHandles.Equals(other.UnityHandles) &&
                   string.Equals(SemanticParticipantId, other.SemanticParticipantId, StringComparison.Ordinal) &&
                   AxisActorId.Equals(other.AxisActorId) &&
                   string.Equals(ActorSetRef, other.ActorSetRef, StringComparison.Ordinal) &&
                   string.Equals(ScopeKey, other.ScopeKey, StringComparison.Ordinal) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   FirstSeenUtcTicks == other.FirstSeenUtcTicks &&
                   LastSeenUtcTicks == other.LastSeenUtcTicks &&
                   Attempts == other.Attempts;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsPendingBindingEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RuntimeActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ UnityHandles.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SemanticParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ AxisActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSetRef ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ScopeKey ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ FirstSeenUtcTicks.GetHashCode();
                hashCode = (hashCode * 397) ^ LastSeenUtcTicks.GetHashCode();
                hashCode = (hashCode * 397) ^ Attempts;
                return hashCode;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorsPendingBindingSnapshot : IEquatable<ActorsPendingBindingSnapshot>
    {
        public ActorsPendingBindingSnapshot(string signature, ActorsPendingBindingEntry[] entries, string reason)
        {
            Signature = Normalize(signature);
            Entries = entries == null ? Array.Empty<ActorsPendingBindingEntry>() : (ActorsPendingBindingEntry[])entries.Clone();
            Reason = Normalize(reason);
        }

        public string Signature { get; }
        public ActorsPendingBindingEntry[] Entries { get; }
        public string Reason { get; }
        public int Count => Entries?.Length ?? 0;
        public bool IsValid => !string.IsNullOrWhiteSpace(Signature);

        public static ActorsPendingBindingSnapshot Empty => new(string.Empty, Array.Empty<ActorsPendingBindingEntry>(), string.Empty);

        public bool Equals(ActorsPendingBindingSnapshot other)
        {
            return string.Equals(Signature, other.Signature, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorsPendingBindingSnapshot other && Equals(other);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
