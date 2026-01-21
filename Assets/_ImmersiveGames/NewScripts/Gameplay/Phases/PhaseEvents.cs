#nullable enable
namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    public readonly struct PhasePendingSetEvent
    {
        public readonly PhasePlan Plan;
        public readonly string Reason;

        public PhasePendingSetEvent(PhasePlan plan, string reason)
        {
            Plan = plan;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapPendingSetEvent
    {
        public readonly PhasePlan Plan;
        public readonly string Reason;

        public ContentSwapPendingSetEvent(PhasePlan plan, string reason)
        {
            Plan = plan;
            Reason = reason ?? string.Empty;
        }

        public ContentSwapPendingSetEvent(PhasePendingSetEvent evt)
            : this(evt.Plan, evt.Reason)
        {
        }
    }

    public readonly struct PhasePendingClearedEvent
    {
        public readonly string Reason;

        public PhasePendingClearedEvent(string reason)
        {
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

        public ContentSwapPendingClearedEvent(PhasePendingClearedEvent evt)
            : this(evt.Reason)
        {
        }
    }

    public readonly struct PhaseCommittedEvent
    {
        public readonly PhasePlan Previous;
        public readonly PhasePlan Current;
        public readonly string Reason;

        public PhaseCommittedEvent(PhasePlan previous, PhasePlan current, string reason)
        {
            Previous = previous;
            Current = current;
            Reason = reason ?? string.Empty;
        }
    }

    public readonly struct ContentSwapCommittedEvent
    {
        public readonly PhasePlan Previous;
        public readonly PhasePlan Current;
        public readonly string Reason;

        public ContentSwapCommittedEvent(PhasePlan previous, PhasePlan current, string reason)
        {
            Previous = previous;
            Current = current;
            Reason = reason ?? string.Empty;
        }

        public ContentSwapCommittedEvent(PhaseCommittedEvent evt)
            : this(evt.Previous, evt.Current, evt.Reason)
        {
        }
    }
}
