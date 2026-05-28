using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public sealed class NonPlayerActorInstanceSource : IActivityActorInstanceSource
    {
        private readonly IReadOnlyList<NonPlayerActorRuntimeEntry> _nonPlayerActors;

        public NonPlayerActorInstanceSource(IReadOnlyList<NonPlayerActorRuntimeEntry> nonPlayerActors)
        {
            _nonPlayerActors = nonPlayerActors ?? Array.Empty<NonPlayerActorRuntimeEntry>();
        }

        public ActivityActorInstanceSourceResult Collect(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            List<ActorInstanceRecord> actorInstances = new();
            List<ActorParticipationRecord> actorParticipations = new();
            if (_nonPlayerActors.Count == 0)
            {
                return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
            }

            for (int index = 0; index < _nonPlayerActors.Count; index++)
            {
                NonPlayerActorRuntimeEntry entry = _nonPlayerActors[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                NonPlayerActorIdentityRecord actorIdentity = entry.ActorIdentity;
                ActorScope scope = actorIdentity.ActorScope == NonPlayerActorScope.RouteScoped
                    ? ActorScope.RouteScoped
                    : ActorScope.ActivityScoped;
                ActorSourceKind sourceKind = actorIdentity.OriginSource == NonPlayerActorOriginSource.RouteScene
                    ? ActorSourceKind.RouteScene
                    : ActorSourceKind.ActivityContent;
                ActorParticipationRecord.ActorParticipationPolicy participationPolicy = MapParticipationPolicy(actorIdentity.ParticipationPolicy);
                IReadOnlyList<string> explicitActivityIds = participationPolicy == ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds
                    ? actorIdentity.ParticipatingActivityIds
                    : Array.Empty<string>();
                string policyMetadata = actorIdentity.ParticipationPolicy.ToString();

                Actor runtimeActor = entry.ActorInstance != null ? entry.ActorInstance.GetComponent<Actor>() : null;
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"NonPlayerActorInstanceSource requires Actor root for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(NonPlayerActorInstanceSource)}:{actorIdentity.NonPlayerActorId}");
                ActorCapabilitySurface capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"NonPlayerActorInstanceSource requires ActorCapabilitySurface for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }

                ActorScope runtimeScope = runtimeActor.ActorScopeMetadata;
                if (runtimeScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"NonPlayerActorInstanceSource requires known ActorScope for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }

                string stableActorId = !string.IsNullOrWhiteSpace(runtimeActor.ActorId)
                    ? runtimeActor.ActorId
                    : actorIdentity.NonPlayerActorId;
                ActorInstanceId actorInstanceId = ActorInstanceId.FromScopedIdentity(
                    identity,
                    ActorKind.NonPlayer,
                    stableActorId,
                    runtimeScope,
                    actorScopeDiscriminator: runtimeScope.ToString());
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceId,
                    runtimeActor.ActorDefinitionRef,
                    stableActorId,
                    ActorKind.NonPlayer,
                    runtimeActor,
                    capabilitySurface,
                    runtimeActor.ActorRoleMetadata,
                    runtimeScope,
                    sourceKind,
                    policyMetadata,
                    entry.ActorInstance,
                    actorIdentity.OriginSceneName,
                    entry.ActorInstance != null ? BuildTransformPath(entry.ActorInstance.transform) : string.Empty,
                    source,
                    reason);

                if (!instance.IsValid)
                {
                    continue;
                }

                actorInstances.Add(instance);
                actorParticipations.Add(new ActorParticipationRecord(
                    identity,
                    actorInstanceId,
                    participatesInCurrentEntry: true,
                    retainedForRoute: scope == ActorScope.RouteScoped,
                    policy: participationPolicy,
                    explicitActivityIds: explicitActivityIds,
                    policyMetadata: policyMetadata,
                    source: source,
                    reason: reason));
            }

            return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
        }

        private static ActorParticipationRecord.ActorParticipationPolicy MapParticipationPolicy(NonPlayerActorParticipationPolicy policy)
        {
            return policy switch
            {
                NonPlayerActorParticipationPolicy.AllActivitiesInRoute => ActorParticipationRecord.ActorParticipationPolicy.AllActivitiesInRoute,
                NonPlayerActorParticipationPolicy.ExplicitActivityIds => ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds,
                _ => ActorParticipationRecord.ActorParticipationPolicy.None,
            };
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            if (transform.parent == null)
            {
                return transform.name ?? string.Empty;
            }

            string parentPath = BuildTransformPath(transform.parent);
            return string.IsNullOrWhiteSpace(parentPath)
                ? (transform.name ?? string.Empty)
                : $"{parentPath}/{transform.name}";
        }
    }
}
