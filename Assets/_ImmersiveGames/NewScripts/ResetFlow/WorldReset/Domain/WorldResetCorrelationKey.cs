using System;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain
{
    /// <summary>
    /// Chave tipada para correlação operacional de requests e completions do WorldReset.
    /// </summary>
    public readonly struct WorldResetCorrelationKey : IEquatable<WorldResetCorrelationKey>
    {
        private readonly string _value;

        private WorldResetCorrelationKey(string value)
        {
            _value = value;
        }

        public static WorldResetCorrelationKey None => default;

        public static WorldResetCorrelationKey Required(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("WorldResetCorrelationKey obrigatória ausente.", nameof(value));
            }

            return new WorldResetCorrelationKey(value.Trim());
        }

        public static WorldResetCorrelationKey FromContextSignature(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return None;
            }

            return new WorldResetCorrelationKey(value.Trim());
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public string Value => _value ?? string.Empty;

        public override string ToString()
        {
            return IsValid ? Value : "<none>";
        }

        public bool Equals(WorldResetCorrelationKey other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldResetCorrelationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public static bool operator ==(WorldResetCorrelationKey left, WorldResetCorrelationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(WorldResetCorrelationKey left, WorldResetCorrelationKey right)
        {
            return !left.Equals(right);
        }
    }
}
