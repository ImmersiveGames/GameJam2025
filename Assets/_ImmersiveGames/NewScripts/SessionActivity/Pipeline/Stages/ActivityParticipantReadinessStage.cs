using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityParticipantReadinessStage
    {
        public static SessionActivityPipeline.ActivityParticipantReadinessStageResult Execute(
            SessionActivityIdentity startedIdentity,
            SessionActivityIdentity expectedParticipantBindingIdentity,
            SessionActivityPipeline.ParticipantBindingStageResult participantBindingResult,
            ActivityPlayerActorRegistry registry)
        {
            if (!participantBindingResult.IsValid || !IsSameActivityCycle(participantBindingResult.Identity, expectedParticipantBindingIdentity))
            {
                return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.Failed, 0, 0, 0, "missing_or_foreign_participant_binding_result");
            }

            if (participantBindingResult.RequiredRequirements <= 0)
            {
                return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.SkippedNoRequiredParticipant, 0, 0, 0, "no_required_participant");
            }

            if (participantBindingResult.RequiredResolvedRequirements < participantBindingResult.RequiredRequirements)
            {
                return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(
                    SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    0,
                    "required_participant_not_ready");
            }

            if (!registry.TryGetActiveActorIdentities(startedIdentity, out IReadOnlyList<PlayerActorIdentityRecord> activeActors) || activeActors == null)
            {
                return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(
                    SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    0,
                    "registry_scope_missing_or_foreign");
            }

            if (activeActors.Count < participantBindingResult.RequiredResolvedRequirements)
            {
                return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(
                    SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.Failed,
                    participantBindingResult.RequiredRequirements,
                    participantBindingResult.RequiredResolvedRequirements,
                    activeActors.Count,
                    "active_actors_insufficient");
            }

            return new SessionActivityPipeline.ActivityParticipantReadinessStageResult(
                SessionActivityPipeline.ActivityParticipantReadinessStageOutcome.Ready,
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
