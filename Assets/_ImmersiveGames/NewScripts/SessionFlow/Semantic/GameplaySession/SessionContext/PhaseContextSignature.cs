using System;

namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    /// <summary>
    /// Assinatura canônica do contexto da phase.
    /// Não deve ser usada como assinatura macro.
    /// </summary>
    public readonly struct PhaseContextSignature : IEquatable<PhaseContextSignature>
    {
        public static readonly PhaseContextSignature Empty = new(string.Empty);

        public PhaseContextSignature(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString() => Value;

        public bool Equals(PhaseContextSignature other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PhaseContextSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public static bool operator ==(PhaseContextSignature left, PhaseContextSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PhaseContextSignature left, PhaseContextSignature right)
        {
            return !left.Equals(right);
        }
    }
}

