using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public readonly struct RuntimeSpawnProfileId : IEquatable<RuntimeSpawnProfileId>
    {
        public RuntimeSpawnProfileId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(RuntimeSpawnProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RuntimeSpawnProfileId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(RuntimeSpawnProfileId left, RuntimeSpawnProfileId right) => left.Equals(right);
        public static bool operator !=(RuntimeSpawnProfileId left, RuntimeSpawnProfileId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
            Source = Normalize(source);
            Reason = Normalize(reason);
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

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
