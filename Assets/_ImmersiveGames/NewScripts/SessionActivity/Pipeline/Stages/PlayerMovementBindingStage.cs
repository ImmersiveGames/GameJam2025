using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class PlayerMovementBindingStage
    {
        public static SessionActivityPipeline.MovementBindingStageResult Execute(
            SessionActivityIdentity startedIdentity,
            SessionActivityPipeline.ParticipantBindingStageResult participantBindingResult,
            IMovementBindingAdapter adapter,
            ActivityPlayerActorRegistry registry,
            IReadOnlyList<PlayerActorIdentityRecord> retainedTargets,
            string source,
            string reason)
        {
            List<MovementBindingRequirement> requirements = new();
            IReadOnlyList<SessionActivityPipeline.ParticipantBindingResolvedRecord> resolvedParticipants = participantBindingResult.ResolvedParticipants;
            for (int index = 0; index < resolvedParticipants.Count; index++)
            {
                SessionActivityPipeline.ParticipantBindingResolvedRecord resolved = resolvedParticipants[index];
                if (!resolved.IsValid || resolved.ParticipantKind != ActivityParticipantRequirementKind.ControllablePlayer)
                {
                    continue;
                }

                requirements.Add(new MovementBindingRequirement(
                    startedIdentity,
                    resolved.RequirementId,
                    resolved.ParticipantBinding,
                    resolved.Required,
                    source,
                    reason));
            }

            int requiredCount = 0;
            for (int index = 0; index < requirements.Count; index++)
            {
                if (requirements[index].Required)
                {
                    requiredCount += 1;
                }
            }

            if (requiredCount <= 0)
            {
                if (retainedTargets != null && retainedTargets.Count > 0)
                {
                    return new SessionActivityPipeline.MovementBindingStageResult(requirements, 0, Array.Empty<MovementBindingRecord>(), retainedTargets, usedRetainedTargets: true, skippedNoRequiredMovement: false);
                }

                return new SessionActivityPipeline.MovementBindingStageResult(requirements, 0, Array.Empty<MovementBindingRecord>(), Array.Empty<PlayerActorIdentityRecord>(), usedRetainedTargets: false, skippedNoRequiredMovement: true);
            }

            MovementBindingCommand bindingCommand = new(startedIdentity, requirements, source, reason);
            if (!bindingCommand.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] Invalid binding command activityId='{startedIdentity.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
            }

            IReadOnlyList<MovementBindingRecord> records = adapter.Execute(bindingCommand, startedIdentity, registry);
            if (records == null || records.Count != requirements.Count)
            {
                throw new InvalidOperationException($"[FATAL][SessionActivityPipeline][MovementBinding] Adapter result mismatch activityId='{startedIdentity.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
            }

            return new SessionActivityPipeline.MovementBindingStageResult(requirements, requiredCount, records, Array.Empty<PlayerActorIdentityRecord>(), usedRetainedTargets: false, skippedNoRequiredMovement: false);
        }

    }
}
