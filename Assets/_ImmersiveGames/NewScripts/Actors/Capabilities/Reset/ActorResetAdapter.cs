using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Capabilities.Reset
{
    public sealed class ActorResetAdapter : IActorResetAdapter
    {
        public ActorResetAdapter()
        {
        }

        public IReadOnlyList<ActorResetResult> Execute(
            ActivityParticipantResetCommand command,
            SessionActivityIdentity activeIdentity)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityParticipantResetCommand is invalid.");
            }

            if (!activeIdentity.IsValid)
            {
                throw new InvalidOperationException("Active identity is invalid for participant actor reset.");
            }

            if (!IsSameActivityCycle(command.Identity, activeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_participant_actor_reset_command: command identity does not match active identity.");
            }

            ActorId actorId = command.ActorIdentity.ActorId;
            ActorKind actorKind = ActorKind.Player;
            List<ActorResetGroup> appliedGroups = new();
            List<ActorResetGroup> skippedGroups = new();
            List<ActorResetSkippedGroupReason> skippedReasons = new();
            HashSet<ActorResetGroup> appliedGroupSet = new();
            HashSet<ActorResetGroup> skippedGroupSet = new();
            ActorInstanceRuntimeId expectedActorInstanceRuntimeId = default;
            ActorInstanceRuntimeId actorInstanceRuntimeId = default;
            bool actorCaptured = false;

            for (int referenceIndex = 0; referenceIndex < command.ResetReferences.Count; referenceIndex++)
            {
                ActorCapabilityResetEndpointReference resetReference = command.ResetReferences[referenceIndex];
                if (resetReference == null || !resetReference.IsValid)
                {
                    throw new InvalidOperationException($"Invalid actor reset inventory reference at index '{referenceIndex}' requirementId='{command.RequirementId}'.");
                }

                if (!actorCaptured)
                {
                    expectedActorInstanceRuntimeId = resetReference.ActorInstanceRuntimeId;
                    actorInstanceRuntimeId = resetReference.ActorInstanceRuntimeId;
                    actorCaptured = true;
                }

                if (resetReference.ActorId != command.ActorIdentity.ActorId)
                {
                    throw new InvalidOperationException(
                        $"stale_or_foreign_actor_reset_inventory_reference: capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' expectedActorId='{command.ActorIdentity.ActorId}'.");
                }

                if (resetReference.ActorInstanceRuntimeId != expectedActorInstanceRuntimeId)
                {
                    throw new InvalidOperationException(
                        $"stale_or_foreign_actor_reset_inventory_reference: capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' actorInstanceRuntimeId='{resetReference.ActorInstanceRuntimeId}' expectedActorInstanceRuntimeId='{expectedActorInstanceRuntimeId}'.");
                }

                Debug.Log(
                    $"[OBS][ActorResetAdapter] event='ActorResetInventoryReferencesResolved' capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' playerActorId='{command.ActorIdentity.PlayerActorId}' playerSlotId='{command.ActorIdentity.PlayerSlotId}' supportedGroups='{FormatActorResetGroups(resetReference.SupportedGroups)}' required='{command.Required}' source='{command.Source}' reason='{command.Reason}'.");

                IReadOnlyList<ActorResetGroup> supportedGroups = resetReference.SupportedGroups;
                for (int groupIndex = 0; groupIndex < supportedGroups.Count; groupIndex++)
                {
                    ActorResetGroup group = supportedGroups[groupIndex];
                    if (group == ActorResetGroup.Unknown)
                    {
                        throw new InvalidOperationException($"Unknown reset group at index '{groupIndex}' for capabilityId='{resetReference.CapabilityId}'.");
                    }

                    ActorResetContext context = new(
                        activeIdentity,
                        actorId,
                        actorInstanceRuntimeId,
                        actorKind,
                        group,
                        command.PlacementRequirementId,
                        command.HasPlacement,
                        command.PlacementRequired,
                        command.PlacementOptional,
                        command.PlacementDeclared,
                        command.PlacementPosition,
                        command.PlacementEulerAngles,
                        command.Source,
                        command.Reason);

                    if (group == ActorResetGroup.Placement)
                    {
                        if (command.PlacementRequired && !command.HasPlacement && string.IsNullOrWhiteSpace(command.PlacementRequirementId))
                        {
                            throw new InvalidOperationException($"invalid_required_placement: actorId='{command.ActorIdentity.ActorId}'.");
                        }

                        if (!command.PlacementDeclared)
                        {
                            if (skippedGroupSet.Add(group))
                            {
                                skippedGroups.Add(group);
                                skippedReasons.Add(new ActorResetSkippedGroupReason(group, "no_placement_declared"));
                            }

                            continue;
                        }

                        if (command.PlacementOptional && !command.HasPlacement)
                        {
                            if (skippedGroupSet.Add(group))
                            {
                                skippedGroups.Add(group);
                                skippedReasons.Add(new ActorResetSkippedGroupReason(group, "optional_placement_missing"));
                            }

                            continue;
                        }

                        if (command.HasPlacement && !command.PlacementRequired)
                        {
                            if (skippedGroupSet.Add(group))
                            {
                                skippedGroups.Add(group);
                                skippedReasons.Add(new ActorResetSkippedGroupReason(group, "placement_not_required"));
                            }

                            continue;
                        }
                    }

                    resetReference.Endpoint.ApplyReset(context);
                    Debug.Log(
                        $"[OBS][ActorResetAdapter] event='ActorResetEndpointAppliedFromInventory' capabilityId='{resetReference.CapabilityId}' actorId='{resetReference.ActorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' playerActorId='{command.ActorIdentity.PlayerActorId}' playerSlotId='{command.ActorIdentity.PlayerSlotId}' group='{group}' source='{command.Source}' reason='{command.Reason}'.");

                    if (appliedGroupSet.Add(group))
                    {
                        appliedGroups.Add(group);
                    }
                }
            }

            if (!actorCaptured)
            {
                throw new InvalidOperationException("ActivityParticipantResetCommand did not resolve any actor reset reference.");
            }

            return new[]
            {
                new ActorResetResult(actorId, actorInstanceRuntimeId, actorKind, appliedGroups, skippedGroups, skippedReasons),
            };
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

        private static string FormatActorResetGroups(IReadOnlyList<ActorResetGroup> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", groups);
        }
    }
}
