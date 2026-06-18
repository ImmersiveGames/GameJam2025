using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityEntryActorAttributeUiBindingResult
    {
        public ActivityEntryActorAttributeUiBindingResult(
            int totalRequests,
            int resolvedCount,
            int boundCount,
            int activeHandleCount,
            bool skipped,
            string reason)
        {
            TotalRequests = totalRequests;
            ResolvedCount = resolvedCount;
            BoundCount = boundCount;
            ActiveHandleCount = activeHandleCount;
            Skipped = skipped;
            Reason = reason.TrimToEmpty();
        }

        public int TotalRequests { get; }
        public int ResolvedCount { get; }
        public int BoundCount { get; }
        public int ActiveHandleCount { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool HasBindings => !Skipped && BoundCount > 0;
    }
}
