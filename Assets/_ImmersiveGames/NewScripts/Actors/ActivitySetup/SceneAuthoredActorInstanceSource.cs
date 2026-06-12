using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public sealed class SceneAuthoredActorInstanceSource : IActivityActorInstanceSource
    {
        private readonly IReadOnlyList<SceneAuthoredActorRuntimeEntry> _actors;

        public SceneAuthoredActorInstanceSource(IReadOnlyList<SceneAuthoredActorRuntimeEntry> actors)
        {
            _actors = actors ?? Array.Empty<SceneAuthoredActorRuntimeEntry>();
        }

        public ActivityActorInstanceSourceResult Collect(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            List<ActorInstanceRecord> actorInstances = new();
            List<ActorParticipationRecord> actorParticipations = new();
            if (_actors.Count == 0)
            {
                return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
            }

            for (int index = 0; index < _actors.Count; index++)
            {
                var entry = _actors[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                var actorIdentity = entry.ActorIdentity;
                var runtimeActor = entry.Actor != null
                    ? entry.Actor
                    : entry.ActorInstance != null
                        ? entry.ActorInstance.GetComponent<Actor>()
                        : null;
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"SceneAuthoredActorInstanceSource requires Actor root for actorId='{actorIdentity.ActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(SceneAuthoredActorInstanceSource)}:{actorIdentity.ActorId}");
                var capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"SceneAuthoredActorInstanceSource requires ActorCapabilitySurface for actorId='{actorIdentity.ActorId}'.");
                }

                var runtimeScope = runtimeActor.ActorScopeMetadata;
                if (runtimeScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"SceneAuthoredActorInstanceSource requires known ActorScope for actorId='{actorIdentity.ActorId}'.");
                }

                string stableActorId = !string.IsNullOrWhiteSpace(runtimeActor.ActorId)
                    ? runtimeActor.ActorId
                    : actorIdentity.ActorId;
                var actorInstanceRuntimeId = actorIdentity.ActorInstanceRuntimeId;
                runtimeActor.SetRuntimeActorInstanceId(actorInstanceRuntimeId);
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceRuntimeId,
                    runtimeActor.ActorDefinitionRef,
                    stableActorId,
                    ActorKind.Actor,
                    runtimeActor,
                    capabilitySurface,
                    runtimeActor.ActorRoleMetadata,
                    runtimeScope,
                    actorIdentity.OriginSourceKind,
                    actorIdentity.ParticipationPolicy.ToString(),
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
                    actorInstanceRuntimeId,
                    participatesInCurrentEntry: true,
                    policy: actorIdentity.ParticipationPolicy,
                    explicitActivityIds: actorIdentity.ParticipationPolicy == ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds
                        ? actorIdentity.ExplicitActivityIds
                        : Array.Empty<string>(),
                    policyMetadata: actorIdentity.ParticipationPolicy.ToString(),
                    source: source,
                    reason: reason));
            }

            return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
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
