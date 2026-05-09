using System;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct SessionActivityEntryHandoff : IEquatable<SessionActivityEntryHandoff>
    {
        public SessionActivityEntryHandoff(
            string activityId,
            int activityOrdinal,
            int entrySequence,
            string sessionStateId,
            string source,
            string reason)
        {
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            SessionStateId = Normalize(sessionStateId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string SessionStateId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasResolvedActivity =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0;

        public bool IsEntryOnly =>
            string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal == 0;

        public bool IsValid =>
            EntrySequence >= 0 &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(Source) &&
            (HasResolvedActivity || IsEntryOnly);

        public bool Equals(SessionActivityEntryHandoff other)
        {
            return string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   ActivityOrdinal == other.ActivityOrdinal &&
                   EntrySequence == other.EntrySequence &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityEntryHandoff other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActivityId ?? string.Empty);
                hashCode = (hashCode * 397) ^ ActivityOrdinal;
                hashCode = (hashCode * 397) ^ EntrySequence;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SessionStateId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? HasResolvedActivity
                    ? $"activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', sessionStateId='{SessionStateId}'"
                    : EntrySequence > 0
                        ? $"activityId='<first-catalog>', activityOrdinal='0', entrySequence='{EntrySequence}', sessionStateId='{SessionStateId}'"
                        : $"activityId='<first-catalog>', activityOrdinal='0', entrySequence='<pipeline-allocated>', sessionStateId='{SessionStateId}'"
                : "<none>";
        }

        public static bool operator ==(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => left.Equals(right);
        public static bool operator !=(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

