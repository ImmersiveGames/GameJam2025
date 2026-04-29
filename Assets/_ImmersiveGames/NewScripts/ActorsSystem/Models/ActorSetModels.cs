using System;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Models
{
    public readonly struct ActorSetRef : IEquatable<ActorSetRef>
    {
        public ActorSetRef(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static ActorSetRef None => default;

        public bool Equals(ActorSetRef other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSetRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorSetMemberRef : IEquatable<ActorSetMemberRef>
    {
        public ActorSetMemberRef(string actorSpecId, int order, bool enabled)
        {
            ActorSpecId = string.IsNullOrWhiteSpace(actorSpecId) ? string.Empty : actorSpecId.Trim();
            Order = order < 0 ? 0 : order;
            Enabled = enabled;
        }

        public string ActorSpecId { get; }
        public int Order { get; }
        public bool Enabled { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(ActorSpecId);

        public bool Equals(ActorSetMemberRef other)
        {
            return string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   Order == other.Order &&
                   Enabled == other.Enabled;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSetMemberRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ Order;
                hashCode = (hashCode * 397) ^ Enabled.GetHashCode();
                return hashCode;
            }
        }
    }
}
