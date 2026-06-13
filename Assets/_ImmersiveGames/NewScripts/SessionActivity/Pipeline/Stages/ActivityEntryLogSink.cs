using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityEntryLogSink
    {
        public void LogEntryOwnerEvent(
            string eventName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string detail = "")
        {
            string normalizedEventName = Normalize(eventName);
            if (string.IsNullOrWhiteSpace(normalizedEventName))
            {
                throw new InvalidOperationException("ActivityEntry owner eventName is required.");
            }

            string normalizedDetail = Normalize(detail);
            string detailSuffix = string.IsNullOrWhiteSpace(normalizedDetail)
                ? string.Empty
                : $" {normalizedDetail}";

            string message =
                $"event='{normalizedEventName}' owner='ActivityEntryPipeline' macroLifecycleOwner='SessionActivityPipeline' pipelineId='{identity.PipelineId}' sessionStateId='{identity.SessionId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' stage='{identity.Stage}' source='{Normalize(source)}' reason='{Normalize(reason)}'{detailSuffix}.";

            DebugUtility.Log(typeof(ActivityEntryPipeline), message, DebugUtility.Colors.Info);
        }

        public void LogPhaseBoundary(
            string phaseName,
            SessionActivityIdentity identity,
            string source,
            string reason,
            bool completed = false,
            string detail = "")
        {
            string normalizedDetail = string.IsNullOrWhiteSpace(detail)
                ? string.Empty
                : $" {detail}";
            DebugUtility.Log(
                typeof(SessionActivityPipeline),
                $"{phaseName} pipelineId='{identity.PipelineId}' sessionStateId='{identity.SessionId}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' stage='{identity.Stage}' source='{source}' reason='{reason}'.{normalizedDetail}",
                completed ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
