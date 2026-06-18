using System;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Foundation
{
    [Serializable]
    public struct ActorId : IEquatable<ActorId>
    {
        public ActorId(string value)
        {
            Value = value.TrimToEmpty();
        }

        [field: SerializeField] public string Value { get; private set; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActorId left, ActorId right)
        {
            return !left.Equals(right);
        }
    }

    [Serializable]
    public struct ActorInstanceRuntimeId : IEquatable<ActorInstanceRuntimeId>
    {
        private const string RuntimeActorTypeDiscriminator = "Actor";

        public ActorInstanceRuntimeId(string value)
        {
            Value = value.TrimToEmpty();
        }

        [field: SerializeField] public string Value { get; private set; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActorInstanceRuntimeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
        public override bool Equals(object obj)
        {
            return obj is ActorInstanceRuntimeId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActorInstanceRuntimeId left, ActorInstanceRuntimeId right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ActorInstanceRuntimeId left, ActorInstanceRuntimeId right)
        {
            return !left.Equals(right);
        }

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

            string normalizedActorId = actorId.TrimToEmpty();
            string normalizedScopeDiscriminator = actorScopeDiscriminator.TrimToEmpty();
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
                _ => default
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

            string normalizedSpawnedActorId = spawnedActorId.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(normalizedSpawnedActorId))
            {
                return default;
            }

            int normalizedSequence = spawnSequence < 0 ? 0 : spawnSequence;
            return new ActorInstanceRuntimeId(
                $"{ownerActorInstanceRuntimeId.Value}|runtime-spawn|{RuntimeActorTypeDiscriminator}|{normalizedSpawnedActorId}|{normalizedSequence}");
        }
    }
}
