using System;

namespace _ImmersiveGames.NewScripts.PlayerParticipation.Contracts
{
    public readonly struct PlayerSlotId : IEquatable<PlayerSlotId>
    {
        public PlayerSlotId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerSlotId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PlayerSlotId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(PlayerSlotId left, PlayerSlotId right) => left.Equals(right);
        public static bool operator !=(PlayerSlotId left, PlayerSlotId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct PlayerSelectionId : IEquatable<PlayerSelectionId>
    {
        public PlayerSelectionId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerSelectionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PlayerSelectionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(PlayerSelectionId left, PlayerSelectionId right) => left.Equals(right);
        public static bool operator !=(PlayerSelectionId left, PlayerSelectionId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SessionParticipantId : IEquatable<SessionParticipantId>
    {
        public SessionParticipantId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SessionParticipantId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SessionParticipantId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(SessionParticipantId left, SessionParticipantId right) => left.Equals(right);
        public static bool operator !=(SessionParticipantId left, SessionParticipantId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActivityParticipantRequirementId : IEquatable<ActivityParticipantRequirementId>
    {
        public ActivityParticipantRequirementId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActivityParticipantRequirementId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActivityParticipantRequirementId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActivityParticipantRequirementId left, ActivityParticipantRequirementId right) => left.Equals(right);
        public static bool operator !=(ActivityParticipantRequirementId left, ActivityParticipantRequirementId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
