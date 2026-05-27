using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct ActorInventoryFeedResult
    {
        public ActorInventoryFeedResult(
            SessionActivityIdentity identity,
            IReadOnlyList<ActorInstanceRecord> actorInstances,
            IReadOnlyList<ActorEntryRecord> actorEntries,
            IReadOnlyList<ActorParticipationRecord> actorParticipations,
            string source,
            string reason)
        {
            Identity = identity;
            ActorInstances = actorInstances ?? Array.Empty<ActorInstanceRecord>();
            ActorEntries = actorEntries ?? Array.Empty<ActorEntryRecord>();
            ActorParticipations = actorParticipations ?? Array.Empty<ActorParticipationRecord>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public IReadOnlyList<ActorInstanceRecord> ActorInstances { get; }
        public IReadOnlyList<ActorEntryRecord> ActorEntries { get; }
        public IReadOnlyList<ActorParticipationRecord> ActorParticipations { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid;

        public IReadOnlyList<ActorScanTarget> BuildScanTargets(string source)
        {
            List<ActorScanTarget> targets = new();
            for (int index = 0; index < ActorInstances.Count; index++)
            {
                if (ActorScanTarget.TryFromInstance(ActorInstances[index], source, out ActorScanTarget target))
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class ActorInventoryFeed
    {
        public ActorInventoryFeedResult BuildFromCurrentSources(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> playerActors,
            ActivityPlayerActorRegistry playerRegistry,
            IReadOnlyList<NonPlayerActorRuntimeEntry> nonPlayerActors,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActorInventoryFeed requires valid SessionActivityIdentity.");
            }

            List<ActorInstanceRecord> actorInstances = new();
            List<ActorEntryRecord> actorEntries = new();
            List<ActorParticipationRecord> actorParticipations = new();

            AppendPlayerActors(identity, playerActors, playerRegistry, source, reason, actorInstances, actorEntries, actorParticipations);
            AppendNonPlayerActors(identity, nonPlayerActors, source, reason, actorInstances, actorEntries, actorParticipations);

            return new ActorInventoryFeedResult(identity, actorInstances, actorEntries, actorParticipations, source, reason);
        }

        private static void AppendPlayerActors(
            SessionActivityIdentity identity,
            IReadOnlyList<PlayerActorIdentityRecord> playerActors,
            ActivityPlayerActorRegistry playerRegistry,
            string source,
            string reason,
            List<ActorInstanceRecord> actorInstances,
            List<ActorEntryRecord> actorEntries,
            List<ActorParticipationRecord> actorParticipations)
        {
            if (playerActors == null || playerActors.Count == 0 || playerRegistry == null)
            {
                return;
            }

            for (int index = 0; index < playerActors.Count; index++)
            {
                PlayerActorIdentityRecord player = playerActors[index];
                if (!player.IsValid)
                {
                    continue;
                }

                if (!playerRegistry.TryResolveInstanceForControl(identity, player.PlayerActorId, out GameObject actorRoot, out PlayerActorIdentityRecord resolvedIdentity) ||
                    actorRoot == null ||
                    !resolvedIdentity.IsValid)
                {
                    continue;
                }

                ActorInstanceId actorInstanceId = ActorInstanceId.FromIdentity(identity, ActorKind.Player, resolvedIdentity.PlayerActorId, actorScopeDiscriminator: "route");
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceId,
                    definitionRef: default,
                    actorId: resolvedIdentity.PlayerActorId,
                    actorKind: ActorKind.Player,
                    actorRole: ActorRole.PrimaryPlayer,
                    actorScope: ActorScope.RouteScoped,
                    actorSourceKind: ActorSourceKind.PlayerParticipation,
                    participationPolicy: "all_activities_in_route",
                    actorRoot: actorRoot,
                    sourceSceneName: actorRoot.scene.name,
                    componentBasePath: BuildTransformPath(actorRoot.transform),
                    source: source,
                    reason: reason);

                if (!instance.IsValid)
                {
                    continue;
                }

                actorInstances.Add(instance);
                actorEntries.Add(new ActorEntryRecord(identity, instance, actorEntries.Count, source, reason));
                actorParticipations.Add(new ActorParticipationRecord(
                    identity,
                    actorInstanceId,
                    participatesInCurrentEntry: true,
                    retainedForRoute: true,
                    policy: "all_activities_in_route",
                    source: source,
                    reason: reason));
            }
        }

        private static void AppendNonPlayerActors(
            SessionActivityIdentity identity,
            IReadOnlyList<NonPlayerActorRuntimeEntry> nonPlayerActors,
            string source,
            string reason,
            List<ActorInstanceRecord> actorInstances,
            List<ActorEntryRecord> actorEntries,
            List<ActorParticipationRecord> actorParticipations)
        {
            if (nonPlayerActors == null || nonPlayerActors.Count == 0)
            {
                return;
            }

            for (int index = 0; index < nonPlayerActors.Count; index++)
            {
                NonPlayerActorRuntimeEntry entry = nonPlayerActors[index];
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
                string policy = actorIdentity.ParticipationPolicy.ToString();

                ActorDefinitionRef definitionRef = default;
                if (entry.Endpoint != null && entry.Endpoint.PresentationProfile != null)
                {
                    definitionRef = new ActorDefinitionRef(
                        default,
                        entry.Endpoint.PresentationProfile.name,
                        entry.Endpoint.PresentationProfile.name);
                }

                ActorInstanceId actorInstanceId = ActorInstanceId.FromIdentity(identity, ActorKind.NonPlayer, actorIdentity.NonPlayerActorId, actorScopeDiscriminator: scope.ToString());
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceId,
                    definitionRef,
                    actorIdentity.NonPlayerActorId,
                    ActorKind.NonPlayer,
                    ActorRole.SceneAuthoredNonPlayer,
                    scope,
                    sourceKind,
                    policy,
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
                actorEntries.Add(new ActorEntryRecord(identity, instance, actorEntries.Count, source, reason));
                actorParticipations.Add(new ActorParticipationRecord(
                    identity,
                    actorInstanceId,
                    participatesInCurrentEntry: true,
                    retainedForRoute: scope == ActorScope.RouteScoped,
                    policy: policy,
                    source: source,
                    reason: reason));
            }
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
