using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct PlayerActorCapabilityIdentity : IEquatable<PlayerActorCapabilityIdentity>
    {
        public PlayerActorCapabilityIdentity(string playerActorId, string playerSlotId)
        {
            PlayerActorId = Normalize(playerActorId);
            PlayerSlotId = Normalize(playerSlotId);
        }

        public string PlayerActorId { get; }
        public string PlayerSlotId { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerActorId) && !string.IsNullOrWhiteSpace(PlayerSlotId);

        public bool Equals(PlayerActorCapabilityIdentity other)
        {
            return string.Equals(PlayerActorId, other.PlayerActorId, StringComparison.Ordinal) &&
                   string.Equals(PlayerSlotId, other.PlayerSlotId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorCapabilityIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerActorId ?? string.Empty, PlayerSlotId ?? string.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
