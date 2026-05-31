using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorParticipationStage
    {
        private sealed class RuntimeBridgeReadinessPolicy : IActorParticipationReadinessPolicy
        {
            private readonly IActivityEntryActorParticipationRuntimeBridge _bridge;

            public RuntimeBridgeReadinessPolicy(IActivityEntryActorParticipationRuntimeBridge bridge)
            {
                _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            }

            public ActorParticipationReadinessEvaluation Evaluate(
                SessionActivityIdentity identity,
                ActorInstanceRecord instance)
            {
                return _bridge.EvaluateActorParticipationReadiness(identity, instance);
            }
        }

        public static ActivityEntryActorParticipationEnterResult ExecuteEnter(
            ActivityEntryActorParticipationEnterCommand command,
            IActivityEntryRuntimeEndpoint endpoint,
            IActivityEntryActorInventoryRuntimeBridge actorInventoryBridge,
            IActivityEntryActorParticipationRuntimeBridge participationBridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorParticipationEnterCommand is invalid.");
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (actorInventoryBridge == null)
            {
                throw new ArgumentNullException(nameof(actorInventoryBridge));
            }

            if (participationBridge == null)
            {
                throw new ArgumentNullException(nameof(participationBridge));
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationEnterStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter started mode='inventory_feed'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_enter_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter started.");
            DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEnterStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            ActorInventoryFeedResult feedResult = actorInventoryBridge.GetCurrentActorInventoryFeedResult();
            if (!feedResult.IsValid || !IsSameActivityCycle(feedResult.Identity, startedIdentity))
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter failed reason='actor_inventory_feed_missing_or_foreign'.");
                endpoint.EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter failed reason='actor_inventory_feed_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryActorParticipationStage][ActorParticipationEnter] Missing or foreign ActorInventoryFeedResult activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            ActorParticipationStageExecutor executor = new(new RuntimeBridgeReadinessPolicy(participationBridge));
            ActorParticipationCommand participationCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            ActorParticipationResult participationResult = executor.Execute(participationCommand);

            int total = participationResult.Total;
            int entered = participationResult.Entered;
            int skipped = participationResult.Skipped;
            int failed = participationResult.Failed;

            for (int index = 0; index < participationResult.ActorResults.Count; index++)
            {
                ActorParticipationActorResult actorResult = participationResult.ActorResults[index];
                if (!actorResult.IsValid)
                {
                    continue;
                }

                ActorInstanceRecord instance = actorResult.Instance;
                if (actorResult.IsSkipped)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterSkipped, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEnterSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' skipKind='{actorResult.SkipKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
                    continue;
                }

                if (actorResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation failed actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEnterFailed' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' failureKind='{actorResult.SkipKind}' failureReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorParticipationStage][ActorParticipationEnter] readiness failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                }

                participationBridge.StoreActiveActorParticipation(instance.ActorInstanceId);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEntered, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation entered actorId='{instance.ActorId}' actorRole='{instance.Role}' actorScope='{instance.Scope}'.");
                endpoint.EmitSnapshot(snapshots, "actor_participation_entered", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation entered actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEntered' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                endpoint.EmitFact(facts, SessionActivityFactKind.ActorReady, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor ready actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
                endpoint.EmitSnapshot(snapshots, "actor_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor ready actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            }

            if (entered == 0)
            {
                skipped += 1;
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                endpoint.EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEnterSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_entered_actors' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationEnterCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationEnterCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_enter_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"[OBS][ActivityEntryPipeline][ActorParticipation] event='ActorParticipationEnterCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            return new ActivityEntryActorParticipationEnterResult(
                completed: true,
                completedIdentity,
                total,
                entered,
                skipped,
                failed,
                "actor_participation_enter_completed");
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
