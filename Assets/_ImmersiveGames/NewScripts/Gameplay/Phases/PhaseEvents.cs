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

    public readonly struct PhasePendingClearedEvent
    {
        public readonly string Reason;

        public PhasePendingClearedEvent(string reason)
        {
            Reason = reason ?? string.Empty;
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
}
