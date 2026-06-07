using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    /// <summary>
    /// Runtime payload resolved for a concrete object emission instance.
    /// </summary>
    [Serializable]
    public readonly struct ObjectEmissionRuntimePayload
    {
        public ObjectEmissionRuntimePayload(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string profileId,
            float speed,
            float lifetimeSeconds,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            string source,
            string reason)
        {
            ActorId = actorId;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ProfileId = Normalize(profileId);
            Speed = speed;
            LifetimeSeconds = lifetimeSeconds;
            SpawnPosition = spawnPosition;
            SpawnRotation = spawnRotation;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorId ActorId { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public string ProfileId { get; }
        public float Speed { get; }
        public float LifetimeSeconds { get; }
        public Vector3 SpawnPosition { get; }
        public Quaternion SpawnRotation { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ActorId.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ProfileId) &&
            Speed > 0f &&
            LifetimeSeconds >= 0f;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Explicit bridge used by the runtime object to request return without coupling itself to the pool service.
    /// </summary>
    public interface IObjectEmissionReturnSink
    {
        void RequestReturn(ObjectEmissionPooledObject pooledObject, in ObjectEmissionRuntimePayload payload, string reason);
    }
}
