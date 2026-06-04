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
            IActivityEntryRuntimeBridge endpoint,
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

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            playerActorRegistry = playerActorRegistry ?? throw new ArgumentNullException(nameof(playerActorRegistry));
            movementBindingAdapter = movementBindingAdapter ?? throw new ArgumentNullException(nameof(movementBindingAdapter));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.MovementBindingStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding started.");
            DebugUtility.Log(
                typeof(ActivityEntryMovementBindingStage),
                $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingStarted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            endpoint.EmitSnapshot(
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
                        PlayerActorIdentityRecord retained = retainedTargets[index];
                        endpoint.EmitFact(
                            facts,
                            SessionActivityFactKind.MovementBindingRetained,
                            startedIdentity,
                            command.Source,
                            command.Reason,
                            $"'{command.ActivityId}' movement binding retained playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}'.");
                        DebugUtility.Log(
                            typeof(ActivityEntryMovementBindingStage),
                            $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingRetained' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' playerSlotId='{retained.PlayerSlotId}' playerActorId='{retained.PlayerActorId}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Info);
                    }

                    SessionActivityIdentity retainedCompletedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
                    endpoint.SetCurrentIdentity(retainedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingCompleted,
                        retainedCompletedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding completed status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false'.");
                    DebugUtility.Log(
                        typeof(ActivityEntryMovementBindingStage),
                        $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' status='RetainedExistingBinding' retained='{retainedTargets.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);
                    endpoint.EmitSnapshot(
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
                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingSkippedNoRequiredMovement);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.MovementBindingSkippedNoRequiredMovement);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingSkippedNoRequiredMovement,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding skipped because no movement capability target is required or retained.");
                DebugUtility.Log(
                    typeof(ActivityEntryMovementBindingStage),
                    $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingSkippedNoRequiredMovement' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                endpoint.EmitSnapshot(
                    snapshots,
                    "movement_binding_skipped_no_required_movement",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding skipped because no movement capability target is required or retained.");

                SessionActivityIdentity skippedCompletedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
                endpoint.SetCurrentIdentity(skippedCompletedIdentity, SessionActivityStage.MovementBindingCompleted);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingCompleted,
                    skippedCompletedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding completed status='NoMovementCapabilityRequired' controlEnabled='false'.");
                DebugUtility.Log(
                    typeof(ActivityEntryMovementBindingStage),
                    $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' status='NoMovementCapabilityRequired' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                endpoint.EmitSnapshot(
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
                MovementBindingRequirement requirement = requirements[index];
                endpoint.EmitFact(
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
                MovementBindingRecord record = records[index];
                if (!record.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.MovementBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' movement binding failed because adapter produced invalid record index='{index}'.");
                    endpoint.EmitSnapshot(
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

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerMovementBound,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement bound requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' endpoint='{record.ObservedEndpoint}'.");
                DebugUtility.Log(
                    typeof(ActivityEntryMovementBindingStage),
                    $"[OBS][ActivityEntryPipeline][MovementBinding] event='PlayerMovementBound' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' endpoint='{record.ObservedEndpoint}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);

                boundTargets.Add(new PlayerActorIdentityRecord(
                    startedIdentity,
                    record.Requirement.ParticipantBinding,
                    record.ActorIdentity.PlayerActorId));
            }

            if (requiredBoundCount < requiredCount)
            {
                SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.MovementBindingFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.MovementBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "movement_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' movement binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][MovementBinding] Required binding incomplete activityId='{command.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            bridge.SetMovementControlTargets(boundTargets, enableAllowed: true);
            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.MovementBindingCompleted);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.MovementBindingCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.MovementBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' movement binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false'.");
            DebugUtility.Log(
                typeof(ActivityEntryMovementBindingStage),
                $"[OBS][ActivityEntryPipeline][MovementBinding] event='MovementBindingCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}' controlEnabled='false' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            endpoint.EmitSnapshot(
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
                ActivityEntryMovementBindingReference binding = bindings[index];
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
