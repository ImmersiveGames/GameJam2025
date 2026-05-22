using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public sealed class PlayerActorResetAdapter : IPlayerActorResetAdapter
    {
        public IReadOnlyList<PlayerActorResetAppliedRecord> Execute(
            PlayerActorResetCommand command,
            SessionActivityIdentity activeIdentity,
            ActivityPlayerActorRegistry registry)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("PlayerActorResetCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for player actor reset.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_reset_command: command identity does not match active identity.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerActorResetAdapter requires non-null registry.");
            }

            List<PlayerActorResetAppliedRecord> records = new(command.Plans.Count);
            for (int index = 0; index < command.Plans.Count; index++)
            {
                PlayerActorResetPlan plan = command.Plans[index];
                if (!plan.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActorResetPlan at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(plan.ActorIdentity.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_actor_reset_plan: plan identity does not match active identity.");
                }

                GameObject instance = registry.ResolveActiveInstanceOrFail(activeIdentity, plan.ActorIdentity.PlayerActorId);
                PlayerActorIdentity boundIdentity = instance.GetComponent<PlayerActorIdentity>();
                if (boundIdentity == null || !boundIdentity.IsValid)
                {
                    throw new InvalidOperationException($"PlayerActor identity component missing/invalid for reset. playerActorId='{plan.ActorIdentity.PlayerActorId}'.");
                }

                EnsureIdentityMatches(boundIdentity, activeIdentity, plan.ActorIdentity);
                EnsureDefaultResetEndpoint(instance);
                MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
                List<IPlayerActorResetEndpoint> endpoints = new();
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    if (behaviours[behaviourIndex] is IPlayerActorResetEndpoint endpoint)
                    {
                        endpoints.Add(endpoint);
                    }
                }

                if (endpoints.Count == 0)
                {
                    throw new InvalidOperationException($"PlayerActor reset endpoints are missing. playerActorId='{plan.ActorIdentity.PlayerActorId}'.");
                }

                List<PlayerActorResetGroup> appliedGroups = new();
                List<PlayerActorResetGroup> skippedGroups = new();
                List<PlayerActorResetSkippedGroupReason> skippedReasons = new();
                for (int groupIndex = 0; groupIndex < plan.Groups.Count; groupIndex++)
                {
                    PlayerActorResetGroup group = plan.Groups[groupIndex];
                    if (group == PlayerActorResetGroup.Unknown)
                    {
                        throw new InvalidOperationException($"Unknown reset group at index '{groupIndex}' for playerActorId='{plan.ActorIdentity.PlayerActorId}'.");
                    }

                    PlayerActorResetContext context = new(
                        activeIdentity,
                        plan.ActorIdentity,
                        group,
                        plan.PlacementId,
                        plan.HasPlacement,
                        plan.PlacementRequired,
                        plan.PlacementOptional,
                        plan.PlacementDeclared,
                        plan.PlacementPosition,
                        plan.PlacementEulerAngles,
                        command.Source,
                        command.Reason);

                    if (group == PlayerActorResetGroup.Placement)
                    {
                        if (plan.PlacementRequired && !plan.HasPlacement && string.IsNullOrWhiteSpace(plan.PlacementId))
                        {
                            throw new InvalidOperationException($"invalid_required_placement: playerActorId='{plan.ActorIdentity.PlayerActorId}'.");
                        }

                        if (!plan.PlacementDeclared)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new PlayerActorResetSkippedGroupReason(group, "no_placement_declared"));
                            continue;
                        }

                        if (plan.PlacementOptional && !plan.HasPlacement)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new PlayerActorResetSkippedGroupReason(group, "optional_placement_missing"));
                            continue;
                        }

                        if (plan.HasPlacement && !plan.PlacementRequired)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new PlayerActorResetSkippedGroupReason(group, "placement_not_required"));
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(plan.PlacementId))
                        {
                            PlacementResolution resolution = ResolvePlacementFromMarker(instance.scene, plan.PlacementId);
                            if (resolution.Status == PlacementResolutionStatus.Duplicate)
                            {
                                throw new InvalidOperationException(
                                    $"duplicate_placement_marker: playerActorId='{plan.ActorIdentity.PlayerActorId}' placementId='{plan.PlacementId}'.");
                            }

                            if (resolution.Status == PlacementResolutionStatus.NotFound)
                            {
                                if (plan.PlacementRequired)
                                {
                                    throw new InvalidOperationException(
                                        $"invalid_required_placement: playerActorId='{plan.ActorIdentity.PlayerActorId}' placementId='{plan.PlacementId}' reason='no_placement_marker_found'.");
                                }

                                skippedGroups.Add(group);
                                skippedReasons.Add(new PlayerActorResetSkippedGroupReason(group, "no_placement_marker_found"));
                                continue;
                            }

                            context = new PlayerActorResetContext(
                                activeIdentity,
                                plan.ActorIdentity,
                                group,
                                plan.PlacementId,
                                hasPlacement: true,
                                plan.PlacementRequired,
                                plan.PlacementOptional,
                                plan.PlacementDeclared,
                                ToLocalPosition(instance.transform.parent, resolution.Position),
                                ToLocalEulerAngles(instance.transform.parent, resolution.Rotation),
                                command.Source,
                                command.Reason);
                        }
                    }

                    bool applied = false;
                    for (int endpointIndex = 0; endpointIndex < endpoints.Count; endpointIndex++)
                    {
                        IPlayerActorResetEndpoint endpoint = endpoints[endpointIndex];
                        if (endpoint == null || !endpoint.Supports(group))
                        {
                            continue;
                        }

                        endpoint.ApplyReset(context);
                        applied = true;
                    }

                    if (applied)
                    {
                        appliedGroups.Add(group);
                    }
                    else
                    {
                        skippedGroups.Add(group);
                        skippedReasons.Add(new PlayerActorResetSkippedGroupReason(group, "no_endpoint_supports_group"));
                    }
                }

                records.Add(new PlayerActorResetAppliedRecord(plan.ActorIdentity, appliedGroups, skippedGroups, skippedReasons));
            }

            return records;
        }

        private static PlacementResolution ResolvePlacementFromMarker(Scene scopeScene, string placementId)
        {
            if (!scopeScene.IsValid() || !scopeScene.isLoaded)
            {
                throw new InvalidOperationException("invalid_required_placement: activity scene is invalid or not loaded.");
            }

            string normalizedId = Normalize(placementId);
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                return PlacementResolution.NotFound();
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
                        return PlacementResolution.Duplicate();
                    }
                }
            }

            return matchCount == 1
                ? PlacementResolution.Found(matchedPosition, matchedRotation)
                : PlacementResolution.NotFound();
        }

        private static Vector3 ToLocalPosition(Transform parent, Vector3 worldPosition)
        {
            return parent == null ? worldPosition : parent.InverseTransformPoint(worldPosition);
        }

        private static Vector3 ToLocalEulerAngles(Transform parent, Quaternion worldRotation)
        {
            Quaternion localRotation = parent == null
                ? worldRotation
                : Quaternion.Inverse(parent.rotation) * worldRotation;
            return localRotation.eulerAngles;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private enum PlacementResolutionStatus
        {
            Unknown = 0,
            Found = 1,
            NotFound = 2,
            Duplicate = 3,
        }

        private readonly struct PlacementResolution
        {
            public PlacementResolution(PlacementResolutionStatus status, Vector3 position, Quaternion rotation)
            {
                Status = status;
                Position = position;
                Rotation = rotation;
            }

            public PlacementResolutionStatus Status { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }

            public static PlacementResolution Found(Vector3 position, Quaternion rotation) =>
                new(PlacementResolutionStatus.Found, position, rotation);

            public static PlacementResolution NotFound() =>
                new(PlacementResolutionStatus.NotFound, Vector3.zero, Quaternion.identity);

            public static PlacementResolution Duplicate() =>
                new(PlacementResolutionStatus.Duplicate, Vector3.zero, Quaternion.identity);
        }

        private static void EnsureDefaultResetEndpoint(GameObject instance)
        {
            if (instance.GetComponent<PlayerActorDefaultResetEndpoint>() == null)
            {
                instance.AddComponent<PlayerActorDefaultResetEndpoint>();
            }
        }

        private static void EnsureIdentityMatches(
            PlayerActorIdentity identity,
            SessionActivityIdentity activeIdentity,
            PlayerActorIdentityRecord expected)
        {
            if (!string.Equals(identity.PipelineId, activeIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(identity.SessionId, activeIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(identity.ActivityId, activeIdentity.ActivityId, StringComparison.Ordinal) ||
                identity.ActivityOrdinal != activeIdentity.ActivityOrdinal ||
                identity.EntrySequence != activeIdentity.EntrySequence ||
                !string.Equals(identity.PlayerId, expected.PlayerId, StringComparison.Ordinal) ||
                !string.Equals(identity.PlayerActorId, expected.PlayerActorId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_player_actor_identity_binding: playerActorId='{expected.PlayerActorId}' does not match current pipeline identity.");
            }
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
    }
}
