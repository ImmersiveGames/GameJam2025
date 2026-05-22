using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct NonPlayerActorIdentityRecord
    {
        public NonPlayerActorIdentityRecord(
            SessionActivityIdentity identity,
            string nonPlayerActorId,
            string actorKind)
        {
            Identity = identity;
            NonPlayerActorId = Normalize(nonPlayerActorId);
            ActorKind = Normalize(actorKind);
        }

        public SessionActivityIdentity Identity { get; }
        public string NonPlayerActorId { get; }
        public string ActorKind { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(NonPlayerActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct NonPlayerActorDiscoveryRecord
    {
        public NonPlayerActorDiscoveryRecord(
            NonPlayerActorIdentityRecord actorIdentity,
            NonPlayerActorEndpoint endpoint,
            GameObject actorInstance,
            string sceneName)
        {
            ActorIdentity = actorIdentity;
            Endpoint = endpoint;
            ActorInstance = actorInstance;
            SceneName = Normalize(sceneName);
        }

        public NonPlayerActorIdentityRecord ActorIdentity { get; }
        public NonPlayerActorEndpoint Endpoint { get; }
        public GameObject ActorInstance { get; }
        public string SceneName { get; }
        public bool IsValid => ActorIdentity.IsValid && Endpoint != null && ActorInstance != null && !string.IsNullOrWhiteSpace(SceneName);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct NonPlayerActorRuntimeEntry
    {
        public NonPlayerActorRuntimeEntry(
            NonPlayerActorIdentityRecord actorIdentity,
            NonPlayerActorEndpoint endpoint,
            GameObject actorInstance,
            ActorPresentationRuntimeHandle presentationHandle)
        {
            ActorIdentity = actorIdentity;
            Endpoint = endpoint;
            ActorInstance = actorInstance;
            PresentationHandle = presentationHandle;
        }

        public NonPlayerActorIdentityRecord ActorIdentity { get; }
        public NonPlayerActorEndpoint Endpoint { get; }
        public GameObject ActorInstance { get; }
        public ActorPresentationRuntimeHandle PresentationHandle { get; }
        public bool HasPresentationHandle => PresentationHandle.IsValid;
        public bool IsValid => ActorIdentity.IsValid && Endpoint != null && ActorInstance != null;
    }
}
