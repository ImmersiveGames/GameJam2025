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
            _pendingReleaseContext = context;
        }

        public void ClearPendingReleaseContext(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _pendingReleaseContext = null;
        }

        public void SetAwaitingContinuation(
            bool value,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            _isAwaitingContinuation = value;
        }
    }
}
