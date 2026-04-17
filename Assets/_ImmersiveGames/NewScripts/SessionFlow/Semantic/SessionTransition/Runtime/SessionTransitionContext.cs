using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public readonly struct SessionTransitionContext
    {
        public SessionTransitionContext(RunContinuationSelection resolvedSelection)
        {
            ResolvedSelection = resolvedSelection;
        }

        public RunContinuationSelection ResolvedSelection { get; }
        public RunContinuationContext ContinuationContext => ResolvedSelection.ContinuationContext;
        public RunContinuationKind ResolvedContinuation => ResolvedSelection.SelectedContinuation;
        public RunDecisionCompletion Completion => ResolvedSelection.Completion;
        public string Reason => ResolvedSelection.Reason;
        public string NextState => ResolvedSelection.NextState;
        public bool IsValid => ResolvedSelection.IsValid;

        public override string ToString()
        {
            return $"Continuation='{ResolvedContinuation}', Reason='{Reason}', NextState='{NextState}'";
        }
    }

    public readonly struct SessionTransitionPhaseLocalEntryReadyEvent : IEvent
    {
        public SessionTransitionPhaseLocalEntryReadyEvent(SessionTransitionPlan plan, string source)
        {
            Plan = plan;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public SessionTransitionPlan Plan { get; }
        public string Source { get; }
        public SessionTransitionContext Context => Plan.Context;
        public bool IsValid => Plan.IsValid;
        public bool IsPhaseLocalEntry => Plan.EmitsPhaseLocalEntryReady;
    }
}

