using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerActorInstanceSource : IActivityActorInstanceSource
    {
        private readonly IReadOnlyList<PlayerActorIdentityRecord> _playerActors;
        private readonly ActivityPlayerActorRegistry _playerRegistry;

        public PlayerActorInstanceSource(
            IReadOnlyList<PlayerActorIdentityRecord> playerActors,
            ActivityPlayerActorRegistry playerRegistry)
        {
            _playerActors = playerActors ?? Array.Empty<PlayerActorIdentityRecord>();
            _playerRegistry = playerRegistry;
        }

        public ActivityActorInstanceSourceResult Collect(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            List<ActorInstanceRecord> actorInstances = new();
            List<ActorParticipationRecord> actorParticipations = new();

            if (_playerRegistry == null || _playerActors.Count == 0)
            {
                return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
            }

            for (int index = 0; index < _playerActors.Count; index++)
            {
                PlayerActorIdentityRecord player = _playerActors[index];
                if (!player.IsValid)
                {
                    continue;
                }

                if (!_playerRegistry.TryResolveHandleForParticipant(identity, player.ParticipantId, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
                {
                    continue;
                }

                GameObject actorRoot = handle.Instance;
                PlayerActorIdentityRecord resolvedIdentity = handle.ActorIdentity;
                Actor runtimeActor = actorRoot.GetComponent<Actor>();
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires Actor root for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(PlayerActorInstanceSource)}:{resolvedIdentity.PlayerActorId}");
                ActorCapabilitySurface capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires ActorCapabilitySurface for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                ActorScope actorScope = runtimeActor.ActorScopeMetadata;
                if (actorScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires known ActorScope for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                ActorParticipationRecord.ActorParticipationPolicy participationPolicy = runtimeActor.ActorParticipationPolicy;
                if (!Enum.IsDefined(typeof(ActorParticipationRecord.ActorParticipationPolicy), participationPolicy) ||
                    participationPolicy == ActorParticipationRecord.ActorParticipationPolicy.None)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires non-empty ActorParticipationPolicy for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                string stableActorId = resolvedIdentity.ActorId.ToString();
                ActorInstanceId actorInstanceId = ActorInstanceId.FromScopedRuntimeActorIdentity(
                    identity,
                    stableActorId,
                    actorScope,
                    actorScopeDiscriminator: actorScope.ToString());
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceId,
                    runtimeActor.ActorDefinitionRef,
                    stableActorId,
                    ActorKind.Player,
                    runtimeActor,
                    capabilitySurface,
                    runtimeActor.ActorRoleMetadata,
                    actorScope,
                    ActorSourceKind.PlayerParticipation,
                    participationPolicy.ToString(),
                    actorRoot,
                    actorRoot.scene.name,
                    BuildTransformPath(actorRoot.transform),
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
                    retainedForRoute: actorScope == ActorScope.RouteScoped,
                    policy: participationPolicy,
                    explicitActivityIds: Array.Empty<string>(),
                    policyMetadata: participationPolicy.ToString(),
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
