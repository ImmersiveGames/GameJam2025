using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class PlayerActorReadinessStage
    {
        public static SessionActivityPipeline.PlayerActorReadinessStageResult Execute(
            SessionActivityIdentity startedIdentity,
            SessionActivityIdentity expectedParticipantBindingIdentity,
            SessionActivityPipeline.ParticipantBindingStageResult participantBindingResult,
            ActivityPlayerActorRegistry registry)
        {
            if (!participantBindingResult.IsValid || !IsSameActivityCycle(participantBindingResult.Identity, expectedParticipantBindingIdentity))
            {
                return new SessionActivityPipeline.PlayerActorReadinessStageResult(SessionActivityPipeline.PlayerActorReadinessStageOutcome.Failed, 0, 0, 0, "missing_or_foreign_participant_binding_result");
            }

            if (participantBindingResult.RequiredRequirements <= 0)
            {
                return new SessionActivityPipeline.PlayerActorReadinessStageResult(SessionActivityPipeline.PlayerActorReadinessStageOutcome.SkippedNoRequiredParticipant, 0, 0, 0, "no_required_participant");
            }

            if (participantBindingResult.RequiredResolvedRequirements < participantBindingResult.RequiredRequirements)
            {
                return new SessionActivityPipeline.PlayerActorReadinessStageResult(
                    SessionActivityPipeline.PlayerActorReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    0,
                    "required_participant_not_ready");
            }

            if (!registry.TryGetActiveActorIdentities(startedIdentity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors) || activeActors == null)
            {
                return new SessionActivityPipeline.PlayerActorReadinessStageResult(
                    SessionActivityPipeline.PlayerActorReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    0,
                    "registry_scope_missing_or_foreign");
            }

            if (activeActors.Count < participantBindingResult.RequiredResolvedRequirements)
            {
                return new SessionActivityPipeline.PlayerActorReadinessStageResult(
                    SessionActivityPipeline.PlayerActorReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    activeActors.Count,
                    "active_actors_insufficient");
            }

            return new SessionActivityPipeline.PlayerActorReadinessStageResult(
                SessionActivityPipeline.PlayerActorReadinessStageOutcome.Ready,
                participantBindingResult.RequiredRequirements,
                participantBindingResult.RequiredResolvedRequirements,
                activeActors.Count,
                "ready");
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
    }
}
