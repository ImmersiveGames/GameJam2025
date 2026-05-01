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

    public enum ActorCardinalityKind
    {
        Unknown = 0,
        ExactlyOne = 1,
        ZeroOrOne = 2,
        OneOrMore = 3,
        Fixed = 4,
        Range = 5
    }

    public readonly struct ActorCardinalitySpec : IEquatable<ActorCardinalitySpec>
    {
        public ActorCardinalitySpec(ActorCardinalityKind kind, int fixedCount, int minCount, int maxCount)
        {
            Kind = kind;
            FixedCount = fixedCount < 0 ? 0 : fixedCount;
            MinCount = minCount < 0 ? 0 : minCount;
            MaxCount = maxCount < 0 ? 0 : maxCount;
        }

        public ActorCardinalityKind Kind { get; }
        public int FixedCount { get; }
        public int MinCount { get; }
        public int MaxCount { get; }

        public bool IsValid
        {
            get
            {
                switch (Kind)
                {
                    case ActorCardinalityKind.ExactlyOne:
                    case ActorCardinalityKind.ZeroOrOne:
                    case ActorCardinalityKind.OneOrMore:
                        return true;
                    case ActorCardinalityKind.Fixed:
                        return FixedCount > 0;
                    case ActorCardinalityKind.Range:
                        return MinCount >= 0 && MaxCount >= MinCount;
                    default:
                        return false;
                }
            }
        }

        public bool Equals(ActorCardinalitySpec other)
        {
            return Kind == other.Kind &&
                   FixedCount == other.FixedCount &&
                   MinCount == other.MinCount &&
                   MaxCount == other.MaxCount;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorCardinalitySpec other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ FixedCount;
                hashCode = (hashCode * 397) ^ MinCount;
                hashCode = (hashCode * 397) ^ MaxCount;
                return hashCode;
            }
        }
    }

    public readonly struct ActorSetMemberId : IEquatable<ActorSetMemberId>
    {
        public ActorSetMemberId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static ActorSetMemberId None => default;

        public bool Equals(ActorSetMemberId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSetMemberId other && Equals(other);
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
        public ActorSetMemberRef(
            ActorSetMemberId actorSetMemberId,
            string actorSpecId,
            int order,
            bool enabled,
            ActorCardinalitySpec cardinality)
        {
            ActorSetMemberId = actorSetMemberId;
            ActorSpecId = string.IsNullOrWhiteSpace(actorSpecId) ? string.Empty : actorSpecId.Trim();
            Order = order < 0 ? 0 : order;
            Enabled = enabled;
            Cardinality = cardinality;
        }

        public ActorSetMemberId ActorSetMemberId { get; }
        public string ActorSpecId { get; }
        public int Order { get; }
        public bool Enabled { get; }
        public ActorCardinalitySpec Cardinality { get; }
        public bool IsValid => ActorSetMemberId.IsValid && !string.IsNullOrWhiteSpace(ActorSpecId) && Cardinality.IsValid;

        public bool Equals(ActorSetMemberRef other)
        {
            return ActorSetMemberId.Equals(other.ActorSetMemberId) &&
                   string.Equals(ActorSpecId, other.ActorSpecId, StringComparison.Ordinal) &&
                   Order == other.Order &&
                   Enabled == other.Enabled &&
                   Cardinality.Equals(other.Cardinality);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSetMemberRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ActorSetMemberId.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ActorSpecId ?? string.Empty);
                hashCode = (hashCode * 397) ^ Order;
                hashCode = (hashCode * 397) ^ Enabled.GetHashCode();
                hashCode = (hashCode * 397) ^ Cardinality.GetHashCode();
                return hashCode;
            }
        }
    }
}
