using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryCameraBindingStage
    {
        public static ActivityEntryCameraBindingResult Execute(
            ActivityEntryCameraBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryCameraBindingRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCameraBindingCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.CameraBindingStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding stage started.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"[OBS][ActivityEntryPipeline][CameraBinding] event='CameraBindingStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ActivitySetupInventory inventory = bridge.GetCurrentActivitySetupInventory();
            IReadOnlyList<CameraBindingRequirement> cameraRequirements = inventory.CameraBindingRequirements ?? Array.Empty<CameraBindingRequirement>();
            bool hasValidCameraInventory = bridge.TryGetCurrentActivityCapabilityInventory(
                startedIdentity,
                out ActivityCapabilityInventory cameraInventory,
                out ActivityCapabilityInventoryValidationResult _);

            int requiredCameraCount = CountRequiredActivityCameraRequirements(cameraRequirements, out bool hasActivityCameraRequirement);

            if (!hasActivityCameraRequirement)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                endpoint.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                DebugUtility.Log(
                    typeof(ActivityEntryCameraBindingStage),
                    $"[OBS][ActivityEntryPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reasonCode='no_activity_camera_requirement' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "no_activity_camera_requirement");
            }

            if (!hasValidCameraInventory)
            {
                if (requiredCameraCount > 0)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    endpoint.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Missing valid camera inventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                endpoint.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                DebugUtility.Log(
                    typeof(ActivityEntryCameraBindingStage),
                    $"[OBS][ActivityEntryPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reasonCode='camera_inventory_missing_or_invalid' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "camera_inventory_missing_or_invalid");
            }

            if (!TryResolveCameraTargetReferenceFromInventory(cameraInventory, cameraRequirements, startedIdentity, bridge, out ActivityCameraTargetReference selectedCameraTarget))
            {
                if (requiredCameraCount > 0)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_target_missing_in_inventory'.");
                    endpoint.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='camera_target_missing_in_inventory'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Missing camera target reference activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingSkippedNoRequiredCamera, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_target_missing_in_inventory'.");
                endpoint.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding skipped reason='camera_target_missing_in_inventory'.");
                DebugUtility.Log(
                    typeof(ActivityEntryCameraBindingStage),
                    $"[OBS][ActivityEntryPipeline][CameraBinding] event='CameraBindingSkippedNoRequiredCamera' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reasonCode='camera_target_missing_in_inventory' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "camera_target_missing_in_inventory");
            }

            endpoint.EmitFact(facts, SessionActivityFactKind.PlayerCameraEndpointResolved, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera endpoint resolved playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}'.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"[OBS][ActivityEntryPipeline][CameraBinding] event='PlayerCameraEndpointResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' capabilityId='{selectedCameraTarget.CapabilityId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!bridge.TryGetActivityCameraPreparationExecutor(out IActivityCameraPreparationExecutor cameraExecutor) || cameraExecutor == null)
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='activity_camera_preparation_executor_missing'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] IActivityCameraPreparationExecutor missing activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            ActivityCameraRebindTargetsCommand rebindCommand = new(startedIdentity.SessionId, selectedCameraTarget.TrackingTarget, selectedCameraTarget.LookAtTarget, command.Source, command.Reason);
            if (!cameraExecutor.TryRebindTargets(rebindCommand, out ActivityCameraRebindTargetsResult rebindResult, out string rebindReason) || rebindResult is not { Success: true })
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                endpoint.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Rebind failed activityId='{definition.ActivityId}' entrySequence='{entrySequence}' reason='{Normalize(rebindReason)}'.");
            }

            endpoint.EmitFact(facts, SessionActivityFactKind.ActivityCameraTargetBound, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activity camera target bound playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}'.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"[OBS][ActivityEntryPipeline][CameraBinding] event='ActivityCameraTargetBound' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' playerSlotId='{selectedCameraTarget.PlayerSlotId}' playerActorId='{selectedCameraTarget.PlayerActorId}' followTarget='{selectedCameraTarget.TrackingTarget.name}' lookAtTarget='{selectedCameraTarget.LookAtTarget?.name ?? "<none>"}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.CameraBindingCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.CameraBindingCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.CameraBindingCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' camera binding completed.");
            endpoint.EmitSnapshot(snapshots, "camera_binding_completed", command.Source, command.Reason, $"'{definition.ActivityId}' camera binding completed.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"[OBS][ActivityEntryPipeline][CameraBinding] event='CameraBindingCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActivityEntryCameraBindingResult(true, completedIdentity, requiredCameraCount, true, false, "camera_binding_completed");
        }

        private static bool TryResolveCameraTargetReferenceFromInventory(
            ActivityCapabilityInventory inventory,
            IReadOnlyList<CameraBindingRequirement> cameraRequirements,
            SessionActivityIdentity activeIdentity,
            IActivityEntryCameraBindingRuntimeBridge bridge,
            out ActivityCameraTargetReference selectedCameraTarget)
        {
            selectedCameraTarget = null;
            if (!inventory.IsValid || !activeIdentity.IsValid || !HasActivityCameraRequirement(cameraRequirements))
            {
                return false;
            }

            IReadOnlyList<PlayerActivityParticipantBinding> participants = bridge.GetActivityParticipantBindings();
            if (participants == null || participants.Count == 0)
            {
                return false;
            }

            for (int participantIndex = 0; participantIndex < participants.Count; participantIndex++)
            {
                PlayerActivityParticipantBinding binding = participants[participantIndex];
                if (!binding.IsValid || !binding.RequiresPlayerActor)
                {
                    continue;
                }

                if (!bridge.TryResolvePlayerActorHandle(activeIdentity, binding, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
                {
                    continue;
                }

                if (TryResolveCameraTargetReferenceForHandle(inventory, handle, out selectedCameraTarget))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveCameraTargetReferenceForHandle(
            ActivityCapabilityInventory inventory,
            PlayerActorRuntimeHandle handle,
            out ActivityCameraTargetReference selectedCameraTarget)
        {
            selectedCameraTarget = null;
            if (!inventory.IsValid || !handle.IsValid)
            {
                return false;
            }

            for (int capabilityIndex = 0; capabilityIndex < inventory.Capabilities.Count; capabilityIndex++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[capabilityIndex];
                if (capability.CapabilityKind != ActivityCapabilityKind.CameraTarget)
                {
                    continue;
                }

                if (!inventory.TryGetRuntimeReference(capability.CapabilityId, out ActivityCameraTargetReference reference) ||
                    reference is not { IsValid: true } ||
                    !reference.Matches(handle))
                {
                    continue;
                }

                selectedCameraTarget = reference;
                return true;
            }

            return false;
        }

        private static int CountRequiredActivityCameraRequirements(IReadOnlyList<CameraBindingRequirement> cameraRequirements, out bool hasActivityCameraRequirement)
        {
            hasActivityCameraRequirement = false;
            if (cameraRequirements == null || cameraRequirements.Count == 0)
            {
                return 0;
            }

            int required = 0;
            for (int index = 0; index < cameraRequirements.Count; index++)
            {
                CameraBindingRequirement requirement = cameraRequirements[index];
                if (!requirement.IsValid || requirement.CameraBindingKind != ActivityCameraBindingRequirementKind.ActivityCamera)
                {
                    continue;
                }

                hasActivityCameraRequirement = true;
                if (requirement.Requirement.IsRequired)
                {
                    required += 1;
                }
            }

            return required;
        }

        private static bool HasActivityCameraRequirement(IReadOnlyList<CameraBindingRequirement> cameraRequirements)
        {
            _ = CountRequiredActivityCameraRequirements(cameraRequirements, out bool hasActivityCameraRequirement);
            return hasActivityCameraRequirement;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
