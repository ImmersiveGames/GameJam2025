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
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            IPlayerInputBindingAdapter adapter,
            ActivityPlayerActorRegistry registry,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPlayerInputBindingCommand is invalid.");
            }

            identityBridge = identityBridge ?? throw new ArgumentNullException(nameof(identityBridge));
            factBridge = factBridge ?? throw new ArgumentNullException(nameof(factBridge));
            adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingStarted);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.PlayerInputBindingStarted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingStarted,
                startedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding started.");
            factBridge.EmitSnapshot(
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
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.PlayerInputBindingSkippedNoRequiredInput);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingSkippedNoRequiredInput,
                    skippedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding skipped because no required controllable participant was resolved.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "player_input_binding_skipped_no_required_input",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding skipped because no required controllable participant was resolved.");

                var completedAfterSkipIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingCompleted);
                identityBridge.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.PlayerInputBindingCompleted);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingCompleted,
                    completedAfterSkipIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding completed with skip.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "player_input_binding_completed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding completed with skip.");
                return new ActivityEntryPlayerInputBindingResult(
                    true,
                    completedAfterSkipIdentity,
                    0,
                    0,
                    0,
                    true,
                    "no_required_controllable_participant");
            }

            for (int index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];
                factBridge.EmitFact(
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
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                    factBridge.EmitFact(
                        facts,
                        SessionActivityFactKind.PlayerInputBindingFailed,
                        failedIdentity,
                        command.Source,
                        command.Reason,
                        $"'{command.ActivityId}' player input binding failed because adapter produced invalid record index='{index}'.");
                    factBridge.EmitSnapshot(
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

                factBridge.EmitFact(
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
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.PlayerInputBindingFailed);
                factBridge.EmitFact(
                    facts,
                    SessionActivityFactKind.PlayerInputBindingFailed,
                    failedIdentity,
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                factBridge.EmitSnapshot(
                    snapshots,
                    "player_input_binding_failed",
                    command.Source,
                    command.Reason,
                    $"'{command.ActivityId}' player input binding failed requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryPipeline][PlayerInputBinding] Required binding incomplete activityId='{command.ActivityId}' entrySequence='{entrySequence}' requiredBound='{requiredBoundCount}' required='{requiredCount}'.");
            }

            var completedIdentity = BuildIdentity(command, SessionActivityStage.PlayerInputBindingCompleted);
            identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.PlayerInputBindingCompleted);
            factBridge.EmitFact(
                facts,
                SessionActivityFactKind.PlayerInputBindingCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");
            factBridge.EmitSnapshot(
                snapshots,
                "player_input_binding_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' player input binding completed requiredBound='{requiredBoundCount}' required='{requiredCount}' totalBound='{records.Count}'.");

            return new ActivityEntryPlayerInputBindingResult(
                true,
                completedIdentity,
                requiredCount,
                requiredBoundCount,
                records.Count,
                false,
                "resolved");
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
