using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionReceiverId : IEquatable<ActivityCapabilityPermissionReceiverId>
    {
        public ActivityCapabilityPermissionReceiverId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActivityCapabilityPermissionReceiverId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActivityCapabilityPermissionReceiverId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActivityCapabilityPermissionReceiverId left, ActivityCapabilityPermissionReceiverId right) => left.Equals(right);
        public static bool operator !=(ActivityCapabilityPermissionReceiverId left, ActivityCapabilityPermissionReceiverId right) => !left.Equals(right);

        public static ActivityCapabilityPermissionReceiverId RuntimeUnbound => new("runtime.unbound");

        public static ActivityCapabilityPermissionReceiverId FromString(string value)
        {
            return new ActivityCapabilityPermissionReceiverId(value);
        }
}
}
