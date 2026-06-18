using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityActorParticipationExitFactWriter
    {
        public static void EmitStarted(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            ActivityExitActorTeardownCommand command = boundaryCommand.Command;
            SessionActivityDefinition definition = boundaryCommand.Definition;
            SessionActivityIdentity startedIdentity = boundaryCommand.StartedIdentity;

            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationExitStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started mode='inventory_feed'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExitStarted' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{command.EntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        public static void EmitSkipped(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            int entrySequence,
            ActorInstanceRecord instance,
            ActorParticipationExitActorResult actorResult)
        {
            SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
            endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
            DebugUtility.LogVerbose(typeof(ActivityExitActorTeardownStage), $"event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' skipKind='{actorResult.SkipOrFailureKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
        }

        public static void EmitFailed(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            int entrySequence,
            ActorInstanceRecord instance,
            ActorParticipationExitActorResult actorResult,
            string reasonCodeOverride = null)
        {
            string reasonCode = reasonCodeOverride.TrimToOrDefault(actorResult.ReasonCode);
            SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitFailed, entrySequence);
            endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationExitFailed);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{reasonCode}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{reasonCode}'.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"event='ActorParticipationExitFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' failureKind='{reasonCode}' failureReason='{reasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
        }

        public static void EmitExited(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity startedIdentity,
            ActorInstanceRecord instance)
        {
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExited, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exited", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}'.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExited' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{command.EntrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' actorScope='{instance.Scope}' resultKind='Exited' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        public static void EmitSkippedForNoExitedActors(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
            endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
            DebugUtility.LogVerbose(typeof(ActivityExitActorTeardownStage), $"event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_exited_actors' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
        }

        public static SessionActivityIdentity EmitCompleted(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int total,
            int exited,
            int skipped,
            int failed)
        {
            ActivityExitActorTeardownCommand command = boundaryCommand.Command;
            SessionActivityDefinition definition = boundaryCommand.Definition;
            int entrySequence = command.EntrySequence;
            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationExitCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExitCompleted' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            return completedIdentity;
        }

        public static void EmitLifetimeDecision(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            ActivityActorParticipationExitDecisionRecord decisionRecord)
        {
            EmitLifetimeDecision(
                endpoint,
                facts,
                decisionRecord,
                typeof(ActivityExitActorTeardownStage),
                nameof(ActivityExitActorTeardownStage));
        }

        public static void EmitLifetimeDecision(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            ActivityActorParticipationExitDecisionRecord decisionRecord,
            Type ownerType,
            string ownerName)
        {
            if (!decisionRecord.IsValid)
            {
                throw new InvalidOperationException("ActivityActorParticipationExitDecisionRecord is invalid.");
            }

            Type resolvedOwnerType = ownerType ?? typeof(ActivityExitActorTeardownStage);
            string resolvedOwnerName = ownerName.TrimToOrDefault(nameof(ActivityExitActorTeardownStage));

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeDecisionResolved,
                decisionRecord.Identity,
                decisionRecord.Source,
                decisionRecord.Reason,
                $"'{decisionRecord.ActivityId}' actor lifetime decision resolved actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}' decision='{decisionRecord.Decision}'.");
            DebugUtility.LogVerbose(
                resolvedOwnerType,
                $"event='ActorLifetimeDecisionResolved' owner='{resolvedOwnerName}' macroLifecycleOwner='SessionActivityPipeline' activityId='{decisionRecord.ActivityId}' entrySequence='{decisionRecord.EntrySequence}' actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}' decision='{decisionRecord.Decision}' source='{decisionRecord.Source}' reason='{decisionRecord.Reason}'.",
                DebugUtility.Colors.Info);

            if (decisionRecord.Decision == ActorLifetimeDecision.Retain)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeRetained,
                    decisionRecord.Identity,
                    decisionRecord.Source,
                    decisionRecord.Reason,
                    $"'{decisionRecord.ActivityId}' actor lifetime retained actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}'.");
                return;
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeReleased,
                decisionRecord.Identity,
                decisionRecord.Source,
                decisionRecord.Reason,
                $"'{decisionRecord.ActivityId}' actor lifetime released actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}'.");
        }
    }
}
