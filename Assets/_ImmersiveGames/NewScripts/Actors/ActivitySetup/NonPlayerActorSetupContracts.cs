using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public enum NonPlayerActorScope
    {
        Unknown = 0,
        ActivityScoped = 1,
        RouteScoped = 2,
        GlobalScopedUnsupported = 3,
    }

    public enum NonPlayerActorParticipationPolicy
    {
        Unknown = 0,
        ExplicitActivityIds = 1,
        AllActivitiesInRoute = 2,
        Disabled = 3,
    }

    public enum NonPlayerActorOriginSource
    {
        Unknown = 0,
        ActivityContent = 1,
        RouteScene = 2,
    }

    public readonly struct NonPlayerActorIdentityRecord
    {
        public NonPlayerActorIdentityRecord(
            SessionActivityIdentity identity,
            string nonPlayerActorId,
            string actorKind,
            NonPlayerActorScope actorScope,
            NonPlayerActorParticipationPolicy participationPolicy,
            IReadOnlyList<string> participatingActivityIds,
            NonPlayerActorOriginSource originSource,
            string originSceneName)
        {
            Identity = identity;
            NonPlayerActorId = Normalize(nonPlayerActorId);
            ActorKind = Normalize(actorKind);
            ActorScope = actorScope;
            ParticipationPolicy = participationPolicy;
            ParticipatingActivityIds = participatingActivityIds ?? Array.Empty<string>();
            OriginSource = originSource;
            OriginSceneName = Normalize(originSceneName);
        }

        public SessionActivityIdentity Identity { get; }
        public string NonPlayerActorId { get; }
        public string ActorKind { get; }
        public NonPlayerActorScope ActorScope { get; }
        public NonPlayerActorParticipationPolicy ParticipationPolicy { get; }
        public IReadOnlyList<string> ParticipatingActivityIds { get; }
        public NonPlayerActorOriginSource OriginSource { get; }
        public string OriginSceneName { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(NonPlayerActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind) &&
            ActorScope != NonPlayerActorScope.Unknown &&
            ActorScope != NonPlayerActorScope.GlobalScopedUnsupported &&
            ParticipationPolicy != NonPlayerActorParticipationPolicy.Unknown &&
            HasValidParticipationActivities() &&
            OriginSource != NonPlayerActorOriginSource.Unknown &&
            !string.IsNullOrWhiteSpace(OriginSceneName);

        private bool HasValidParticipationActivities()
        {
            if (ParticipationPolicy != NonPlayerActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (ParticipatingActivityIds == null || ParticipatingActivityIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < ParticipatingActivityIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(Normalize(ParticipatingActivityIds[index])))
                {
                    return false;
                }
            }

            return true;
        }

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
