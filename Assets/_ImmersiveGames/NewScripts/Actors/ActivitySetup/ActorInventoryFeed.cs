using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
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

                Actor runtimeActor = actorRoot.GetComponent<Actor>();
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires Actor root for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(ActorInventoryFeed)}:player:{resolvedIdentity.PlayerActorId}");
                ActorCapabilitySurface capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires ActorCapabilitySurface for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                ActorRole actorRole = runtimeActor != null ? runtimeActor.ActorRoleMetadata : ActorRole.PrimaryPlayer;
                ActorScope actorScope = runtimeActor != null ? runtimeActor.ActorScopeMetadata : ActorScope.RouteScoped;
                if (actorScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires known ActorScope for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                string stableActorId = runtimeActor != null && !string.IsNullOrWhiteSpace(runtimeActor.ActorId)
                    ? runtimeActor.ActorId
                    : resolvedIdentity.PlayerActorId;
                ActorInstanceId actorInstanceId = ActorInstanceId.FromScopedIdentity(
                    identity,
                    ActorKind.Player,
                    stableActorId,
                    actorScope,
                    actorScopeDiscriminator: actorScope.ToString());
                ActorDefinitionRef definitionRef = runtimeActor != null ? runtimeActor.ActorDefinitionRef : default;
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceId,
                    definitionRef: definitionRef,
                    actorId: stableActorId,
                    actorKind: ActorKind.Player,
                    runtimeActor: runtimeActor,
                    capabilitySurface: capabilitySurface,
                    actorRole: actorRole,
                    actorScope: actorScope,
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
                    policy: ActorParticipationRecord.ActorParticipationPolicy.AllActivitiesInRoute,
                    explicitActivityIds: Array.Empty<string>(),
                    policyMetadata: "all_activities_in_route",
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
                ActorParticipationRecord.ActorParticipationPolicy participationPolicy = MapParticipationPolicy(actorIdentity.ParticipationPolicy);
                IReadOnlyList<string> explicitActivityIds = participationPolicy == ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds
                    ? actorIdentity.ParticipatingActivityIds
                    : Array.Empty<string>();
                string policyMetadata = actorIdentity.ParticipationPolicy.ToString();

                Actor runtimeActor = entry.ActorInstance != null ? entry.ActorInstance.GetComponent<Actor>() : null;
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires Actor root for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(ActorInventoryFeed)}:nonPlayer:{actorIdentity.NonPlayerActorId}");
                ActorDefinitionRef definitionRef = default;
                if (runtimeActor != null)
                {
                    definitionRef = runtimeActor.ActorDefinitionRef;
                }
                else if (entry.Endpoint != null && entry.Endpoint.PresentationProfile != null)
                {
                    definitionRef = new ActorDefinitionRef(
                        default,
                        entry.Endpoint.PresentationProfile.name,
                        entry.Endpoint.PresentationProfile.name);
                }

                ActorCapabilitySurface capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires ActorCapabilitySurface for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }
                ActorRole actorRole = runtimeActor != null ? runtimeActor.ActorRoleMetadata : ActorRole.SceneAuthoredNonPlayer;
                ActorScope runtimeScope = runtimeActor != null ? runtimeActor.ActorScopeMetadata : scope;
                if (runtimeScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"ActorInventoryFeed requires known ActorScope for nonPlayerActorId='{actorIdentity.NonPlayerActorId}'.");
                }

                string stableActorId = runtimeActor != null && !string.IsNullOrWhiteSpace(runtimeActor.ActorId)
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
                    definitionRef,
                    stableActorId,
                    ActorKind.NonPlayer,
                    runtimeActor,
                    capabilitySurface,
                    actorRole,
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
                actorEntries.Add(new ActorEntryRecord(identity, instance, actorEntries.Count, source, reason));
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
