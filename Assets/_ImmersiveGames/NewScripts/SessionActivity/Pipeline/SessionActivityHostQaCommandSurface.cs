using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class SessionActivityHostQaCommandSurface
    {
        public static void CompleteCurrentActivity(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult(pipeline, "CompleteCurrentActivity", result);
        }

        public static void CompleteActivationWindow(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).CompleteActivationWindow(QaSource("CompleteActivationWindow"), QaReason("CompleteActivationWindow"));
            LogResult(pipeline, "CompleteActivationWindow", result);
        }

        public static void CompleteDeactivationWindow(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).CompleteDeactivationWindow(QaSource("CompleteDeactivationWindow"), QaReason("CompleteDeactivationWindow"));
            LogResult(pipeline, "CompleteDeactivationWindow", result);
        }

        public static void ContinueToNextActivity(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
            LogResult(pipeline, "ContinueToNextActivity", result);
        }

        public static void RestartCurrentActivity(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult(pipeline, "RestartCurrentActivity", result);
        }

        public static void ResetSession(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).ResetSession(QaSource("ResetSession"), QaReason("ResetSession"));
            LogResult(pipeline, "ResetSession", result);
        }

        public static void RequestPause(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult(pipeline, "RequestPause", result);
        }

        public static void RequestPauseToggle(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).PauseToggleRequested(QaSource("RequestPauseToggle"), QaReason("RequestPauseToggle"));
            LogResult(pipeline, "RequestPauseToggle", result);
        }

        public static void RequestPauseToggle(SessionActivityPipeline pipeline, string source, string reason)
        {
            string resolvedSource = source.TrimToOrDefault(QaSource("RequestPauseToggle"));
            string resolvedReason = reason.TrimToOrDefault(QaReason("RequestPauseToggle"));
            var result = RequirePipeline(pipeline).PauseToggleRequested(resolvedSource, resolvedReason);
            LogResult(pipeline, "RequestPauseToggle", result);
        }

        public static void RequestResume(SessionActivityPipeline pipeline)
        {
            var result = RequirePipeline(pipeline).ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult(pipeline, "RequestResume", result);
        }

        public static SessionActivityCommandResult ExecuteCommand(SessionActivityPipeline pipeline, SessionActivityCommand command, string actionLabel)
        {
            var result = RequirePipeline(pipeline).Execute(command);
            LogResult(pipeline, actionLabel, result);
            return result;
        }

        public static bool ResetCurrentPlayerActor(SessionActivityPipeline pipeline)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied;
            string outcomeReason;
            try
            {
                applied = pipeline.TryQaResetCurrentPlayerActor(
                    state.CurrentIdentity,
                    QaSource("QaResetCurrentPlayerActor"),
                    QaReason("QaResetCurrentPlayerActor"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                applied = false;
                outcomeReason = $"actor_reset_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActorResetQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            }

            DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                $"action='QaResetCurrentPlayerActor' outcomeKind='{(applied ? "Applied" : "Rejected")}' reason='{outcomeReason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            return applied;
        }

        public static bool ResetCurrentActivityObjects(SessionActivityPipeline pipeline)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied;
            string outcomeReason;
            try
            {
                applied = pipeline.TryQaResetCurrentActivityObjects(
                    state.CurrentIdentity,
                    QaSource("QaResetCurrentActivityObjects"),
                    QaReason("QaResetCurrentActivityObjects"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                applied = false;
                outcomeReason = $"activity_object_reset_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActivityObjectResetQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            }

            DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                $"action='QaResetCurrentActivityObjects' outcomeKind='{(applied ? "Applied" : "SkippedOrRejected")}' reason='{outcomeReason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            return applied;
        }

        public static bool CaptureCurrentActivitySnapshotPayload(SessionActivityPipeline pipeline)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool captured;
            string outcomeReason;
            try
            {
                captured = pipeline.TryQaCaptureCurrentActivitySnapshotPayload(
                    state.CurrentIdentity,
                    QaSource("QaCaptureCurrentActivitySnapshotPayload"),
                    QaReason("QaCaptureCurrentActivitySnapshotPayload"),
                    out outcomeReason);
            }
            catch (Exception exception)
            {
                captured = false;
                outcomeReason = $"activity_snapshot_capture_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogError(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActivitySnapshotCaptureQaFailed' reason='{outcomeReason}' error='{exception.Message}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            }

            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='QaCaptureCurrentActivitySnapshotPayload' outcomeKind='{(captured ? "Captured" : "SkippedOrRejected")}' reason='{outcomeReason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' stage='{state.CurrentStage}'.");
            return captured;
        }

        public static bool ApplyActorAttributeCommand(
            SessionActivityPipeline pipeline,
            string action,
            string actorId,
            string attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied = pipeline.TryApplyActorAttributeCommand(
                state.CurrentIdentity,
                actorId,
                operation,
                attributeId,
                amount,
                setValue,
                QaSource(action),
                QaReason(action),
                out var result);

            string outcome = applied ? "Applied" : result.Rejected ? "Rejected" : "Failed";
            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='{action}' outcomeKind='{outcome}' operation='{operation}' actorId='{actorId.TrimToEmpty()}' attributeId='{attributeId.TrimToEmpty()}' amount='{amount:0.###}' setValue='{setValue:0.###}' reason='{result.Reason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}'");

            if (applied && result.HasFact)
            {
                var fact = result.Fact;
                DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                    $"operation='{fact.Operation}' actorId='{actorId.TrimToEmpty()}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
            }

            return applied;
        }

        public static bool ApplyActorAttributeMutationIntent(
            SessionActivityPipeline pipeline,
            string action,
            string actorId,
            string attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied = pipeline.TryApplyActorAttributeMutationIntent(
                state.CurrentIdentity,
                actorId,
                operation,
                attributeId,
                amount,
                setValue,
                QaSource(action),
                QaReason(action),
                out var result);

            string outcome = applied ? "Applied" : result.Rejected ? "Rejected" : "Failed";
            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='{action}' outcomeKind='{outcome}' operation='{operation}' actorId='{actorId.TrimToEmpty()}' attributeId='{attributeId.TrimToEmpty()}' amount='{amount:0.###}' setValue='{setValue:0.###}' reason='{result.Reason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' receiver='ActorAttributeMutationReceiverEndpoint'");

            if (applied && result.HasChangedFact)
            {
                var fact = result.ApplyResult.Fact;
                DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActorAttributeMutationQaApplied' operation='{fact.Operation}' actorId='{actorId.TrimToEmpty()}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' thresholdFactCount='{result.ApplyResult.ThresholdFactCount}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
            }

            return applied;
        }

        public static bool ApplyActorDamageIntent(
            SessionActivityPipeline pipeline,
            string action,
            string actorId,
            float rawDamageAmount)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied = pipeline.TryApplyActorDamageIntent(
                state.CurrentIdentity,
                actorId,
                rawDamageAmount,
                QaSource(action),
                QaReason(action),
                out var result);

            string outcome = applied ? "Applied" : result.Rejected ? "Rejected" : "Failed";
            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='{action}' outcomeKind='{outcome}' actorId='{actorId.TrimToEmpty()}' rawDamageAmount='{rawDamageAmount:0.###}' effectiveDamageAmount='{result.EffectiveDamageAmount:0.###}' targetAttributeId='{result.TargetAttributeId}' reason='{result.Reason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' receiver='ActorDamageableEndpoint'");

            if (applied && result.HasChangedFact)
            {
                var fact = result.MutationResult.ApplyResult.Fact;
                DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActorDamageQaApplied' actorId='{actorId.TrimToEmpty()}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' targetAttributeId='{fact.AttributeId}' rawDamageAmount='{result.RawDamageAmount:0.###}' effectiveDamageAmount='{result.EffectiveDamageAmount:0.###}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' thresholdFactCount='{result.MutationResult.ApplyResult.ThresholdFactCount}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
            }

            return applied;
        }

        public static bool ApplyActorDamageSourceIntent(
            SessionActivityPipeline pipeline,
            string action,
            string sourceActorId,
            string targetActorId,
            float rawDamageAmount)
        {
            pipeline = RequirePipeline(pipeline);
            var state = pipeline.State;
            bool applied = pipeline.TryApplyActorDamageSourceIntent(
                state.CurrentIdentity,
                sourceActorId,
                targetActorId,
                rawDamageAmount,
                QaSource(action),
                QaReason(action),
                out var result);

            string outcome = applied ? "Applied" : result.Rejected ? "Rejected" : "Failed";
            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='{action}' outcomeKind='{outcome}' sourceActorId='{sourceActorId.TrimToEmpty()}' targetActorId='{targetActorId.TrimToEmpty()}' rawDamageAmount='{rawDamageAmount:0.###}' reason='{result.Reason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' receiver='ActorDamageSourceEndpoint'");

            if (applied && result.HasChangedFact)
            {
                var damageResult = result.DamageResult;
                var fact = damageResult.MutationResult.ApplyResult.Fact;
                DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                    $"event='ActorDamageSourceQaApplied' sourceActorId='{sourceActorId.TrimToEmpty()}' sourceActorInstanceRuntimeId='{result.SourceActorInstanceRuntimeId}' targetActorId='{targetActorId.TrimToEmpty()}' targetActorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' targetAttributeId='{fact.AttributeId}' rawDamageAmount='{damageResult.RawDamageAmount:0.###}' effectiveDamageAmount='{damageResult.EffectiveDamageAmount:0.###}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' thresholdFactCount='{damageResult.MutationResult.ApplyResult.ThresholdFactCount}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
            }

            return applied;
        }

        private static SessionActivityPipeline RequirePipeline(SessionActivityPipeline pipeline)
        {
            if (pipeline != null)
            {
                return pipeline;
            }

            throw new InvalidOperationException("SessionActivityHost QA command surface requires SessionActivityPipeline.");
        }

        private static void LogResult(SessionActivityPipeline pipeline, string action, SessionActivityCommandResult result)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Invalid result returned by action '{action}'.");
            }

            if (!result.IsStarted)
            {
                string outcome = result.Kind.ToString();
                DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface), $"action='{action}' outcomeKind='{outcome}' reason='{result.Reason}' entrySequence='{pipeline.State.CurrentEntrySequence}' executionState='{pipeline.State.CurrentExecutionState}' gateState='{pipeline.GateState}'");
            }

            for (int index = 0; index < result.Facts.Count; index++)
            {
                DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface), $"{result.Facts[index]}");
            }
        }

        private static string QaSource(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }

        private static string QaReason(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }
    }
}
