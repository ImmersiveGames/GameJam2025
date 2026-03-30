using System;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Assinatura de contexto do dominio LevelFlow.
    /// Nao deve ser reutilizada como MacroSignature do SceneFlow.
    /// </summary>
    public readonly struct LevelContextSignature : IEquatable<LevelContextSignature>
    {
        public static readonly LevelContextSignature Empty = new(string.Empty);

        public LevelContextSignature(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString() => Value;

        public bool Equals(LevelContextSignature other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LevelContextSignature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public static bool operator ==(LevelContextSignature left, LevelContextSignature right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LevelContextSignature left, LevelContextSignature right)
        {
            return !left.Equals(right);
        }
    }
}
