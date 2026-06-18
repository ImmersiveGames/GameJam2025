using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public readonly struct RuntimeSpawnProfileId : IEquatable<RuntimeSpawnProfileId>
    {
        public RuntimeSpawnProfileId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(RuntimeSpawnProfileId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is RuntimeSpawnProfileId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(RuntimeSpawnProfileId left, RuntimeSpawnProfileId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(RuntimeSpawnProfileId left, RuntimeSpawnProfileId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct RuntimeSpawnOriginMetadata
    {
        public RuntimeSpawnOriginMetadata(
            ActorId ownerActorId,
            ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
            RuntimeSpawnProfileId spawnProfileId,
            PoolDefinitionAsset poolDefinition,
            int commandSequence,
            string source,
            string reason)
        {
            OwnerActorId = ownerActorId;
            OwnerActorInstanceRuntimeId = ownerActorInstanceRuntimeId;
            SpawnProfileId = spawnProfileId;
            PoolDefinition = poolDefinition;
            CommandSequence = commandSequence < 0 ? 0 : commandSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId OwnerActorId { get; }
        public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId { get; }
        public RuntimeSpawnProfileId SpawnProfileId { get; }
        public PoolDefinitionAsset PoolDefinition { get; }
        public int CommandSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            OwnerActorId.IsValid &&
            OwnerActorInstanceRuntimeId.IsValid &&
            SpawnProfileId.IsValid &&
            PoolDefinition != null;

        public string PoolDefinitionName => PoolDefinition == null ? string.Empty : PoolDefinition.name;
    }
}
