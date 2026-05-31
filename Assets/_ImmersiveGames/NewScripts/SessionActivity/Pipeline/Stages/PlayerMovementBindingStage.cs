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

                PlayerActorIdentityRecord actorIdentity = BuildParticipantActorIdentity(startedIdentity, resolved.ParticipantBinding);
                requirements.Add(new MovementBindingRequirement(
                    startedIdentity,
                    resolved.RequirementId,
                    actorIdentity.PlayerSlotId,
                    actorIdentity.PlayerActorId,
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

        private static PlayerActorIdentityRecord BuildParticipantActorIdentity(SessionActivityIdentity identity, ActivityParticipantBinding participantBinding)
        {
            string playerSlotId = participantBinding.PlayerSlotId.IsValid ? Normalize(participantBinding.PlayerSlotId.Value) : string.Empty;
            string actorId = participantBinding.ActorId.IsValid ? Normalize(participantBinding.ActorId.Value) : string.Empty;
            if (!identity.IsValid || string.IsNullOrWhiteSpace(playerSlotId) || string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException("Cannot build participant actor identity with invalid ActivityParticipantBinding.");
            }

            return new PlayerActorIdentityRecord(identity, playerSlotId, $"{identity.SessionId}|{actorId}");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
