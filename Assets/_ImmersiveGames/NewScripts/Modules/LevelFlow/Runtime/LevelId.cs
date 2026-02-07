using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Identificador tipado para níveis do LevelFlow.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de nível.
    /// </summary>
    [Serializable]
    public struct LevelId : IEquatable<LevelId>
    {
        [SerializeField] private string _value;

        public string Value => _value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public LevelId(string value)
        {
            _value = Normalize(value);
        }

        public static LevelId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(LevelId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is LevelId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(LevelId left, LevelId right) => left.Equals(right);
        public static bool operator !=(LevelId left, LevelId right) => !left.Equals(right);

        public static implicit operator LevelId(string value) => new(value);
        public static implicit operator string(LevelId id) => id.Value;

        public static LevelId None => default;
    }
}
