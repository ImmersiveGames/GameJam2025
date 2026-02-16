using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Identificador tipado para intents de navegação.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de intent.
    /// </summary>
    [Serializable]
    public struct NavigationIntentId : IEquatable<NavigationIntentId>
    {
        [SerializeField] private string _value;

        public string Value => _value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public NavigationIntentId(string value)
        {
            _value = Normalize(value);
        }

        public static NavigationIntentId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(NavigationIntentId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is NavigationIntentId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(NavigationIntentId left, NavigationIntentId right) => left.Equals(right);
        public static bool operator !=(NavigationIntentId left, NavigationIntentId right) => !left.Equals(right);

        public static implicit operator NavigationIntentId(string value) => new(value);
        public static implicit operator string(NavigationIntentId id) => id.Value;

        public static NavigationIntentId None => default;
    }
}
