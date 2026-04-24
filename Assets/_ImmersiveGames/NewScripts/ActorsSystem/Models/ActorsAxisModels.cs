using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public enum ActorRole
    {
        Unknown = 0,
        Player = 1,
        Actor = 2,
        Spectator = 3,
        System = 4
    }

    public enum ActorRelevance
    {
        Unknown = 0,
        Optional = 1,
        Observed = 2,
        Required = 3,
        Local = 4,
        Primary = 5
    }

    public readonly struct AxisActorId : IEquatable<AxisActorId>
    {
        public AxisActorId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static AxisActorId None => default;

        public static AxisActorId FromParticipantId(string participantId)
        {
            string normalized = Normalize(participantId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return None;
            }

            return new AxisActorId($"axis:participant:{normalized}");
        }

        public static AxisActorId FromActorSpecId(string actorSpecId)
        {
            string normalized = Normalize(actorSpecId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return None;
            }

            return new AxisActorId($"axis:actorspec:{normalized}");
        }

        public bool Equals(AxisActorId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AxisActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        public static bool operator ==(AxisActorId left, AxisActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AxisActorId left, AxisActorId right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct RuntimeActorId : IEquatable<RuntimeActorId>
    {
        public RuntimeActorId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static RuntimeActorId None => default;

        public bool Equals(RuntimeActorId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        public static bool operator ==(RuntimeActorId left, RuntimeActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeActorId left, RuntimeActorId right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
