namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityContentReleaseRuntimeState
    {

        public SessionActivityPipeline.PendingActivityContentReleaseContext PendingReleaseContext { get; private set; }
        public bool IsAwaitingContinuation { get; private set; }

        public bool HasPendingReleaseContext => PendingReleaseContext is { IsValid: true };

        public void SetPendingReleaseContext(
            SessionActivityPipeline.PendingActivityContentReleaseContext context,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            PendingReleaseContext = context;
        }

        public void ClearPendingReleaseContext(
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            PendingReleaseContext = null;
        }

        public void SetAwaitingContinuation(
            bool value,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            IsAwaitingContinuation = value;
        }
    }
}
