#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.ContentSwap
{
    public readonly struct ContentSwapPendingSetEvent
    {
        public readonly ContentSwapPlan plan;
        public readonly string reason;

        public ContentSwapPendingSetEvent(ContentSwapPlan plan, string? reason)
        {
            this.plan = plan;
            this.reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapPendingClearedEvent
    {
        public readonly string reason;

        public ContentSwapPendingClearedEvent(string? reason)
        {
            this.reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapCommittedEvent
    {
        public readonly ContentSwapPlan previous;
        public readonly ContentSwapPlan current;
        public readonly string reason;

        public ContentSwapCommittedEvent(ContentSwapPlan previous, ContentSwapPlan current, string? reason)
        {
            this.previous = previous;
            this.current = current;
            this.reason = reason ?? string.Empty;
        }
    }

}
