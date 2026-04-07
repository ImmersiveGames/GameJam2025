using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
{
    /// <summary>
    /// Identificador tipado e normalizado para PhaseDefinition.
    /// </summary>
    [Serializable]
    public struct PhaseDefinitionId : IEquatable<PhaseDefinitionId>
    {
        [SerializeField] private string value;

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public PhaseDefinitionId(string value)
        {
            this.value = Normalize(value);
        }

        public static PhaseDefinitionId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(PhaseDefinitionId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is PhaseDefinitionId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(PhaseDefinitionId left, PhaseDefinitionId right) => left.Equals(right);
        public static bool operator !=(PhaseDefinitionId left, PhaseDefinitionId right) => !left.Equals(right);

        public static implicit operator PhaseDefinitionId(string value) => new(value);
        public static implicit operator string(PhaseDefinitionId id) => id.Value;

        public static PhaseDefinitionId None => default;
    }
}
