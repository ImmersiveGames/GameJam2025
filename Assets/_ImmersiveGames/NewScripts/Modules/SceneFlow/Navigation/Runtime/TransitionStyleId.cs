using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Identificador tipado para estilos de transição do SceneFlow.
    ///
    /// Regras:
    /// - O valor é normalizado (trim + lower-invariant).
    /// - Comparação é case-insensitive.
    /// - <see cref="None"/> representa ausência de estilo.
    /// </summary>
    [Serializable]
    public struct TransitionStyleId : IEquatable<TransitionStyleId>
    {
        [SerializeField] private string _value;

        public string Value => _value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public TransitionStyleId(string value)
        {
            _value = Normalize(value);
        }

        public static TransitionStyleId FromName(string name) => new(name);

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        public override string ToString() => Value;

        public bool Equals(TransitionStyleId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is TransitionStyleId other && Equals(other);

        public override int GetHashCode() =>
            (Value ?? string.Empty).ToLowerInvariant().GetHashCode();

        public static bool operator ==(TransitionStyleId left, TransitionStyleId right) => left.Equals(right);
        public static bool operator !=(TransitionStyleId left, TransitionStyleId right) => !left.Equals(right);

        public static implicit operator TransitionStyleId(string value) => new(value);
        public static implicit operator string(TransitionStyleId id) => id.Value;

        public static TransitionStyleId None => default;
    }
}
