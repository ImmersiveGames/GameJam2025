using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.PlayerParticipation.Contracts
{
    public readonly struct PlayerSlotId : IEquatable<PlayerSlotId>
    {
        public PlayerSlotId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerSlotId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is PlayerSlotId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PlayerSlotId left, PlayerSlotId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PlayerSlotId left, PlayerSlotId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct PlayerSelectionId : IEquatable<PlayerSelectionId>
    {
        public PlayerSelectionId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerSelectionId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is PlayerSelectionId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PlayerSelectionId left, PlayerSelectionId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PlayerSelectionId left, PlayerSelectionId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct SessionParticipantId : IEquatable<SessionParticipantId>
    {
        public SessionParticipantId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SessionParticipantId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is SessionParticipantId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(SessionParticipantId left, SessionParticipantId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SessionParticipantId left, SessionParticipantId right)
        {
            return !left.Equals(right);
        }

        public static SessionParticipantId FromPlayerSlotId(PlayerSlotId playerSlotId)
        {
            if (!playerSlotId.IsValid)
            {
                return default;
            }

            return new SessionParticipantId($"participant.{playerSlotId}");
        }
    }

    public readonly struct PlayerActorId : IEquatable<PlayerActorId>
    {
        public PlayerActorId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PlayerActorId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is PlayerActorId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(PlayerActorId left, PlayerActorId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PlayerActorId left, PlayerActorId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct ActivityParticipantRequirementId : IEquatable<ActivityParticipantRequirementId>
    {
        public ActivityParticipantRequirementId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActivityParticipantRequirementId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActivityParticipantRequirementId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActivityParticipantRequirementId left, ActivityParticipantRequirementId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActivityParticipantRequirementId left, ActivityParticipantRequirementId right)
        {
            return !left.Equals(right);
        }
    }
}
