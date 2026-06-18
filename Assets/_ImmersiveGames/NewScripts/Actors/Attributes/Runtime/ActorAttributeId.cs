using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    [Serializable]
    public struct ActorAttributeId : IEquatable<ActorAttributeId>
    {
        public static readonly ActorAttributeId Empty = new(string.Empty);

        [field: SerializeField] public string Value { get; private set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public ActorAttributeId(string value)
        {
            Value = value ?? string.Empty;
        }

        public static ActorAttributeId FromDefinition(ActorAttributeDefinitionAsset definition)
        {
            if (definition == null)
            {
                return Empty;
            }

            return new ActorAttributeId(definition.AttributeId);
        }

        public bool Equals(ActorAttributeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorAttributeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(ActorAttributeId left, ActorAttributeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorAttributeId left, ActorAttributeId right)
        {
            return !left.Equals(right);
        }
    }
}
