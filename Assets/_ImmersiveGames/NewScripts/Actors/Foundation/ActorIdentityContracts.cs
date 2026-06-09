using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    public readonly struct ActorId : IEquatable<ActorId>
    {
        public ActorId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorId left, ActorId right) => left.Equals(right);
        public static bool operator !=(ActorId left, ActorId right) => !left.Equals(right);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct ActorInstanceRuntimeId : IEquatable<ActorInstanceRuntimeId>
    {
        private const string RuntimeActorTypeDiscriminator = "Actor";

        public ActorInstanceRuntimeId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorInstanceRuntimeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorInstanceRuntimeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorInstanceRuntimeId left, ActorInstanceRuntimeId right) => left.Equals(right);
        public static bool operator !=(ActorInstanceRuntimeId left, ActorInstanceRuntimeId right) => !left.Equals(right);

        public static ActorInstanceRuntimeId FromScopedRuntimeActorIdentity(
            SessionActivityIdentity identity,
            string actorId,
            ActorScope actorScope,
            string actorScopeDiscriminator)
        {
            if (!identity.IsValid)
            {
                return default;
            }

            string normalizedActorId = Normalize(actorId);
            string normalizedScopeDiscriminator = Normalize(actorScopeDiscriminator);
            if (string.IsNullOrWhiteSpace(normalizedActorId))
            {
                return default;
            }

            return actorScope switch
            {
                ActorScope.SessionScoped => new ActorInstanceRuntimeId(
                    $"{identity.PipelineId}|{identity.SessionId}|session|{RuntimeActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                ActorScope.RouteScoped => new ActorInstanceRuntimeId(
                    $"{identity.PipelineId}|{identity.SessionId}|route|{RuntimeActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                ActorScope.ActivityScoped => new ActorInstanceRuntimeId(
                    $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.EntrySequence}|{RuntimeActorTypeDiscriminator}|{normalizedActorId}|{normalizedScopeDiscriminator}"),
                _ => default,
            };
        }

        public static ActorInstanceRuntimeId FromRuntimeSpawnedActorIdentity(
            ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
            string spawnedActorId,
            int spawnSequence)
        {
            if (!ownerActorInstanceRuntimeId.IsValid)
            {
                return default;
            }

            string normalizedSpawnedActorId = Normalize(spawnedActorId);
            if (string.IsNullOrWhiteSpace(normalizedSpawnedActorId))
            {
                return default;
            }

            int normalizedSequence = spawnSequence < 0 ? 0 : spawnSequence;
            return new ActorInstanceRuntimeId(
                $"{ownerActorInstanceRuntimeId.Value}|runtime-spawn|{RuntimeActorTypeDiscriminator}|{normalizedSpawnedActorId}|{normalizedSequence}");
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
