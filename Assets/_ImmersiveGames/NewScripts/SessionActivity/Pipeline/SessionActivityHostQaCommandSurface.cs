using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class SessionActivityHostQaCommandSurface
    {
        public static void CompleteCurrentActivity(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult(pipeline, "CompleteCurrentActivity", result);
        }

        public static void CompleteActivationWindow(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).CompleteActivationWindow(QaSource("CompleteActivationWindow"), QaReason("CompleteActivationWindow"));
            LogResult(pipeline, "CompleteActivationWindow", result);
        }

        public static void CompleteDeactivationWindow(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).CompleteDeactivationWindow(QaSource("CompleteDeactivationWindow"), QaReason("CompleteDeactivationWindow"));
            LogResult(pipeline, "CompleteDeactivationWindow", result);
        }

        public static void ContinueToNextActivity(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
            LogResult(pipeline, "ContinueToNextActivity", result);
        }

        public static void RestartCurrentActivity(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult(pipeline, "RestartCurrentActivity", result);
        }

        public static void ResetSession(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).ResetSession(QaSource("ResetSession"), QaReason("ResetSession"));
            LogResult(pipeline, "ResetSession", result);
        }

        public static void RequestPause(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult(pipeline, "RequestPause", result);
        }

        public static void RequestResume(SessionActivityPipeline pipeline)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult(pipeline, "RequestResume", result);
        }

        public static SessionActivityCommandResult ExecuteCommand(SessionActivityPipeline pipeline, SessionActivityCommand command, string actionLabel)
        {
            SessionActivityCommandResult result = RequirePipeline(pipeline).Execute(command);
            LogResult(pipeline, actionLabel, result);
            return result;
        }

        public static bool ResetCurrentPlayerActor(SessionActivityPipeline pipeline)
        {
            pipeline = RequirePipeline(pipeline);
            SessionActivityRuntimeState state = pipeline.State;
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
            SessionActivityRuntimeState state = pipeline.State;
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
            SessionActivityRuntimeState state = pipeline.State;
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
            SessionActivityRuntimeState state = pipeline.State;
            bool applied = pipeline.TryApplyActorAttributeCommand(
                state.CurrentIdentity,
                actorId,
                operation,
                attributeId,
                amount,
                setValue,
                QaSource(action),
                QaReason(action),
                out ActorAttributeApplyResult result);

            string outcome = applied ? "Applied" : (result.Rejected ? "Rejected" : "Failed");
            DebugUtility.LogVerbose(typeof(SessionActivityHostQaCommandSurface),
                $"action='{action}' outcomeKind='{outcome}' operation='{operation}' actorId='{Normalize(actorId)}' attributeId='{Normalize(attributeId)}' amount='{amount:0.###}' setValue='{setValue:0.###}' reason='{result.Reason}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}'");

            if (applied && result.HasFact)
            {
                ActorAttributeChangedFact fact = result.Fact;
                DebugUtility.Log(typeof(SessionActivityHostQaCommandSurface),
                    $"operation='{fact.Operation}' actorId='{Normalize(actorId)}' actorInstanceRuntimeId='{fact.ActorInstanceRuntimeId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' activityIdentity='{fact.ActivityIdentity}' pipelineId='{fact.ActivityIdentity.PipelineId}'");
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
