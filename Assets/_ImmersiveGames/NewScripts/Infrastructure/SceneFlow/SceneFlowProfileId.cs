using System;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow
{
    /// <summary>
    /// Identificador tipado para o Profile do SceneFlow.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de profile.
    /// </summary>
    public readonly struct SceneFlowProfileId : IEquatable<SceneFlowProfileId>
    {
        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public SceneFlowProfileId(string value)
        {
            Value = Normalize(value);
        }

        public static SceneFlowProfileId FromName(string name) => new SceneFlowProfileId(name);

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value ?? string.Empty;

        public bool Equals(SceneFlowProfileId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is SceneFlowProfileId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(SceneFlowProfileId left, SceneFlowProfileId right) => left.Equals(right);
        public static bool operator !=(SceneFlowProfileId left, SceneFlowProfileId right) => !left.Equals(right);

        public static SceneFlowProfileId None => default;

        // Profiles oficiais (evidence-based) atualmente usados no projeto.
        public static SceneFlowProfileId Startup => new SceneFlowProfileId("startup");
        public static SceneFlowProfileId Frontend => new SceneFlowProfileId("frontend");
        public static SceneFlowProfileId Gameplay => new SceneFlowProfileId("gameplay");

        public bool IsStartupOrFrontend => this == Startup || this == Frontend;
        public bool IsGameplay => this == Gameplay;
    }
}
