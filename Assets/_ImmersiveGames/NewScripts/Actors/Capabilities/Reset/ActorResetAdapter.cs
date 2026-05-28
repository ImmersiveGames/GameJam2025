using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public sealed class ActorResetAdapter : IActorResetAdapter
    {
        private readonly IActorResetEndpointResolver _resolver;

        public ActorResetAdapter(IActorResetEndpointResolver resolver)
        {
            _resolver = resolver ?? throw new InvalidOperationException("ActorResetAdapter requires non-null endpoint resolver.");
        }

        public IReadOnlyList<ActorResetResult> Execute(
            ActorResetCommand command,
            SessionActivityIdentity activeIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActorResetCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for player actor reset.");
            }

            if (!IsSameActivityCycle(command.PipelineIdentity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_reset_command: command identity does not match active identity.");
            }

            List<ActorResetResult> records = new(command.Targets.Count);
            for (int index = 0; index < command.Targets.Count; index++)
            {
                ActorResetTargetRef target = command.Targets[index];
                if (!target.IsValid)
                {
                    throw new InvalidOperationException($"ActorResetTargetRef at index '{index}' is invalid.");
                }

                if (!IsSameActivityCycle(target.Actor.Identity, activeIdentity))
                {
                    throw new InvalidOperationException("stale_or_foreign_player_actor_reset_plan: plan identity does not match active identity.");
                }

                GameObject instance = _resolver.ResolveOrFail(activeIdentity, target.Actor);
                _resolver.EnsureIdentityMatchesOrFail(instance, activeIdentity, target.Actor);
                IReadOnlyList<IActorResetEndpoint> endpoints = _resolver.ResolveEndpointsOrFail(instance, activeIdentity, target.Actor);

                List<ActorResetGroup> appliedGroups = new();
                List<ActorResetGroup> skippedGroups = new();
                List<ActorResetSkippedGroupReason> skippedReasons = new();
                for (int groupIndex = 0; groupIndex < target.Groups.Count; groupIndex++)
                {
                    ActorResetGroup group = target.Groups[groupIndex];
                    if (group == ActorResetGroup.Unknown)
                    {
                        throw new InvalidOperationException($"Unknown reset group at index '{groupIndex}' for actorId='{target.Actor.ActorId}'.");
                    }

                    ActorResetContext context = new(
                        activeIdentity,
                        target.Actor,
                        group,
                        target.PlacementId,
                        target.HasPlacement,
                        target.PlacementRequired,
                        target.PlacementOptional,
                        target.PlacementDeclared,
                        target.PlacementPosition,
                        target.PlacementEulerAngles,
                        command.Source,
                        command.Reason);

                    if (group == ActorResetGroup.Placement)
                    {
                        if (target.PlacementRequired && !target.HasPlacement && string.IsNullOrWhiteSpace(target.PlacementId))
                        {
                            throw new InvalidOperationException($"invalid_required_placement: actorId='{target.Actor.ActorId}'.");
                        }

                        if (!target.PlacementDeclared)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new ActorResetSkippedGroupReason(group, "no_placement_declared"));
                            continue;
                        }

                        if (target.PlacementOptional && !target.HasPlacement)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new ActorResetSkippedGroupReason(group, "optional_placement_missing"));
                            continue;
                        }

                        if (target.HasPlacement && !target.PlacementRequired)
                        {
                            skippedGroups.Add(group);
                            skippedReasons.Add(new ActorResetSkippedGroupReason(group, "placement_not_required"));
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(target.PlacementId))
                        {
                            ActorResetPlacementResolution resolution = _resolver.ResolvePlacementFromMarker(instance.scene, target.PlacementId);
                            if (resolution.Status == ActorResetPlacementResolutionStatus.Duplicate)
                            {
                                throw new InvalidOperationException(
                                    $"duplicate_placement_marker: actorId='{target.Actor.ActorId}' placementId='{target.PlacementId}'.");
                            }

                            if (resolution.Status == ActorResetPlacementResolutionStatus.NotFound)
                            {
                                if (target.PlacementRequired)
                                {
                                    throw new InvalidOperationException(
                                        $"invalid_required_placement: actorId='{target.Actor.ActorId}' placementId='{target.PlacementId}' reason='no_placement_marker_found'.");
                                }

                                skippedGroups.Add(group);
                                skippedReasons.Add(new ActorResetSkippedGroupReason(group, "no_placement_marker_found"));
                                continue;
                            }

                            context = new ActorResetContext(
                                activeIdentity,
                                target.Actor,
                                group,
                                target.PlacementId,
                                hasPlacement: true,
                                target.PlacementRequired,
                                target.PlacementOptional,
                                target.PlacementDeclared,
                                ToLocalPosition(instance.transform.parent, resolution.Position),
                                ToLocalEulerAngles(instance.transform.parent, resolution.Rotation),
                                command.Source,
                                command.Reason);
                        }
                    }

                    bool applied = false;
                    for (int endpointIndex = 0; endpointIndex < endpoints.Count; endpointIndex++)
                    {
                        IActorResetEndpoint endpoint = endpoints[endpointIndex];
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
                        skippedReasons.Add(new ActorResetSkippedGroupReason(group, "no_endpoint_supports_group"));
                    }
                }

                records.Add(new ActorResetResult(target.Actor, appliedGroups, skippedGroups, skippedReasons));
            }

            return records;
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

    public interface IActorResetEndpointResolver
    {
        GameObject ResolveOrFail(SessionActivityIdentity activeIdentity, ActorResetActorRef actor);
        void EnsureIdentityMatchesOrFail(GameObject instance, SessionActivityIdentity activeIdentity, ActorResetActorRef actor);
        IReadOnlyList<IActorResetEndpoint> ResolveEndpointsOrFail(GameObject instance, SessionActivityIdentity activeIdentity, ActorResetActorRef actor);
        ActorResetPlacementResolution ResolvePlacementFromMarker(Scene scopeScene, string placementId);
    }

    public enum ActorResetPlacementResolutionStatus
    {
        Unknown = 0,
        Found = 1,
        NotFound = 2,
        Duplicate = 3,
    }

    public readonly struct ActorResetPlacementResolution
    {
        public ActorResetPlacementResolution(ActorResetPlacementResolutionStatus status, Vector3 position, Quaternion rotation)
        {
            Status = status;
            Position = position;
            Rotation = rotation;
        }

        public ActorResetPlacementResolutionStatus Status { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public static ActorResetPlacementResolution Found(Vector3 position, Quaternion rotation) =>
            new(ActorResetPlacementResolutionStatus.Found, position, rotation);

        public static ActorResetPlacementResolution NotFound() =>
            new(ActorResetPlacementResolutionStatus.NotFound, Vector3.zero, Quaternion.identity);

        public static ActorResetPlacementResolution Duplicate() =>
            new(ActorResetPlacementResolutionStatus.Duplicate, Vector3.zero, Quaternion.identity);
    }
}
