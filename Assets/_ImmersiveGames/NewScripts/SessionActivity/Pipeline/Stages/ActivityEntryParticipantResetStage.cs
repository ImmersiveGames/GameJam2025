using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Policies;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using UnityEngine;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;
using PlayerSessionParticipantId = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipantId;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryParticipantResetStage
    {
        public static void Execute(
            ActivityEntryCommand command,
            ActivityResetScopePlan resetScopePlan,
            ActivityEntryParticipantBindingResult participantBindingResult,
            ActorInventoryFeedResult actorInventoryFeed,
            ActivityCapabilityInventory capabilityInventoryPreview,
            IActorResetAdapter actorResetAdapter,
            ActivityPlayerActorRegistry playerActorRegistry,
            IReadOnlyList<SessionActivityActorMaterializationPlanEntry> actorMaterializationPlanEntries,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            IActivityEntryRuntimeBridge endpoint,
            ActivityEntryLogSink logSink,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCommand is invalid for participant reset execution.");
            }

            if (!resetScopePlan.IsValid)
            {
                throw new InvalidOperationException("ActivityResetScopePlan is invalid for participant reset execution.");
            }

            if (!resetScopePlan.Identity.CycleKey.Equals(command.Identity.CycleKey))
            {
                throw new InvalidOperationException(
                    $"ActivityEntryParticipantResetStage requires reset scope plan from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            if (!participantBindingResult.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantBindingResult is invalid for participant reset execution.");
            }

            if (!actorInventoryFeed.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantResetStage requires a valid actor inventory feed result.");
            }

            if (!actorInventoryFeed.Identity.CycleKey.Equals(command.Identity.CycleKey))
            {
                throw new InvalidOperationException(
                    $"ActivityEntryParticipantResetStage requires actor inventory feed from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            if (!capabilityInventoryPreview.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantResetStage requires a valid ActivityCapabilityInventory preview.");
            }

            if (!capabilityInventoryPreview.Id.Equals(ActivityCapabilityInventoryId.FromIdentity(command.Identity)))
            {
                throw new InvalidOperationException(
                    $"ActivityEntryParticipantResetStage requires ActivityCapabilityInventory preview from the current activity cycle. activityId='{command.ActivityId}'.");
            }

            if (actorResetAdapter == null)
            {
                throw new ArgumentNullException(nameof(actorResetAdapter));
            }

            if (playerActorRegistry == null)
            {
                throw new ArgumentNullException(nameof(playerActorRegistry));
            }

            if (placementMarkerLookup == null)
            {
                throw new ArgumentNullException(nameof(placementMarkerLookup));
            }

            SessionActivityIdentity identity = command.Identity;
            IReadOnlyList<ActivityEntryParticipantBindingResolvedRecord> resolvedParticipants = participantBindingResult.ResolvedParticipants;
            if (resolvedParticipants == null || resolvedParticipants.Count == 0)
            {
                logSink.LogEntryOwnerEvent(
                    "ActivityEntryParticipantResetCompleted",
                    identity,
                    command.Source,
                    command.Reason,
                    "owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' block='participant_reset' total='0' applied='0' skipped='0'");
                return;
            }

            int appliedCount = 0;
            int skippedCount = 0;

            for (int participantIndex = 0; participantIndex < resolvedParticipants.Count; participantIndex++)
            {
                ActivityEntryParticipantBindingResolvedRecord resolvedParticipant = resolvedParticipants[participantIndex];
                if (!resolvedParticipant.IsValid)
                {
                    continue;
                }

                PlayerActivityParticipantBinding participantBinding = resolvedParticipant.ParticipantBinding;
                if (!TryResolveActivePlayerActorHandleForParticipant(playerActorRegistry, participantBinding.ParticipantId, out PlayerActorRuntimeHandle actorHandle))
                {
                    throw new InvalidOperationException(
                        $"Activity participant '{participantBinding.ParticipantId}' is not available for operation='reset' activityId='{command.ActivityId}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}'.");
                }

                IReadOnlyList<ActorCapabilityResetEndpointReference> sourceResetReferences = ResolveParticipantResetReferencesFromInventory(
                    capabilityInventoryPreview,
                    actorHandle,
                    participantBinding);
                if (sourceResetReferences == null || sourceResetReferences.Count == 0)
                {
                    if (resolvedParticipant.Required)
                    {
                        throw new InvalidOperationException(
                            $"required_activity_participant_reset_endpoint_missing: activityId='{command.ActivityId}' participantId='{participantBinding.ParticipantId}' playerSlotId='{participantBinding.PlayerSlotId}' actorId='{participantBinding.ActorId}' requirementId='{resolvedParticipant.RequirementId}'.");
                    }

                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantResetApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant reset skipped requirementId='{resolvedParticipant.RequirementId}' participantId='{participantBinding.ParticipantId}' role='{participantBinding.Role}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}' placementRequirementId='<none>' resetDescriptor='<none>' descriptorMode='endpoint_inventory' appliedReferenceCount='0' skippedReferenceCount='0' adapterExecution='false' source='{command.Source}' reason='{command.Reason}' skipReason='reset_inventory_reference_missing'.");
                    DebugUtility.LogVerbose(
                        typeof(ActivityEntryParticipantResetStage),
                        $"event='ActivityParticipantResetSkippedFromInventory' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{resolvedParticipant.RequirementId}' actorId='{participantBinding.ActorId}' actorScope='{participantBinding.ActorScope}' playerSlotId='{participantBinding.PlayerSlotId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' reason='reset_inventory_reference_missing' owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    continue;
                }

                IReadOnlyList<ActorCapabilityResetEndpointReference> resetReferences = ActivityResetBoundaryPolicy.FilterActorResetReferencesByScopePlan(
                    resetScopePlan,
                    sourceResetReferences,
                    command.ActivityId,
                    resolvedParticipant.RequirementId);
                if (resetReferences.Count == 0)
                {
                    skippedCount += 1;
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.ActivityParticipantResetApplied,
                        identity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' participant reset skipped requirementId='{resolvedParticipant.RequirementId}' participantId='{participantBinding.ParticipantId}' role='{participantBinding.Role}' playerSlotId='{participantBinding.PlayerSlotId}' actorDefinitionId='{participantBinding.ActorDefinitionId}' actorId='{participantBinding.ActorId}' placementRequirementId='<none>' resetDescriptor='<none>' descriptorMode='endpoint_inventory' appliedReferenceCount='0' skippedReferenceCount='0' adapterExecution='false' source='{command.Source}' reason='{command.Reason}' skipReason='reset_references_filtered_by_boundary_policy' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' sourceReferenceCount='{sourceResetReferences.Count}' filteredReferenceCount='0'.");
                    DebugUtility.Log(
                        typeof(ActivityEntryParticipantResetStage),
                        $"event='ActivityParticipantResetSkippedFromInventory' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{resolvedParticipant.RequirementId}' actorId='{participantBinding.ActorId}' actorScope='{participantBinding.ActorScope}' playerSlotId='{participantBinding.PlayerSlotId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' reason='reset_references_filtered_by_boundary_policy' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' sourceReferenceCount='{sourceResetReferences.Count}' filteredReferenceCount='0' owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    continue;
                }

                SessionActivityActorMaterializationPlanEntry materializationPlanEntry =
                    ResolveMaterializationPlanEntryForActivityParticipantOrFail(
                        command.ActivityId,
                        participantBinding,
                        actorMaterializationPlanEntries,
                        "reset");

                string placementId = Normalize(materializationPlanEntry.PlacementId);
                ResolvePlacementPlanForEntry(
                    command.ActivityId,
                    identity,
                    placementMarkerLookup,
                    playerActorRegistry,
                    materializationPlanEntry,
                    placementId,
                    resolvedParticipant.RequirementId,
                    participantBinding,
                    "reset",
                    out bool placementDeclared,
                    out bool placementRequired,
                    out bool placementOptional,
                    out bool hasPlacement,
                    out Vector3 placementPosition,
                    out Vector3 placementEuler);

                ActivityParticipantResetCommand resolvedResetCommand = new(
                    identity,
                    resolvedParticipant.RequirementId,
                    participantBinding,
                    placementId,
                    actorHandle.ActorIdentity,
                    resetReferences,
                    resetScopePlan,
                    resolvedParticipant.Required,
                    placementDeclared,
                    placementRequired,
                    placementOptional,
                    hasPlacement,
                    placementPosition,
                    placementEuler,
                    command.Source,
                    command.Reason);

                if (!resolvedResetCommand.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Invalid participant reset command for participantId='{participantBinding.ParticipantId}' playerSlotId='{participantBinding.PlayerSlotId}' requirementId='{resolvedParticipant.RequirementId}' resetReferences='{resetReferences.Count}'.");
                }

                DebugUtility.LogVerbose(
                    typeof(ActivityEntryParticipantResetStage),
                    $"event='ActorResetInventoryReferencesResolved' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{resolvedResetCommand.RequirementId}' actorId='{resolvedResetCommand.ParticipantBinding.ActorId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' participantId='{resolvedResetCommand.ParticipantBinding.ParticipantId}' referenceCount='{resetReferences.Count}' providerTypes='{FormatResetReferenceProviderTypes(resetReferences)}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                IReadOnlyList<ActorResetResult> resetRecords = actorResetAdapter.Execute(resolvedResetCommand, identity);
                if (resetRecords.Count != 1 || !resetRecords[0].IsValid)
                {
                    throw new InvalidOperationException(
                        $"Invalid participant reset apply record for participantId='{participantBinding.ParticipantId}' playerSlotId='{participantBinding.PlayerSlotId}' requirementId='{resolvedParticipant.RequirementId}'.");
                }

                ValidateRequiredResetReferencesOrFail(resolvedResetCommand, resetRecords[0]);

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActivityParticipantResetApplied,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' participant reset applied from inventory requirementId='{resolvedResetCommand.RequirementId}' participantId='{resolvedResetCommand.ParticipantBinding.ParticipantId}' role='{resolvedResetCommand.ParticipantBinding.Role}' playerSlotId='{resolvedResetCommand.ParticipantBinding.PlayerSlotId}' actorDefinitionId='{resolvedResetCommand.ParticipantBinding.ActorDefinitionId}' actorId='{resolvedResetCommand.ParticipantBinding.ActorId}' placementRequirementId='{(string.IsNullOrWhiteSpace(resolvedResetCommand.PlacementRequirementId) ? "<none>" : resolvedResetCommand.PlacementRequirementId)}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' appliedReferenceCount='{resetRecords[0].AppliedReferenceCount}' skippedReferenceCount='{resetRecords[0].SkippedReferenceCount}' resetDescriptor='endpoint_inventory' descriptorMode='endpoint_inventory' adapterExecution='true' commandOwner='ActivityEntryPipeline' participantOwnership='ActivityParticipationContext' activityOwnership='true' inventoryReferenceCount='{resetReferences.Count}' sourceInventoryReferenceCount='{sourceResetReferences.Count}'.");
                DebugUtility.Log(
                    typeof(ActivityEntryParticipantResetStage),
                $"event='ActivityParticipantResetAppliedFromInventory' activityId='{command.ActivityId}' entrySequence='{identity.EntrySequence}' requirementId='{resolvedResetCommand.RequirementId}' actorId='{resolvedResetCommand.ParticipantBinding.ActorId}' actorScope='{resolvedResetCommand.ParticipantBinding.ActorScope}' playerSlotId='{resolvedResetCommand.ParticipantBinding.PlayerSlotId}' actorInstanceRuntimeId='{actorHandle.ActorInstanceRuntimeId}' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' appliedReferenceCount='{resetRecords[0].AppliedReferenceCount}' skippedReferenceCount='{resetRecords[0].SkippedReferenceCount}' resetDescriptor='endpoint_inventory' descriptorMode='endpoint_inventory' inventoryReferenceCount='{resetReferences.Count}' owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
                appliedCount += 1;
            }

            logSink.LogEntryOwnerEvent(
                "ActivityEntryParticipantResetCompleted",
                identity,
                command.Source,
                command.Reason,
                $"owner='ActivityEntryParticipantResetStage' entryPipelineOwner='ActivityEntryPipeline' block='participant_reset' resetIntent='{resetScopePlan.ResetIntent}' resetStateProfile='{resetScopePlan.StateProfileKind}' resetBoundaryKind='{resetScopePlan.BoundaryKind}' resetTargetScope='{resetScopePlan.TargetScope}' resetPolicyId='{resetScopePlan.PolicyId}' total='{resolvedParticipants.Count}' applied='{appliedCount}' skipped='{skippedCount}'");
        }

        private static bool TryResolveActivePlayerActorHandleForParticipant(
            ActivityPlayerActorRegistry playerActorRegistry,
            PlayerSessionParticipantId participantId,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (playerActorRegistry == null || !participantId.IsValid)
            {
                return false;
            }

            return playerActorRegistry.TryGetActiveHandleByParticipant(participantId, out handle) && handle.IsValid;
        }

        private static IReadOnlyList<ActorCapabilityResetEndpointReference> ResolveParticipantResetReferencesFromInventory(
            ActivityCapabilityInventory capabilityInventoryPreview,
            PlayerActorRuntimeHandle actorHandle,
            PlayerActivityParticipantBinding participantBinding)
        {
            if (!capabilityInventoryPreview.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryParticipantResetStage requires a valid ActivityCapabilityInventory preview for participant reset execution.");
            }

            if (!actorHandle.IsValid || !participantBinding.IsValid)
            {
                throw new InvalidOperationException("Cannot resolve participant reset references from invalid actor identity or participant binding.");
            }

            IReadOnlyList<ActorCapabilityResetEndpointReference> allReferences =
                capabilityInventoryPreview.GetRuntimeReferences<ActorCapabilityResetEndpointReference>();
            if (allReferences == null || allReferences.Count == 0)
            {
                return Array.Empty<ActorCapabilityResetEndpointReference>();
            }

            List<ActorCapabilityResetEndpointReference> resolved = new(allReferences.Count);
            for (int index = 0; index < allReferences.Count; index++)
            {
                ActorCapabilityResetEndpointReference reference = allReferences[index];
                if (reference == null || !reference.IsValid)
                {
                    continue;
                }

                if (reference.ActorId != actorHandle.ActorId ||
                    reference.ActorInstanceRuntimeId != actorHandle.ActorInstanceRuntimeId)
                {
                    continue;
                }

                resolved.Add(reference);
            }

            resolved.Sort(static (left, right) => string.Compare(left.CapabilityId, right.CapabilityId, StringComparison.Ordinal));
            return resolved;
        }

        private static string FormatResetReferenceProviderTypes(IReadOnlyList<ActorCapabilityResetEndpointReference> references)
        {
            if (references == null || references.Count == 0)
            {
                return "<none>";
            }

            SortedSet<string> providerTypes = new(StringComparer.Ordinal);
            for (int index = 0; index < references.Count; index++)
            {
                ActorCapabilityResetEndpointReference reference = references[index];
                if (reference == null || !reference.IsValid)
                {
                    continue;
                }

                string providerType = Normalize(reference.ProviderType);
                if (!string.IsNullOrWhiteSpace(providerType))
                {
                    providerTypes.Add(providerType);
                }
            }

            return providerTypes.Count == 0 ? "<none>" : string.Join(",", providerTypes);
        }

        private static SessionActivityActorMaterializationPlanEntry ResolveMaterializationPlanEntryForActivityParticipantOrFail(
            string activityId,
            PlayerActivityParticipantBinding participant,
            IReadOnlyList<SessionActivityActorMaterializationPlanEntry> materializationPlanEntries,
            string operation)
        {
            if (!participant.IsValid || !participant.ParticipantId.IsValid)
            {
                throw new InvalidOperationException(
                    $"missing_activity_participant_materialization_plan: activityId='{activityId}' operation='{operation}' reason='participant_binding_invalid'.");
            }

            if (materializationPlanEntries != null)
            {
                for (int index = 0; index < materializationPlanEntries.Count; index++)
                {
                    SessionActivityActorMaterializationPlanEntry entry = materializationPlanEntries[index];
                    if (entry.IsValid && entry.ParticipantId == participant.ParticipantId)
                    {
                        return entry;
                    }
                }
            }

            string actorDefinitionId = participant.ActorDefinitionId.IsValid ? Normalize(participant.ActorDefinitionId.Value) : string.Empty;
            string playerSlotId = participant.PlayerSlotId.IsValid ? Normalize(participant.PlayerSlotId.Value) : string.Empty;
            throw new InvalidOperationException(
                $"missing_activity_participant_materialization_plan: activityId='{activityId}' participantId='{participant.ParticipantId}' playerSlotId='{playerSlotId}' actorDefinitionId='{actorDefinitionId}' operation='{operation}' resolutionKey='SessionParticipantId'.");
        }

        private static void ResolvePlacementPlanForEntry(
            string activityId,
            SessionActivityIdentity identity,
            IActivityEntryPlacementMarkerLookup placementMarkerLookup,
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry,
            string placementId,
            string requirementId,
            PlayerActivityParticipantBinding participantBinding,
            string operation,
            out bool placementDeclared,
            out bool placementRequired,
            out bool placementOptional,
            out bool hasPlacement,
            out Vector3 placementPosition,
            out Vector3 placementEuler)
        {
            ResolvePlacementPlanFromDefinition(
                materializationPlanEntry,
                out placementDeclared,
                out placementRequired,
                out placementOptional,
                out hasPlacement,
                out placementPosition,
                out placementEuler);

            if (materializationPlanEntry.PlacementMode != ActorPlacementMode.SceneMarker)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(placementId))
            {
                if (placementRequired)
                {
                    throw new InvalidOperationException(
                        $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' operation='{operation}' reason='scene_marker_placement_id_missing'.");
                }

                return;
            }

            if (!placementMarkerLookup.TryResolvePlacementMarker(identity, placementId, out Vector3 markerPosition, out Vector3 markerEuler, out string resolutionReason))
            {
                if (placementRequired)
                {
                    throw new InvalidOperationException(
                        $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' placementId='{placementId}' operation='{operation}' reason='{resolutionReason}'.");
                }

                return;
            }

            hasPlacement = true;
            if (!playerActorRegistry.TryGetActiveHandleByParticipant(participantBinding.ParticipantId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid ||
                handle.Instance == null)
            {
                throw new InvalidOperationException(
                    $"invalid_required_placement: activityId='{activityId}' entrySequence='{identity.EntrySequence}' requirementId='{requirementId}' participantId='{participantBinding.ParticipantId}' actorId='{participantBinding.ActorId}' placementId='{placementId}' operation='{operation}' reason='actor_handle_missing_for_placement_space_resolution'.");
            }

            Transform parent = handle.Instance.transform.parent;
            placementPosition = parent == null ? markerPosition : parent.InverseTransformPoint(markerPosition);
            Quaternion markerRotation = Quaternion.Euler(markerEuler);
            Quaternion localRotation = parent == null ? markerRotation : Quaternion.Inverse(parent.rotation) * markerRotation;
            placementEuler = localRotation.eulerAngles;
        }

        private static void ResolvePlacementPlanFromDefinition(
            SessionActivityActorMaterializationPlanEntry materializationPlanEntry,
            out bool placementDeclared,
            out bool placementRequired,
            out bool placementOptional,
            out bool hasPlacement,
            out Vector3 placementPosition,
            out Vector3 placementEuler)
        {
            placementDeclared = materializationPlanEntry.PlacementMode != ActorPlacementMode.None;
            hasPlacement = materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform;
            placementRequired =
                materializationPlanEntry.PlacementMode == ActorPlacementMode.FixedTransform ||
                materializationPlanEntry.PlacementMode == ActorPlacementMode.SceneMarker;
            placementOptional = placementDeclared && !placementRequired;
            placementPosition = hasPlacement ? materializationPlanEntry.LocalPosition : Vector3.zero;
            placementEuler = hasPlacement ? materializationPlanEntry.LocalEulerAngles : Vector3.zero;
        }

        private static void ValidateRequiredResetReferencesOrFail(
            ActivityParticipantResetCommand resetCommand,
            ActorResetResult record)
        {
            if (record.SkippedReferenceCount == 0)
            {
                return;
            }

            if (record.SkippedReferenceReasons == null || record.SkippedReferenceReasons.Count == 0)
            {
                throw new InvalidOperationException(
                    $"required_reset_reference_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' skippedReferences='{record.SkippedReferenceCount}' reason='missing_skip_reason'.");
            }

            for (int index = 0; index < record.SkippedReferenceReasons.Count; index++)
            {
                ActorResetSkippedReferenceReason reason = record.SkippedReferenceReasons[index];
                if (!reason.IsValid)
                {
                    continue;
                }

                if (string.Equals(reason.ReasonCode, "optional_placement_missing", StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"required_reset_reference_failed: requirementId='{resetCommand.RequirementId}' participantId='{resetCommand.ParticipantBinding.ParticipantId}' playerSlotId='{resetCommand.ParticipantBinding.PlayerSlotId}' capabilityId='{reason.CapabilityId}' reason='{reason.ReasonCode}'.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
