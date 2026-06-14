using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Camera;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryCameraBindingStage
    {
        public static ActivityEntryCameraBindingResult Execute(
            ActivityEntryCameraBindingCommand command,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            IActivityCameraPreparationExecutor cameraExecutor,
            ActivitySetupInventory inventory,
            ActivityCapabilityInventory cameraInventory,
            IReadOnlyList<ActorCameraBindingContribution> cameraBindingContributions,
            IReadOnlyList<PlayerActivityParticipantBinding> participantBindings,
            IActivityEntryCameraBindingRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryCameraBindingCommand is invalid.");
            }

            identityBridge = identityBridge ?? throw new ArgumentNullException(nameof(identityBridge));
            factBridge = factBridge ?? throw new ArgumentNullException(nameof(factBridge));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            cameraExecutor = cameraExecutor ?? throw new ArgumentNullException(nameof(cameraExecutor));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            IReadOnlyList<ActorCameraBindingContribution> bindingContributions = cameraBindingContributions ?? Array.Empty<ActorCameraBindingContribution>();

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingStarted);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.CameraBindingStarted);
            factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingStarted, startedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding stage started.");
            IReadOnlyList<CameraBindingRequirement> cameraRequirements = inventory.CameraBindingRequirements ?? Array.Empty<CameraBindingRequirement>();
            bool hasValidCameraInventory =
                startedIdentity.IsValid &&
                cameraInventory.IsValid &&
                string.Equals(cameraInventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) &&
                string.Equals(cameraInventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) &&
                string.Equals(cameraInventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) &&
                cameraInventory.Id.EntrySequence == startedIdentity.EntrySequence;

            int requiredCameraCount = CountRequiredActivityCameraRequirements(cameraRequirements, out bool hasActivityCameraRequirement);

            if (!hasActivityCameraRequirement)
            {
                var skippedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                factBridge.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='no_activity_camera_requirement'.");
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryCameraBindingStage),
                    $"event='CameraBindingSkippedNoRequiredCamera' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' reasonCode='no_activity_camera_requirement' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "no_activity_camera_requirement");
            }

            if (!hasValidCameraInventory)
            {
                if (requiredCameraCount > 0)
                {
                    var failedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingFailed);
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    factBridge.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='camera_inventory_missing_or_invalid'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Missing valid camera inventory activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                var skippedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                factBridge.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='camera_inventory_missing_or_invalid'.");
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryCameraBindingStage),
                    $"event='CameraBindingSkippedNoRequiredCamera' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' reasonCode='camera_inventory_missing_or_invalid' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "camera_inventory_missing_or_invalid");
            }

            if (!TryResolveCameraBindingContribution(bindingContributions, startedIdentity, bridge, participantBindings, out var selectedCameraBinding))
            {
                if (requiredCameraCount > 0)
                {
                    var failedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingFailed);
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                    factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='camera_target_missing_in_binding_contributions'.");
                    factBridge.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='camera_target_missing_in_binding_contributions'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Missing camera binding contribution activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
                }

                var skippedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.CameraBindingSkippedNoRequiredCamera);
                factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingSkippedNoRequiredCamera, skippedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='camera_target_missing_in_binding_contributions'.");
                factBridge.EmitSnapshot(snapshots, "camera_binding_skipped_no_required_camera", command.Source, command.Reason, $"'{command.ActivityId}' camera binding skipped reason='camera_target_missing_in_binding_contributions'.");
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryCameraBindingStage),
                    $"event='CameraBindingSkippedNoRequiredCamera' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' reasonCode='camera_target_missing_in_binding_contributions' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityEntryCameraBindingResult(true, skippedIdentity, requiredCameraCount, false, true, "camera_target_missing_in_binding_contributions");
            }

            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.PlayerCameraEndpointResolved,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' camera endpoint resolved playerSlotId='{selectedCameraBinding.PlayerSlotId}' playerActorId='{selectedCameraBinding.PlayerActorId}' actorId='{selectedCameraBinding.ActorId}' followTarget='{selectedCameraBinding.Endpoint.FollowTarget.name}' lookAtTarget='{selectedCameraBinding.Endpoint.LookAtTarget?.name ?? "<none>"}' mode='BindingContributions'.");
            DebugUtility.LogVerbose(
                typeof(ActivityEntryCameraBindingStage),
                $"event='PlayerCameraEndpointResolved' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' playerSlotId='{selectedCameraBinding.PlayerSlotId}' playerActorId='{selectedCameraBinding.PlayerActorId}' actorId='{selectedCameraBinding.ActorId}' source='{command.Source}' reason='{command.Reason}' mode='BindingContributions'.",
                DebugUtility.Colors.Info);

            ActivityCameraRebindTargetsCommand rebindCommand = new(startedIdentity.SessionId, selectedCameraBinding.Endpoint.FollowTarget, selectedCameraBinding.Endpoint.LookAtTarget, command.Source, command.Reason);
            if (!cameraExecutor.TryRebindTargets(rebindCommand, out var rebindResult, out string rebindReason) || rebindResult is not { Success: true })
            {
                var failedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingFailed);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.CameraBindingFailed);
                factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                factBridge.EmitSnapshot(snapshots, "camera_binding_failed", command.Source, command.Reason, $"'{command.ActivityId}' camera binding failed reason='{Normalize(rebindReason)}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][CameraBinding] Rebind failed activityId='{command.ActivityId}' entrySequence='{entrySequence}' reason='{Normalize(rebindReason)}'.");
            }

            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.ActivityCameraTargetBound,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity camera target bound playerSlotId='{selectedCameraBinding.PlayerSlotId}' playerActorId='{selectedCameraBinding.PlayerActorId}' followTarget='{selectedCameraBinding.Endpoint.FollowTarget.name}' lookAtTarget='{selectedCameraBinding.Endpoint.LookAtTarget?.name ?? "<none>"}'.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"event='ActivityCameraTargetBound' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' playerSlotId='{selectedCameraBinding.PlayerSlotId}' playerActorId='{selectedCameraBinding.PlayerActorId}' followTarget='{selectedCameraBinding.Endpoint.FollowTarget.name}' lookAtTarget='{selectedCameraBinding.Endpoint.LookAtTarget?.name ?? "<none>"}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            var completedIdentity = BuildIdentity(command, SessionActivityStage.CameraBindingCompleted);
            identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.CameraBindingCompleted);
            factBridge.EmitFact(facts, SessionActivityFactKind.CameraBindingCompleted, completedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' camera binding completed.");
            factBridge.EmitSnapshot(snapshots, "camera_binding_completed", command.Source, command.Reason, $"'{command.ActivityId}' camera binding completed.");
            DebugUtility.Log(
                typeof(ActivityEntryCameraBindingStage),
                $"event='CameraBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryCameraBindingStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActivityEntryCameraBindingResult(true, completedIdentity, requiredCameraCount, true, false, "camera_binding_completed");
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryCameraBindingCommand command,
            SessionActivityStage stage)
        {
            return new SessionActivityIdentity(
                command.Identity.PipelineId,
                command.Identity.SessionId,
                command.ActivityId,
                command.ActivityOrdinal,
                command.Identity.EntrySequence,
                stage,
                command.Source);
        }

        private static bool TryResolveCameraBindingContribution(
            IReadOnlyList<ActorCameraBindingContribution> bindingContributions,
            SessionActivityIdentity activeIdentity,
            IActivityEntryCameraBindingRuntimeBridge bridge,
            IReadOnlyList<PlayerActivityParticipantBinding> participants,
            out ActorCameraBindingContribution selectedCameraBinding)
        {
            selectedCameraBinding = default;
            if (bindingContributions == null || bindingContributions.Count == 0 || !activeIdentity.IsValid || participants == null || participants.Count == 0)
            {
                return false;
            }

            for (int participantIndex = 0; participantIndex < participants.Count; participantIndex++)
            {
                var binding = participants[participantIndex];
                if (!binding.IsValid || !binding.RequiresPlayerActor)
                {
                    continue;
                }

                if (!bridge.TryResolvePlayerActorHandle(activeIdentity, binding, out var handle) || !handle.IsValid)
                {
                    continue;
                }

                if (TryResolveCameraBindingContributionForHandle(bindingContributions, handle, out selectedCameraBinding))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveCameraBindingContributionForHandle(
            IReadOnlyList<ActorCameraBindingContribution> bindingContributions,
            PlayerActorRuntimeHandle handle,
            out ActorCameraBindingContribution selectedCameraBinding)
        {
            selectedCameraBinding = default;
            if (bindingContributions == null || bindingContributions.Count == 0 || !handle.IsValid)
            {
                return false;
            }

            for (int contributionIndex = 0; contributionIndex < bindingContributions.Count; contributionIndex++)
            {
                var contribution = bindingContributions[contributionIndex];
                if (!contribution.Matches(handle))
                {
                    continue;
                }

                selectedCameraBinding = contribution;
                return true;
            }

            return false;
        }

        private static int CountRequiredActivityCameraRequirements(
            IReadOnlyList<CameraBindingRequirement> requirements,
            out bool hasActivityCameraRequirement)
        {
            int requiredCount = 0;
            hasActivityCameraRequirement = false;
            for (int index = 0; index < requirements.Count; index++)
            {
                if (requirements[index].Requirement.IsRequired)
                {
                    requiredCount += 1;
                }

                hasActivityCameraRequirement = true;
            }

            return requiredCount;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
