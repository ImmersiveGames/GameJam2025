using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryMovementBindingStage
    {
        public static ActivityEntryMovementBindingResult Execute(
            ActivityEntryMovementBindingCommand command,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            ActivityPlayerActorRegistry playerActorRegistry,
            IMovementBindingAdapter movementBindingAdapter,
            IActivityEntryMovementBindingRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryMovementBindingCommand is invalid.");
            }

            identityBridge = identityBridge ?? throw new ArgumentNullException(nameof(identityBridge));
            factBridge = factBridge ?? throw new ArgumentNullException(nameof(factBridge));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            playerActorRegistry = playerActorRegistry ?? throw new ArgumentNullException(nameof(playerActorRegistry));
            movementBindingAdapter = movementBindingAdapter ?? throw new ArgumentNullException(nameof(movementBindingAdapter));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingStarted);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.MovementBindingStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding started.");
            factBridge.EmitSnapshot(
                snapshots,
                "movement_binding_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding started.");

            List<MovementBindingRequirement> requirements = BuildRequirements(startedIdentity, command);
            int requiredCount = CountRequired(requirements);
            if (requiredCount <= 0)
            {
                IReadOnlyList<PlayerActorIdentityRecord> retainedTargets = bridge.ResolveRetainedMovementTargets(startedIdentity) ?? Array.Empty<PlayerActorIdentityRecord>();
                if (retainedTargets.Count > 0)
                {
                    bridge.SetMovementControlTargets(retainedTargets, enableAllowed: true);
                    for (int index = 0; index < retainedTargets.Count; index++)
                    {
                        var retained = retainedTargets[index];
                        factBridge.EmitFact(
                            facts,
                            SessionActivityFactKind.MovementBindingRetained,
                            startedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' movement binding retained playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}'.");
                        DebugUtility.LogVerbose(
                            typeof(ActivityEntryMovementBindingStage),
                            $"event='MovementBindingRetained' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Info);
                    }

                    var retainedCompletedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
                    identityBridge.SetCurrentIdentity(retainedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingCompleted,
                        retainedCompletedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding completed status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false'.");
                    DebugUtility.Log(
                        typeof(ActivityEntryMovementBindingStage),
                        $"event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                    factBridge.EmitSnapshot(
                        snapshots,
                        "movement_binding_completed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding completed status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false'.");
                    return new ActivityEntryMovementBindingResult(
                        completed: true,
                        identity: retainedCompletedIdentity,
                        requiredCount: 0,
                        requiredBoundCount: 0,
                        totalBoundCount: 0,
                        retainedCount: retainedTargets.Count,
                        retainedExistingBinding: true,
                        skipped: false,
                        reason: "retained_existing_binding");
                }

                bridge.SetMovementControlTargets(Array.Empty<PlayerActorIdentityRecord>(), enableAllowed: false);
                var skippedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingSkippedNoRequiredMovement);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.MovementBindingSkippedNoRequiredMovement);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingSkippedNoRequiredMovement,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding skipped because no movement capability target is required or retained.");
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryMovementBindingStage),
                    $"event='MovementBindingSkippedNoRequiredMovement' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                factBridge.EmitSnapshot(
                    snapshots,
                    "movement_binding_skipped_no_required_movement",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding skipped because no movement capability target is required or retained.");

                var skippedCompletedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
                identityBridge.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingCompleted,
                    skippedCompletedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding completed status='NoMovementCapabilityRequired' controlEnabled='false'.");
                DebugUtility.Log(
                    typeof(ActivityEntryMovementBindingStage),
                    $"event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' status='NoMovementCapabilityRequired' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                factBridge.EmitSnapshot(
                    snapshots,
                    "movement_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding completed status='NoMovementCapabilityRequired' controlEnabled='false'.");
                return new ActivityEntryMovementBindingResult(
                    completed: true,
                    identity: skippedCompletedIdentity,
                    requiredCount: 0,
                    requiredBoundCount: 0,
                    totalBoundCount: 0,
                    retainedCount: 0,
                    retainedExistingBinding: false,
                    skipped: true,
                    reason: "no_required_movement");
            }

            for (int index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingCommandIssued,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding command issued requirementId='{requirement.RequirementId}' playerSlotId='{requirement.PlayerSlotId}' actorId='{requirement.ActorId}' required='{requirement.Required}'.");
            }

            MovementBindingCommand bindingCommand = new(startedIdentity, requirements, command.Source, command.Reason);
            if (!bindingCommand.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][MovementBinding] Invalid binding command activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<MovementBindingRecord> records = movementBindingAdapter.Execute(
                bindingCommand,
                startedIdentity,
                playerActorRegistry);
            if (records == null || records.Count != requirements.Count)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][MovementBinding] Adapter result mismatch activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            int requiredBoundCount = 0;
            List<PlayerActorIdentityRecord> boundTargets = new(records.Count);
            for (int index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (!record.IsValid)
                {
                    var failedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingFailed);
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding failed because adapter produced invalid record index='{index}'.");
                    factBridge.EmitSnapshot(
                        snapshots,
                        "movement_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding failed because adapter produced invalid record index='{index}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][MovementBinding] Invalid binding record activityId='{command.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                if (record.Requirement.Required)
                {
                    requiredBoundCount += 1;
                }

                boundTargets.Add(new PlayerActorIdentityRecord(
                    startedIdentity,
                    record.Requirement.ParticipantBinding,
                    record.ActorIdentity.PlayerActorId));
            }

            bridge.SetMovementControlTargets(boundTargets, enableAllowed: true);
            if (boundTargets.Count > 0)
            {
                bridge.PublishInitialMovementControlBlocked(
                    startedIdentity,
                    boundTargets,
                    command.Source,
                    command.Reason,
                    facts,
                    snapshots);
            }

            for (int index = 0; index < records.Count; index++)
            {
                var record = records[index];
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerMovementBound,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement bound requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' endpoint='{record.ObservedEndpoint}'.");
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryMovementBindingStage),
                    $"event='PlayerMovementBound' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' endpoint='{record.ObservedEndpoint}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
            }

            if (requiredBoundCount < requiredCount)
            {
                var failedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingFailed);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][MovementBinding] Required binding incomplete activityId='{command.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            var completedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
            identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.MovementBindingCompleted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false'.");
            DebugUtility.Log(
                typeof(ActivityEntryMovementBindingStage),
                $"event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryMovementBindingStage' entryPipelineOwner='ActivityEntryPipeline' requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            factBridge.EmitSnapshot(
                snapshots,
                "movement_binding_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false'.");

            return new ActivityEntryMovementBindingResult(
                completed: true,
                identity: completedIdentity,
                requiredCount: requiredCount,
                requiredBoundCount: requiredBoundCount,
                totalBoundCount: records.Count,
                retainedCount: 0,
                retainedExistingBinding: false,
                skipped: false,
                reason: "movement_bound");
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryMovementBindingCommand command,
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

        private static List<MovementBindingRequirement> BuildRequirements(
            SessionActivityIdentity startedIdentity,
            ActivityEntryMovementBindingCommand command)
        {
            List<MovementBindingRequirement> requirements = new();
            IReadOnlyList<ActivityEntryMovementBindingReference> bindings = command.ParticipantBindings ?? Array.Empty<ActivityEntryMovementBindingReference>();
            for (int index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                if (!binding.IsValid || binding.ParticipantKind != ActivityParticipantRequirementKind.ControllablePlayer)
                {
                    continue;
                }

                requirements.Add(new MovementBindingRequirement(
                    startedIdentity,
                    binding.RequirementId,
                    binding.ParticipantBinding,
                    binding.Required,
                    command.Source,
                    command.Reason));
            }

            return requirements;
        }

        private static int CountRequired(IReadOnlyList<MovementBindingRequirement> requirements)
        {
            int requiredCount = 0;
            for (int index = 0; index < requirements.Count; index++)
            {
                if (requirements[index].Required)
                {
                    requiredCount += 1;
                }
            }

            return requiredCount;
        }
    }
}
