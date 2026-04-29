using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public enum SessionTransitionPhaseAction
    {
        None = 0,
        NextPhase = 1,
        StayOnCurrentPhase = 2,
        RestartFromFirstPhase = 3,
        OrdinalNavigation = 4,
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
        public SessionTransitionOrigin Origin => Context.Origin;
        public SessionTransitionIntentKind IntentKind => Context.IntentKind;
        public bool HasRunContinuationSelection => Context.HasRunContinuationSelection;
        public RunContinuationSelection ResolvedSelection => Context.ResolvedSelection;
        public RunContinuationContext ContinuationContext => Context.ContinuationContext;
        public RunContinuationKind ResolvedContinuation => Context.ResolvedContinuation;
        public RunContinuationKind LegacyRunContinuation => Composition.LegacyRunContinuation;
        public string ContextSignature => Context.ContextSignature;
        public string Reason => Context.Reason;
        public string NextState => Context.NextState;
        public bool IsValid =>
            Context.IsValid &&
            Composition.IntentKind == IntentKind &&
            (Execution.Kind != SessionTransitionExecutionKind.NoOp ||
             IntentKind == SessionTransitionIntentKind.TerminateRun);

        public override string ToString()
        {
            return $"Origin='{Origin}', Intent='{IntentKind}', LegacyContinuation='{ResolvedContinuation}', Composition='{Composition}', EmitsPhaseLocalEntryReady='{EmitsPhaseLocalEntryReady}', Execution='{Execution}', Reason='{Reason}', NextState='{NextState}'";
        }
    }
}
