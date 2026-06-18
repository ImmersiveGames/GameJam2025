using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal enum ActivityActorParticipationExitBindingDecisionKind
    {
        Unknown = 0,
        NotRequired = 1,
        Resolved = 2,
        Failed = 3
    }

    internal readonly struct ActivityActorParticipationExitBindingDecision
    {
        private ActivityActorParticipationExitBindingDecision(
            ActivityActorParticipationExitBindingDecisionKind kind,
            ActivityActorParticipationExitBindingResolutionResult resolution,
            PlayerActorIdentityRecord playerActorIdentity,
            string reason)
        {
            Kind = kind;
            Resolution = resolution;
            PlayerActorIdentity = playerActorIdentity;
            Reason = reason.TrimToEmpty();
        }

        public ActivityActorParticipationExitBindingDecisionKind Kind { get; }
        public ActivityActorParticipationExitBindingResolutionResult Resolution { get; }
        public PlayerActorIdentityRecord PlayerActorIdentity { get; }
        public string Reason { get; }

        public bool ShouldRecordExitedPlayerActor =>
            Kind == ActivityActorParticipationExitBindingDecisionKind.Resolved;

        public bool IsFatal =>
            Kind == ActivityActorParticipationExitBindingDecisionKind.Failed;

        public static ActivityActorParticipationExitBindingDecision NotRequired(
            ActivityActorParticipationExitBindingResolutionResult resolution) =>
            new(ActivityActorParticipationExitBindingDecisionKind.NotRequired, resolution, default, "player_identity_not_resolved_for_actor");

        public static ActivityActorParticipationExitBindingDecision Resolved(
            ActivityActorParticipationExitBindingResolutionResult resolution,
            PlayerActorIdentityRecord playerActorIdentity) =>
            new(ActivityActorParticipationExitBindingDecisionKind.Resolved, resolution, playerActorIdentity, "player_participant_binding_resolved");

        public static ActivityActorParticipationExitBindingDecision Failed(
            ActivityActorParticipationExitBindingResolutionResult resolution,
            string reason) =>
            new(ActivityActorParticipationExitBindingDecisionKind.Failed, resolution, default, reason);
}

    internal static class ActivityActorParticipationExitBindingDecisionBuilder
    {
        public static ActivityActorParticipationExitBindingDecision Resolve(
            ActivityActorExitRuntimeState runtimeState,
            ActorParticipationExitActorResult actorResult,
            ActorInstanceRecord instance,
            SessionActivityIdentity startedIdentity)
        {
            ActivityActorParticipationExitBindingResolutionResult bindingResolution =
                runtimeState.ResolveActivePlayerParticipantBindingForExit(startedIdentity, instance);

            if (bindingResolution.IsResolved)
            {
                PlayerActorIdentityRecord resolvedIdentity = new(
                    startedIdentity,
                    bindingResolution.Binding,
                    actorResult.PlayerActorId);
                return ActivityActorParticipationExitBindingDecision.Resolved(bindingResolution, resolvedIdentity);
            }

            if (!actorResult.HasResolvedPlayerIdentity)
            {
                return ActivityActorParticipationExitBindingDecision.NotRequired(bindingResolution);
            }

            return ActivityActorParticipationExitBindingDecision.Failed(
                bindingResolution,
                "player_participant_binding_resolution_failed");
        }
    }

    internal static class ActivityActorParticipationExitBindingDecisionApplicator
    {
        public static void Apply(
            ActivityActorParticipationExitBindingDecision bindingDecision,
            ActorParticipationExitActorResult actorResult,
            ActorInstanceRecord instance,
            string activityId,
            int entrySequence,
            List<PlayerActorIdentityRecord> exitedPlayerActors)
        {
            if (exitedPlayerActors == null)
            {
                throw new ArgumentNullException(nameof(exitedPlayerActors));
            }

            if (bindingDecision.ShouldRecordExitedPlayerActor)
            {
                exitedPlayerActors.Add(bindingDecision.PlayerActorIdentity);
                return;
            }

            if (!bindingDecision.IsFatal)
            {
                return;
            }

            ActivityActorParticipationExitBindingResolutionResult bindingResolution = bindingDecision.Resolution;
            throw new InvalidOperationException(
                $"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] {bindingDecision.Reason} kind='{bindingResolution.Kind}' sourceKind='{bindingResolution.SourceKind}' reason='{bindingResolution.Reason}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{actorResult.PlayerSlotId}' activityId='{activityId}' entrySequence='{entrySequence}'.");
        }
    }
}
