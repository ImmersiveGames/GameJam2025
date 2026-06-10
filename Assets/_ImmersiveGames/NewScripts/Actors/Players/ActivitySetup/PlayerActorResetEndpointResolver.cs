using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerActorResetEndpointResolver : IActorResetEndpointResolver
    {
        private readonly ActivityPlayerActorRegistry _registry;
        private readonly SessionActorRuntimeStore _sessionActorRuntimeStore;

        public PlayerActorResetEndpointResolver(ActivityPlayerActorRegistry registry, SessionActorRuntimeStore sessionActorRuntimeStore)
        {
            _registry = registry ?? throw new InvalidOperationException("PlayerActorResetEndpointResolver requires non-null registry.");
            _sessionActorRuntimeStore = sessionActorRuntimeStore ?? throw new InvalidOperationException("PlayerActorResetEndpointResolver requires non-null session actor runtime store.");
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

            if ((_registry.TryGetActiveHandleByActorInstance(actor.ActorInstanceRuntimeId, out PlayerActorRuntimeHandle handle) ||
                _registry.TryGetRouteScopedHandleByActorInstance(actor.ActorInstanceRuntimeId, out handle)) &&
                handle.IsValid &&
                handle.Instance != null)
            {
                return handle.Instance;
            }

            if (_sessionActorRuntimeStore.TryGetByRuntimeId(activeIdentity, actor.ActorInstanceRuntimeId, out SessionActorRuntimeEntry stored) &&
                stored.IsValid &&
                stored.Instance != null)
            {
                return stored.Instance;
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
                !string.Equals(identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"actor_reset_player_identity_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' pipelineId='{activeIdentity.PipelineId}' sessionId='{activeIdentity.SessionId}' does not match endpoint identity.");
            }

            bool isRetainedAcrossActivity = ActorLifetimePolicyRuntime.IsRetainedAcrossActivity(runtimeActor.ActorScopeMetadata);
            if (!isRetainedAcrossActivity &&
                (!string.Equals(identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) ||
                 identity.ActivityOrdinal != activeIdentity.ActivityOrdinal ||
                 identity.EntrySequence != activeIdentity.EntrySequence))
            {
                throw new InvalidOperationException(
                    $"actor_reset_activity_context_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}' does not match endpoint activity context.");
            }

            if (!runtimeActor.RuntimeActorInstanceId.IsValid ||
                runtimeActor.RuntimeActorInstanceId != actor.ActorInstanceRuntimeId ||
                new ActorId(runtimeActor.ActorId) != actor.ActorId)
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

            Actor runtimeActor = instance.GetComponent<Actor>();
            if (runtimeActor == null)
            {
                throw new InvalidOperationException(
                    $"actor_reset_actor_target_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' reason='runtime_actor_missing'.");
            }

            ActorCapabilitySurface surface = runtimeActor.CapabilitySurface;
            if (surface == null)
            {
                throw new InvalidOperationException(
                    $"actor_reset_capability_surface_missing: actorId='{actor.ActorId}' actorRoot='{instance.name}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
            }

            surface.RefreshFromLocalActorRoot();
            IReadOnlyList<IActorResetContributionProvider> providers = surface.ResetContributionProviders;
            if (providers == null || providers.Count == 0)
            {
                throw new InvalidOperationException(
                    $"actor_reset_contribution_provider_missing: actorId='{actor.ActorId}' actorRoot='{instance.name}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}'.");
            }

            List<IActorResetEndpoint> endpoints = new();
            HashSet<IActorResetEndpoint> indexedEndpoints = new();
            for (int index = 0; index < providers.Count; index++)
            {
                IActorResetContributionProvider provider = providers[index];
                if (provider == null)
                {
                    continue;
                }

                ActorCapabilityContributionContext context = BuildContributionContext(
                    activeIdentity,
                    actor,
                    runtimeActor,
                    provider,
                    source: nameof(PlayerActorResetEndpointResolver),
                    reason: "resolve_reset_lifecycle_contributions");

                if (!provider.TryCreateResetContribution(context, out IActorResetContribution contribution))
                {
                    continue;
                }

                if (contribution == null || !contribution.IsValid)
                {
                    throw new InvalidOperationException(
                        $"actor_reset_contribution_invalid: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}'.");
                }

                ActorResetGroup[] supportedGroups = contribution.SupportedGroups;
                if (supportedGroups == null || supportedGroups.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"actor_reset_contribution_without_groups: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}'.");
                }

                if (provider is not IActorResetEndpoint endpoint)
                {
                    throw new InvalidOperationException(
                        $"actor_reset_contribution_not_executable: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}' reason='provider_does_not_implement_IActorResetEndpoint'.");
                }

                ValidateEndpointSupportsContributionOrFail(endpoint, contribution, actor, provider);
                if (indexedEndpoints.Add(endpoint))
                {
                    endpoints.Add(endpoint);
                }
            }

            if (endpoints.Count == 0)
            {
                throw new InvalidOperationException(
                    $"actor_reset_endpoint_missing: actorId='{actor.ActorId}' actorRoot='{instance.name}' activityId='{activeIdentity.ActivityId}' entrySequence='{activeIdentity.EntrySequence}' reason='no_reset_contribution_provider_produced_executable_endpoint'.");
            }

            return endpoints;
        }

        private static ActorCapabilityContributionContext BuildContributionContext(
            SessionActivityIdentity activeIdentity,
            ActorResetActorRef actor,
            Actor runtimeActor,
            IActorResetContributionProvider provider,
            string source,
            string reason)
        {
            string componentPath = provider is Component component
                ? BuildTransformPath(component.transform)
                : BuildTransformPath(runtimeActor != null ? runtimeActor.transform : null);

            ActorRole actorRole = runtimeActor != null ? runtimeActor.ActorRoleMetadata : ActorRole.Unknown;
            ActorScope actorScope = runtimeActor != null ? runtimeActor.ActorScopeMetadata : ActorScope.Unknown;
            return new ActorCapabilityContributionContext(
                activeIdentity,
                actor.ActorId,
                actor.ActorInstanceRuntimeId,
                actor.ActorKind,
                actorRole,
                actorScope,
                componentPath,
                source,
                reason);
        }

        private static void ValidateEndpointSupportsContributionOrFail(
            IActorResetEndpoint endpoint,
            IActorResetContribution contribution,
            ActorResetActorRef actor,
            IActorResetContributionProvider provider)
        {
            ActorResetGroup[] supportedGroups = contribution.SupportedGroups;
            for (int index = 0; index < supportedGroups.Length; index++)
            {
                ActorResetGroup group = supportedGroups[index];
                if (group == ActorResetGroup.Unknown)
                {
                    throw new InvalidOperationException(
                        $"actor_reset_contribution_unknown_group: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}'.");
                }

                if (!endpoint.Supports(group))
                {
                    throw new InvalidOperationException(
                        $"actor_reset_contribution_endpoint_mismatch: actorId='{actor.ActorId}' actorInstanceRuntimeId='{actor.ActorInstanceRuntimeId}' provider='{provider.GetType().Name}' group='{group}' reason='endpoint_does_not_support_declared_group'.");
                }
            }
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string parentPath = BuildTransformPath(transform.parent);
            return string.IsNullOrWhiteSpace(parentPath)
                ? transform.name
                : $"{parentPath}/{transform.name}";
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
