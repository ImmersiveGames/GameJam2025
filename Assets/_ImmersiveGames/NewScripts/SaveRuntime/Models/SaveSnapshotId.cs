using System;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public readonly struct SaveSnapshotId : IEquatable<SaveSnapshotId>
    {
        public SaveSnapshotId(string value)
        {
            Value = NormalizeRequired(value, nameof(value));
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public bool Equals(SaveSnapshotId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SaveSnapshotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public static bool operator ==(SaveSnapshotId left, SaveSnapshotId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SaveSnapshotId left, SaveSnapshotId right)
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
