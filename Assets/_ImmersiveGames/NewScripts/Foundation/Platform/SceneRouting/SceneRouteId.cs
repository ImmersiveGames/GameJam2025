using System;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.SceneRouting
{
    /// <summary>
    /// Identificador tipado para rotas de scene routing.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de rota.
    /// </summary>
    [Serializable]
    public struct SceneRouteId : IEquatable<SceneRouteId>
    {
        [SerializeField] private string value;

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SceneRouteId(string value)
        {
            this.value = value.TrimToEmpty().ToLowerInvariant();
        }

        public static SceneRouteId FromName(string name)
        {
            return new SceneRouteId(name);
        }

        public override string ToString()
        {
            return Value;
        }

        public bool Equals(SceneRouteId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is SceneRouteId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Value ?? string.Empty).ToLowerInvariant().GetHashCode();
        }

        public static bool operator ==(SceneRouteId left, SceneRouteId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SceneRouteId left, SceneRouteId right)
        {
            return !left.Equals(right);
        }

        public static implicit operator SceneRouteId(string value)
        {
            return new SceneRouteId(value);
        }
        public static implicit operator string(SceneRouteId id)
        {
            return id.Value;
        }

        public static SceneRouteId None => default;
    }
}
