using System;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public struct ActorAttributeThresholdId : IEquatable<ActorAttributeThresholdId>
    {
        public static readonly ActorAttributeThresholdId Empty = new ActorAttributeThresholdId(string.Empty);

        [field: SerializeField] public string Value { get; private set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public ActorAttributeThresholdId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public bool Equals(ActorAttributeThresholdId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorAttributeThresholdId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(ActorAttributeThresholdId left, ActorAttributeThresholdId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorAttributeThresholdId left, ActorAttributeThresholdId right)
        {
            return !left.Equals(right);
        }
}
}
