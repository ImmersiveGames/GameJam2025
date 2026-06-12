using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryPlayerInputBindingStage
    {
        public static ActivityEntryPlayerInputBindingResult Execute(
            ActivityEntryPlayerInputBindingCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IPlayerInputBindingAdapter adapter,
            ActivityPlayerActorRegistry registry,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPlayerInputBindingCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.PlayerInputBindingStarted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding started.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_input_binding_started",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding started.");

            List<PlayerInputBindingRequirement> requirements = BuildRequirements(startedIdentity, command);
            int requiredCount = CountRequired(requirements);
            if (requiredCount <= 0)
            {
                var skippedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingSkippedNoRequiredInput);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.PlayerInputBindingSkippedNoRequiredInput);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingSkippedNoRequiredInput,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding skipped because no required controllable participant was resolved.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "player_input_binding_skipped_no_required_input",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding skipped because no required controllable participant was resolved.");

                var completedAfterSkipIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingCompleted);
                endpoint.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.PlayerInputBindingCompleted);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingCompleted,
                    completedAfterSkipIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding completed with skip.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "player_input_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding completed with skip.");
                return new ActivityEntryPlayerInputBindingResult(
                    completed: true,
                    completedAfterSkipIdentity,
                    requiredCount: 0,
                    requiredBoundCount: 0,
                    totalBoundCount: 0,
                    skipped: true,
                    reason: "no_required_controllable_participant");
            }

            for (int index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingCommandIssued,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding command issued requirementId='{requirement.RequirementId}' playerSlotId='{requirement.PlayerSlotId}' actorId='{requirement.ActorId}' required='{requirement.Required}'.");
            }

            PlayerInputBindingCommand bindingCommand = new(startedIdentity, requirements, command.Source, command.Reason);
            if (!bindingCommand.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PlayerInputBinding] Invalid binding command activityId='{startedIdentity.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
            }

            IReadOnlyList<PlayerInputBindingRecord> records = adapter.Execute(bindingCommand, startedIdentity, registry);
            if (records == null || records.Count != requirements.Count)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PlayerInputBinding] Adapter result mismatch activityId='{startedIdentity.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
            }

            int requiredBoundCount = 0;
            for (int index = 0; index < records.Count; index++)
            {
                var record = records[index];
                if (!record.IsValid)
                {
                    var failedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                    endpoint.EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerInputBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' player input binding failed because adapter produced invalid record index='{index}'.");
                    endpoint.EmitSnapshot(
                        snapshots,
                        "player_input_binding_failed",
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' player input binding failed because adapter produced invalid record index='{index}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PlayerInputBinding] Invalid binding record activityId='{command.ActivityId}' entrySequence='{entrySequence}' index='{index}'.");
                }

                if (record.Requirement.Required)
                {
                    requiredBoundCount += 1;
                }

                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBound,
                    startedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input bound requirementId='{record.Requirement.RequirementId}' playerSlotId='{record.Requirement.PlayerSlotId}' playerActorId='{record.ActorIdentity.PlayerActorId}' observedInput='{record.ObservedInputId}'.");
            }

            if (requiredBoundCount < requiredCount)
            {
                var failedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingFailed);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                endpoint.EmitSnapshot(
                    snapshots,
                    "player_input_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PlayerInputBinding] Required binding incomplete activityId='{command.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            var completedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingCompleted);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.PlayerInputBindingCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_input_binding_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");

            return new ActivityEntryPlayerInputBindingResult(
                completed: true,
                completedIdentity,
                requiredCount,
                requiredBoundCount,
                records.Count,
                skipped: false,
                reason: "resolved");
        }

        private static List<PlayerInputBindingRequirement> BuildRequirements(
            SessionActivityIdentity startedIdentity,
            ActivityEntryPlayerInputBindingCommand command)
        {
            List<PlayerInputBindingRequirement> requirements = new();
            IReadOnlyList<ActivityEntryPlayerInputBindingReference> references = command.ParticipantBindings ?? Array.Empty<ActivityEntryPlayerInputBindingReference>();
            for (int index = 0; index < references.Count; index++)
            {
                var reference = references[index];
                if (!reference.IsValid || reference.ParticipantKind != ActivityParticipantRequirementKind.ControllablePlayer)
                {
                    continue;
                }

                requirements.Add(new PlayerInputBindingRequirement(
                    startedIdentity,
                    reference.RequirementId,
                    reference.ParticipantBinding,
                    reference.Required,
                    command.Source,
                    command.Reason));
            }

            return requirements;
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryPlayerInputBindingCommand command,
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

        private static int CountRequired(IReadOnlyList<PlayerInputBindingRequirement> requirements)
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
