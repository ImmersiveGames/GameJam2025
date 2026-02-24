using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Identificador tipado para rotas do SceneFlow.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de rota.
    /// </summary>
    [Serializable]
    public struct SceneRouteId : IEquatable<SceneRouteId>
    {
        [SerializeField] private string _value;

        public string Value => _value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SceneRouteId(string value)
        {
            _value = Normalize(value);
        }

        public static SceneRouteId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(SceneRouteId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is SceneRouteId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(SceneRouteId left, SceneRouteId right) => left.Equals(right);
        public static bool operator !=(SceneRouteId left, SceneRouteId right) => !left.Equals(right);

        public static implicit operator SceneRouteId(string value) => new(value);
        public static implicit operator string(SceneRouteId id) => id.Value;

        public static SceneRouteId None => default;
    }
}
