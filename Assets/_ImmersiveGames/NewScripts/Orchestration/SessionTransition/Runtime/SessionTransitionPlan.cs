using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionTransition.Runtime
{
    public enum SessionTransitionPhaseAction
    {
        None = 0,
        NextPhase = 1,
        StayOnCurrentPhase = 2,
    }

    public enum SessionTransitionResetAction
    {
        None = 0,
        PhaseReset = 1,
    }

    public enum SessionTransitionHandoffAction
    {
        None = 0,
        GoToMenu = 1,
    }

    public readonly struct SessionTransitionPlan
    {
        public SessionTransitionPlan(
            SessionTransitionContext context,
            SessionTransitionComposition composition,
            SessionTransitionExecution execution)
        {
            Context = context;
            Composition = composition;
            Execution = execution;
        }

        public SessionTransitionContext Context { get; }
        public SessionTransitionComposition Composition { get; }
        public SessionTransitionExecution Execution { get; }
        public bool EmitsPhaseLocalEntryReady => Composition.EmitsPhaseLocalEntryReady;
        public RunContinuationSelection ResolvedSelection => Context.ResolvedSelection;
        public RunContinuationContext ContinuationContext => Context.ContinuationContext;
        public RunContinuationKind ResolvedContinuation => Context.ResolvedContinuation;
        public string Reason => Context.Reason;
        public string NextState => Context.NextState;
        public bool IsValid => Context.IsValid && ResolvedContinuation != RunContinuationKind.Unknown;

        public override string ToString()
        {
            return $"Continuation='{ResolvedContinuation}', Composition='{Composition}', EmitsPhaseLocalEntryReady='{EmitsPhaseLocalEntryReady}', Execution='{Execution}', Reason='{Reason}', NextState='{NextState}'";
        }
    }
}
