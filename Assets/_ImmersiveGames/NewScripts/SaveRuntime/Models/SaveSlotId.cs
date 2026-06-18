using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public readonly struct SaveSlotId : IEquatable<SaveSlotId>
    {
        public SaveSlotId(string value)
        {
            Value = NormalizeRequired(value, nameof(value));
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public bool Equals(SaveSlotId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SaveSlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public static bool operator ==(SaveSlotId left, SaveSlotId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SaveSlotId left, SaveSlotId right)
        {
            return !left.Equals(right);
        }

        private static string NormalizeRequired(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", paramName);
            }

            return value.Trim();
        }
    }
}
