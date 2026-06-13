using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityContentReleaseRuntimeState
    {
        private SessionActivityPipeline.PendingActivityContentReleaseContext _pendingReleaseContext;
        private bool _isAwaitingContinuation;

        public SessionActivityPipeline.PendingActivityContentReleaseContext PendingReleaseContext => _pendingReleaseContext;
        public bool IsAwaitingContinuation => _isAwaitingContinuation;

        public bool HasPendingReleaseContext => _pendingReleaseContext is { IsValid: true };

        public void SetPendingReleaseContext(
            SessionActivityPipeline.PendingActivityContentReleaseContext context,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = HasPendingReleaseContext;
            _pendingReleaseContext = context;
            LogStateChanged(
                "ActivityContentReleaseRuntimeStatePendingContextStored",
                activityId,
                entrySequence,
                before,
                HasPendingReleaseContext,
                _isAwaitingContinuation,
                _isAwaitingContinuation,
                source,
                reason);
        }

        public void ClearPendingReleaseContext(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = HasPendingReleaseContext;
            _pendingReleaseContext = null;
            LogStateChanged(
                "ActivityContentReleaseRuntimeStatePendingContextCleared",
                activityId,
                entrySequence,
                before,
                HasPendingReleaseContext,
                _isAwaitingContinuation,
                _isAwaitingContinuation,
                source,
                reason);
        }

        public void SetAwaitingContinuation(
            bool value,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = _isAwaitingContinuation;
            _isAwaitingContinuation = value;
            LogStateChanged(
                "ActivityContentReleaseRuntimeStateAwaitingContinuationChanged",
                activityId,
                entrySequence,
                HasPendingReleaseContext,
                HasPendingReleaseContext,
                before,
                _isAwaitingContinuation,
                source,
                reason);
        }

        private static void LogStateChanged(
            string eventName,
            string activityId,
            int entrySequence,
            bool hasPendingContextBefore,
            bool hasPendingContextAfter,
            bool awaitingBefore,
            bool awaitingAfter,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActivityContentReleaseRuntimeState),
                $"event='{Normalize(eventName)}' owner='ActivityContentReleaseRuntimeState' activityId='{Normalize(activityId)}' entrySequence='{entrySequence}' hasPendingContextBefore='{ToLowerInvariant(hasPendingContextBefore)}' hasPendingContextAfter='{ToLowerInvariant(hasPendingContextAfter)}' awaitingContinuationBefore='{ToLowerInvariant(awaitingBefore)}' awaitingContinuationAfter='{ToLowerInvariant(awaitingAfter)}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ToLowerInvariant(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
