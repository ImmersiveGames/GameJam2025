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
            Reason = Normalize(reason);
        }

        public int TotalRequests { get; }
        public int ResolvedCount { get; }
        public int BoundCount { get; }
        public int ActiveHandleCount { get; }
        public bool Skipped { get; }
        public string Reason { get; }

        public bool HasBindings => !Skipped && BoundCount > 0;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
