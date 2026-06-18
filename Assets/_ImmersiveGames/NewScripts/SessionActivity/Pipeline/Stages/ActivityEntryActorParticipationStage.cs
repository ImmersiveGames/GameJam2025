using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

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
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            ActorInventoryFeedResult feedResult,
            ActivityActorExitRuntimeState runtimeState,
            IActivityEntryActorParticipationRuntimeBridge participationBridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorParticipationEnterCommand is invalid.");
            }

            if (identityBridge == null)
            {
                throw new ArgumentNullException(nameof(identityBridge));
            }

            if (factBridge == null)
            {
                throw new ArgumentNullException(nameof(factBridge));
            }

            if (participationBridge == null)
            {
                throw new ArgumentNullException(nameof(participationBridge));
            }

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            int entrySequence = command.Identity.EntrySequence;
            var startedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterStarted, command.Source);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationEnterStarted);
            factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterStarted, startedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter started mode='inventory_feed'.");
            factBridge.EmitSnapshot(snapshots, "actor_participation_enter_started", command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter started.");
            if (!feedResult.IsValid || !IsSameActivityCycle(feedResult.Identity, startedIdentity))
            {
                var failedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterFailed, command.Source);
                identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter failed reason='actor_inventory_feed_missing_or_foreign'.");
                factBridge.EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter failed reason='actor_inventory_feed_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryActorParticipationStage][ActorParticipationEnter] Missing or foreign ActorInventoryFeedResult activityId='{command.ActivityId}' entrySequence='{entrySequence}'.");
            }

            ActorParticipationStageExecutor executor = new(new RuntimeBridgeReadinessPolicy(participationBridge));
            ActorParticipationCommand participationCommand = new(
                startedIdentity,
                command.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            var participationResult = executor.Execute(participationCommand);

            int total = participationResult.Total;
            int entered = participationResult.Entered;
            int skipped = participationResult.Skipped;
            int failed = participationResult.Failed;

            for (int index = 0; index < participationResult.ActorResults.Count; index++)
            {
                var actorResult = participationResult.ActorResults[index];
                if (!actorResult.IsValid)
                {
                    continue;
                }

                var instance = actorResult.Instance;
                if (actorResult.IsSkipped)
                {
                    var skippedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterSkipped, command.Source);
                    identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation skipped actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    factBridge.EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{command.ActivityId}' actor participation skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.LogVerbose(typeof(ActivityEntryActorParticipationStage),
                        $"event='ActorParticipationEnterSkipped' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' skipKind='{actorResult.SkipKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    continue;
                }

                if (actorResult.IsFailed)
                {
                    var failedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterFailed, command.Source);
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation failed actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='{actorResult.ReasonCode}'.");
                    factBridge.EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{command.ActivityId}' actor participation failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorParticipationStage),
                        $"event='ActorParticipationEnterFailed' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' failureKind='{actorResult.SkipKind}' failureReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorParticipationStage][ActorParticipationEnter] readiness failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                }

                var actorInstanceRuntimeId = instance.ActorInstanceRuntimeId;
                if (!actorInstanceRuntimeId.IsValid)
                {
                    var failedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterFailed, command.Source);
                    identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationEnterFailed);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterFailed, failedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation failed actorId='{instance.ActorId}' actorRole='{instance.Role}' reason='actor_instance_runtime_id_invalid'.");
                    factBridge.EmitSnapshot(snapshots, "actor_participation_enter_failed", command.Source, command.Reason, $"'{command.ActivityId}' actor participation failed actorId='{instance.ActorId}' reason='actor_instance_runtime_id_invalid'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorParticipationStage][ActorParticipationEnter] runtime actor instance id invalid actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}'.");
                }

                runtimeState.StoreActiveActorParticipation(actorInstanceRuntimeId, command.ActivityId, entrySequence, "ActivityEntryActorParticipationStage", "store_active_actor_participation");
                factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEntered, startedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation entered actorId='{instance.ActorId}' actorRole='{instance.Role}' actorScope='{instance.Scope}'.");
                factBridge.EmitSnapshot(snapshots, "actor_participation_entered", command.Source, command.Reason, $"'{command.ActivityId}' actor participation entered actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorParticipationStage),
                    $"event='ActorParticipationEntered' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                factBridge.EmitFact(facts, SessionActivityFactKind.ActorReady, startedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor ready actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
                factBridge.EmitSnapshot(snapshots, "actor_ready", command.Source, command.Reason, $"'{command.ActivityId}' actor ready actorId='{instance.ActorId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorParticipationStage),
                    $"event='ActorReady' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{instance.ActorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            }

            if (entered == 0)
            {
                skipped += 1;
                var skippedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterSkipped, command.Source);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationEnterSkipped);
                factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterSkipped, skippedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                factBridge.EmitSnapshot(snapshots, "actor_participation_enter_skipped", command.Source, command.Reason, $"'{command.ActivityId}' actor participation skipped reason='no_entered_actors'.");
                DebugUtility.LogVerbose(typeof(ActivityEntryActorParticipationStage), $"event='ActorParticipationEnterSkipped' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_entered_actors' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
            }

            var completedIdentity = BuildIdentity(command.Identity, SessionActivityStage.ActorParticipationEnterCompleted, command.Source);
            identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationEnterCompleted);
            factBridge.EmitFact(facts, SessionActivityFactKind.ActorParticipationEnterCompleted, completedIdentity, command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            factBridge.EmitSnapshot(snapshots, "actor_participation_enter_completed", command.Source, command.Reason, $"'{command.ActivityId}' actor participation enter completed total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.Log(typeof(ActivityEntryActorParticipationStage), $"event='ActorParticipationEnterCompleted' activityId='{command.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorParticipationStage' entryPipelineOwner='ActivityEntryPipeline' total='{total}' entered='{entered}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            return new ActivityEntryActorParticipationEnterResult(
                true,
                completedIdentity,
                total,
                entered,
                skipped,
                failed,
                "actor_participation_enter_completed");
        }

        private static SessionActivityIdentity BuildIdentity(
            SessionActivityIdentity identity,
            SessionActivityStage stage,
            string source)
        {
            return new SessionActivityIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                stage,
                source);
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
