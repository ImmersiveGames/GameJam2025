using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime
{
    /// <summary>
    /// Identificador tipado para o Profile do SceneFlow.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de profile.
    /// </summary>
    [Serializable]
    public struct SceneFlowProfileId : IEquatable<SceneFlowProfileId>
    {
        [SerializeField] private string _value;

        public string Value => _value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SceneFlowProfileId(string value)
        {
            _value = Normalize(value);
        }

        public static SceneFlowProfileId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(SceneFlowProfileId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is SceneFlowProfileId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(SceneFlowProfileId left, SceneFlowProfileId right) => left.Equals(right);
        public static bool operator !=(SceneFlowProfileId left, SceneFlowProfileId right) => !left.Equals(right);

        public static implicit operator SceneFlowProfileId(string value) => new(value);
        public static implicit operator string(SceneFlowProfileId id) => id.Value;

        public static SceneFlowProfileId None => default;

        // Profiles oficiais (evidence-based) atualmente usados no projeto.
        public static SceneFlowProfileId Startup => new("startup");
        public static SceneFlowProfileId Frontend => new("frontend");
        public static SceneFlowProfileId Gameplay => new("gameplay");

        public bool IsStartupOrFrontend => this == Startup || this == Frontend;
        public bool IsGameplay => this == Gameplay;
    }
}
