using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    /// <summary>
    /// Identificador simples e imutável de fase.
    /// </summary>
    public readonly struct PhaseId : IEquatable<PhaseId>
    {
        public PhaseId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString() => Value;

        public bool Equals(PhaseId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PhaseId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public static bool operator ==(PhaseId left, PhaseId right) => left.Equals(right);

        public static bool operator !=(PhaseId left, PhaseId right) => !left.Equals(right);
    }
}
