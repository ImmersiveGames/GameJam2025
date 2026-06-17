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
        private readonly SessionActorRuntimeStore _sessionActorStore;

        public PlayerActorInstanceSource(
            IReadOnlyList<PlayerActorIdentityRecord> playerActors,
            ActivityPlayerActorRegistry playerRegistry,
            SessionActorRuntimeStore sessionActorStore)
        {
            _playerActors = playerActors ?? Array.Empty<PlayerActorIdentityRecord>();
            _playerRegistry = playerRegistry;
            _sessionActorStore = sessionActorStore;
        }

        public ActivityActorInstanceSourceResult Collect(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            List<ActorInstanceRecord> actorInstances = new();
            List<ActorParticipationRecord> actorParticipations = new();

            if (_playerRegistry == null && _sessionActorStore == null || _playerActors.Count == 0)
            {
                return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
            }

            for (int index = 0; index < _playerActors.Count; index++)
            {
                var player = _playerActors[index];
                if (!player.IsValid)
                {
                    continue;
                }

                if (!TryResolveHandle(identity, player, out var handle) || !handle.IsValid)
                {
                    continue;
                }

                var actorRoot = handle.Instance;
                var resolvedIdentity = handle.ActorIdentity;
                var runtimeActor = actorRoot.GetComponent<Actor>();
                if (runtimeActor == null)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires Actor root for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                runtimeActor.ValidateLocalConfigurationOrThrow($"{nameof(PlayerActorInstanceSource)}:{resolvedIdentity.PlayerActorId}");
                var capabilitySurface = runtimeActor.CapabilitySurface;
                if (capabilitySurface == null)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires ActorCapabilitySurface for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                var actorScope = runtimeActor.ActorScopeMetadata;
                if (actorScope == ActorScope.Unknown)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires known ActorScope for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                var participationPolicy = runtimeActor.ActorParticipationPolicy;
                if (!Enum.IsDefined(typeof(ActorParticipationRecord.ActorParticipationPolicy), participationPolicy) ||
                    participationPolicy == ActorParticipationRecord.ActorParticipationPolicy.None)
                {
                    throw new InvalidOperationException($"PlayerActorInstanceSource requires non-empty ActorParticipationPolicy for playerActorId='{resolvedIdentity.PlayerActorId}'.");
                }

                string stableActorId = resolvedIdentity.ActorId.ToString();
                var actorInstanceRuntimeId = ActorInstanceRuntimeId.FromScopedRuntimeActorIdentity(
                    identity,
                    stableActorId,
                    actorScope,
                    actorScopeDiscriminator: actorScope.ToString());
                if (!actorInstanceRuntimeId.IsValid)
                {
                    throw new InvalidOperationException(
                        $"PlayerActorInstanceSource generated invalid actor instance runtime identity playerActorId='{resolvedIdentity.PlayerActorId}' actorId='{stableActorId}' actorScope='{actorScope}'.");
                }

                runtimeActor.SetRuntimeActorInstanceId(actorInstanceRuntimeId);
                ActorInstanceRecord instance = new(
                    identity,
                    actorInstanceRuntimeId,
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
                    actorInstanceRuntimeId,
                    participatesInCurrentEntry: true,
                    policy: participationPolicy,
                    explicitActivityIds: Array.Empty<string>(),
                    policyMetadata: participationPolicy.ToString(),
                    source: source,
                    reason: reason));
            }

            return new ActivityActorInstanceSourceResult(actorInstances, actorParticipations);
        }

        private bool TryResolveHandle(
            SessionActivityIdentity identity,
            PlayerActorIdentityRecord player,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (_playerRegistry != null &&
                (_playerRegistry.TryGetActiveHandleByParticipant(player.ParticipantId, out handle) ||
                 _playerRegistry.TryGetRouteScopedHandleByParticipant(player.ParticipantId, out handle)) &&
                handle.IsValid)
            {
                return true;
            }

            if (_sessionActorStore != null &&
                _sessionActorStore.TryGetByParticipantId(identity, player.ParticipantId, out var stored) &&
                stored.IsValid)
            {
                handle = new PlayerActorRuntimeHandle(player, stored.Instance, stored.Actor);
                return handle.IsValid;
            }

            return false;
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
                ? transform.name ?? string.Empty
                : $"{parentPath}/{transform.name}";
        }
    }
}
