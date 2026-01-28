#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    public readonly struct ContentSwapPendingSetEvent
    {
        public readonly ContentSwapPlan Plan;
        public readonly string Reason;

        public ContentSwapPendingSetEvent(ContentSwapPlan plan, string reason)
        {
            Plan = plan;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapPendingClearedEvent
    {
        public readonly string Reason;

        public ContentSwapPendingClearedEvent(string reason)
        {
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapCommittedEvent
    {
        public readonly ContentSwapPlan Previous;
        public readonly ContentSwapPlan Current;
        public readonly string Reason;

        public ContentSwapCommittedEvent(ContentSwapPlan previous, ContentSwapPlan current, string reason)
        {
            Previous = previous;
            Current = current;
            Reason = reason ?? string.Empty;
        }
    }

}
