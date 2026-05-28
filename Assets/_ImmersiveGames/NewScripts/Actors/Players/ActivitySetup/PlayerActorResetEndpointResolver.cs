using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    // Debito transitório da Base 1.2: resolução concreta ainda usa registry de PlayerActor.
    public sealed class PlayerActorResetEndpointResolver : IActorResetEndpointResolver
    {
        private readonly ActivityPlayerActorRegistry _registry;

        public PlayerActorResetEndpointResolver(ActivityPlayerActorRegistry registry)
        {
            _registry = registry ?? throw new InvalidOperationException("PlayerActorResetEndpointResolver requires non-null registry.");
        }

        public GameObject ResolveOrFail(SessionActivityIdentity activeIdentity, ActorResetActorRef actor)
        {
            if (_registry == null)
            {
                throw new InvalidOperationException("actor_reset_registry_missing: PlayerActor reset endpoint resolver requires ActivityPlayerActorRegistry.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("PlayerActorResetEndpointResolver requires valid activity identity.");
            }

            if (!actor.IsValid)
            {
                throw new InvalidOperationException("PlayerActorResetEndpointResolver requires valid actor reference.");
            }

            if (!actor.IsPlayer)
            {
                throw new InvalidOperationException(
                    $"actor_reset_actor_kind_unsupported: expected Player actor kind for resolver actorId='{actor.ActorId}' actorKind='{actor.ActorKind}'.");
            }

            if (!IsSameActivityCycle(activeIdentity, actor.Identity))
            {
                throw new InvalidOperationException("actor_reset_identity_mismatch: actor identity does not match active identity.");
            }

            if (string.IsNullOrWhiteSpace(actor.PlayerActorId))
            {
                throw new InvalidOperationException(
                    $"actor_reset_player_identity_missing: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
            }

            if (_registry.TryResolveInstanceForControl(activeIdentity, actor.PlayerActorId, out GameObject instance, out PlayerActorIdentityRecord observedIdentity) &&
                instance != null &&
                observedIdentity.IsValid)
            {
                return instance;
            }

            throw new InvalidOperationException(
                $"actor_reset_actor_not_found: actorId='{actor.ActorId}' playerActorId='{actor.PlayerActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
        }

        public void EnsureIdentityMatchesOrFail(GameObject instance, SessionActivityIdentity activeIdentity, ActorResetActorRef actor)
        {
            if (instance == null)
            {
                throw new InvalidOperationException("PlayerActorResetEndpointResolver cannot validate null instance.");
            }

            PlayerActorIdentity identity = instance.GetComponent<PlayerActorIdentity>();
            Actor runtimeActor = instance.GetComponent<Actor>();
            if (identity == null || !identity.IsValid)
            {
                throw new InvalidOperationException($"PlayerActor identity component missing/invalid for reset. actorId='{actor.ActorId}'.");
            }

            if (runtimeActor == null)
            {
                throw new InvalidOperationException(
                    $"actor_reset_actor_target_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' reason='runtime_actor_missing'.");
            }

            if (!string.Equals(identity.PipelineId, activeIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(identity.PlayerSlotId, actor.PlayerSlotId, StringComparison.Ordinal) ||
                !string.Equals(identity.PlayerActorId, actor.PlayerActorId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"actor_reset_player_identity_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' playerActorId='{actor.PlayerActorId}' playerSlotId='{actor.PlayerSlotId}' does not match endpoint identity.");
            }

            bool isRouteScoped = runtimeActor.ActorScopeMetadata == Actors.Foundation.ActorScope.RouteScoped;
            if (!isRouteScoped &&
                (!string.Equals(identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) ||
                 identity.ActivityOrdinal != activeIdentity.ActivityOrdinal ||
                 identity.EntrySequence != activeIdentity.EntrySequence))
            {
                throw new InvalidOperationException(
                    $"actor_reset_activity_context_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}' does not match endpoint activity context.");
            }

            if (!runtimeActor.RuntimeActorInstanceId.IsValid ||
                !string.Equals(runtimeActor.RuntimeActorInstanceId.Value, actor.ActorInstanceRuntimeId, StringComparison.Ordinal) ||
                !string.Equals(runtimeActor.ActorId, actor.ActorId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"actor_reset_actor_target_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' does not match resolved actor target.");
            }
        }

        public IReadOnlyList<IActorResetEndpoint> ResolveEndpointsOrFail(GameObject instance, SessionActivityIdentity activeIdentity, ActorResetActorRef actor)
        {
            if (instance == null)
            {
                throw new InvalidOperationException("actor_reset_actor_not_found: cannot resolve endpoints from null instance.");
            }

            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            List<IActorResetEndpoint> endpoints = new();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IActorResetEndpoint endpoint)
                {
                    endpoints.Add(endpoint);
                }
            }

            if (endpoints.Count == 0)
            {
                throw new InvalidOperationException(
                    $"actor_reset_endpoint_missing: actorId='{actor.ActorId}' actorRoot='{instance.name}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
            }

            return endpoints;
        }

        public ActorResetPlacementResolution ResolvePlacementFromMarker(Scene scopeScene, string placementId)
        {
            if (!scopeScene.IsValid() || !scopeScene.isLoaded)
            {
                throw new InvalidOperationException("invalid_required_placement: activity scene is invalid or not loaded.");
            }

            string normalizedId = Normalize(placementId);
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return ActorResetPlacementResolution.NotFound();
            }

            int matchCount = 0;
            Vector3 matchedPosition = Vector3.zero;
            Quaternion matchedRotation = Quaternion.identity;
            GameObject[] roots = scopeScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                PlayerActorPlacementMarker[] markers = root.GetComponentsInChildren<PlayerActorPlacementMarker>(true);
                for (int markerIndex = 0; markerIndex < markers.Length; markerIndex++)
                {
                    PlayerActorPlacementMarker marker = markers[markerIndex];
                    if (marker == null || !marker.IsValid)
                    {
                        continue;
                    }

                    if (!string.Equals(marker.PlacementId, normalizedId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matchCount += 1;
                    matchedPosition = marker.Position;
                    matchedRotation = marker.Rotation;
                    if (matchCount > 1)
                    {
                        return ActorResetPlacementResolution.Duplicate();
                    }
                }
            }

            return matchCount == 1
                ? ActorResetPlacementResolution.Found(matchedPosition, matchedRotation)
                : ActorResetPlacementResolution.NotFound();
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
