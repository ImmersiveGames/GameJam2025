using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityContentReleaseRuntimeState
    {
        private ActivityContentLoadedSet _currentLoadedSet;
        private SessionActivityPipeline.PendingActivityContentReleaseContext _pendingReleaseContext;
        private bool _isAwaitingContinuation;

        public ActivityContentLoadedSet CurrentLoadedSet => _currentLoadedSet;
        public SessionActivityPipeline.PendingActivityContentReleaseContext PendingReleaseContext => _pendingReleaseContext;
        public bool IsAwaitingContinuation => _isAwaitingContinuation;

        public bool HasCurrentLoadedSet => _currentLoadedSet.IsValid || _currentLoadedSet.HasScenes;
        public bool HasPendingReleaseContext => _pendingReleaseContext != null && _pendingReleaseContext.IsValid;

        public void StoreCurrentLoadedSet(
            ActivityContentLoadedSet loadedSet,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = HasCurrentLoadedSet;
            _currentLoadedSet = loadedSet;
            LogStateChanged(
                "ActivityContentReleaseRuntimeStateLoadedSetStored",
                activityId,
                entrySequence,
                before,
                HasCurrentLoadedSet,
                HasPendingReleaseContext,
                HasPendingReleaseContext,
                _isAwaitingContinuation,
                _isAwaitingContinuation,
                source,
                reason);
        }

        public void ClearCurrentLoadedSet(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            bool before = HasCurrentLoadedSet;
            _currentLoadedSet = default;
            LogStateChanged(
                "ActivityContentReleaseRuntimeStateLoadedSetCleared",
                activityId,
                entrySequence,
                before,
                HasCurrentLoadedSet,
                HasPendingReleaseContext,
                HasPendingReleaseContext,
                _isAwaitingContinuation,
                _isAwaitingContinuation,
                source,
                reason);
        }

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
                HasCurrentLoadedSet,
                HasCurrentLoadedSet,
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
                HasCurrentLoadedSet,
                HasCurrentLoadedSet,
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
                HasCurrentLoadedSet,
                HasCurrentLoadedSet,
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
            bool hasLoadedSetBefore,
            bool hasLoadedSetAfter,
            bool hasPendingContextBefore,
            bool hasPendingContextAfter,
            bool awaitingBefore,
            bool awaitingAfter,
            string source,
            string reason)
        {
            DebugUtility.Log(
                typeof(ActivityContentReleaseRuntimeState),
                $"[OBS][ActivityContentReleaseRuntimeState] event='{Normalize(eventName)}' owner='ActivityContentReleaseRuntimeState' activityId='{Normalize(activityId)}' entrySequence='{entrySequence}' hasLoadedSetBefore='{ToLowerInvariant(hasLoadedSetBefore)}' hasLoadedSetAfter='{ToLowerInvariant(hasLoadedSetAfter)}' hasPendingContextBefore='{ToLowerInvariant(hasPendingContextBefore)}' hasPendingContextAfter='{ToLowerInvariant(hasPendingContextAfter)}' awaitingContinuationBefore='{ToLowerInvariant(awaitingBefore)}' awaitingContinuationAfter='{ToLowerInvariant(awaitingAfter)}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
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
