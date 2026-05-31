using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct PlayerActorCapabilityIdentity : IEquatable<PlayerActorCapabilityIdentity>
    {
        public PlayerActorCapabilityIdentity(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            PlayerActorId = playerActorId;
            PlayerSlotId = playerSlotId;
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public PlayerActorId PlayerActorId { get; }
        public PlayerSlotId PlayerSlotId { get; }

        public bool IsValid => ActorId.IsValid && ActorInstanceRuntimeId.IsValid && PlayerActorId.IsValid && PlayerSlotId.IsValid;

        public bool Equals(PlayerActorCapabilityIdentity other)
        {
            return ActorId == other.ActorId &&
                   ActorInstanceRuntimeId == other.ActorInstanceRuntimeId &&
                   PlayerActorId == other.PlayerActorId &&
                   PlayerSlotId == other.PlayerSlotId;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorCapabilityIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActorId, ActorInstanceRuntimeId, PlayerActorId, PlayerSlotId);
        }
    }
}
